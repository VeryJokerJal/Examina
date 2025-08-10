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
            // 选中该操作点，让用户可以在右侧面板查看
            SelectedOperationPoint = operationPoint;

            // 为每个参数显示编辑对话框
            foreach (ConfigurationParameter parameter in operationPoint.Parameters)
            {
                string title = $"编辑参数: {parameter.DisplayName}";
                string message = string.IsNullOrWhiteSpace(parameter.Description) ?
                    $"请输入 {parameter.DisplayName} 的值:" :
                    $"{parameter.Description}\n\n请输入 {parameter.DisplayName} 的值:";

                string? newValue = await ExamLab.Services.NotificationService.ShowInputDialogAsync(
                    title,
                    message,
                    parameter.Value ?? parameter.DefaultValue ?? "");

                if (newValue != null) // 用户点击了确定
                {
                    // 验证输入值
                    if (parameter.IsRequired && string.IsNullOrWhiteSpace(newValue))
                    {
                        await ExamLab.Services.NotificationService.ShowErrorAsync("验证错误", $"参数 '{parameter.DisplayName}' 是必填项");
                        return;
                    }

                    // 验证数字类型参数
                    if (parameter.Type == ParameterType.Number && !string.IsNullOrWhiteSpace(newValue))
                    {
                        if (!int.TryParse(newValue, out int numValue))
                        {
                            await ExamLab.Services.NotificationService.ShowErrorAsync("验证错误", $"参数 '{parameter.DisplayName}' 必须是有效的数字");
                            return;
                        }

                        if (parameter.MinValue.HasValue && numValue < parameter.MinValue.Value)
                        {
                            await ExamLab.Services.NotificationService.ShowErrorAsync("验证错误", $"参数 '{parameter.DisplayName}' 不能小于 {parameter.MinValue.Value}");
                            return;
                        }

                        if (parameter.MaxValue.HasValue && numValue > parameter.MaxValue.Value)
                        {
                            await ExamLab.Services.NotificationService.ShowErrorAsync("验证错误", $"参数 '{parameter.DisplayName}' 不能大于 {parameter.MaxValue.Value}");
                            return;
                        }
                    }

                    parameter.Value = newValue;
                }
            }

            // 刷新界面显示
            if (SelectedQuestion != null)
            {
                this.RaisePropertyChanged(nameof(SelectedQuestion));
            }
            ClearError();
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
