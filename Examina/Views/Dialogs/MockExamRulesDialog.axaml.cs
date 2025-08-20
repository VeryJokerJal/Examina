using Avalonia.Controls;
using Avalonia.Interactivity;
using Examina.ViewModels.Dialogs;
using System;

namespace Examina.Views.Dialogs;

public partial class MockExamRulesDialog : Window
{
    private MockExamRulesViewModel? _viewModel;

    public MockExamRulesDialog()
    {
        InitializeComponent();
        _viewModel = new MockExamRulesViewModel();
        DataContext = _viewModel;

        SetupCommandSubscriptions();
    }

    public MockExamRulesDialog(MockExamRulesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        SetupCommandSubscriptions();
    }

    private void SetupCommandSubscriptions()
    {
        if (_viewModel != null)
        {
            // 订阅确认命令
            _viewModel.ConfirmCommand.Subscribe(result =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"MockExamRulesDialog: 确认命令执行，结果: {result}");
                    Close(result);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MockExamRulesDialog: 确认命令异常: {ex.Message}");
                }
            });

            // 订阅取消命令
            _viewModel.CancelCommand.Subscribe(result =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"MockExamRulesDialog: 取消命令执行，结果: {result}");
                    Close(result);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MockExamRulesDialog: 取消命令异常: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// 取消按钮点击事件处理器
    /// </summary>
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MockExamRulesDialog: 取消按钮被点击");
            Close(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamRulesDialog: 取消按钮点击异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 确认按钮点击事件处理器
    /// </summary>
    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MockExamRulesDialog: 确认按钮被点击");
            Close(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamRulesDialog: 确认按钮点击异常: {ex.Message}");
        }
    }
}
