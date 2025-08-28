using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BenchSuite.Models;
using Examina.Models;
using Examina.Models.Exam;
using Examina.Services;
using Examina.Views;
using Prism.Commands;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 上机统考页面视图模型
/// </summary>
public class ExamViewModel : ViewModelBase
{
    private readonly IAuthenticationService? _authenticationService;
    private readonly IStudentExamService? _studentExamService;
    private readonly IExamAttemptService? _examAttemptService;
    private readonly EnhancedExamToolbarService? _enhancedExamToolbarService;

    #region 属性

    /// <summary>
    /// 页面标题
    /// </summary>
    [Reactive]
    public string PageTitle { get; set; } = "上机统考";

    /// <summary>
    /// 考试状态
    /// </summary>
    [Reactive]
    public string ExamStatusMessage { get; set; } = "暂无正在进行的考试";

    /// <summary>
    /// 是否有正在进行的考试
    /// </summary>
    [Reactive]
    public bool HasActiveExam { get; set; } = false;

    /// <summary>
    /// 考试名称
    /// </summary>
    [Reactive]
    public string ExamName { get; set; } = string.Empty;

    /// <summary>
    /// 考试开始时间
    /// </summary>
    [Reactive]
    public DateTime? ExamStartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    [Reactive]
    public DateTime? ExamEndTime { get; set; }

    /// <summary>
    /// 剩余时间
    /// </summary>
    [Reactive]
    public TimeSpan? RemainingTime { get; set; }

    /// <summary>
    /// 用户是否拥有完整功能权限
    /// </summary>
    [Reactive]
    public bool HasFullAccess { get; set; }

    /// <summary>
    /// 开始考试按钮文本
    /// </summary>
    public string StartExamButtonText => HasFullAccess ? "开始考试" : "解锁";

    /// <summary>
    /// 错误消息
    /// </summary>
    [Reactive]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 可用考试列表
    /// </summary>
    [Reactive]
    public ObservableCollection<StudentExamDto> AvailableExams { get; set; } = [];

    /// <summary>
    /// 选中的考试
    /// </summary>
    [Reactive]
    public StudentExamDto? SelectedExam { get; set; }

    /// <summary>
    /// 考试次数限制信息
    /// </summary>
    [Reactive]
    public ExamAttemptLimitDto? ExamAttemptLimit { get; set; }

    /// <summary>
    /// 考试尝试历史
    /// </summary>
    [Reactive]
    public ObservableCollection<ExamAttemptDto> ExamAttemptHistory { get; set; } = [];

    /// <summary>
    /// 当前考试尝试
    /// </summary>
    [Reactive]
    public ExamAttemptDto? CurrentExamAttempt { get; set; }

    /// <summary>
    /// 是否可以重考
    /// </summary>
    [Reactive]
    public bool CanRetake { get; set; }

    /// <summary>
    /// 是否可以重做练习
    /// </summary>
    [Reactive]
    public bool CanPractice { get; set; }

    /// <summary>
    /// 重考按钮文本
    /// </summary>
    public string RetakeButtonText => ExamAttemptLimit?.RemainingRetakeCount > 0
        ? $"重考 (剩余{ExamAttemptLimit.RemainingRetakeCount}次)"
        : "重考";

    /// <summary>
    /// 考试状态描述
    /// </summary>
    public string ExamStatusDescription => ExamAttemptLimit?.StatusDisplay ?? "未知状态";

    /// <summary>
    /// 考试次数统计
    /// </summary>
    public string AttemptCountDescription => ExamAttemptLimit?.AttemptCountDisplay ?? "";

    /// <summary>
    /// 当前考试工具栏窗口
    /// </summary>
    private ExamToolbarWindow? _currentExamToolbar;

    /// <summary>
    /// 当前考试工具栏ViewModel
    /// </summary>
    private ExamToolbarViewModel? _currentToolbarViewModel;

    #endregion

    #region 命令

    /// <summary>
    /// 开始考试命令
    /// </summary>
    public ICommand StartExamCommand { get; }

    /// <summary>
    /// 继续考试命令
    /// </summary>
    public ICommand ContinueExamCommand { get; }

    /// <summary>
    /// 刷新考试状态命令
    /// </summary>
    public ICommand RefreshExamStatusCommand { get; }

    /// <summary>
    /// 重考命令
    /// </summary>
    public ICommand RetakeExamCommand { get; }

    /// <summary>
    /// 重做练习命令
    /// </summary>
    public ICommand PracticeExamCommand { get; }

    /// <summary>
    /// 选择考试命令
    /// </summary>
    public ICommand SelectExamCommand { get; }

    /// <summary>
    /// 查看考试历史命令
    /// </summary>
    public ICommand ViewExamHistoryCommand { get; }

    #endregion

    #region 构造函数

    public ExamViewModel(
        IAuthenticationService? authenticationService = null,
        IStudentExamService? studentExamService = null,
        IExamAttemptService? examAttemptService = null)
    {
        _authenticationService = authenticationService;
        _studentExamService = studentExamService;
        _examAttemptService = examAttemptService;

        // 尝试获取EnhancedExamToolbarService（可选依赖）
        try
        {
            _enhancedExamToolbarService = AppServiceManager.GetService<EnhancedExamToolbarService>();
        }
        catch
        {
            _enhancedExamToolbarService = null;
        }

        StartExamCommand = new DelegateCommand(StartExam, CanStartExam);
        ContinueExamCommand = new DelegateCommand(ContinueExam, CanContinueExam);
        RefreshExamStatusCommand = new DelegateCommand(RefreshExamStatus);
        RetakeExamCommand = new DelegateCommand(RetakeExam, CanRetakeExam);
        PracticeExamCommand = new DelegateCommand(PracticeExam, CanPracticeExam);
        SelectExamCommand = new DelegateCommand<StudentExamDto>(SelectExam);
        ViewExamHistoryCommand = new DelegateCommand(ViewExamHistory, CanViewExamHistory);

        // 初始化用户权限状态
        UpdateUserPermissions();

        // 监听用户信息更新事件
        if (_authenticationService != null)
        {
            _authenticationService.UserInfoUpdated += OnUserInfoUpdated;
        }

        // 监听选中考试变化
        _ = this.WhenAnyValue(x => x.SelectedExam)
            .Subscribe(async exam => await OnSelectedExamChanged(exam));

        // 监听考试状态变化，实时同步到工具栏
        _ = this.WhenAnyValue(x => x.CurrentExamAttempt)
            .Subscribe(SyncToolbarStatus);

        LoadExamStatus();
    }

    #endregion

    #region 方法

    /// <summary>
    /// 加载考试状态
    /// </summary>
    private async void LoadExamStatus()
    {
        try
        {
            if (_studentExamService == null || _examAttemptService == null)
            {
                ExamStatusMessage = "服务未初始化";
                return;
            }

            // 加载可用考试列表
            await LoadAvailableExams();

            // 检查是否有进行中的考试
            UserInfo? currentUser = _authenticationService?.CurrentUser;
            if (currentUser != null && int.TryParse(currentUser.Id, out int studentId))
            {
                CurrentExamAttempt = await _examAttemptService.GetCurrentExamAttemptAsync(studentId);
                HasActiveExam = CurrentExamAttempt != null;

                if (HasActiveExam && CurrentExamAttempt != null)
                {
                    string statusText = CurrentExamAttempt.Status switch
                    {
                        ExamAttemptStatus.InProgress => "考试进行中",
                        ExamAttemptStatus.Completed => "考试已完成",
                        ExamAttemptStatus.Abandoned => "考试已放弃",
                        ExamAttemptStatus.TimedOut => "考试已超时",
                        _ => "考试状态未知"
                    };
                    ExamStatusMessage = $"{statusText} - {CurrentExamAttempt.AttemptTypeDisplay}";

                    // 加载当前考试的详情
                    SelectedExam = await _studentExamService.GetExamDetailsAsync(CurrentExamAttempt.ExamId);
                }
                else
                {
                    ExamStatusMessage = "暂无正在进行的考试";
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载考试状态失败: {ex.Message}";
            ExamStatusMessage = "加载失败";
        }
    }

    /// <summary>
    /// 加载可用考试列表
    /// </summary>
    private async Task LoadAvailableExams()
    {
        try
        {
            if (_studentExamService == null)
            {
                return;
            }

            List<StudentExamDto> exams = await _studentExamService.GetAvailableExamsAsync();
            AvailableExams.Clear();
            foreach (StudentExamDto exam in exams)
            {
                AvailableExams.Add(exam);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载考试列表失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 开始考试
    /// </summary>
    private async void StartExam()
    {
        if (!HasFullAccess)
        {
            // 用户没有完整权限，显示解锁提示
            ErrorMessage = "您需要解锁权限才能开始考试。请加入学校组织或联系管理员进行解锁。";
            return;
        }

        if (SelectedExam == null)
        {
            ErrorMessage = "请先选择要参加的考试";
            return;
        }

        await StartExamAttempt(ExamAttemptType.FirstAttempt);
    }

    /// <summary>
    /// 是否可以开始考试
    /// </summary>
    private bool CanStartExam()
    {
        return !HasActiveExam;
    }

    /// <summary>
    /// 继续考试
    /// </summary>
    private void ContinueExam()
    {
        // TODO: 实现继续考试逻辑
    }

    /// <summary>
    /// 是否可以继续考试
    /// </summary>
    private bool CanContinueExam()
    {
        return HasActiveExam;
    }

    /// <summary>
    /// 刷新考试状态
    /// </summary>
    private void RefreshExamStatus()
    {
        LoadExamStatus();
    }

    /// <summary>
    /// 更新用户权限状态
    /// </summary>
    private void UpdateUserPermissions()
    {
        UserInfo? currentUser = _authenticationService?.CurrentUser;
        HasFullAccess = currentUser?.HasFullAccess ?? false;
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    private void OnUserInfoUpdated(object? sender, UserInfo? userInfo)
    {
        UpdateUserPermissions();
    }

    /// <summary>
    /// 开始考试尝试
    /// </summary>
    private async Task StartExamAttempt(ExamAttemptType attemptType)
    {
        try
        {
            if (SelectedExam == null || _examAttemptService == null)
            {
                return;
            }

            UserInfo? currentUser = _authenticationService?.CurrentUser;
            if (currentUser == null || !int.TryParse(currentUser.Id, out int studentId))
            {
                ErrorMessage = "用户未登录或用户ID无效";
                return;
            }

            // 验证权限
            (bool isValid, string? errorMessage) = await _examAttemptService.ValidateExamAttemptPermissionAsync(
                SelectedExam.Id, studentId, attemptType);

            if (!isValid)
            {
                ErrorMessage = errorMessage;
                return;
            }

            // 开始考试尝试
            ExamAttemptDto? attempt = await _examAttemptService.StartExamAttemptAsync(
                SelectedExam.Id, studentId, attemptType);

            if (attempt != null)
            {
                CurrentExamAttempt = attempt;
                HasActiveExam = true;
                ExamStatusMessage = $"考试进行中 - {attempt.AttemptTypeDisplay}";

                // 启动考试工具栏
                StartExamToolbar(SelectedExam, attemptType);
            }
            else
            {
                ErrorMessage = "开始考试失败";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"开始考试时发生错误: {ex.Message}";
        }
    }

    /// <summary>
    /// 重考
    /// </summary>
    private async void RetakeExam()
    {
        await StartExamAttempt(ExamAttemptType.Retake);
    }

    /// <summary>
    /// 是否可以重考
    /// </summary>
    private bool CanRetakeExam()
    {
        return CanRetake && !HasActiveExam;
    }

    /// <summary>
    /// 重做练习
    /// </summary>
    private async void PracticeExam()
    {
        await StartExamAttempt(ExamAttemptType.Practice);
    }

    /// <summary>
    /// 是否可以重做练习
    /// </summary>
    private bool CanPracticeExam()
    {
        return CanPractice && !HasActiveExam;
    }

    /// <summary>
    /// 选择考试
    /// </summary>
    private async void SelectExam(StudentExamDto? exam)
    {
        SelectedExam = exam;
        await OnSelectedExamChanged(exam);
    }

    /// <summary>
    /// 选中考试变化处理
    /// </summary>
    private async Task OnSelectedExamChanged(StudentExamDto? exam)
    {
        try
        {
            if (exam == null || _examAttemptService == null)
            {
                ExamAttemptLimit = null;
                ExamAttemptHistory.Clear();
                CanRetake = false;
                CanPractice = false;
                return;
            }

            UserInfo? currentUser = _authenticationService?.CurrentUser;
            if (currentUser == null || !int.TryParse(currentUser.Id, out int studentId))
            {
                return;
            }

            // 检查考试次数限制
            ExamAttemptLimit = await _examAttemptService.CheckExamAttemptLimitAsync(exam.Id, studentId);

            // 更新按钮状态
            CanRetake = ExamAttemptLimit.CanRetake;
            CanPractice = ExamAttemptLimit.CanPractice;

            // 加载考试历史
            List<ExamAttemptDto> history = await _examAttemptService.GetExamAttemptHistoryAsync(exam.Id, studentId);
            ExamAttemptHistory.Clear();
            foreach (ExamAttemptDto attempt in history)
            {
                ExamAttemptHistory.Add(attempt);
            }

            // 触发属性更新通知
            this.RaisePropertyChanged(nameof(RetakeButtonText));
            this.RaisePropertyChanged(nameof(ExamStatusDescription));
            this.RaisePropertyChanged(nameof(AttemptCountDescription));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载考试信息失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 查看考试历史
    /// </summary>
    private void ViewExamHistory()
    {
        // TODO: 打开考试历史窗口
        System.Diagnostics.Debug.WriteLine("查看考试历史");
    }

    /// <summary>
    /// 是否可以查看考试历史
    /// </summary>
    private bool CanViewExamHistory()
    {
        return SelectedExam != null && ExamAttemptHistory.Count > 0;
    }

    /// <summary>
    /// 启动考试工具栏
    /// </summary>
    private void StartExamToolbar(StudentExamDto exam, ExamAttemptType attemptType)
    {
        try
        {
            // 获取主窗口
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 创建工具栏ViewModel
                ExamToolbarViewModel toolbarViewModel = new(
                    _authenticationService ?? throw new InvalidOperationException("认证服务未初始化"),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ExamToolbarViewModel>.Instance
                );

                // 设置考试信息
                ExamType examType = attemptType switch
                {
                    ExamAttemptType.FirstAttempt => ExamType.FormalExam,
                    ExamAttemptType.Retake => ExamType.FormalExam,
                    ExamAttemptType.Practice => ExamType.Practice,
                    _ => ExamType.FormalExam
                };

                // 计算总题目数
                int totalQuestions = exam.Subjects.Sum(s => s.QuestionCount) + exam.Modules.Sum(m => m.Questions.Count);

                // 如果QuestionCount为0，尝试从Questions集合计算
                if (totalQuestions == 0)
                {
                    totalQuestions = exam.Subjects.Sum(s => s.Questions.Count) + exam.Modules.Sum(m => m.Questions.Count);
                }

                // 设置考试信息
                toolbarViewModel.SetExamInfo(
                    examType,
                    exam.Id,
                    exam.Name,
                    totalQuestions,
                    exam.DurationMinutes * 60 // 转换为秒
                );

                // 设置学生信息
                UserInfo? currentUser = _authenticationService?.CurrentUser;
                if (currentUser != null)
                {
                    toolbarViewModel.StudentName = currentUser.Username;
                }

                // 创建考试工具栏窗口
                ExamToolbarWindow examToolbar = new();
                examToolbar.SetViewModel(toolbarViewModel);

                // 保存工具栏实例以便后续状态同步
                _currentExamToolbar = examToolbar;
                _currentToolbarViewModel = toolbarViewModel;

                // 订阅工具栏事件
                examToolbar.ExamAutoSubmitted += OnExamAutoSubmitted;
                examToolbar.ExamManualSubmitted += OnExamManualSubmitted;

                // 显示工具栏窗口
                examToolbar.Show();

                // 开始考试（启动倒计时器并设置状态为进行中）
                toolbarViewModel.StartExam();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExamViewModel: 无法获取主窗口，启动考试界面失败");
                ErrorMessage = "无法启动考试界面，请重试";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamViewModel: 启动考试工具栏异常: {ex.Message}");
            ErrorMessage = $"启动考试界面失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 考试自动提交事件处理
    /// </summary>
    private async void OnExamAutoSubmitted(object? sender, EventArgs e)
    {
        try
        {
            if (CurrentExamAttempt != null && _examAttemptService != null)
            {
                double? score = null;
                double? maxScore = null;
                string notes = "自动提交";

                // 尝试获取BenchSuite评分结果
                if (_enhancedExamToolbarService != null && CurrentExamAttempt.AttemptType != ExamAttemptType.Practice)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("ExamViewModel: 开始获取BenchSuite评分结果（自动提交）");
                        Dictionary<ModuleType, ScoringResult>? scoringResults = await _enhancedExamToolbarService.SubmitFormalExamWithResultAsync(CurrentExamAttempt.ExamId);

                        if (scoringResults != null && scoringResults.Count > 0)
                        {
                            score = scoringResults.Values.Sum(r => r.AchievedScore);
                            maxScore = scoringResults.Values.Sum(r => r.TotalScore);
                            bool isSuccess = scoringResults.Values.Any(r => r.IsSuccess);
                            notes = isSuccess ? "BenchSuite自动评分完成（自动提交）" : "BenchSuite评分失败（自动提交）";
                            System.Diagnostics.Debug.WriteLine($"ExamViewModel: BenchSuite评分结果 - Score: {score}, MaxScore: {maxScore}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ExamViewModel: BenchSuite评分结果为空");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ExamViewModel: 获取BenchSuite评分结果异常: {ex.Message}");
                        notes = $"评分异常: {ex.Message}（自动提交）";
                    }
                }

                // 更新考试尝试状态为已完成，包含分数信息
                bool success = await _examAttemptService.CompleteExamAttemptAsync(
                    CurrentExamAttempt.Id,
                    score,
                    maxScore,
                    null, // 用时
                    notes
                );

                if (success)
                {
                    CurrentExamAttempt.Status = ExamAttemptStatus.Completed;
                    // 更新CurrentExamAttempt中的分数信息
                    CurrentExamAttempt.Score = score;
                    CurrentExamAttempt.MaxScore = maxScore;
                    HasActiveExam = false;
                    ExamStatusMessage = "考试已完成 - 自动提交";

                    // 显示考试结果窗口
                    await ShowExamResultAsync(CurrentExamAttempt, true);
                }
                else
                {
                    // 即使提交失败也显示结果窗口
                    await ShowExamResultAsync(CurrentExamAttempt, false);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamViewModel: 处理考试自动提交异常: {ex.Message}");
        }
        finally
        {
            // 清理工具栏引用
            CleanupToolbar();
        }
    }

    /// <summary>
    /// 考试手动提交事件处理
    /// </summary>
    private async void OnExamManualSubmitted(object? sender, EventArgs e)
    {
        try
        {
            if (CurrentExamAttempt != null && _examAttemptService != null)
            {
                double? score = null;
                double? maxScore = null;
                string notes = "手动提交";

                // 尝试获取BenchSuite评分结果
                if (_enhancedExamToolbarService != null && CurrentExamAttempt.AttemptType != ExamAttemptType.Practice)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("ExamViewModel: 开始获取BenchSuite评分结果（手动提交）");
                        Dictionary<ModuleType, ScoringResult>? scoringResults = await _enhancedExamToolbarService.SubmitFormalExamWithResultAsync(CurrentExamAttempt.ExamId);

                        if (scoringResults != null && scoringResults.Count > 0)
                        {
                            score = scoringResults.Values.Sum(r => r.AchievedScore);
                            maxScore = scoringResults.Values.Sum(r => r.TotalScore);
                            bool isSuccess = scoringResults.Values.Any(r => r.IsSuccess);
                            notes = isSuccess ? "BenchSuite自动评分完成（手动提交）" : "BenchSuite评分失败（手动提交）";
                            System.Diagnostics.Debug.WriteLine($"ExamViewModel: BenchSuite评分结果 - Score: {score}, MaxScore: {maxScore}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ExamViewModel: BenchSuite评分结果为空");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ExamViewModel: 获取BenchSuite评分结果异常: {ex.Message}");
                        notes = $"评分异常: {ex.Message}（手动提交）";
                    }
                }

                // 更新考试尝试状态为已完成，包含分数信息
                bool success = await _examAttemptService.CompleteExamAttemptAsync(
                    CurrentExamAttempt.Id,
                    score,
                    maxScore,
                    null, // 用时
                    notes
                );

                if (success)
                {
                    CurrentExamAttempt.Status = ExamAttemptStatus.Completed;
                    // 更新CurrentExamAttempt中的分数信息
                    CurrentExamAttempt.Score = score;
                    CurrentExamAttempt.MaxScore = maxScore;
                    HasActiveExam = false;
                    ExamStatusMessage = "考试已完成 - 手动提交";

                    // 显示考试结果窗口
                    await ShowExamResultAsync(CurrentExamAttempt, true);
                }
                else
                {
                    // 即使提交失败也显示结果窗口
                    await ShowExamResultAsync(CurrentExamAttempt, false);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamViewModel: 处理考试手动提交异常: {ex.Message}");
        }
        finally
        {
            // 清理工具栏引用
            CleanupToolbar();
        }
    }

    /// <summary>
    /// 清理工具栏引用
    /// </summary>
    private void CleanupToolbar()
    {
        if (_currentExamToolbar != null)
        {
            _currentExamToolbar.ExamAutoSubmitted -= OnExamAutoSubmitted;
            _currentExamToolbar.ExamManualSubmitted -= OnExamManualSubmitted;
            _currentExamToolbar = null;
        }
        _currentToolbarViewModel = null;
    }

    /// <summary>
    /// 同步工具栏状态
    /// </summary>
    private void SyncToolbarStatus(ExamAttemptDto? attempt)
    {
        if (_currentToolbarViewModel == null || attempt == null)
        {
            return;
        }

        try
        {
            // 将ExamAttemptStatus映射到ExamStatus
            ExamStatus toolbarStatus = attempt.Status switch
            {
                ExamAttemptStatus.InProgress => ExamStatus.InProgress,
                ExamAttemptStatus.Completed => ExamStatus.Submitted,
                ExamAttemptStatus.Abandoned => ExamStatus.Ended,
                ExamAttemptStatus.TimedOut => ExamStatus.Ended,
                _ => ExamStatus.Preparing
            };

            // 更新工具栏状态
            if (_currentToolbarViewModel.CurrentExamStatus != toolbarStatus)
            {
                _currentToolbarViewModel.CurrentExamStatus = toolbarStatus;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamViewModel: 同步工具栏状态异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示考试结果窗口
    /// </summary>
    private async Task ShowExamResultAsync(ExamAttemptDto examAttempt, bool isSuccessful)
    {
        try
        {
            // 获取考试信息
            string examName = "考试";
            ExamType examType = ExamType.FormalExam;
            int? durationMinutes = null;

            // 根据考试尝试类型确定ExamType
            // 正式考试和重考都应该显示分数区域，练习模式不显示
            examType = examAttempt.AttemptType switch
            {
                ExamAttemptType.FirstAttempt => ExamType.FormalExam,
                ExamAttemptType.Retake => ExamType.FormalExam,
                ExamAttemptType.Practice => ExamType.Practice,
                _ => ExamType.FormalExam
            };

            System.Diagnostics.Debug.WriteLine($"ExamViewModel: 考试尝试类型: {examAttempt.AttemptType} -> ExamType: {examType}");

            if (_studentExamService != null)
            {
                try
                {
                    StudentExamDto? exam = await _studentExamService.GetExamDetailsAsync(examAttempt.ExamId);
                    if (exam != null)
                    {
                        examName = exam.Name;
                        // 根据考试类型设置ExamType
                        examType = exam.ExamType switch
                        {
                            "MockExam" => ExamType.MockExam,
                            "ComprehensiveTraining" => ExamType.ComprehensiveTraining,
                            "SpecializedTraining" => ExamType.SpecializedTraining,
                            "FormalExam" => ExamType.FormalExam,
                            "Practice" => ExamType.Practice,
                            _ => ExamType.FormalExam
                        };

                        System.Diagnostics.Debug.WriteLine($"ExamViewModel: 考试类型映射 - 原始: '{exam.ExamType}' -> 映射后: {examType}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ExamViewModel: 获取考试信息失败: {ex.Message}");
                }
            }

            // 计算考试用时
            if (examAttempt.CompletedAt.HasValue)
            {
                TimeSpan duration = examAttempt.CompletedAt.Value - examAttempt.StartedAt;
                durationMinutes = (int)duration.TotalMinutes;
            }

            System.Diagnostics.Debug.WriteLine($"ExamViewModel: 准备显示全屏考试结果窗口 - {examName}");
            System.Diagnostics.Debug.WriteLine($"ExamViewModel: 分数信息 - Score: {examAttempt.Score}, MaxScore: {examAttempt.MaxScore}");
            System.Diagnostics.Debug.WriteLine($"ExamViewModel: 考试尝试信息 - ID: {examAttempt.Id}, ExamId: {examAttempt.ExamId}, AttemptType: {examAttempt.AttemptType}");

            // 显示全屏考试结果窗口
            _ = await Views.Dialogs.FullScreenExamResultWindow.ShowFullScreenExamResultAsync(
                examName,
                examType,
                isSuccessful,
                examAttempt.StartedAt,
                examAttempt.CompletedAt,
                durationMinutes,
                examAttempt.Score,
                examAttempt.MaxScore,
                isSuccessful ? "" : "考试提交失败",
                GetExamModeDisplayText(examAttempt.AttemptType),
                true, // showContinue
                false // showClose - 只显示确认按钮
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamViewModel: 显示考试结果窗口异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取考试模式显示文本
    /// </summary>
    private string GetExamModeDisplayText(ExamAttemptType attemptType)
    {
        return attemptType switch
        {
            ExamAttemptType.FirstAttempt => "首次考试完成",
            ExamAttemptType.Retake => "重考完成",
            ExamAttemptType.Practice => "练习模式完成",
            _ => "考试完成"
        };
    }

    #endregion
}
