using Microsoft.UI.Xaml.Controls;
using ExamLab.ViewModels;
using ExamLab.Models;
using ExamLab.Services;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using ReactiveUI;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Threading.Tasks;

namespace ExamLab.Views;

/// <summary>
/// 操作点配置视图
/// </summary>
public sealed partial class OperationPointConfigView : UserControl
{
    public OperationPointConfigView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// 数据上下文变更事件处理
    /// </summary>
    private void OnDataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
    {
        if (DataContext is ModuleViewModelBase viewModel)
        {
            // 监听选中操作点的变更
            viewModel.WhenAnyValue(x => x.SelectedOperationPoint)
                .Subscribe(operationPoint =>
                {
                    if (operationPoint != null)
                    {
                        InitializeParameterDependencies(operationPoint);
                    }
                });
        }
    }

    /// <summary>
    /// 初始化参数依赖关系
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    private void InitializeParameterDependencies(OperationPoint operationPoint)
    {
        // 初始化位置参数控制器
        if (IsPositionKnowledgePoint(operationPoint))
        {
            PositionParameterController.InitializePositionParameters(operationPoint.Parameters);
        }

        // 初始化背景填充参数可见性
        if (IsBackgroundFillKnowledgePoint(operationPoint))
        {
            InitializeBackgroundFillParameters(operationPoint.Parameters);
        }

        // 为参数添加变更监听
        SetupParameterChangeListeners(operationPoint.Parameters);
    }

    /// <summary>
    /// 设置参数变更监听器
    /// </summary>
    /// <param name="parameters">参数列表</param>
    private void SetupParameterChangeListeners(ObservableCollection<ConfigurationParameter> parameters)
    {
        // 监听填充类型参数的变更
        ConfigurationParameter? fillTypeParam = parameters.FirstOrDefault(p => p.Name == "FillType");
        if (fillTypeParam != null)
        {
            fillTypeParam.WhenAnyValue(x => x.Value)
                .Subscribe(value => UpdateBackgroundFillParameterVisibility(parameters, value));
        }

        // 监听位置类型参数的变更
        ConfigurationParameter? horizontalTypeParam = parameters.FirstOrDefault(p => p.Name == "HorizontalPositionType");
        ConfigurationParameter? verticalTypeParam = parameters.FirstOrDefault(p => p.Name == "VerticalPositionType");

        if (horizontalTypeParam != null)
        {
            horizontalTypeParam.WhenAnyValue(x => x.Value)
                .Subscribe(value => PositionParameterController.UpdateParameterVisibility(parameters, "HorizontalPositionType", value));
        }

        if (verticalTypeParam != null)
        {
            verticalTypeParam.WhenAnyValue(x => x.Value)
                .Subscribe(value => PositionParameterController.UpdateParameterVisibility(parameters, "VerticalPositionType", value));
        }
    }

    /// <summary>
    /// 判断是否为位置相关的知识点
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    /// <returns>是否为位置知识点</returns>
    private static bool IsPositionKnowledgePoint(OperationPoint operationPoint)
    {
        string[] positionKnowledgePoints =
        [
            "设置文本框位置",
            "设置自选图形位置",
            "设置图片位置"
        ];

        return positionKnowledgePoints.Contains(operationPoint.Name);
    }

    /// <summary>
    /// 判断是否为背景填充相关的知识点
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    /// <returns>是否为背景填充知识点</returns>
    private static bool IsBackgroundFillKnowledgePoint(OperationPoint operationPoint)
    {
        return operationPoint.Name == "设置幻灯片背景";
    }

    /// <summary>
    /// 初始化背景填充参数
    /// </summary>
    /// <param name="parameters">参数列表</param>
    private static void InitializeBackgroundFillParameters(ObservableCollection<ConfigurationParameter> parameters)
    {
        // 获取填充类型参数
        ConfigurationParameter? fillTypeParam = parameters.FirstOrDefault(p => p.Name == "FillType");

        // 初始化依赖参数的可见性
        List<ConfigurationParameter> dependentParameters = parameters
            .Where(p => p.DependsOn == "FillType")
            .ToList();

        foreach (ConfigurationParameter parameter in dependentParameters)
        {
            // 初始状态根据当前填充类型值设置可见性
            bool shouldBeVisible = !string.IsNullOrEmpty(fillTypeParam?.Value) &&
                                   !string.IsNullOrEmpty(parameter.DependsOnValue) &&
                                   parameter.DependsOnValue == fillTypeParam.Value;
            parameter.IsVisible = shouldBeVisible;
        }
    }

    /// <summary>
    /// 更新背景填充参数可见性
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <param name="fillTypeValue">填充类型值</param>
    private static void UpdateBackgroundFillParameterVisibility(ObservableCollection<ConfigurationParameter> parameters, string? fillTypeValue)
    {
        // 获取依赖于FillType的所有参数
        List<ConfigurationParameter> dependentParameters = parameters
            .Where(p => p.DependsOn == "FillType")
            .ToList();

        foreach (ConfigurationParameter parameter in dependentParameters)
        {
            bool shouldBeVisible = !string.IsNullOrEmpty(fillTypeValue) &&
                                   !string.IsNullOrEmpty(parameter.DependsOnValue) &&
                                   parameter.DependsOnValue == fillTypeValue;

            if (parameter.IsVisible != shouldBeVisible)
            {
                parameter.IsVisible = shouldBeVisible;

                // 如果参数变为不可见，清空其值
                if (!shouldBeVisible)
                {
                    parameter.Value = null;
                }
            }
        }
    }

    /// <summary>
    /// 保存配置按钮点击事件
    /// </summary>
    private async void SaveConfiguration_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ModuleViewModelBase viewModel || viewModel.SelectedOperationPoint == null)
        {
            await NotificationService.ShowErrorAsync("错误", "没有选中的操作点");
            return;
        }

        try
        {
            // 验证必填参数
            var requiredParameters = viewModel.SelectedOperationPoint.Parameters
                .Where(p => p.IsRequired && string.IsNullOrWhiteSpace(p.Value))
                .ToList();

            if (requiredParameters.Count > 0)
            {
                string missingParams = string.Join(", ", requiredParameters.Select(p => p.DisplayName));
                await NotificationService.ShowErrorAsync("验证失败", $"以下必填参数未填写：{missingParams}");
                return;
            }

            // 这里可以添加更多的验证逻辑
            await NotificationService.ShowSuccessAsync("保存成功", "操作点配置已保存");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("保存失败", ex.Message);
        }
    }

    /// <summary>
    /// 重置配置按钮点击事件
    /// </summary>
    private async void ResetConfiguration_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ModuleViewModelBase viewModel || viewModel.SelectedOperationPoint == null)
        {
            await NotificationService.ShowErrorAsync("错误", "没有选中的操作点");
            return;
        }

        bool confirmed = await NotificationService.ShowConfirmationAsync(
            "确认重置",
            "确定要重置所有参数配置吗？此操作不可撤销。");

        if (!confirmed)
        {
            return;
        }

        try
        {
            // 重置所有参数为默认值
            foreach (var parameter in viewModel.SelectedOperationPoint.Parameters)
            {
                parameter.Value = parameter.DefaultValue;
            }

            await NotificationService.ShowSuccessAsync("重置成功", "操作点配置已重置为默认值");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("重置失败", ex.Message);
        }
    }

    /// <summary>
    /// 删除操作点按钮点击事件
    /// </summary>
    private async void DeleteOperationPoint_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ModuleViewModelBase viewModel || viewModel.SelectedOperationPoint == null)
        {
            await NotificationService.ShowErrorAsync("错误", "没有选中的操作点");
            return;
        }

        try
        {
            // 使用ViewModel中的删除命令
            await viewModel.DeleteOperationPointCommand.Execute(viewModel.SelectedOperationPoint);
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("删除失败", ex.Message);
        }
    }

    /// <summary>
    /// 文件浏览按钮点击事件
    /// </summary>
    private async void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();

            // 获取当前窗口句柄
            var window = App.MainWindow;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            StorageFile? file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // 找到触发事件的按钮对应的参数
                if (sender is Button button && button.DataContext is ConfigurationParameter parameter)
                {
                    parameter.Value = file.Path;
                }
            }
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("文件选择失败", ex.Message);
        }
    }
}
