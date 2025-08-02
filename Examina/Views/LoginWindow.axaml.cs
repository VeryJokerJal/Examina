using Avalonia.Controls;
using Examina.ViewModels;

namespace Examina.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }
    
    public LoginWindow(LoginViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
