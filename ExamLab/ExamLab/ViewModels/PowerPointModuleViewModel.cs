using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using ExamLab.Models;
using ExamLab.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// PowerPoint模块ViewModel
/// </summary>
public class PowerPointModuleViewModel : ModuleViewModelBase
{
    /// <summary>
    /// 可用的知识点列表
    /// </summary>
    public ObservableCollection<PowerPointKnowledgeConfig> AvailableKnowledgePoints { get; set; } = [];

    /// <summary>
    /// 当前选中的知识点类型
    /// </summary>
    [Reactive] public PowerPointKnowledgeType? SelectedKnowledgeType { get; set; }

    /// <summary>
    /// 添加知识点操作命令
    /// </summary>
    public ReactiveCommand<PowerPointKnowledgeType, Unit> AddKnowledgePointCommand { get; }

    /// <summary>
    /// 添加指定类型操作点命令
    /// </summary>
    public ReactiveCommand<string, Unit> AddOperationPointByTypeCommand { get; }

    /// <summary>
    /// 编辑操作点命令
    /// </summary>
    public ReactiveCommand<OperationPoint, Unit> EditOperationPointCommand { get; }

    public PowerPointModuleViewModel(ExamModule module) : base(module)
    {
        // 初始化可用知识点
        foreach (PowerPointKnowledgeConfig config in PowerPointKnowledgeService.Instance.GetAllKnowledgeConfigs())
        {
            AvailableKnowledgePoints.Add(config);
        }

        // 初始化命令
        AddKnowledgePointCommand = ReactiveCommand.Create<PowerPointKnowledgeType>(AddKnowledgePoint);
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
            OperationPoint operationPoint = PowerPointKnowledgeService.Instance.CreateOperationPoint(SelectedKnowledgeType.Value);
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

    private void AddKnowledgePoint(PowerPointKnowledgeType knowledgeType)
    {
        SelectedKnowledgeType = knowledgeType;
        AddOperationPoint();
    }

    private void AddOperationPointByType(string knowledgeTypeString)
    {
        if (Enum.TryParse(knowledgeTypeString, out PowerPointKnowledgeType knowledgeType))
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
            // 创建编辑对话框
            Views.OperationPointEditDialog dialog = new(operationPoint);

            // 设置XamlRoot
            if (XamlRootService.GetXamlRoot() is { } xamlRoot)
            {
                dialog.XamlRoot = xamlRoot;
            }

            // 显示对话框
            Microsoft.UI.Xaml.Controls.ContentDialogResult result = await dialog.ShowAsync();

            // 如果用户点击了保存并且保存成功，则更新界面
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary && dialog.ViewModel?.IsSaved == true)
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
        catch (Exception ex)
        {
            SetError($"编辑操作点失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取知识点分类
    /// </summary>
    public IEnumerable<IGrouping<string, PowerPointKnowledgeConfig>> GetKnowledgePointsByCategory()
    {
        return AvailableKnowledgePoints.GroupBy(k => k.Category);
    }
}
