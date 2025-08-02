using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Examina.Models;
using Examina.Services;
using Prism.Commands;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels;

/// <summary>
/// 用户信息完善视图模型
/// </summary>
public class UserInfoCompletionViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;

    public UserInfoCompletionViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;

        CompleteInfoCommand = new DelegateCommand(async () => await CompleteInfoAsync(), CanCompleteInfo);
        SkipCommand = new DelegateCommand(Skip);
        
        // 初始化当前用户信息
        InitializeUserInfo();
    }

    #region 属性

    /// <summary>
    /// 用户名
    /// </summary>
    [Reactive]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Reactive]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 确认密码
    /// </summary>
    [Reactive]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// 是否正在处理
    /// </summary>
    [Reactive]
    public bool IsProcessing { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [Reactive]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 成功消息
    /// </summary>
    [Reactive]
    public string SuccessMessage { get; set; } = string.Empty;

    /// <summary>
    /// 当前用户信息
    /// </summary>
    [Reactive]
    public UserInfo? CurrentUser { get; set; }

    #endregion

    #region 命令

    /// <summary>
    /// 完善信息命令
    /// </summary>
    public ICommand CompleteInfoCommand { get; }

    /// <summary>
    /// 跳过命令
    /// </summary>
    public ICommand SkipCommand { get; }

    #endregion

    #region 方法

    /// <summary>
    /// 初始化用户信息
    /// </summary>
    private void InitializeUserInfo()
    {
        CurrentUser = _authenticationService.CurrentUser;
        if (CurrentUser != null)
        {
            Username = CurrentUser.Username;
        }
    }

    /// <summary>
    /// 是否可以完善信息
    /// </summary>
    private bool CanCompleteInfo()
    {
        return !IsProcessing;
    }

    /// <summary>
    /// 完善信息
    /// </summary>
    private async Task CompleteInfoAsync()
    {
        if (IsProcessing) return;

        IsProcessing = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            // 验证输入
            if (!ValidateInput())
            {
                return;
            }

            CompleteUserInfoRequest request = new()
            {
                Username = string.IsNullOrWhiteSpace(Username) ? null : Username.Trim(),
                Password = string.IsNullOrWhiteSpace(Password) ? null : Password
            };

            UserInfo? updatedUser = await _authenticationService.CompleteUserInfoAsync(request);
            if (updatedUser != null)
            {
                SuccessMessage = "用户信息更新成功！";
                CurrentUser = updatedUser;

                // 延迟一下显示成功消息
                await Task.Delay(1500);

                // 导航到主页面
                NavigateToMainWindow();
            }
            else
            {
                ErrorMessage = "更新用户信息失败，请重试";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"更新用户信息时发生错误: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// 验证输入
    /// </summary>
    private bool ValidateInput()
    {
        if (!string.IsNullOrWhiteSpace(Password))
        {
            if (Password.Length < 6)
            {
                ErrorMessage = "密码长度不能少于6位";
                return false;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "两次输入的密码不一致";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(Username))
        {
            if (Username.Trim().Length < 2)
            {
                ErrorMessage = "用户名长度不能少于2位";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 跳过完善信息
    /// </summary>
    private async void Skip()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Skip: 开始跳过流程，当前用户IsFirstLogin: {CurrentUser?.IsFirstLogin}");

            // 即使跳过，也需要调用API更新IsFirstLogin状态
            CompleteUserInfoRequest request = new();
            UserInfo? updatedUser = await _authenticationService.CompleteUserInfoAsync(request);
            if (updatedUser != null)
            {
                System.Diagnostics.Debug.WriteLine($"Skip: API调用成功，返回的IsFirstLogin: {updatedUser.IsFirstLogin}");
                CurrentUser = updatedUser;
                System.Diagnostics.Debug.WriteLine($"Skip: 更新CurrentUser后，IsFirstLogin: {CurrentUser?.IsFirstLogin}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Skip: API调用失败，返回null");
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不阻止导航
            System.Diagnostics.Debug.WriteLine($"跳过时更新用户状态失败: {ex.Message}");
        }

        System.Diagnostics.Debug.WriteLine("Skip: 准备导航到主窗口");
        NavigateToMainWindow();
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
            if (desktop.MainWindow != null)
            {
                desktop.MainWindow.Close();
            }

            desktop.MainWindow = mainWindow;
        }
    }

    #endregion
}
