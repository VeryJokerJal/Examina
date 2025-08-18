using Avalonia.Controls.ApplicationLifetimes;
using Examina.ViewModels;
using Examina.Views;

namespace Examina.Services;

/// <summary>
/// 窗口管理服务实现
/// </summary>
public class WindowManagerService : IWindowManagerService
{
    private readonly IAuthenticationService _authenticationService;

    public WindowManagerService(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// 导航到登录窗口
    /// </summary>
    public void NavigateToLogin()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                // 从App获取LoginViewModel实例
                LoginViewModel? loginViewModel = null;
                if (Avalonia.Application.Current is App app)
                {
                    loginViewModel = app.GetService<LoginViewModel>();
                }

                LoginWindow loginWindow = loginViewModel != null
                    ? new LoginWindow(loginViewModel)
                    : new LoginWindow();

                loginWindow.Show();

                // 关闭当前窗口
                desktop.MainWindow?.Close();

                desktop.MainWindow = loginWindow;

                System.Diagnostics.Debug.WriteLine("已导航到登录窗口");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到登录窗口失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 导航到主窗口
    /// </summary>
    public void NavigateToMain()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                MainWindow mainWindow = new();

                // 为MainView设置MainViewModel
                if (Avalonia.Application.Current is App app)
                {
                    MainViewModel? mainViewModel = app.GetService<MainViewModel>();
                    if (mainViewModel != null)
                    {
                        // 找到MainView并设置DataContext
                        if (mainWindow.Content is MainView mainView)
                        {
                            mainView.DataContext = mainViewModel;
                        }

                        // 异步初始化MainViewModel
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await mainViewModel.InitializeAsync();
                                System.Diagnostics.Debug.WriteLine("MainViewModel初始化完成");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"MainViewModel初始化失败: {ex.Message}");
                            }
                        });
                    }
                }

                mainWindow.Show();

                // 关闭当前窗口
                desktop.MainWindow?.Close();

                desktop.MainWindow = mainWindow;

                System.Diagnostics.Debug.WriteLine("已导航到主窗口");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到主窗口失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 导航到用户信息完善窗口
    /// </summary>
    public void NavigateToUserInfoCompletion()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                UserInfoCompletionViewModel userInfoViewModel = new(_authenticationService);
                UserInfoCompletionWindow userInfoWindow = new(userInfoViewModel);
                userInfoWindow.Show();

                // 关闭当前窗口
                desktop.MainWindow?.Close();

                desktop.MainWindow = userInfoWindow;

                System.Diagnostics.Debug.WriteLine("已导航到用户信息完善窗口");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到用户信息完善窗口失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 导航到加载窗口
    /// </summary>
    public void NavigateToLoading()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                LoadingViewModel? loadingViewModel = null;
                if (Avalonia.Application.Current is App app)
                {
                    loadingViewModel = app.GetService<LoadingViewModel>();
                }

                LoadingWindow loadingWindow = loadingViewModel != null
                    ? new LoadingWindow(loadingViewModel)
                    : new LoadingWindow();

                loadingWindow.Show();

                // 关闭当前窗口
                desktop.MainWindow?.Close();

                desktop.MainWindow = loadingWindow;

                System.Diagnostics.Debug.WriteLine("已导航到加载窗口");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到加载窗口失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 关闭当前窗口
    /// </summary>
    public void CloseCurrentWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                desktop.MainWindow?.Close();
                System.Diagnostics.Debug.WriteLine("已关闭当前窗口");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"关闭当前窗口失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 退出应用程序
    /// </summary>
    public void ExitApplication()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                desktop.Shutdown();
                System.Diagnostics.Debug.WriteLine("应用程序已退出");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"退出应用程序失败: {ex.Message}");
            }
        }
    }
}
