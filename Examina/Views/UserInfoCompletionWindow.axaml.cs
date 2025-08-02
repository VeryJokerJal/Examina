using Avalonia.Controls;
using Examina.ViewModels;

namespace Examina.Views;

public partial class UserInfoCompletionWindow : Window
{
    public UserInfoCompletionWindow()
    {
        InitializeComponent();
    }

    public UserInfoCompletionWindow(UserInfoCompletionViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
