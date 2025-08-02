using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Examina.Models;
using Examina.Services;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels;

/// <summary>
/// 加载窗口视图模型
/// </summary>
public class LoadingViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;

    public LoadingViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    #region 属性

    /// <summary>
    /// 加载状态文本
    /// </summary>
    [Reactive]
    public string StatusText { get; set; } = "正在验证登录状态...";

    /// <summary>
    /// 进度值 (0-100)
    /// </summary>
    [Reactive]
    public int Progress { get; set; } = 0;

    /// <summary>
    /// 是否显示进度条
    /// </summary>
    [Reactive]
    public bool ShowProgress { get; set; } = true;

    /// <summary>
    /// 错误消息
    /// </summary>
    [Reactive]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 是否显示错误
    /// </summary>
    [Reactive]
    public bool HasError { get; set; } = false;

    #endregion

    #region 方法

    /// <summary>
    /// 开始自动认证流程
    /// </summary>
    public async Task StartAutoAuthenticationAsync()
    {
        try
        {
            // 阶段1：检查本地存储
            UpdateStatus("检查本地登录信息...", 20);

            PersistentLoginData? loginData = await _authenticationService.LoadLoginDataAsync();
            if (loginData == null)
            {
                // 没有本地登录信息，跳转到登录页面
                UpdateStatus("未找到登录信息", 100);
                await Task.Delay(1000);
                NavigateToLogin();
                return;
            }

            // 阶段2：验证令牌
            UpdateStatus("验证访问令牌...", 50);

            AuthenticationResult result = await _authenticationService.AutoAuthenticateAsync();

            if (result.IsSuccess)
            {
                // 阶段3：验证成功
                UpdateStatus("登录验证成功！", 80);

                // 检查是否需要完善用户信息
                if (_authenticationService.RequiresUserInfoCompletion())
                {
                    UpdateStatus("需要完善用户信息", 100);
                    await Task.Delay(1000);
                    NavigateToUserInfoCompletion();
                }
                else
                {
                    UpdateStatus("正在进入主界面...", 100);
                    await Task.Delay(100);
                    NavigateToMainWindow();
                }
            }
            else
            {
                // 验证失败，显示错误并跳转到登录页面
                ShowError($"登录验证失败: {result.ErrorMessage}");
                await Task.Delay(2000);
                NavigateToLogin();
            }
        }
        catch (Exception ex)
        {
            ShowError($"自动登录过程中发生错误: {ex.Message}");
            await Task.Delay(2000);
            NavigateToLogin();
        }
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    private void UpdateStatus(string status, int progress)
    {
        StatusText = status;
        Progress = progress;
        HasError = false;
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// 显示错误
    /// </summary>
    private void ShowError(string error)
    {
        ErrorMessage = error;
        HasError = true;
        ShowProgress = false;
        StatusText = "验证失败";
    }

    /// <summary>
    /// 导航到登录窗口
    /// </summary>
    private static void NavigateToLogin()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 从App获取LoginViewModel实例
            LoginViewModel? loginViewModel = null;
            if (Avalonia.Application.Current is App app)
            {
                loginViewModel = app.GetService<LoginViewModel>();
            }

            Views.LoginWindow loginWindow = loginViewModel != null
                ? new Views.LoginWindow(loginViewModel)
                : new Views.LoginWindow();

            loginWindow.Show();

            // 关闭当前窗口
            desktop.MainWindow?.Close();

            desktop.MainWindow = loginWindow;
        }
    }

    /// <summary>
    /// 导航到用户信息完善窗口
    /// </summary>
    private void NavigateToUserInfoCompletion()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            UserInfoCompletionViewModel userInfoViewModel = new(_authenticationService);
            Views.UserInfoCompletionWindow userInfoWindow = new(userInfoViewModel);
            userInfoWindow.Show();

            // 关闭当前窗口
            desktop.MainWindow?.Close();

            desktop.MainWindow = userInfoWindow;
        }
    }

    /// <summary>
    /// 导航到主窗口
    /// </summary>
    private static void NavigateToMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Views.MainWindow mainWindow = new();

            // 为MainView设置MainViewModel
            if (Avalonia.Application.Current is App app)
            {
                MainViewModel? mainViewModel = app.GetService<MainViewModel>();
                if (mainViewModel != null)
                {
                    // 找到MainView并设置DataContext
                    if (mainWindow.Content is Views.MainView mainView)
                    {
                        mainView.DataContext = mainViewModel;
                    }
                }
            }

            mainWindow.Show();

            // 关闭当前窗口
            desktop.MainWindow?.Close();

            desktop.MainWindow = mainWindow;
        }
    }

    #endregion
}
