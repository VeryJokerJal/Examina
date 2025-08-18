using Avalonia.Controls;
using Avalonia.Interactivity;
using Examina.ViewModels.Dialogs;

namespace Examina.Views.Dialogs;

public partial class MockExamRulesDialog : Window
{
    public MockExamRulesDialog()
    {
        InitializeComponent();
        DataContext = new MockExamRulesViewModel();
        
        // 订阅命令事件
        if (DataContext is MockExamRulesViewModel viewModel)
        {
            viewModel.ConfirmCommand.Subscribe(result =>
            {
                Close(result);
            });
            
            viewModel.CancelCommand.Subscribe(result =>
            {
                Close(result);
            });
        }
    }

    public MockExamRulesDialog(MockExamRulesViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
