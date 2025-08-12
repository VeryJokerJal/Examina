using System;
using System.Reactive;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ExamLab.Models;
using ExamLab.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// Word模块ViewModel
/// </summary>
public class WordModuleViewModel : ModuleViewModelBase
{
    /// <summary>
    /// 可用的知识点列表
    /// </summary>
    public ObservableCollection<WordKnowledgeConfig> AvailableKnowledgePoints { get; set; } = [];

    /// <summary>
    /// 当前选中的知识点类型
    /// </summary>
    [Reactive] public WordKnowledgeType? SelectedKnowledgeType { get; set; }

    /// <summary>
    /// 添加知识点操作命令
    /// </summary>
    public ReactiveCommand<WordKnowledgeType, Unit> AddKnowledgePointCommand { get; }

    /// <summary>
    /// 添加指定类型操作点命令
    /// </summary>
    public ReactiveCommand<string, Unit> AddOperationPointByTypeCommand { get; }

    /// <summary>
    /// 编辑操作点命令
    /// </summary>
    public ReactiveCommand<OperationPoint, Unit> EditOperationPointCommand { get; }

    public WordModuleViewModel(ExamModule module) : base(module)
    {
        // 初始化可用知识点
        foreach (WordKnowledgeConfig config in WordKnowledgeService.Instance.GetAllKnowledgeConfigs())
        {
            AvailableKnowledgePoints.Add(config);
        }

        // 初始化命令
        AddKnowledgePointCommand = ReactiveCommand.Create<WordKnowledgeType>(AddKnowledgePoint);
        AddOperationPointByTypeCommand = ReactiveCommand.Create<string>(AddOperationPointByType);
        EditOperationPointCommand = ReactiveCommand.Create<OperationPoint>(EditOperationPoint);
    }

    protected override void AddOperationPoint()
    {
        if (SelectedQuestion == null)
        {
            SetError("请先选择一个题目");
            return;
        }

        if (SelectedKnowledgeType == null)
        {
            SetError("请先选择一个知识点类型");
            return;
        }

        try
        {
            OperationPoint operationPoint = WordKnowledgeService.Instance.CreateOperationPoint(SelectedKnowledgeType.Value);
            operationPoint.ScoringQuestionId = SelectedQuestion.Id;

            SelectedQuestion.OperationPoints.Add(operationPoint);
            SelectedOperationPoint = operationPoint;

            ClearError();
        }
        catch (Exception ex)
        {
            SetError($"添加操作点失败：{ex.Message}");
        }
    }

    private void AddKnowledgePoint(WordKnowledgeType knowledgeType)
    {
        SelectedKnowledgeType = knowledgeType;
        AddOperationPoint();
    }

    private void AddOperationPointByType(string knowledgeTypeString)
    {
        if (Enum.TryParse(knowledgeTypeString, out WordKnowledgeType knowledgeType))
        {
            AddKnowledgePoint(knowledgeType);
        }
        else
        {
            SetError($"未知的知识点类型：{knowledgeTypeString}");
        }
    }

    /// <summary>
    /// 编辑操作点
    /// </summary>
    /// <param name="operationPoint">要编辑的操作点</param>
    private async void EditOperationPoint(OperationPoint operationPoint)
    {
        if (operationPoint == null)
        {
            SetError("操作点不能为空");
            return;
        }

        try
        {
            // 检查XamlRoot是否可用
            Microsoft.UI.Xaml.XamlRoot? xamlRoot = App.MainWindow?.Content.XamlRoot;
            if (xamlRoot == null)
            {
                SetError("无法显示编辑对话框：XamlRoot未设置");
                return;
            }

            // 创建编辑页面
            Views.OperationPointEditPage editPage = new();
            editPage.Initialize(operationPoint);

            // 创建ContentDialog并设置内容
            Microsoft.UI.Xaml.Controls.ContentDialog dialog = new()
            {
                Title = "编辑Word操作点",
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                Content = editPage,
                XamlRoot = xamlRoot
            };

            // 显示对话框
            Microsoft.UI.Xaml.Controls.ContentDialogResult result = await dialog.ShowAsync();

            // 如果用户点击了保存，则验证并保存参数
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                // 更新参数值
                foreach (ConfigurationParameter parameter in operationPoint.Parameters)
                {
                    parameter.Value = editPage.GetParameterValue(parameter);
                }

                // 验证参数
                if (ValidateOperationPointParameters(operationPoint))
                {
                    // 选中该操作点，让用户可以在右侧面板查看更新后的内容
                    SelectedOperationPoint = operationPoint;

                    // 刷新操作点列表显示
                    if (SelectedQuestion != null)
                    {
                        this.RaisePropertyChanged(nameof(SelectedQuestion));
                    }
                    ClearError();
                }
            }
        }
        catch (Exception ex)
        {
            SetError($"编辑操作点失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证操作点参数
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    /// <returns>验证是否通过</returns>
    private bool ValidateOperationPointParameters(OperationPoint operationPoint)
    {
        // 验证必填参数
        foreach (ConfigurationParameter parameter in operationPoint.Parameters)
        {
            if (parameter.IsRequired && string.IsNullOrWhiteSpace(parameter.Value))
            {
                SetError($"参数 '{parameter.DisplayName}' 是必填项");
                return false;
            }

            // 验证数字类型参数
            if (parameter.Type == ParameterType.Number && !string.IsNullOrWhiteSpace(parameter.Value))
            {
                if (!double.TryParse(parameter.Value, out double numValue))
                {
                    SetError($"参数 '{parameter.DisplayName}' 必须是有效的数字");
                    return false;
                }

                if (parameter.MinValue.HasValue && numValue < parameter.MinValue.Value)
                {
                    // 如果是编号参数且值为-1，则允许（-1代表任意一个）
                    bool isIndexParameter = IsIndexParameter(parameter.Name);
                    if (!(isIndexParameter && Math.Abs(numValue - (-1)) < 0.001))
                    {
                        SetError($"参数 '{parameter.DisplayName}' 不能小于 {parameter.MinValue.Value}");
                        return false;
                    }
                }

                if (parameter.MaxValue.HasValue && numValue > parameter.MaxValue.Value)
                {
                    SetError($"参数 '{parameter.DisplayName}' 不能大于 {parameter.MaxValue.Value}");
                    return false;
                }
            }

            // 验证枚举类型参数
            if (parameter.Type == ParameterType.Enum && parameter.EnumOptionsList.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(parameter.Value) && !parameter.EnumOptionsList.Contains(parameter.Value))
                {
                    SetError($"参数 '{parameter.DisplayName}' 的值必须是以下选项之一：{string.Join(", ", parameter.EnumOptionsList)}");
                    return false;
                }
            }
        }

        ClearError();
        return true;
    }

    /// <summary>
    /// 获取知识点分类
    /// </summary>
    public IEnumerable<IGrouping<string, WordKnowledgeConfig>> GetKnowledgePointsByCategory()
    {
        return AvailableKnowledgePoints.GroupBy(k => k.Category);
    }

    /// <summary>
    /// 检查参数是否为编号类型
    /// </summary>
    private static bool IsIndexParameter(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return false;

        string[] indexPatterns =
        {
            "ParagraphNumbers", "ParagraphIndex", "ParagraphNumber",
            "PageIndex", "PageNumber", "PageOrder",
            "SectionIndex", "SectionNumber", "SectionOrder",
            "TableIndex", "TableNumber", "TableOrder",
            "ImageIndex", "ImageNumber", "ImageOrder",
            "HeaderIndex", "HeaderNumber", "HeaderOrder",
            "FooterIndex", "FooterNumber", "FooterOrder"
        };

        return indexPatterns.Any(pattern =>
            parameterName.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
            parameterName.Contains("Index", StringComparison.OrdinalIgnoreCase) ||
            parameterName.Contains("Number", StringComparison.OrdinalIgnoreCase) ||
            parameterName.Contains("Order", StringComparison.OrdinalIgnoreCase));
    }
}
