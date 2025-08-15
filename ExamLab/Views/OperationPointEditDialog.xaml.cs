using ExamLab.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ExamLab.Views;

/// <summary>
/// 操作点编辑对话框
/// </summary>
public sealed partial class OperationPointEditDialog : ContentDialog
{
    /// <summary>
    /// 视图模型
    /// </summary>
    public OperationPointEditViewModel ViewModel { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="viewModel">视图模型</param>
    public OperationPointEditDialog(OperationPointEditViewModel viewModel)
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
        // 验证操作点信息
        if (string.IsNullOrWhiteSpace(ViewModel.OperationPoint.Name))
        {
            args.Cancel = true;
            await ViewModel.ShowErrorAsync("验证失败", "操作点名称不能为空");
            return;
        }

        if (string.IsNullOrWhiteSpace(ViewModel.OperationPoint.Description))
        {
            args.Cancel = true;
            await ViewModel.ShowErrorAsync("验证失败", "操作点描述不能为空");
            return;
        }

        if (ViewModel.OperationPoint.Score < 0)
        {
            args.Cancel = true;
            await ViewModel.ShowErrorAsync("验证失败", "操作点分值不能小于0");
            return;
        }

        // 保存操作点
        try
        {
            await ViewModel.SaveOperationPointAsync();
        }
        catch (System.Exception ex)
        {
            args.Cancel = true;
            await ViewModel.ShowErrorAsync("保存失败", $"保存操作点时发生错误：{ex.Message}");
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
