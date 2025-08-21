using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Examina.Models;
using Examina.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels;

/// <summary>
/// 考试状态枚举
/// </summary>
public enum ExamStatus
{
    /// <summary>
    /// 准备中
    /// </summary>
    Preparing,

    /// <summary>
    /// 进行中
    /// </summary>
    InProgress,

    /// <summary>
    /// 即将结束（最后5分钟）
    /// </summary>
    AboutToEnd,

    /// <summary>
    /// 已结束
    /// </summary>
    Ended,

    /// <summary>
    /// 已提交
    /// </summary>
    Submitted
}

/// <summary>
/// 提交状态枚举
/// </summary>
public enum SubmitStatus
{
    /// <summary>
    /// 准备提交
    /// </summary>
    Ready,

    /// <summary>
    /// 等待确认
    /// </summary>
    WaitingConfirmation,

    /// <summary>
    /// 提交中
    /// </summary>
    Submitting,

    /// <summary>
    /// 提交成功
    /// </summary>
    Success,

    /// <summary>
    /// 提交失败
    /// </summary>
    Failed
}

/// <summary>
/// 考试工具栏的ViewModel
/// </summary>
public class ExamToolbarViewModel : ViewModelBase, IDisposable
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<ExamToolbarViewModel> _logger;
    private Timer? _countdownTimer;
    private bool _disposed;

    /// <summary>
    /// 考试类型
    /// </summary>
    [Reactive] public ExamType CurrentExamType { get; set; } = ExamType.MockExam;

    /// <summary>
    /// 考试状态
    /// </summary>
    [Reactive] public ExamStatus CurrentExamStatus { get; set; } = ExamStatus.Preparing;

    /// <summary>
    /// 学生姓名
    /// </summary>
    [Reactive] public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// 学生学号
    /// </summary>
    [Reactive] public string StudentId { get; set; } = string.Empty;

    /// <summary>
    /// 考试剩余时间（秒）
    /// </summary>
    [Reactive] public int RemainingTimeSeconds { get; set; }

    /// <summary>
    /// 格式化的剩余时间显示（HH:MM:SS）
    /// </summary>
    [Reactive] public string FormattedRemainingTime { get; set; } = "00:00:00";

    /// <summary>
    /// 考试名称
    /// </summary>
    [Reactive] public string ExamName { get; set; } = string.Empty;

    /// <summary>
    /// 考试ID
    /// </summary>
    [Reactive] public int ExamId { get; set; }

    /// <summary>
    /// 题目总数
    /// </summary>
    [Reactive] public int TotalQuestions { get; set; }

    /// <summary>
    /// 时间警告阈值（秒，默认5分钟）
    /// </summary>
    [Reactive] public int TimeWarningThreshold { get; set; } = 300;

    /// <summary>
    /// 是否时间紧急（剩余时间少于警告阈值）
    /// </summary>
    [Reactive] public bool IsTimeUrgent { get; set; }

    /// <summary>
    /// 是否可以提交考试
    /// </summary>
    [Reactive] public bool CanSubmitExam { get; set; } = true;

    /// <summary>
    /// 是否正在提交
    /// </summary>
    [Reactive] public bool IsSubmitting { get; set; }

    /// <summary>
    /// 网络连接状态
    /// </summary>
    [Reactive] public bool IsNetworkConnected { get; set; } = true;

    /// <summary>
    /// 提交状态
    /// </summary>
    [Reactive] public SubmitStatus CurrentSubmitStatus { get; set; } = SubmitStatus.Ready;

    /// <summary>
    /// 提交进度（0-100）
    /// </summary>
    [Reactive] public double SubmitProgress { get; set; } = 0;

    /// <summary>
    /// 提交消息
    /// </summary>
    [Reactive] public string SubmitMessage { get; set; } = string.Empty;

    /// <summary>
    /// 是否显示提交结果对话框
    /// </summary>
    [Reactive] public bool ShowSubmitResultDialog { get; set; } = false;

    /// <summary>
    /// 是否可以重试提交
    /// </summary>
    [Reactive] public bool CanRetrySubmit { get; set; } = false;

    /// <summary>
    /// 所有题目是否已完成
    /// </summary>
    [Reactive] public bool AllQuestionsCompleted { get; set; } = false;

    /// <summary>
    /// 查看题目命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ViewQuestionsCommand { get; }

    /// <summary>
    /// 提交考试命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SubmitExamCommand { get; }

    /// <summary>
    /// 确认提交命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ConfirmSubmitCommand { get; }

    /// <summary>
    /// 重试提交命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RetrySubmitCommand { get; }

    /// <summary>
    /// 关闭结果对话框命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> CloseResultDialogCommand { get; }

    /// <summary>
    /// 考试自动提交事件
    /// </summary>
    public event EventHandler? ExamAutoSubmitted;

    /// <summary>
    /// 考试手动提交事件
    /// </summary>
    public event EventHandler? ExamManualSubmitted;

    /// <summary>
    /// 提交完成后窗口关闭事件
    /// </summary>
    public event EventHandler? WindowCloseRequested;

    /// <summary>
    /// 查看题目请求事件
    /// </summary>
    public event EventHandler? ViewQuestionsRequested;

    /// <summary>
    /// 无参数构造函数（用于设计时）
    /// </summary>
    public ExamToolbarViewModel()
    {
        _authenticationService = new DesignTimeAuthenticationService();
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ExamToolbarViewModel>.Instance;

        // 初始化命令
        ViewQuestionsCommand = ReactiveCommand.Create(ViewQuestions);
        SubmitExamCommand = ReactiveCommand.CreateFromTask(ShowSubmitConfirmationAsync, this.WhenAnyValue(x => x.CanSubmitExam, x => x.IsSubmitting, (canSubmit, isSubmitting) => canSubmit && !isSubmitting));
        ConfirmSubmitCommand = ReactiveCommand.CreateFromTask(PerformSubmitAsync);
        RetrySubmitCommand = ReactiveCommand.CreateFromTask(PerformSubmitAsync, this.WhenAnyValue(x => x.CanRetrySubmit));
        CloseResultDialogCommand = ReactiveCommand.Create(CloseResultDialog);

        // 监听剩余时间变化，更新格式化时间和紧急状态
        _ = this.WhenAnyValue(x => x.RemainingTimeSeconds)
            .Subscribe(UpdateTimeDisplay);

        // 设置设计时数据
        StudentName = "张三";
        StudentId = "2021001";
        ExamName = "模拟考试";
        RemainingTimeSeconds = 3600; // 1小时
        CanSubmitExam = true;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamToolbarViewModel(IAuthenticationService authenticationService, ILogger<ExamToolbarViewModel>? logger)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ExamToolbarViewModel>.Instance;

        // 初始化命令
        ViewQuestionsCommand = ReactiveCommand.Create(ViewQuestions);
        SubmitExamCommand = ReactiveCommand.CreateFromTask(ShowSubmitConfirmationAsync, this.WhenAnyValue(x => x.CanSubmitExam, x => x.IsSubmitting, (canSubmit, isSubmitting) => canSubmit && !isSubmitting));
        ConfirmSubmitCommand = ReactiveCommand.CreateFromTask(PerformSubmitAsync);
        RetrySubmitCommand = ReactiveCommand.CreateFromTask(PerformSubmitAsync, this.WhenAnyValue(x => x.CanRetrySubmit));
        CloseResultDialogCommand = ReactiveCommand.Create(CloseResultDialog);

        // 监听剩余时间变化，更新格式化时间和紧急状态
        _ = this.WhenAnyValue(x => x.RemainingTimeSeconds)
            .Subscribe(UpdateTimeDisplay);

        // 初始化学生信息
        InitializeStudentInfo();
    }

    /// <summary>
    /// 初始化学生信息
    /// </summary>
    private void InitializeStudentInfo()
    {
        UserInfo? currentUser = _authenticationService.CurrentUser;
        if (currentUser != null)
        {
            StudentName = currentUser.RealName ?? currentUser.Username;
            StudentId = currentUser.Id;
        }
    }

    /// <summary>
    /// 开始考试倒计时
    /// </summary>
    /// <param name="durationSeconds">考试时长（秒）</param>
    public void StartCountdown(int durationSeconds)
    {
        RemainingTimeSeconds = durationSeconds;
        CurrentExamStatus = ExamStatus.InProgress;

        _countdownTimer?.Dispose();
        _countdownTimer = new Timer(CountdownTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        _logger.LogInformation("考试倒计时开始，时长: {Duration}秒", durationSeconds);
    }

    /// <summary>
    /// 停止倒计时
    /// </summary>
    public void StopCountdown()
    {
        _countdownTimer?.Dispose();
        _countdownTimer = null;
        _logger.LogInformation("考试倒计时停止");
    }

    /// <summary>
    /// 倒计时回调
    /// </summary>
    private void CountdownTick(object? state)
    {
        if (_disposed || CurrentExamStatus == ExamStatus.Ended || CurrentExamStatus == ExamStatus.Submitted)
        {
            return;
        }

        RemainingTimeSeconds--;

        // 检查自动提交条件
        if (CheckAutoSubmitConditions())
        {
            CurrentExamStatus = ExamStatus.Ended;
            StopCountdown();

            _logger.LogWarning("满足自动提交条件，触发自动提交");
            _ = Task.Run(TriggerAutoSubmitAsync);
        }
        else if (RemainingTimeSeconds <= TimeWarningThreshold && CurrentExamStatus != ExamStatus.AboutToEnd)
        {
            // 进入即将结束状态
            CurrentExamStatus = ExamStatus.AboutToEnd;
            _logger.LogInformation("考试进入即将结束状态，剩余时间: {RemainingTime}秒", RemainingTimeSeconds);
        }
    }

    /// <summary>
    /// 检查自动提交条件
    /// </summary>
    private bool CheckAutoSubmitConditions()
    {
        // 时间到期
        if (RemainingTimeSeconds <= 0)
        {
            _logger.LogInformation("自动提交条件：时间到期");
            return true;
        }

        // 所有题目已完成且剩余时间少于30秒
        if (AllQuestionsCompleted && RemainingTimeSeconds <= 30)
        {
            _logger.LogInformation("自动提交条件：所有题目已完成且剩余时间少于30秒");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 更新时间显示
    /// </summary>
    private void UpdateTimeDisplay(int remainingSeconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(Math.Max(0, remainingSeconds));
        FormattedRemainingTime = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        IsTimeUrgent = remainingSeconds <= TimeWarningThreshold && remainingSeconds > 0;
    }

    /// <summary>
    /// 查看题目
    /// </summary>
    private void ViewQuestions()
    {
        ViewQuestionsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 显示提交确认对话框
    /// </summary>
    private async Task ShowSubmitConfirmationAsync()
    {
        try
        {
            _logger.LogInformation("显示手动提交确认对话框");
            CurrentSubmitStatus = SubmitStatus.WaitingConfirmation;
            SubmitMessage = "确定要提交考试吗？提交后将无法继续答题。";

            // 创建 ContentDialog
            var dialog = new ContentDialog
            {
                Title = "提交确认",
                Content = SubmitMessage,
                PrimaryButtonText = "确认提交",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            // 获取当前窗口作为父窗口
            Window? parentWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                parentWindow = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
            }

            if (parentWindow != null)
            {
                var result = await dialog.ShowAsync(parentWindow);

                if (result == ContentDialogResult.Primary)
                {
                    // 用户点击了"确认提交"
                    await PerformSubmitAsync();
                }
                else
                {
                    // 用户点击了"取消"或关闭了对话框
                    CancelSubmit();
                }
            }
            else
            {
                _logger.LogWarning("无法找到父窗口，无法显示确认对话框");
                // 如果找不到父窗口，直接执行提交
                await PerformSubmitAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "显示提交确认对话框失败");
            // 发生异常时，取消提交状态
            CancelSubmit();
        }
    }

    /// <summary>
    /// 执行提交操作
    /// </summary>
    private async Task PerformSubmitAsync()
    {
        try
        {
            _logger.LogInformation("开始执行考试提交，考试ID: {ExamId}", ExamId);

            // 隐藏确认对话框
            ShowSubmitConfirmDialog = false;

            // 设置提交状态
            CurrentSubmitStatus = SubmitStatus.Submitting;
            IsSubmitting = true;
            CanSubmitExam = false;
            SubmitProgress = 0;
            SubmitMessage = "正在提交考试，请稍候...";

            // 停止倒计时
            StopCountdown();

            // 模拟提交进度
            for (int i = 0; i <= 100; i += 10)
            {
                SubmitProgress = i;
                await Task.Delay(200); // 模拟网络延迟
            }

            // 触发提交事件
            ExamManualSubmitted?.Invoke(this, EventArgs.Empty);

            // 设置成功状态
            await HandleSubmitSuccessAsync();
        }
        catch (Exception ex)
        {
            HandleSubmitFailure(ex);
        }
    }

    /// <summary>
    /// 处理提交成功
    /// </summary>
    private async Task HandleSubmitSuccessAsync()
    {
        try
        {
            _logger.LogInformation("考试提交成功");

            CurrentSubmitStatus = SubmitStatus.Success;
            CurrentExamStatus = ExamStatus.Submitted;
            IsSubmitting = false;
            SubmitProgress = 100;
            SubmitMessage = "考试提交成功！";
            ShowSubmitResultDialog = true;

            // 延迟3秒后自动关闭窗口
            await Task.Delay(3000);
            await CloseWindowAfterSubmitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理提交成功状态时发生错误");
        }
    }

    /// <summary>
    /// 处理提交失败
    /// </summary>
    private void HandleSubmitFailure(Exception exception)
    {
        try
        {
            _logger.LogError(exception, "考试提交失败");

            CurrentSubmitStatus = SubmitStatus.Failed;
            IsSubmitting = false;
            CanSubmitExam = true;
            CanRetrySubmit = true;
            SubmitMessage = $"提交失败：{exception.Message}";
            ShowSubmitResultDialog = true;

            // 恢复倒计时（如果还有时间）
            if (RemainingTimeSeconds > 0)
            {
                StartCountdown(RemainingTimeSeconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理提交失败状态时发生错误");
        }
    }

    /// <summary>
    /// 触发自动提交
    /// </summary>
    private async Task TriggerAutoSubmitAsync()
    {
        try
        {
            _logger.LogWarning("触发自动提交");

            CurrentSubmitStatus = SubmitStatus.Submitting;
            IsSubmitting = true;
            SubmitMessage = "考试时间到，正在自动提交...";

            // 触发自动提交事件
            ExamAutoSubmitted?.Invoke(this, EventArgs.Empty);

            // 设置成功状态
            await HandleSubmitSuccessAsync();
        }
        catch (Exception ex)
        {
            HandleSubmitFailure(ex);
        }
    }

    /// <summary>
    /// 取消提交
    /// </summary>
    private void CancelSubmit()
    {
        try
        {
            _logger.LogInformation("用户取消提交");

            CurrentSubmitStatus = SubmitStatus.Ready;
            SubmitMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消提交时发生错误");
        }
    }

    /// <summary>
    /// 关闭结果对话框
    /// </summary>
    private void CloseResultDialog()
    {
        try
        {
            ShowSubmitResultDialog = false;

            if (CurrentSubmitStatus == SubmitStatus.Success)
            {
                _ = Task.Run(CloseWindowAfterSubmitAsync);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关闭结果对话框时发生错误");
        }
    }

    /// <summary>
    /// 提交完成后关闭窗口
    /// </summary>
    private async Task CloseWindowAfterSubmitAsync()
    {
        try
        {
            _logger.LogInformation("准备关闭考试工具栏窗口");

            // 保存考试数据
            await SaveExamDataAsync();

            // 清理资源
            await CleanupResourcesAsync();

            // 触发窗口关闭事件
            WindowCloseRequested?.Invoke(this, EventArgs.Empty);

            // 恢复主窗口
            await RestoreMainWindowAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关闭窗口时发生错误");
        }
    }

    /// <summary>
    /// 保存考试数据
    /// </summary>
    private async Task SaveExamDataAsync()
    {
        try
        {
            _logger.LogInformation("保存考试数据");
            // TODO: 实现考试数据保存逻辑
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存考试数据失败");
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    private async Task CleanupResourcesAsync()
    {
        try
        {
            _logger.LogInformation("清理考试资源");

            // 停止倒计时
            StopCountdown();

            // 清理临时文件
            // TODO: 实现临时文件清理逻辑

            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理资源失败");
        }
    }

    /// <summary>
    /// 恢复主窗口
    /// </summary>
    private async Task RestoreMainWindowAsync()
    {
        try
        {
            _logger.LogInformation("恢复主窗口显示");

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
                desktop.MainWindow.Activate();
            }

            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复主窗口失败");
        }
    }

    /// <summary>
    /// 设置考试信息
    /// </summary>
    public void SetExamInfo(ExamType examType, int examId, string examName, int totalQuestions, int durationSeconds)
    {
        CurrentExamType = examType;
        ExamId = examId;
        ExamName = examName;
        TotalQuestions = totalQuestions;
        RemainingTimeSeconds = durationSeconds;
        CurrentExamStatus = ExamStatus.Preparing;

        _logger.LogInformation("设置考试信息 - 类型: {ExamType}, ID: {ExamId}, 名称: {ExamName}, 题目数: {TotalQuestions}, 时长: {Duration}秒",
            examType, examId, examName, totalQuestions, durationSeconds);
    }

    /// <summary>
    /// 开始考试（设置状态为进行中并开始倒计时）
    /// </summary>
    public void StartExam()
    {
        if (CurrentExamStatus == ExamStatus.Preparing)
        {
            CurrentExamStatus = ExamStatus.InProgress;

            // 开始倒计时
            _countdownTimer?.Dispose();
            _countdownTimer = new Timer(CountdownTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            _logger.LogInformation("考试开始 - 类型: {ExamType}, ID: {ExamId}, 名称: {ExamName}, 剩余时间: {RemainingTime}秒",
                CurrentExamType, ExamId, ExamName, RemainingTimeSeconds);
        }
        else
        {
            _logger.LogWarning("无法开始考试，当前状态: {CurrentStatus}", CurrentExamStatus);
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _countdownTimer?.Dispose();

            // 清理事件订阅
            ExamAutoSubmitted = null;
            ExamManualSubmitted = null;
            ViewQuestionsRequested = null;
            WindowCloseRequested = null;

            _disposed = true;
            _logger.LogInformation("ExamToolbarViewModel资源已释放");
        }
    }
}

/// <summary>
/// 设计时认证服务（仅用于XAML设计时）
/// </summary>
internal class DesignTimeAuthenticationService : IAuthenticationService
{
    public UserInfo? CurrentUser => new()
    {
        Id = "1",
        Username = "design_user",
        RealName = "设计时用户",
        HasFullAccess = true
    };

    public bool IsAuthenticated => true;

    public string? CurrentAccessToken => throw new NotImplementedException();

    public string? CurrentRefreshToken => throw new NotImplementedException();

    public DateTime? TokenExpiresAt => throw new NotImplementedException();

    public bool NeedsTokenRefresh => throw new NotImplementedException();

    public event EventHandler<UserInfo?>? UserInfoUpdated;

    public Task<bool> LoginAsync(string username, string password)
    {
        return Task.FromResult(true);
    }

    public Task LogoutAsync()
    {
        return Task.CompletedTask;
    }

    public Task<UserInfo?> GetCurrentUserAsync()
    {
        return Task.FromResult(CurrentUser);
    }

    public Task<bool> RefreshTokenAsync()
    {
        return Task.FromResult(true);
    }

    public Task<bool> ValidateTokenAsync()
    {
        return Task.FromResult(true);
    }

    public Task<AuthenticationResult> LoginWithCredentialsAsync(string username, string password)
    {
        throw new NotImplementedException();
    }

    public Task<AuthenticationResult> LoginWithSmsAsync(string phoneNumber, string smsCode)
    {
        throw new NotImplementedException();
    }

    public Task<AuthenticationResult> LoginWithWeChatAsync(string qrCode)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SendSmsCodeAsync(string phoneNumber)
    {
        throw new NotImplementedException();
    }

    public Task<WeChatQrCodeInfo?> GetWeChatQrCodeAsync()
    {
        throw new NotImplementedException();
    }

    public Task<WeChatScanStatus?> CheckWeChatStatusAsync(string qrCodeKey)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        throw new NotImplementedException();
    }

    Task<AuthenticationResult> IAuthenticationService.RefreshTokenAsync()
    {
        throw new NotImplementedException();
    }

    public Task<List<DeviceInfo>> GetUserDevicesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<UserInfo?> CompleteUserInfoAsync(CompleteUserInfoRequest request)
    {
        throw new NotImplementedException();
    }

    public bool RequiresUserInfoCompletion()
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateUserProfileAsync(UpdateUserProfileRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<string?> GetAccessTokenAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> RefreshUserInfoAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> SaveLoginDataAsync(LoginResponse loginResponse)
    {
        throw new NotImplementedException();
    }

    public Task<PersistentLoginData?> LoadLoginDataAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> ClearLoginDataAsync()
    {
        throw new NotImplementedException();
    }

    public Task<AuthenticationResult> AutoAuthenticateAsync()
    {
        throw new NotImplementedException();
    }

    public Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
    {
        throw new NotImplementedException();
    }
}
