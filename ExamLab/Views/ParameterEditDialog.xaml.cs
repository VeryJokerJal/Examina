using ExamLab.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ExamLab.Views;

/// <summary>
/// 参数编辑对话框
/// </summary>
public sealed partial class ParameterEditDialog : ContentDialog
{
    /// <summary>
    /// 视图模型
    /// </summary>
    public ParameterEditViewModel ViewModel { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="viewModel">视图模型</param>
    public ParameterEditDialog(ParameterEditViewModel viewModel)
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
        // 验证参数信息
        if (string.IsNullOrWhiteSpace(ViewModel.Parameter.Name))
        {
            args.Cancel = true;
            await ViewModel.ShowErrorAsync("验证失败", "参数名称不能为空");
            return;
        }

        if (string.IsNullOrWhiteSpace(ViewModel.Parameter.DisplayName))
        {
            args.Cancel = true;
            await ViewModel.ShowErrorAsync("验证失败", "显示名称不能为空");
            return;
        }

        // 保存参数
        try
        {
            await ViewModel.SaveParameterAsync();
        }
        catch (System.Exception ex)
        {
            args.Cancel = true;
            await ViewModel.ShowErrorAsync("保存失败", $"保存参数时发生错误：{ex.Message}");
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
