using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using ExamLab.Models;
using ExamLab.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// 操作点配置视图模型
/// </summary>
public class OperationPointConfigurationViewModel : ViewModelBase
{
    /// <summary>
    /// 当前配置的操作点
    /// </summary>
    [Reactive] public OperationPoint OperationPoint { get; set; }

    /// <summary>
    /// 当前选中的参数
    /// </summary>
    [Reactive] public ConfigurationParameter? SelectedParameter { get; set; }

    /// <summary>
    /// 必填参数数量
    /// </summary>
    public int RequiredParameterCount => OperationPoint?.Parameters.Count(p => p.IsRequired) ?? 0;

    /// <summary>
    /// 已配置参数数量（有值的参数）
    /// </summary>
    public int ConfiguredParameterCount => OperationPoint?.Parameters.Count(p => !string.IsNullOrWhiteSpace(p.Value)) ?? 0;

    /// <summary>
    /// 添加参数命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddParameterCommand { get; }

    /// <summary>
    /// 编辑参数命令
    /// </summary>
    public ReactiveCommand<ConfigurationParameter, Unit> EditParameterCommand { get; }

    /// <summary>
    /// 删除参数命令
    /// </summary>
    public ReactiveCommand<ConfigurationParameter, Unit> DeleteParameterCommand { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    public OperationPointConfigurationViewModel(OperationPoint operationPoint)
    {
        OperationPoint = operationPoint ?? throw new ArgumentNullException(nameof(operationPoint));

        Title = "操作点配置";

        // 初始化命令
        AddParameterCommand = ReactiveCommand.CreateFromTask(AddParameterAsync);
        EditParameterCommand = ReactiveCommand.CreateFromTask<ConfigurationParameter>(EditParameterAsync);
        DeleteParameterCommand = ReactiveCommand.CreateFromTask<ConfigurationParameter>(DeleteParameterAsync);

        // 监听参数集合变化，更新统计信息
        OperationPoint.Parameters.CollectionChanged += (sender, e) =>
        {
            this.RaisePropertyChanged(nameof(RequiredParameterCount));
            this.RaisePropertyChanged(nameof(ConfiguredParameterCount));
        };
    }

    /// <summary>
    /// 添加参数
    /// </summary>
    private async Task AddParameterAsync()
    {
        try
        {
            string? parameterName = await NotificationService.ShowInputDialogAsync(
                "添加参数",
                "请输入参数名称",
                "新参数");

            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            ConfigurationParameter newParameter = new()
            {
                Id = $"param-{OperationPoint.Parameters.Count + 1}",
                Name = parameterName.Replace(" ", "_").ToLower(),
                DisplayName = parameterName,
                Description = "请输入参数描述",
                Type = ParameterType.Text,
                IsRequired = false,
                Order = OperationPoint.Parameters.Count + 1,
                IsEnabled = true
            };

            OperationPoint.Parameters.Add(newParameter);
            SelectedParameter = newParameter;

            await NotificationService.ShowSuccessAsync("添加成功", $"已添加参数：{parameterName}");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("添加失败", $"添加参数时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 编辑参数
    /// </summary>
    private async Task EditParameterAsync(ConfigurationParameter parameter)
    {
        if (parameter == null) return;

        try
        {
            // 创建参数编辑对话框
            ParameterEditViewModel editViewModel = new(parameter);
            ParameterEditDialog editDialog = new(editViewModel);

            var result = await editDialog.ShowAsync();
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                // 更新统计信息
                this.RaisePropertyChanged(nameof(RequiredParameterCount));
                this.RaisePropertyChanged(nameof(ConfiguredParameterCount));
                
                await NotificationService.ShowSuccessAsync("编辑成功", $"参数"{parameter.DisplayName}"已更新");
            }
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("编辑失败", $"编辑参数时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 删除参数
    /// </summary>
    private async Task DeleteParameterAsync(ConfigurationParameter parameter)
    {
        if (parameter == null) return;

        try
        {
            bool confirmed = await NotificationService.ShowConfirmationAsync(
                "确认删除",
                $"确定要删除参数"{parameter.DisplayName}"吗？此操作不可撤销。");

            if (!confirmed) return;

            OperationPoint.Parameters.Remove(parameter);

            if (SelectedParameter == parameter)
            {
                SelectedParameter = null;
            }

            // 重新排序
            for (int i = 0; i < OperationPoint.Parameters.Count; i++)
            {
                OperationPoint.Parameters[i].Order = i + 1;
            }

            await NotificationService.ShowSuccessAsync("删除成功", $"参数"{parameter.DisplayName}"已删除");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("删除失败", $"删除参数时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public async Task SaveConfigurationAsync()
    {
        try
        {
            // 这里可以添加保存逻辑，比如验证、数据持久化等
            // 目前操作点已经是引用类型，修改会直接反映到原对象

            await NotificationService.ShowSuccessAsync("保存成功", $"操作点"{OperationPoint.Name}"的配置已保存");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("保存失败", $"保存配置时发生错误：{ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    public async Task ShowErrorAsync(string title, string message)
    {
        await NotificationService.ShowErrorAsync(title, message);
    }
}
