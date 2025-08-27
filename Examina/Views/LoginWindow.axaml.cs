using Avalonia.Controls;
using Avalonia.Interactivity;
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

    /// <summary>
    /// 打开设备管理页面事件处理
    /// </summary>
    private void OnOpenDeviceManagementClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.OpenDeviceManagement();
        }
    }

    /// <summary>
    /// 重试登录事件处理
    /// </summary>
    private async void OnRetryLoginClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            await viewModel.RetryLoginAsync();
        }
    }
}
