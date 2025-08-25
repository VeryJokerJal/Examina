using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Examina.Models;
using Examina.Services;
using Prism.Commands;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Timer = System.Timers.Timer;

namespace Examina.ViewModels;

/// <summary>
/// 登录视图模型
/// </summary>
public class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;

    public LoginViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        InitializeCommands();
        InitializePropertyWatchers();
        UpdateModeProperties();
        CheckAutoLoginFailure();
    }

    /// <summary>
    /// 初始化命令
    /// </summary>
    private void InitializeCommands()
    {
        LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);
        SwitchToWeChatCommand = new DelegateCommand(SwitchToWeChat);
        SwitchToCredentialsCommand = new DelegateCommand(SwitchToCredentials);
        SwitchToSmsCommand = new DelegateCommand(SwitchToSms);
        SendSmsCodeCommand = new DelegateCommand(async () => await SendSmsCodeAsync(), CanSendSmsCode);
        // RefreshQrCodeCommand 已移除，微信登录改为直接跳转浏览器
    }

    /// <summary>
    /// 初始化属性监听器
    /// </summary>
    private void InitializePropertyWatchers()
    {
        // 监听属性更改以更新命令状态
        _ = this.WhenAnyValue(x => x.Username, x => x.Password, x => x.PhoneNumber, x => x.SmsCode,
                         x => x.IsLoading, x => x.LoginMode)
            .Subscribe(_ =>
            {
                ((DelegateCommand)LoginCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)SendSmsCodeCommand).RaiseCanExecuteChanged();
            });

        // 单独监听短信验证码倒计时
        _ = this.WhenAnyValue(x => x.SmsCodeCountdown)
            .Subscribe(_ =>
            {
                ((DelegateCommand)SendSmsCodeCommand).RaiseCanExecuteChanged();
            });

        // 监听LoginMode变化并更新便利属性
        _ = this.WhenAnyValue(x => x.LoginMode)
            .Subscribe(mode =>
            {
                IsCredentialsMode = mode == LoginMode.Credentials;
                IsSmsMode = mode == LoginMode.SmsCode;
                IsWeChatMode = mode == LoginMode.WeChat;
            });
    }

    // 基础属性
    [Reactive] public string Username { get; set; } = string.Empty;
    [Reactive] public string Password { get; set; } = string.Empty;
    [Reactive] public string PhoneNumber { get; set; } = string.Empty;
    [Reactive] public string SmsCode { get; set; } = string.Empty;
    [Reactive] public bool IsLoading { get; set; } = false;
    [Reactive] public string ErrorMessage { get; set; } = string.Empty;
    [Reactive] public string SuccessMessage { get; set; } = string.Empty;

    // 登录模式
    [Reactive] public LoginMode LoginMode { get; set; } = LoginMode.Credentials;

    // 微信登录相关 - 已简化为直接跳转浏览器方式

    // 短信验证码相关
    [Reactive] public bool IsSmsCodeSent { get; set; } = false;
    [Reactive] public int SmsCodeCountdown { get; set; } = 0;

    // 设备绑定相关
    [Reactive] public bool RequireDeviceBinding { get; set; } = false;
    [Reactive] public string DeviceBindingMessage { get; set; } = string.Empty;

    // 便利属性 - 使用Reactive特性确保属性通知
    [Reactive] public bool IsCredentialsMode { get; set; }
    [Reactive] public bool IsSmsMode { get; set; }
    [Reactive] public bool IsWeChatMode { get; set; }

    // 命令
    public ICommand LoginCommand { get; private set; } = null!;
    public ICommand SwitchToWeChatCommand { get; private set; } = null!;
    public ICommand SwitchToCredentialsCommand { get; private set; } = null!;
    public ICommand SwitchToSmsCommand { get; private set; } = null!;
    public ICommand SendSmsCodeCommand { get; private set; } = null!;
    // RefreshQrCodeCommand 已移除，微信登录改为直接跳转浏览器

    private bool CanExecuteLogin()
    {
        return !IsLoading && LoginMode switch
        {
            LoginMode.Credentials => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password),
            LoginMode.SmsCode => !string.IsNullOrEmpty(PhoneNumber) && !string.IsNullOrEmpty(SmsCode),
            LoginMode.WeChat => true, // 微信登录直接跳转浏览器，无需验证
            _ => false
        };
    }

    private bool CanSendSmsCode()
    {
        return !IsLoading && !string.IsNullOrEmpty(PhoneNumber) && SmsCodeCountdown == 0;
    }

    private async Task ExecuteLoginAsync()
    {


        IsLoading = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            AuthenticationResult result = LoginMode switch
            {
                LoginMode.Credentials => await _authenticationService.LoginWithCredentialsAsync(Username, Password),
                LoginMode.SmsCode => await _authenticationService.LoginWithSmsAsync(PhoneNumber, SmsCode),
                LoginMode.WeChat => await ExecuteWeChatLoginAsync(),
                _ => new AuthenticationResult { IsSuccess = false, ErrorMessage = "不支持的登录方式" }
            };

            if (result.IsSuccess)
            {
                if (result.RequireDeviceBinding)
                {
                    RequireDeviceBinding = true;
                    DeviceBindingMessage = "检测到新设备，已自动绑定到您的账户";
                }

                SuccessMessage = "登录成功！";

                // 延迟一下显示成功消息
                await Task.Delay(1000);

                // 检查是否需要完善用户信息
                if (_authenticationService.RequiresUserInfoCompletion())
                {
                    // 导航到用户信息完善页面
                    NavigateToUserInfoCompletion();
                }
                else
                {
                    // 登录成功，导航到主页面
                    NavigateToMainWindow();
                }
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "登录失败";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"登录过程中发生错误: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SwitchToWeChat()
    {
        LoginMode = LoginMode.WeChat;
        ClearMessages();
    }

    private void SwitchToCredentials()
    {
        LoginMode = LoginMode.Credentials;
        ClearMessages();
    }

    private void SwitchToSms()
    {
        LoginMode = LoginMode.SmsCode;
        ClearMessages();
    }

    private async Task SendSmsCodeAsync()
    {
        if (string.IsNullOrEmpty(PhoneNumber))
        {
            ErrorMessage = "请输入手机号";
            return;
        }



        try
        {
            IsLoading = true;
            bool success = await _authenticationService.SendSmsCodeAsync(PhoneNumber);

            if (success)
            {
                IsSmsCodeSent = true;
                SuccessMessage = "验证码已发送，请查收短信";
                StartSmsCodeCountdown();
            }
            else
            {
                ErrorMessage = "发送验证码失败，请稍后重试";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"发送验证码失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 执行微信登录 - 跳转到微信登录页面
    /// </summary>
    private Task<AuthenticationResult> ExecuteWeChatLoginAsync()
    {
        try
        {
            // 构建微信登录页面URL
            string weChatLoginUrl = GetWeChatLoginPageUrl();

            try
            {
                // 打开微信登录页面
                System.Diagnostics.ProcessStartInfo startInfo = new()
                {
                    FileName = weChatLoginUrl,
                    UseShellExecute = true,
                    Verb = "open"
                };
                using System.Diagnostics.Process? process = System.Diagnostics.Process.Start(startInfo);
                _ = process; // 忽略返回值，仅触发浏览器打开

                // 返回提示信息，告知用户在浏览器中完成登录
                return Task.FromResult(new AuthenticationResult
                {
                    IsSuccess = false, // 暂时返回false，因为需要用户在浏览器中完成授权
                    ErrorMessage = "已打开微信登录页面，请在浏览器中完成微信扫码登录"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"无法打开微信登录页面: {ex.Message}"
                });
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"微信登录失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// 获取微信登录页面URL
    /// </summary>
    private string GetWeChatLoginPageUrl()
    {
        // 从配置或服务中获取后端服务地址
        string baseUrl = GetBackendBaseUrl();
        return $"{baseUrl}/wechat-login";
    }

    /// <summary>
    /// 获取后端服务基础URL
    /// </summary>
    private string GetBackendBaseUrl()
    {
        // 这里应该从配置文件或服务中获取，暂时硬编码
        // 在实际部署时，应该从配置中读取
        return "https://localhost:7125"; // 开发环境地址
    }



    private void StartSmsCodeCountdown()
    {
        SmsCodeCountdown = 60;
        Timer timer = new(1000);
        timer.Elapsed += (sender, e) =>
        {
            // 确保UI更新在UI线程中执行
            Dispatcher.UIThread.Post(() =>
            {
                SmsCodeCountdown--;
                if (SmsCodeCountdown <= 0)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            });
        };
        timer.Start();
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        RequireDeviceBinding = false;
        DeviceBindingMessage = string.Empty;
    }

    private void UpdateModeProperties()
    {
        IsCredentialsMode = LoginMode == LoginMode.Credentials;
        IsSmsMode = LoginMode == LoginMode.SmsCode;
        IsWeChatMode = LoginMode == LoginMode.WeChat;
    }

    /// <summary>
    /// 检查自动登录失败情况
    /// </summary>
    private void CheckAutoLoginFailure()
    {
        // 如果用户之前有登录信息但现在显示登录页面，说明自动登录失败
        // 可以显示一个友好的提示信息
        _ = Task.Run(async () =>
        {
            try
            {
                PersistentLoginData? loginData = await _authenticationService.LoadLoginDataAsync();
                if (loginData != null)
                {
                    // 有本地登录数据但还是显示了登录页面，说明自动登录失败
                    ErrorMessage = "自动登录失败，请重新登录";

                    // 清除可能已过期的本地数据
                    _ = await _authenticationService.ClearLoginDataAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查自动登录失败状态时出错: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 导航到用户信息完善页面
    /// </summary>
    private void NavigateToUserInfoCompletion()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            UserInfoCompletionViewModel userInfoViewModel = new(_authenticationService);
            Views.UserInfoCompletionWindow userInfoWindow = new(userInfoViewModel);
            userInfoWindow.Show();

            // 关闭登录窗口
            if (desktop.MainWindow is Views.LoginWindow loginWindow)
            {
                loginWindow.Close();
            }

            desktop.MainWindow = userInfoWindow;
        }
    }

    /// <summary>
    /// 导航到主窗口
    /// </summary>
    private void NavigateToMainWindow()
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

            // 关闭登录窗口
            if (desktop.MainWindow is Views.LoginWindow loginWindow)
            {
                loginWindow.Close();
            }

            desktop.MainWindow = mainWindow;
        }
    }

    // 析构函数 - 二维码定时器已移除，无需清理
}
