using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Examina.Models;
using Examina.Services;
using Prism.Commands;
using ReactiveUI.Fody.Helpers;
using Timer = System.Timers.Timer;

namespace Examina.ViewModels;

/// <summary>
/// 用户信息完善视图模型
/// </summary>
public class UserInfoCompletionViewModel : ViewModelBase
{
    private readonly IAuthenticationService? _authenticationService;

    /// <summary>
    /// 无参构造函数，用于设计时
    /// </summary>
    public UserInfoCompletionViewModel()
    {
        _authenticationService = null;
        CompleteInfoCommand = new DelegateCommand(async () => await CompleteInfoAsync(), CanCompleteInfo);
        SendSmsCodeCommand = new DelegateCommand(async () => await SendSmsCodeAsync(), CanSendSmsCode);
    }

    public UserInfoCompletionViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;

        CompleteInfoCommand = new DelegateCommand(async () => await CompleteInfoAsync(), CanCompleteInfo);
        SendSmsCodeCommand = new DelegateCommand(async () => await SendSmsCodeAsync(), CanSendSmsCode);

        // 初始化当前用户信息
        InitializeUserInfo();

        // 监听属性变化以更新命令状态
        this.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PhoneNumber) ||
                e.PropertyName == nameof(SmsCode) ||
                e.PropertyName == nameof(CanSendSmsCodeValue) ||
                e.PropertyName == nameof(IsProcessing) ||
                e.PropertyName == nameof(IsPhoneVerified))
            {
                ((DelegateCommand)SendSmsCodeCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)CompleteInfoCommand).RaiseCanExecuteChanged();
            }
        };
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

    /// <summary>
    /// 手机号码
    /// </summary>
    [Reactive]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 短信验证码
    /// </summary>
    [Reactive]
    public string SmsCode { get; set; } = string.Empty;

    /// <summary>
    /// 手机号是否已验证
    /// </summary>
    [Reactive]
    public bool IsPhoneVerified { get; set; }

    /// <summary>
    /// 发送验证码按钮文本
    /// </summary>
    [Reactive]
    public string SmsCodeButtonText { get; set; } = "发送验证码";

    /// <summary>
    /// 是否可以发送验证码
    /// </summary>
    [Reactive]
    public bool CanSendSmsCodeValue { get; set; } = true;

    #endregion

    #region 命令

    /// <summary>
    /// 完善信息命令
    /// </summary>
    public ICommand CompleteInfoCommand { get; }

    /// <summary>
    /// 发送短信验证码命令
    /// </summary>
    public ICommand SendSmsCodeCommand { get; }

    #endregion

    #region 私有字段

    /// <summary>
    /// 短信验证码倒计时定时器
    /// </summary>
    private Timer? _smsCodeTimer;

    /// <summary>
    /// 短信验证码倒计时秒数
    /// </summary>
    private int _smsCodeCountdown;

    #endregion

    #region 方法

    /// <summary>
    /// 初始化用户信息
    /// </summary>
    private void InitializeUserInfo()
    {
        if (_authenticationService != null)
        {
            CurrentUser = _authenticationService.CurrentUser;
            if (CurrentUser != null)
            {
                Username = CurrentUser.Username;
            }
        }
    }

    /// <summary>
    /// 是否可以完善信息
    /// </summary>
    private bool CanCompleteInfo()
    {
        bool canComplete = !IsProcessing &&
                          !string.IsNullOrWhiteSpace(PhoneNumber) &&
                          PhoneNumber.Length == 11 &&
                          !string.IsNullOrWhiteSpace(SmsCode);
        System.Diagnostics.Debug.WriteLine($"[UserInfo] CanCompleteInfo: {canComplete}, IsProcessing: {IsProcessing}, PhoneNumber: '{PhoneNumber}', SmsCode: '{SmsCode}'");
        return canComplete;
    }

    /// <summary>
    /// 是否可以发送短信验证码
    /// </summary>
    private bool CanSendSmsCode()
    {
        bool canSend = CanSendSmsCodeValue && !string.IsNullOrWhiteSpace(PhoneNumber) && PhoneNumber.Length == 11;
        System.Diagnostics.Debug.WriteLine($"[UserInfo] CanSendSmsCode: {canSend}, CanSendSmsCodeValue: {CanSendSmsCodeValue}, PhoneNumber: '{PhoneNumber}', Length: {PhoneNumber?.Length ?? 0}");
        return canSend;
    }

    /// <summary>
    /// 完善信息
    /// </summary>
    private async Task CompleteInfoAsync()
    {
        if (IsProcessing)
        {
            return;
        }

        if (_authenticationService == null)
        {
            ErrorMessage = "服务不可用";
            return;
        }

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

            // 验证短信验证码
            bool isCodeValid = await _authenticationService.VerifySmsCodeAsync(PhoneNumber, SmsCode);
            if (!isCodeValid)
            {
                ErrorMessage = "验证码错误或已过期";
                return;
            }

            // 验证成功，标记手机号已验证
            IsPhoneVerified = true;

            CompleteUserInfoRequest request = new()
            {
                Username = string.IsNullOrWhiteSpace(Username) ? null : Username.Trim(),
                Password = string.IsNullOrWhiteSpace(Password) ? null : Password,
                PhoneNumber = PhoneNumber.Trim()
            };

            System.Diagnostics.Debug.WriteLine($"[用户信息完善] 当前用户: {CurrentUser?.Username}, ID: {CurrentUser?.Id}, 原手机号: {CurrentUser?.PhoneNumber}");
            System.Diagnostics.Debug.WriteLine($"[用户信息完善] 请求更新手机号为: {request.PhoneNumber}");

            UserInfo? updatedUser = await _authenticationService.CompleteUserInfoAsync(request);
            if (updatedUser != null)
            {
                System.Diagnostics.Debug.WriteLine($"[用户信息完善] 更新成功，用户: {updatedUser.Username}, ID: {updatedUser.Id}, 新手机号: {updatedUser.PhoneNumber}");
                SuccessMessage = "用户信息更新成功！";
                CurrentUser = updatedUser;

                // 延迟一下显示成功消息
                await Task.Delay(1500);

                // 导航到主页面
                NavigateToMainWindow();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[用户信息完善] 更新失败，返回null");
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
    /// 发送短信验证码
    /// </summary>
    private async Task SendSmsCodeAsync()
    {
        if (_authenticationService == null)
        {
            ErrorMessage = "服务不可用";
            return;
        }

        if (string.IsNullOrWhiteSpace(PhoneNumber) || PhoneNumber.Length != 11)
        {
            ErrorMessage = "请输入正确的手机号码";
            return;
        }

        try
        {
            ErrorMessage = string.Empty;
            bool success = await _authenticationService.SendSmsCodeAsync(PhoneNumber);

            if (success)
            {
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
            ErrorMessage = $"发送验证码时发生错误: {ex.Message}";
        }
    }

    /// <summary>
    /// 开始短信验证码倒计时
    /// </summary>
    private void StartSmsCodeCountdown()
    {
        _smsCodeCountdown = 60;
        CanSendSmsCodeValue = false;
        SmsCodeButtonText = $"重新发送({_smsCodeCountdown}s)";

        _smsCodeTimer = new Timer(1000);
        _smsCodeTimer.Elapsed += (sender, e) =>
        {
            _smsCodeCountdown--;
            if (_smsCodeCountdown <= 0)
            {
                _smsCodeTimer?.Stop();
                _smsCodeTimer?.Dispose();
                _smsCodeTimer = null;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    CanSendSmsCodeValue = true;
                    SmsCodeButtonText = "发送验证码";
                });
            }
            else
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    SmsCodeButtonText = $"重新发送({_smsCodeCountdown}s)";
                });
            }
        };
        _smsCodeTimer.Start();
    }

    /// <summary>
    /// 验证输入
    /// </summary>
    private bool ValidateInput()
    {
        // 验证手机号
        if (string.IsNullOrWhiteSpace(PhoneNumber) || PhoneNumber.Length != 11)
        {
            ErrorMessage = "请输入正确的手机号码";
            return false;
        }

        // 验证短信验证码
        if (string.IsNullOrWhiteSpace(SmsCode))
        {
            ErrorMessage = "请输入短信验证码";
            return false;
        }

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
