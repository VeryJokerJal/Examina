using Avalonia.Controls;
using Avalonia.Interactivity;
using Examina.ViewModels.Dialogs;
using System;

namespace Examina.Views.Dialogs;

public partial class ComprehensiveTrainingRulesDialog : Window
{
    private ComprehensiveTrainingRulesViewModel? _viewModel;

    public ComprehensiveTrainingRulesDialog()
    {
        InitializeComponent();
        _viewModel = new ComprehensiveTrainingRulesViewModel();
        DataContext = _viewModel;

        SetupCommandSubscriptions();
    }

    public ComprehensiveTrainingRulesDialog(ComprehensiveTrainingRulesViewModel viewModel)
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
                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingRulesDialog: 确认命令结果: {result}");
                Close(result);
            });

            // 订阅取消命令
            _viewModel.CancelCommand.Subscribe(result =>
            {
                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingRulesDialog: 取消命令结果: {result}");
                Close(result);
            });
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // 清理资源
        if (_viewModel != null)
        {
            _viewModel.ConfirmCommand?.Dispose();
            _viewModel.CancelCommand?.Dispose();
        }
        
        base.OnClosed(e);
    }
}
