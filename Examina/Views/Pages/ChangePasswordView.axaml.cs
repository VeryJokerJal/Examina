using Avalonia.Controls;
using Examina.ViewModels.Pages;

namespace Examina.Views.Pages;

public partial class ChangePasswordView : UserControl
{
    public ChangePasswordView()
    {
        InitializeComponent();
    }

    public ChangePasswordView(ChangePasswordViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
