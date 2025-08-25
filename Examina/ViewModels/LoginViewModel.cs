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
    private Timer? _qrCodeStatusTimer;
    private string? _currentQrCodeKey;

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
        RefreshQrCodeCommand = new DelegateCommand(async () => await RefreshQrCodeAsync());
    }

    /// <summary>
    /// 初始化属性监听器
    /// </summary>
    private void InitializePropertyWatchers()
    {
        // 监听属性更改以更新命令状态
        _ = this.WhenAnyValue(x => x.Username, x => x.Password, x => x.PhoneNumber, x => x.SmsCode,
                         x => x.IsLoading, x => x.QrCodeUrl, x => x.LoginMode)
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

    // 微信登录相关
    [Reactive] public string QrCodeUrl { get; set; } = string.Empty;
    [Reactive] public string QrCodeStatus { get; set; } = "等待扫描";

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
    public ICommand RefreshQrCodeCommand { get; private set; } = null!;

    private bool CanExecuteLogin()
    {
        return !IsLoading && LoginMode switch
        {
            LoginMode.Credentials => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password),
            LoginMode.SmsCode => !string.IsNullOrEmpty(PhoneNumber) && !string.IsNullOrEmpty(SmsCode),
            LoginMode.WeChat => !string.IsNullOrEmpty(QrCodeUrl),
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
                LoginMode.WeChat => await _authenticationService.LoginWithWeChatAsync(_currentQrCodeKey ?? ""),
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
        _ = RefreshQrCodeAsync();
    }

    private void SwitchToCredentials()
    {
        LoginMode = LoginMode.Credentials;
        ClearMessages();
        StopQrCodeStatusTimer();
    }

    private void SwitchToSms()
    {
        LoginMode = LoginMode.SmsCode;
        ClearMessages();
        StopQrCodeStatusTimer();
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

    private async Task RefreshQrCodeAsync()
    {
        try
        {
            IsLoading = true;
            QrCodeStatus = "正在获取二维码...";

            WeChatQrCodeInfo? qrCodeInfo = await _authenticationService.GetWeChatQrCodeAsync();
            if (qrCodeInfo != null)
            {
                _currentQrCodeKey = qrCodeInfo.QrCodeKey;
                QrCodeUrl = qrCodeInfo.QrCodeUrl;
                QrCodeStatus = "请使用微信扫描二维码";

                try
                {
                    // 打开系统默认浏览器，导航到EW微信授权页面（便于在桌面端完成扫码授权）
                    System.Diagnostics.ProcessStartInfo startInfo = new()
                    {
                        FileName = QrCodeUrl,
                        UseShellExecute = true,
                        Verb = "open"
                    };
                    using System.Diagnostics.Process? process = System.Diagnostics.Process.Start(startInfo);
                    _ = process; // 忽略返回值，仅触发浏览器打开
                }
                catch (Exception)
                {
                    // 忽略浏览器打开异常，用户仍可手动扫码
                }

                // 开始轮询二维码状态
                StartQrCodeStatusTimer();
            }
            else
            {
                ErrorMessage = "获取微信登录二维码失败";
                QrCodeStatus = "获取二维码失败";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"获取二维码失败: {ex.Message}";
            QrCodeStatus = "获取二维码失败";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void StartQrCodeStatusTimer()
    {
        StopQrCodeStatusTimer();

        _qrCodeStatusTimer = new Timer(3000); // 每3秒检查一次
        _qrCodeStatusTimer.Elapsed += async (sender, e) =>
        {
            // 确保异步操作在UI线程中执行
            await Dispatcher.UIThread.InvokeAsync(async () => await CheckQrCodeStatusAsync());
        };
        _qrCodeStatusTimer.Start();
    }

    private void StopQrCodeStatusTimer()
    {
        _qrCodeStatusTimer?.Stop();
        _qrCodeStatusTimer?.Dispose();
        _qrCodeStatusTimer = null;
    }

    private async Task CheckQrCodeStatusAsync()
    {
        if (string.IsNullOrEmpty(_currentQrCodeKey))
        {
            return;
        }

        try
        {
            WeChatScanStatus? status = await _authenticationService.CheckWeChatStatusAsync(_currentQrCodeKey);
            if (status != null)
            {
                QrCodeStatus = status.Status switch
                {
                    0 => "等待扫描",
                    1 => "已扫描，等待确认",
                    2 => "已确认，正在登录...",
                    3 => "二维码已过期",
                    _ => "未知状态"
                };

                if (status.Status == 2 && !string.IsNullOrEmpty(status.Code))
                {
                    // 二维码已确认，执行登录
                    StopQrCodeStatusTimer();
                    // 不要覆盖二维码Key，后端使用该Key查询状态；授权码在服务端使用
                    await ExecuteLoginAsync();
                }
                else if (status.Status == 3)
                {
                    // 二维码已过期，停止轮询
                    StopQrCodeStatusTimer();
                    ErrorMessage = "二维码已过期，请刷新";
                }
            }
        }
        catch
        {
            // 忽略状态检查错误
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

    // 析构函数，确保定时器被清理
    ~LoginViewModel()
    {
        StopQrCodeStatusTimer();
    }
}
