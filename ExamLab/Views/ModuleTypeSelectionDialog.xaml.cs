using System;
using System.Reactive.Linq;
using ExamLab.Models;
using ExamLab.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ExamLab.Views;

/// <summary>
/// 模块类型选择对话框
/// </summary>
public sealed partial class ModuleTypeSelectionDialog : ContentDialog
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public ModuleTypeSelectionDialogViewModel ViewModel { get; }

    /// <summary>
    /// 选择的模块类型
    /// </summary>
    public ModuleType? SelectedModuleType => ViewModel.DialogResult;

    public ModuleTypeSelectionDialog()
    {
        InitializeComponent();
        ViewModel = new ModuleTypeSelectionDialogViewModel();
        DataContext = ViewModel;

        // 绑定按钮事件
        PrimaryButtonClick += OnPrimaryButtonClick;
        SecondaryButtonClick += OnSecondaryButtonClick;
    }

    /// <summary>
    /// 确定按钮点击事件
    /// </summary>
    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (ViewModel.SelectedModuleType != null)
        {
            ViewModel.ConfirmCommand.Execute().Subscribe();
        }
        else
        {
            // 如果没有选择，阻止对话框关闭
            args.Cancel = true;
        }
    }

    /// <summary>
    /// 取消按钮点击事件
    /// </summary>
    private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ViewModel.CancelCommand.Execute().Subscribe();
    }
}
