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

            // 显示操作点信息和参数概览
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

            // 显示参数概览，让用户了解要编辑的内容
            await NotificationService.ShowSuccessAsync("参数编辑", $"{operationInfo}{parameterInfo}\n接下来将逐个编辑这些参数。");

            // 创建参数副本用于编辑
            Dictionary<string, string> originalValues = new();
            foreach (ConfigurationParameter parameter in operationPoint.Parameters)
            {
                originalValues[parameter.Name] = parameter.Value ?? "";
            }

            bool hasChanges = false;
            int currentParameterIndex = 0;
            int totalParameters = operationPoint.Parameters.Count;

            // 为每个参数显示编辑对话框
            foreach (ConfigurationParameter parameter in operationPoint.Parameters)
            {
                currentParameterIndex++;

                string title = $"编辑参数 ({currentParameterIndex}/{totalParameters}): {parameter.DisplayName}";
                string message = $"操作点：{operationPoint.Name}\n\n";

                if (!string.IsNullOrWhiteSpace(parameter.Description))
                {
                    message += $"参数说明：{parameter.Description}\n\n";
                }

                // 根据参数类型添加特定信息
                if (parameter.Type == ParameterType.Number)
                {
                    message += "类型：数字\n";
                    if (parameter.MinValue.HasValue || parameter.MaxValue.HasValue)
                    {
                        string range = "";
                        if (parameter.MinValue.HasValue && parameter.MaxValue.HasValue)
                        {
                            range = $"范围：{parameter.MinValue.Value} - {parameter.MaxValue.Value}";
                        }
                        else if (parameter.MinValue.HasValue)
                        {
                            range = $"最小值：{parameter.MinValue.Value}";
                        }
                        else if (parameter.MaxValue.HasValue)
                        {
                            range = $"最大值：{parameter.MaxValue.Value}";
                        }
                        message += $"{range}\n";
                    }
                }
                else if (parameter.Type == ParameterType.Enum && parameter.EnumOptionsList.Count > 0)
                {
                    message += "类型：选择项\n";
                    message += $"可选值：{string.Join(", ", parameter.EnumOptionsList)}\n";
                }
                else if (parameter.Type == ParameterType.Boolean)
                {
                    message += "类型：布尔值（true/false）\n";
                }
                else
                {
                    message += "类型：文本\n";
                }

                message += $"\n当前值：{(string.IsNullOrWhiteSpace(parameter.Value) ? "(未设置)" : parameter.Value)}\n";
                message += $"请输入新的值：";

                if (parameter.IsRequired)
                {
                    message += "\n（此参数为必填项）";
                }

                // 显示编辑对话框
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
                            hasChanges = false;
                        }
                    }
                    break;
                }

                // 验证输入值
                string? validationError = ValidateParameterValue(parameter, newValue);
                if (!string.IsNullOrEmpty(validationError))
                {
                    await NotificationService.ShowErrorAsync("验证错误", validationError);
                    // 重新编辑这个参数
                    currentParameterIndex--;
                    continue;
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
    /// 验证参数值
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <param name="value">值</param>
    /// <returns>验证错误信息，如果验证通过则返回null</returns>
    private static string? ValidateParameterValue(ConfigurationParameter parameter, string value)
    {
        // 验证必填项
        if (parameter.IsRequired && string.IsNullOrWhiteSpace(value))
        {
            return $"参数 '{parameter.DisplayName}' 是必填项，不能为空。";
        }

        // 验证数字类型参数
        if (parameter.Type == ParameterType.Number && !string.IsNullOrWhiteSpace(value))
        {
            if (!int.TryParse(value, out int numValue))
            {
                return $"参数 '{parameter.DisplayName}' 必须是有效的数字。";
            }

            if (parameter.MinValue.HasValue && numValue < parameter.MinValue.Value)
            {
                return $"参数 '{parameter.DisplayName}' 不能小于 {parameter.MinValue.Value}。";
            }

            if (parameter.MaxValue.HasValue && numValue > parameter.MaxValue.Value)
            {
                return $"参数 '{parameter.DisplayName}' 不能大于 {parameter.MaxValue.Value}。";
            }
        }

        // 验证枚举类型参数
        if (parameter.Type == ParameterType.Enum && parameter.EnumOptionsList.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(value) && !parameter.EnumOptionsList.Contains(value))
            {
                return $"参数 '{parameter.DisplayName}' 的值必须是以下选项之一：{string.Join(", ", parameter.EnumOptionsList)}";
            }
        }

        return null; // 验证通过
    }

    /// <summary>
    /// 获取知识点分类
    /// </summary>
    public IEnumerable<IGrouping<string, PowerPointKnowledgeConfig>> GetKnowledgePointsByCategory()
    {
        return AvailableKnowledgePoints.GroupBy(k => k.Category);
    }
}
