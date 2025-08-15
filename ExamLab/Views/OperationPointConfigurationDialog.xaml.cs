using ExamLab.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ExamLab.Views;

/// <summary>
/// 操作点配置对话框
/// </summary>
public sealed partial class OperationPointConfigurationDialog : ContentDialog
{
    /// <summary>
    /// 视图模型
    /// </summary>
    public OperationPointConfigurationViewModel ViewModel { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="viewModel">视图模型</param>
    public OperationPointConfigurationDialog(OperationPointConfigurationViewModel viewModel)
    {
        this.InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        
        // 设置对话框事件
        PrimaryButtonClick += OnPrimaryButtonClick;
        SecondaryButtonClick += OnSecondaryButtonClick;
    }

    /// <summary>
    /// 主按钮点击事件（保存）
    /// </summary>
    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 保存配置
        try
        {
            await ViewModel.SaveConfigurationAsync();
        }
        catch (System.Exception ex)
        {
            args.Cancel = true;
            await ViewModel.ShowErrorAsync("保存失败", $"保存配置时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 次按钮点击事件（取消）
    /// </summary>
    private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 取消操作，不需要特殊处理
    }
}
