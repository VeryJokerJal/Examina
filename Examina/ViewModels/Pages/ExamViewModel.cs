using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Prism.Commands;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Examina.Models;
using Examina.Models.Exam;
using Examina.Services;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 上机统考页面视图模型
/// </summary>
public class ExamViewModel : ViewModelBase
{
    private readonly IAuthenticationService? _authenticationService;
    private readonly IStudentExamService? _studentExamService;
    private readonly IExamAttemptService? _examAttemptService;

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
    public string ExamStatus { get; set; } = "暂无正在进行的考试";

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
        this.WhenAnyValue(x => x.SelectedExam)
            .Subscribe(async exam => await OnSelectedExamChanged(exam));

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
                ExamStatus = "服务未初始化";
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
                    ExamStatus = $"正在进行考试: {CurrentExamAttempt.AttemptTypeDisplay}";

                    // 加载当前考试的详情
                    SelectedExam = await _studentExamService.GetExamDetailsAsync(CurrentExamAttempt.ExamId);
                }
                else
                {
                    ExamStatus = "暂无正在进行的考试";
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载考试状态失败: {ex.Message}";
            ExamStatus = "加载失败";
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
                return;

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
            System.Diagnostics.Debug.WriteLine("用户尝试开始考试但没有完整权限");
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

        System.Diagnostics.Debug.WriteLine($"ExamViewModel: 用户权限状态更新 - HasFullAccess: {HasFullAccess}");
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
                return;

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
                ExamStatus = $"正在进行考试: {attempt.AttemptTypeDisplay}";

                // TODO: 打开考试窗口或导航到考试页面
                System.Diagnostics.Debug.WriteLine($"开始考试尝试: {attemptType}, ID: {attempt.Id}");
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
                return;

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

    #endregion
}
