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

            // 显示操作点信息
            string operationInfo = $"操作点：{operationPoint.Name}\n描述：{operationPoint.Description}\n\n";

            // 构建参数信息
            string parameterInfo = "当前参数配置：\n";
            foreach (ConfigurationParameter parameter in operationPoint.Parameters)
            {
                string requiredMark = parameter.IsRequired ? " *" : "";
                string currentValue = string.IsNullOrWhiteSpace(parameter.Value) ? "(未设置)" : parameter.Value;
                parameterInfo += $"• {parameter.DisplayName}{requiredMark}: {currentValue}\n";
                if (!string.IsNullOrWhiteSpace(parameter.Description))
                {
                    parameterInfo += $"  说明：{parameter.Description}\n";
                }
            }

            // 显示确认对话框
            bool confirmed = await NotificationService.ShowConfirmationAsync(
                "编辑操作点参数",
                $"{operationInfo}{parameterInfo}\n是否要编辑这些参数？");

            if (!confirmed)
            {
                return;
            }

            // 创建参数副本用于编辑
            Dictionary<string, string> originalValues = new();
            foreach (ConfigurationParameter parameter in operationPoint.Parameters)
            {
                originalValues[parameter.Name] = parameter.Value ?? "";
            }

            bool hasChanges = false;

            // 为每个参数显示编辑对话框
            foreach (ConfigurationParameter parameter in operationPoint.Parameters)
            {
                string title = $"编辑参数: {parameter.DisplayName}";
                string message = $"操作点：{operationPoint.Name}\n\n";

                if (!string.IsNullOrWhiteSpace(parameter.Description))
                {
                    message += $"参数说明：{parameter.Description}\n\n";
                }

                if (parameter.Type == ParameterType.Number)
                {
                    if (parameter.MinValue.HasValue || parameter.MaxValue.HasValue)
                    {
                        string range = "";
                        if (parameter.MinValue.HasValue && parameter.MaxValue.HasValue)
                        {
                            range = $"（范围：{parameter.MinValue.Value} - {parameter.MaxValue.Value}）";
                        }
                        else if (parameter.MinValue.HasValue)
                        {
                            range = $"（最小值：{parameter.MinValue.Value}）";
                        }
                        else if (parameter.MaxValue.HasValue)
                        {
                            range = $"（最大值：{parameter.MaxValue.Value}）";
                        }
                        message += $"数字类型参数 {range}\n\n";
                    }
                }
                else if (parameter.Type == ParameterType.Enum && parameter.EnumOptionsList.Count > 0)
                {
                    message += $"可选值：{string.Join(", ", parameter.EnumOptionsList)}\n\n";
                }

                message += $"请输入 {parameter.DisplayName} 的值：";
                if (parameter.IsRequired)
                {
                    message += "\n（此参数为必填项）";
                }

                string? newValue = await NotificationService.ShowInputDialogAsync(
                    title,
                    message,
                    parameter.Value ?? parameter.DefaultValue ?? "");

                if (newValue == null) // 用户取消了
                {
                    // 询问是否要保存已修改的参数
                    if (hasChanges)
                    {
                        bool savePartial = await NotificationService.ShowConfirmationAsync(
                            "保存更改",
                            "您已经修改了一些参数，是否要保存这些更改？");

                        if (!savePartial)
                        {
                            // 恢复所有参数的原始值
                            foreach (ConfigurationParameter param in operationPoint.Parameters)
                            {
                                param.Value = originalValues[param.Name];
                            }
                        }
                    }
                    break;
                }

                // 验证输入值
                if (parameter.IsRequired && string.IsNullOrWhiteSpace(newValue))
                {
                    await NotificationService.ShowErrorAsync("验证错误", $"参数 '{parameter.DisplayName}' 是必填项，请重新输入。");
                    continue; // 重新输入这个参数
                }

                // 验证数字类型参数
                if (parameter.Type == ParameterType.Number && !string.IsNullOrWhiteSpace(newValue))
                {
                    if (!int.TryParse(newValue, out int numValue))
                    {
                        await NotificationService.ShowErrorAsync("验证错误", $"参数 '{parameter.DisplayName}' 必须是有效的数字，请重新输入。");
                        continue; // 重新输入这个参数
                    }

                    if (parameter.MinValue.HasValue && numValue < parameter.MinValue.Value)
                    {
                        await NotificationService.ShowErrorAsync("验证错误", $"参数 '{parameter.DisplayName}' 不能小于 {parameter.MinValue.Value}，请重新输入。");
                        continue; // 重新输入这个参数
                    }

                    if (parameter.MaxValue.HasValue && numValue > parameter.MaxValue.Value)
                    {
                        await NotificationService.ShowErrorAsync("验证错误", $"参数 '{parameter.DisplayName}' 不能大于 {parameter.MaxValue.Value}，请重新输入。");
                        continue; // 重新输入这个参数
                    }
                }

                // 验证枚举类型参数
                if (parameter.Type == ParameterType.Enum && parameter.EnumOptionsList.Count > 0)
                {
                    if (!parameter.EnumOptionsList.Contains(newValue))
                    {
                        await NotificationService.ShowErrorAsync("验证错误", $"参数 '{parameter.DisplayName}' 的值必须是以下选项之一：{string.Join(", ", parameter.EnumOptionsList)}");
                        continue; // 重新输入这个参数
                    }
                }

                // 更新参数值
                if (parameter.Value != newValue)
                {
                    parameter.Value = newValue;
                    hasChanges = true;
                }
            }

            // 显示编辑结果
            if (hasChanges)
            {
                await NotificationService.ShowSuccessAsync("编辑完成", "操作点参数已成功更新！");

                // 刷新界面显示
                if (SelectedQuestion != null)
                {
                    this.RaisePropertyChanged(nameof(SelectedQuestion));
                }
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
