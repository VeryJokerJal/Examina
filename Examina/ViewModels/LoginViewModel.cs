using System.IO;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Examina.Models;
using Examina.Services;
using Examina.Views;
using Prism.Commands;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Timer = System.Timers.Timer;

namespace Examina.ViewModels;

/// <summary>
/// 微信登录信息模型
/// </summary>
public class WeChatLoginInfo
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserInfo? User { get; set; }
}

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
        TestWeChatFileCommand = new DelegateCommand(TestWeChatFilePolling);
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
    public ICommand TestWeChatFileCommand { get; private set; } = null!;
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

                // 刷新用户信息以确保获取最新状态
                await _authenticationService.RefreshUserInfoAsync();

                // 根据登录方式决定导航逻辑
                if (LoginMode == LoginMode.SmsCode)
                {
                    // 手机号登录直接进入主界面（已验证手机号）
                    NavigateToMainWindow();
                }
                else
                {
                    // 微信登录和用户名密码登录都需要检查是否需要完善用户信息
                    bool requiresCompletion = _authenticationService.RequiresUserInfoCompletion();

                    if (requiresCompletion)
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
    /// 执行微信登录 - 通过服务端生成二维码并轮询服务端状态（无本地文件）
    /// </summary>
    private async Task<AuthenticationResult> ExecuteWeChatLoginAsync()
    {
        try
        {
            // 1) 向服务端请求二维码信息
            WeChatQrCodeInfo? qr = await _authenticationService.GetWeChatQrCodeAsync();
            if (qr == null || string.IsNullOrEmpty(qr.QrCodeUrl) || string.IsNullOrEmpty(qr.QrCodeKey))
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "无法获取微信二维码，请稍后再试"
                };
            }

            // 2) 打开浏览器至微信授权页
            try
            {
                System.Diagnostics.ProcessStartInfo startInfo = new()
                {
                    FileName = qr.QrCodeUrl,
                    UseShellExecute = true,
                    Verb = "open"
                };
                using System.Diagnostics.Process? process = System.Diagnostics.Process.Start(startInfo);
                _ = process;
            }
            catch (Exception ex)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"无法打开微信登录页面: {ex.Message}"
                };
            }

            // 3) 轮询服务端二维码状态，检测到确认后，由服务端完成登录并返回令牌
            return await WaitForWeChatServerStatusAndLoginAsync(qr.QrCodeKey);
        }
        catch (Exception ex)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"微信登录失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 等待微信登录完成 - 改为轮询服务端二维码状态并在确认后完成登录
    /// </summary>
    private async Task<AuthenticationResult> WaitForWeChatServerStatusAndLoginAsync(string qrCodeKey)
    {
        const int maxAttempts = 120; // 最多等待10分钟（每5秒检查一次）
        const int intervalSeconds = 5;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                WeChatScanStatus? status = await _authenticationService.CheckWeChatStatusAsync(qrCodeKey);
                if (status != null)
                {
                    if (status.Status == 2) // confirmed
                    {
                        // 服务端标记为已确认，此时直接走服务端登录流程
                        AuthenticationResult result = await _authenticationService.LoginWithWeChatAsync(qrCodeKey);
                        return result;
                    }
                    else if (status.Status == 3) // expired
                    {
                        return new AuthenticationResult { IsSuccess = false, ErrorMessage = "二维码已过期，请重试" };
                    }
                }

                // 等待下次检查
                await Task.Delay(intervalSeconds * 1000);
            }
            catch (Exception ex)
            {
                await Task.Delay(intervalSeconds * 1000);
            }
        }

        return new AuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = "微信登录超时，请重新尝试"
        };
    }

    /// <summary>
    /// 处理微信登录成功
    /// </summary>
    private async Task<AuthenticationResult> ProcessWeChatLoginSuccess(WeChatLoginInfo loginInfo)
    {
        try
        {
            // 设置认证令牌
            _authenticationService.SetAuthenticationToken(loginInfo.AccessToken, loginInfo.RefreshToken, loginInfo.User);

            // 验证令牌是否有效
            bool isValid = await _authenticationService.ValidateTokenAsync(loginInfo.AccessToken);

            if (isValid)
            {
                return new AuthenticationResult
                {
                    IsSuccess = true,
                    AccessToken = loginInfo.AccessToken,
                    RefreshToken = loginInfo.RefreshToken,
                    User = loginInfo.User
                };
            }
            else
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "登录令牌验证失败"
                };
            }
        }
        catch (Exception ex)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"处理登录信息失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 获取微信登录状态文件路径
    /// </summary>
    private static string GetWeChatLoginStatusFilePath()
    {
        string tempPath = Path.GetTempPath();
        string filePath = Path.Combine(tempPath, "examina_wechat_login.json");
        return filePath;
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
        return "https://www.qiuzhenbd.com";
    }

    /// <summary>
    /// 测试微信登录（服务端状态轮询版）
    /// </summary>
    private async void TestWeChatFilePolling()
    {
        try
        {
            // 1) 请求二维码
            WeChatQrCodeInfo? qr = await _authenticationService.GetWeChatQrCodeAsync();
            if (qr == null || string.IsNullOrEmpty(qr.QrCodeUrl) || string.IsNullOrEmpty(qr.QrCodeKey))
            {
                return;
            }

            // 2) 打开浏览器
            System.Diagnostics.ProcessStartInfo startInfo = new()
            {
                FileName = qr.QrCodeUrl,
                UseShellExecute = true,
                Verb = "open"
            };
            using System.Diagnostics.Process? process = System.Diagnostics.Process.Start(startInfo);
            _ = process;

            // 3) 服务端轮询等待确认并登录
            AuthenticationResult result = await WaitForWeChatServerStatusAndLoginAsync(qr.QrCodeKey);
        }
        catch (Exception ex)
        {
            // 测试失败，静默处理
        }
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
                // 检查自动登录失败状态时出错，静默处理
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
            UserInfoCompletionWindow userInfoWindow = new(userInfoViewModel);
            userInfoWindow.Show();

            // 关闭登录窗口
            if (desktop.MainWindow is LoginWindow loginWindow)
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
                }
            }

            mainWindow.Show();

            // 关闭登录窗口
            if (desktop.MainWindow is Examina.Views.LoginWindow loginWindow)
            {
                loginWindow.Close();
            }

            desktop.MainWindow = mainWindow;
        }
    }

    // 析构函数 - 二维码定时器已移除，无需清理
}
