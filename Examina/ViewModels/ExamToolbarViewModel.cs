using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Examina.Models;
using Examina.Services;
using Microsoft.Extensions.Logging;

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
    /// 查看题目命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ViewQuestionsCommand { get; }

    /// <summary>
    /// 提交考试命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SubmitExamCommand { get; }

    /// <summary>
    /// 考试自动提交事件
    /// </summary>
    public event EventHandler? ExamAutoSubmitted;

    /// <summary>
    /// 考试手动提交事件
    /// </summary>
    public event EventHandler? ExamManualSubmitted;

    /// <summary>
    /// 查看题目请求事件
    /// </summary>
    public event EventHandler? ViewQuestionsRequested;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamToolbarViewModel(IAuthenticationService authenticationService, ILogger<ExamToolbarViewModel>? logger)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ExamToolbarViewModel>.Instance;

        // 初始化命令
        ViewQuestionsCommand = ReactiveCommand.Create(ViewQuestions);
        SubmitExamCommand = ReactiveCommand.CreateFromTask(SubmitExamAsync, this.WhenAnyValue(x => x.CanSubmitExam, x => x.IsSubmitting, (canSubmit, isSubmitting) => canSubmit && !isSubmitting));

        // 监听剩余时间变化，更新格式化时间和紧急状态
        this.WhenAnyValue(x => x.RemainingTimeSeconds)
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

        if (RemainingTimeSeconds <= 0)
        {
            // 时间到，自动提交
            CurrentExamStatus = ExamStatus.Ended;
            StopCountdown();
            
            _logger.LogWarning("考试时间到，触发自动提交");
            ExamAutoSubmitted?.Invoke(this, EventArgs.Empty);
        }
        else if (RemainingTimeSeconds <= TimeWarningThreshold && CurrentExamStatus != ExamStatus.AboutToEnd)
        {
            // 进入即将结束状态
            CurrentExamStatus = ExamStatus.AboutToEnd;
            _logger.LogInformation("考试进入即将结束状态，剩余时间: {RemainingTime}秒", RemainingTimeSeconds);
        }
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
    /// 提交考试
    /// </summary>
    private async Task SubmitExamAsync()
    {
        try
        {
            IsSubmitting = true;
            _logger.LogInformation("开始手动提交考试，考试ID: {ExamId}", ExamId);

            // 停止倒计时
            StopCountdown();
            CurrentExamStatus = ExamStatus.Submitted;

            // 触发提交事件
            ExamManualSubmitted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交考试失败");
            IsSubmitting = false;
            // 恢复倒计时
            if (RemainingTimeSeconds > 0)
            {
                StartCountdown(RemainingTimeSeconds);
            }
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
            _disposed = true;
            _logger.LogInformation("ExamToolbarViewModel资源已释放");
        }
    }
}
