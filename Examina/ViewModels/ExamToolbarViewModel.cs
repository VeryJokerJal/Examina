using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Examina.Models;
using Examina.Models.BenchSuite;
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
    private readonly IBenchSuiteDirectoryService? _benchSuiteDirectoryService;
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
    /// 是否显示答案解析按钮
    /// </summary>
    public bool ShowAnswerAnalysisButton => CurrentExamType == ExamType.ComprehensiveTraining || CurrentExamType == ExamType.SpecializedTraining;

    /// <summary>
    /// 考试剩余时间（秒）
    /// </summary>
    [Reactive] public int RemainingTimeSeconds { get; set; }

    /// <summary>
    /// 已用时间（秒）- 用于专项训练和综合实训的正向计时
    /// </summary>
    [Reactive] public int ElapsedTimeSeconds { get; set; }

    /// <summary>
    /// 格式化的剩余时间显示（HH:MM:SS）
    /// </summary>
    [Reactive] public string FormattedRemainingTime { get; set; } = "00:00:00";

    /// <summary>
    /// 格式化的已用时间显示（HH:MM:SS）
    /// </summary>
    [Reactive] public string FormattedElapsedTime { get; set; } = "00:00:00";

    /// <summary>
    /// 是否使用正向计时（专项训练和综合实训使用）
    /// </summary>
    [Reactive] public bool UseForwardTiming { get; set; }

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
    /// 考试开始时间
    /// </summary>
    public DateTime? ExamStartTime { get; private set; }

    /// <summary>
    /// 考试总时长（秒）
    /// </summary>
    public int TotalDurationSeconds { get; private set; }

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
    /// 所有题目是否已完成
    /// </summary>
    [Reactive] public bool AllQuestionsCompleted { get; set; } = false;

    /// <summary>
    /// 查看题目命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ViewQuestionsCommand { get; }

    /// <summary>
    /// 查看答案解析命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ViewAnswerAnalysisCommand { get; }

    /// <summary>
    /// 提交考试命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SubmitExamCommand { get; }

    /// <summary>
    /// 确认提交命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ConfirmSubmitCommand { get; }

    /// <summary>
    /// 打开目录命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenDirectoryCommand { get; }

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
    /// 查看答案解析请求事件
    /// </summary>
    public event EventHandler? ViewAnswerAnalysisRequested;

    /// <summary>
    /// 无参数构造函数（用于设计时）
    /// </summary>
    public ExamToolbarViewModel()
    {
        _authenticationService = new DesignTimeAuthenticationService();
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ExamToolbarViewModel>.Instance;

        // 初始化命令
        ViewQuestionsCommand = ReactiveCommand.Create(ViewQuestions);
        ViewAnswerAnalysisCommand = ReactiveCommand.Create(ViewAnswerAnalysis);
        SubmitExamCommand = ReactiveCommand.CreateFromTask(PerformSubmitAsync, this.WhenAnyValue(x => x.CanSubmitExam, x => x.IsSubmitting, (canSubmit, isSubmitting) => canSubmit && !isSubmitting));
        ConfirmSubmitCommand = ReactiveCommand.CreateFromTask(PerformSubmitAsync);
        OpenDirectoryCommand = ReactiveCommand.CreateFromTask(OpenDirectoryAsync);

        // 监听剩余时间变化，更新格式化时间和紧急状态
        _ = this.WhenAnyValue(x => x.RemainingTimeSeconds)
            .Subscribe(seconds =>
            {
                _logger.LogDebug("响应式监听触发（设计时） - RemainingTimeSeconds变化为: {Seconds}", seconds);
                UpdateTimeDisplay(seconds);
            });

        // 监听已用时间变化，更新格式化已用时间
        _ = this.WhenAnyValue(x => x.ElapsedTimeSeconds)
            .Subscribe(seconds =>
            {
                _logger.LogDebug("响应式监听触发（设计时） - ElapsedTimeSeconds变化为: {Seconds}", seconds);
                UpdateElapsedTimeDisplay(seconds);
            });

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
    public ExamToolbarViewModel(IAuthenticationService authenticationService, ILogger<ExamToolbarViewModel>? logger, IBenchSuiteDirectoryService? benchSuiteDirectoryService = null)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _benchSuiteDirectoryService = benchSuiteDirectoryService;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ExamToolbarViewModel>.Instance;

        // 初始化命令
        ViewQuestionsCommand = ReactiveCommand.Create(ViewQuestions);
        ViewAnswerAnalysisCommand = ReactiveCommand.Create(ViewAnswerAnalysis);
        SubmitExamCommand = ReactiveCommand.CreateFromTask(PerformSubmitAsync, this.WhenAnyValue(x => x.CanSubmitExam, x => x.IsSubmitting, (canSubmit, isSubmitting) => canSubmit && !isSubmitting));
        ConfirmSubmitCommand = ReactiveCommand.CreateFromTask(PerformSubmitAsync);
        OpenDirectoryCommand = ReactiveCommand.CreateFromTask(OpenDirectoryAsync);

        // 监听剩余时间变化，更新格式化时间和紧急状态
        _ = this.WhenAnyValue(x => x.RemainingTimeSeconds)
            .Subscribe(seconds =>
            {
                _logger.LogDebug("响应式监听触发 - RemainingTimeSeconds变化为: {Seconds}", seconds);
                UpdateTimeDisplay(seconds);
            });

        // 监听已用时间变化，更新格式化已用时间
        _ = this.WhenAnyValue(x => x.ElapsedTimeSeconds)
            .Subscribe(seconds =>
            {
                _logger.LogDebug("响应式监听触发 - ElapsedTimeSeconds变化为: {Seconds}", seconds);
                UpdateElapsedTimeDisplay(seconds);
            });

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
    /// 获取实际用时（秒）
    /// </summary>
    public int GetActualDurationSeconds()
    {
        if (UseForwardTiming)
        {
            // 正向计时模式：直接返回已用时间
            _logger.LogInformation("正向计时模式 - 已用时间: {ElapsedTime}秒", ElapsedTimeSeconds);
            return ElapsedTimeSeconds;
        }

        if (ExamStartTime.HasValue)
        {
            int actualSeconds = (int)(DateTime.Now - ExamStartTime.Value).TotalSeconds;
            _logger.LogInformation("计算实际用时 - 开始时间: {StartTime}, 当前时间: {CurrentTime}, 实际用时: {ActualDuration}秒",
                ExamStartTime.Value, DateTime.Now, actualSeconds);
            return actualSeconds;
        }

        // 如果没有开始时间，使用总时长减去剩余时间
        int fallbackDuration = TotalDurationSeconds - RemainingTimeSeconds;
        _logger.LogWarning("无开始时间记录，使用备用计算方式 - 总时长: {TotalDuration}秒, 剩余时间: {RemainingTime}秒, 计算用时: {FallbackDuration}秒",
            TotalDurationSeconds, RemainingTimeSeconds, fallbackDuration);
        return Math.Max(0, fallbackDuration);
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

        if (UseForwardTiming)
        {
            // 正向计时：专项训练和综合实训
            ElapsedTimeSeconds++;

            // 每30秒记录一次时间状态（避免日志过多）
            if (ElapsedTimeSeconds % 30 == 0)
            {
                _logger.LogDebug("正向计时更新 - 已用时间: {ElapsedTime}秒, 格式化时间: {FormattedTime}",
                    ElapsedTimeSeconds, FormattedElapsedTime);
            }
        }
        else
        {
            // 倒计时：正式考试和模拟考试
            RemainingTimeSeconds--;

            // 每30秒记录一次时间状态（避免日志过多）
            if (RemainingTimeSeconds % 30 == 0)
            {
                _logger.LogDebug("倒计时更新 - 剩余时间: {RemainingTime}秒, 格式化时间: {FormattedTime}",
                    RemainingTimeSeconds, FormattedRemainingTime);
            }

            // 检查自动提交条件（仅倒计时模式）
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
        try
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(Math.Max(0, remainingSeconds));
            string newFormattedTime = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";

            // 确保在UI线程上更新属性
            if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                FormattedRemainingTime = newFormattedTime;
                IsTimeUrgent = remainingSeconds <= TimeWarningThreshold && remainingSeconds > 0;
            }
            else
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    FormattedRemainingTime = newFormattedTime;
                    IsTimeUrgent = remainingSeconds <= TimeWarningThreshold && remainingSeconds > 0;
                });
            }

            _logger.LogDebug("更新时间显示 - 剩余秒数: {RemainingSeconds}, 格式化时间: {FormattedTime}, 时间紧急: {IsUrgent}",
                remainingSeconds, newFormattedTime, IsTimeUrgent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新时间显示时发生异常");
        }
    }

    /// <summary>
    /// 更新已用时间显示
    /// </summary>
    private void UpdateElapsedTimeDisplay(int elapsedSeconds)
    {
        try
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(Math.Max(0, elapsedSeconds));
            string newFormattedTime = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";

            // 确保在UI线程上更新属性
            if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                FormattedElapsedTime = newFormattedTime;
            }
            else
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    FormattedElapsedTime = newFormattedTime;
                });
            }

            _logger.LogDebug("更新已用时间显示 - 已用秒数: {ElapsedSeconds}, 格式化时间: {FormattedTime}",
                elapsedSeconds, newFormattedTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新已用时间显示时发生异常");
        }
    }

    /// <summary>
    /// 查看题目
    /// </summary>
    private void ViewQuestions()
    {
        ViewQuestionsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 查看答案解析
    /// </summary>
    private void ViewAnswerAnalysis()
    {
        ViewAnswerAnalysisRequested?.Invoke(this, EventArgs.Empty);
    }



    /// <summary>
    /// 执行提交操作
    /// </summary>
    private async Task PerformSubmitAsync()
    {
        try
        {
            _logger.LogInformation("开始执行考试提交，考试ID: {ExamId}", ExamId);

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
            await HandleSubmitFailureAsync(ex);
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

            // 直接关闭窗口，无需显示结果对话框
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
    private async Task HandleSubmitFailureAsync(Exception exception)
    {
        try
        {
            _logger.LogError(exception, "考试提交失败");

            CurrentSubmitStatus = SubmitStatus.Failed;
            IsSubmitting = false;
            SubmitMessage = $"提交失败：{exception.Message}";

            // 提交失败也直接关闭窗口，不显示错误对话框
            await CloseWindowAfterSubmitAsync();
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
            await HandleSubmitFailureAsync(ex);
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

            // 移除主窗口恢复逻辑，让各个训练/考试ViewModel控制主窗口显示时机
            // await RestoreMainWindowAsync(); // 已移除，避免过早显示主窗口
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
    /// 恢复主窗口（已废弃，不再在提交流程中使用）
    /// 注意：此方法已从CloseWindowAfterSubmitAsync中移除，
    /// 主窗口显示时机现在由各个训练/考试ViewModel控制，
    /// 确保结果窗口显示完成后再显示主窗口
    /// </summary>
    [Obsolete("此方法不再在提交流程中使用，主窗口显示由各个ViewModel控制")]
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
        CurrentExamStatus = ExamStatus.Preparing;
        TotalDurationSeconds = durationSeconds;

        // 触发答案解析按钮可见性更新
        this.RaisePropertyChanged(nameof(ShowAnswerAnalysisButton));

        // 根据考试类型设置计时模式
        UseForwardTiming = examType == ExamType.SpecializedTraining || examType == ExamType.ComprehensiveTraining;

        if (UseForwardTiming)
        {
            // 正向计时：从0开始
            ElapsedTimeSeconds = 0;
            _logger.LogInformation("设置考试信息（正向计时） - 类型: {ExamType}, ID: {ExamId}, 名称: {ExamName}, 题目数: {TotalQuestions}, 时长: {Duration}秒",
                examType, examId, examName, totalQuestions, durationSeconds);
        }
        else
        {
            // 倒计时：设置剩余时间，响应式监听会自动触发UpdateTimeDisplay
            RemainingTimeSeconds = durationSeconds;
            _logger.LogInformation("设置考试信息（倒计时） - 类型: {ExamType}, ID: {ExamId}, 名称: {ExamName}, 题目数: {TotalQuestions}, 时长: {Duration}秒, 格式化时间: {FormattedTime}",
                examType, examId, examName, totalQuestions, durationSeconds, FormattedRemainingTime);
        }
    }

    /// <summary>
    /// 开始考试（设置状态为进行中并开始倒计时）
    /// </summary>
    public void StartExam()
    {
        if (CurrentExamStatus == ExamStatus.Preparing)
        {
            CurrentExamStatus = ExamStatus.InProgress;
            ExamStartTime = DateTime.Now; // 记录开始时间

            // 启动倒计时器
            _countdownTimer?.Dispose();
            _countdownTimer = new Timer(CountdownTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            _logger.LogInformation("考试开始 - 类型: {ExamType}, ID: {ExamId}, 名称: {ExamName}, 开始时间: {StartTime}, 剩余时间: {RemainingTime}秒, 格式化时间: {FormattedTime}",
                CurrentExamType, ExamId, ExamName, ExamStartTime, RemainingTimeSeconds, FormattedRemainingTime);
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

    /// <summary>
    /// 打开考试目录
    /// </summary>
    private async Task OpenDirectoryAsync()
    {
        try
        {
            _logger.LogInformation("开始打开考试目录 - 考试类型: {ExamType}, 考试ID: {ExamId}", CurrentExamType, ExamId);

            if (_benchSuiteDirectoryService == null)
            {
                _logger.LogWarning("BenchSuiteDirectoryService未注入，无法打开目录");
                return;
            }

            // 获取考试根目录路径
            string examRootDirectory = GetExamRootDirectory();

            if (string.IsNullOrEmpty(examRootDirectory))
            {
                _logger.LogWarning("无法获取考试目录路径");
                return;
            }

            // 确保目录存在
            if (!Directory.Exists(examRootDirectory))
            {
                _logger.LogInformation("目录不存在，正在创建: {Directory}", examRootDirectory);
                Directory.CreateDirectory(examRootDirectory);
            }

            // 使用系统默认文件管理器打开目录
            await OpenDirectoryWithSystemExplorerAsync(examRootDirectory);

            _logger.LogInformation("成功打开考试目录: {Directory}", examRootDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开考试目录时发生异常");
        }
    }

    /// <summary>
    /// 获取考试根目录路径
    /// </summary>
    private string GetExamRootDirectory()
    {
        if (_benchSuiteDirectoryService == null)
        {
            return string.Empty;
        }

        try
        {
            // 获取基础路径
            string basePath = _benchSuiteDirectoryService.GetBasePath();

            // 获取考试类型文件夹名称
            string examTypeFolder = GetExamTypeFolder(CurrentExamType);

            // 组合完整路径
            return Path.Combine(basePath, examTypeFolder, ExamId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试根目录路径时发生异常");
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取考试类型对应的文件夹名称
    /// </summary>
    private static string GetExamTypeFolder(ExamType examType)
    {
        return examType switch
        {
            ExamType.MockExam => "MockExams",
            ExamType.FormalExam => "OnlineExams",
            ExamType.ComprehensiveTraining => "ComprehensiveTraining",
            ExamType.SpecializedTraining => "SpecializedTraining",
            ExamType.Practice => "Practice",
            ExamType.SpecialPractice => "SpecialPractice",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// 使用系统默认文件管理器打开目录
    /// </summary>
    private async Task OpenDirectoryWithSystemExplorerAsync(string directoryPath)
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = directoryPath,
                UseShellExecute = true,
                Verb = "open"
            };

            using Process? process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用系统文件管理器打开目录失败: {Directory}", directoryPath);

            // 尝试备用方法
            await TryAlternativeOpenMethodAsync(directoryPath);
        }
    }

    /// <summary>
    /// 尝试备用的目录打开方法
    /// </summary>
    private async Task TryAlternativeOpenMethodAsync(string directoryPath)
    {
        try
        {
            _logger.LogInformation("尝试备用方法打开目录: {Directory}", directoryPath);

            ProcessStartInfo startInfo = new()
            {
                FileName = "explorer.exe",
                Arguments = $"\"{directoryPath}\"",
                UseShellExecute = false
            };

            using Process? process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                _logger.LogInformation("备用方法成功打开目录");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "备用方法也无法打开目录: {Directory}", directoryPath);
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
