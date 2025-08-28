using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Examina.Models;
using BenchSuite.Models;
using Examina.Models.Exam;
using Examina.Services;
using Examina.Views;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 考试模式枚举
/// </summary>
public enum ExamMode
{
    /// <summary>
    /// 正式考试模式
    /// </summary>
    Normal,

    /// <summary>
    /// 重考模式（记录分数和排名）
    /// </summary>
    Retake,

    /// <summary>
    /// 练习模式（不记录分数和排名）
    /// </summary>
    Practice
}

/// <summary>
/// 统考ViewModel，支持全省统考和学校统考两个独立列表
/// </summary>
public class UnifiedExamViewModel : ViewModelBase
{
    #region 私有字段

    private readonly IStudentExamService _studentExamService;
    private readonly IAuthenticationService? _authenticationService;
    private readonly IExamAttemptService? _examAttemptService;
    private readonly MainViewModel? _mainViewModel;
    private readonly EnhancedExamToolbarService? _enhancedExamToolbarService;

    #endregion

    #region 属性

    /// <summary>
    /// 全省统考列表（进行中的考试）
    /// </summary>
    [Reactive] public ObservableCollection<ExamWithPermissionsDto> ProvincialExams { get; set; } = [];

    /// <summary>
    /// 学校统考列表（进行中的考试）
    /// </summary>
    [Reactive] public ObservableCollection<ExamWithPermissionsDto> SchoolExams { get; set; } = [];

    /// <summary>
    /// 已结束的全省统考列表
    /// </summary>
    [Reactive] public ObservableCollection<ExamWithPermissionsDto> CompletedProvincialExams { get; set; } = [];

    /// <summary>
    /// 已结束的学校统考列表
    /// </summary>
    [Reactive] public ObservableCollection<ExamWithPermissionsDto> CompletedSchoolExams { get; set; } = [];

    /// <summary>
    /// 全省统考总数
    /// </summary>
    [Reactive] public int ProvincialExamCount { get; set; }

    /// <summary>
    /// 学校统考总数
    /// </summary>
    [Reactive] public int SchoolExamCount { get; set; }

    /// <summary>
    /// 是否正在加载全省统考
    /// </summary>
    [Reactive] public bool IsLoadingProvincialExams { get; set; }

    /// <summary>
    /// 是否正在加载学校统考
    /// </summary>
    [Reactive] public bool IsLoadingSchoolExams { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [Reactive] public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 当前选中的全省统考
    /// </summary>
    [Reactive] public ExamWithPermissionsDto? SelectedProvincialExam { get; set; }

    /// <summary>
    /// 当前选中的学校统考
    /// </summary>
    [Reactive] public ExamWithPermissionsDto? SelectedSchoolExam { get; set; }

    /// <summary>
    /// 全省统考当前页码
    /// </summary>
    [Reactive] public int ProvincialCurrentPage { get; set; } = 1;

    /// <summary>
    /// 学校统考当前页码
    /// </summary>
    [Reactive] public int SchoolCurrentPage { get; set; } = 1;

    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; } = 20;

    /// <summary>
    /// 用户是否有完整权限
    /// </summary>
    [Reactive] public bool HasFullAccess { get; set; }

    /// <summary>
    /// 用户权限状态描述
    /// </summary>
    [Reactive] public string UserPermissionStatus { get; set; } = string.Empty;

    #endregion

    #region 命令

    /// <summary>
    /// 刷新全省统考命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshProvincialExamsCommand { get; }

    /// <summary>
    /// 刷新学校统考命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshSchoolExamsCommand { get; }

    /// <summary>
    /// 刷新所有数据命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshAllCommand { get; }

    /// <summary>
    /// 加载更多全省统考命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadMoreProvincialExamsCommand { get; }

    /// <summary>
    /// 加载更多学校统考命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadMoreSchoolExamsCommand { get; }

    /// <summary>
    /// 查看全省统考详情命令
    /// </summary>
    public ReactiveCommand<ExamWithPermissionsDto, Unit> ViewProvincialExamDetailsCommand { get; }

    /// <summary>
    /// 查看学校统考详情命令
    /// </summary>
    public ReactiveCommand<ExamWithPermissionsDto, Unit> ViewSchoolExamDetailsCommand { get; }

    /// <summary>
    /// 开始考试命令
    /// </summary>
    public ReactiveCommand<ExamWithPermissionsDto, Unit> StartExamCommand { get; }

    /// <summary>
    /// 重新考试命令（记录分数和排名）
    /// </summary>
    public ReactiveCommand<ExamWithPermissionsDto, Unit> RetakeExamCommand { get; }

    /// <summary>
    /// 练习模式命令（不记录分数和排名）
    /// </summary>
    public ReactiveCommand<ExamWithPermissionsDto, Unit> PracticeExamCommand { get; }

    #endregion

    #region 构造函数

    public UnifiedExamViewModel(
        IStudentExamService studentExamService,
        IAuthenticationService? authenticationService,
        IExamAttemptService? examAttemptService = null,
        MainViewModel? mainViewModel = null)
    {
        _studentExamService = studentExamService;
        _authenticationService = authenticationService;
        _examAttemptService = examAttemptService;
        _mainViewModel = mainViewModel;

        // 尝试获取EnhancedExamToolbarService（可选依赖）
        try
        {
            _enhancedExamToolbarService = AppServiceManager.GetService<EnhancedExamToolbarService>();
        }
        catch
        {
            _enhancedExamToolbarService = null;
        }

        // 初始化命令
        RefreshProvincialExamsCommand = ReactiveCommand.CreateFromTask(RefreshProvincialExamsAsync);
        RefreshSchoolExamsCommand = ReactiveCommand.CreateFromTask(RefreshSchoolExamsAsync);
        RefreshAllCommand = ReactiveCommand.CreateFromTask(RefreshAllAsync);
        LoadMoreProvincialExamsCommand = ReactiveCommand.CreateFromTask(LoadMoreProvincialExamsAsync);
        LoadMoreSchoolExamsCommand = ReactiveCommand.CreateFromTask(LoadMoreSchoolExamsAsync);
        ViewProvincialExamDetailsCommand = ReactiveCommand.Create<ExamWithPermissionsDto>(ViewProvincialExamDetails);
        ViewSchoolExamDetailsCommand = ReactiveCommand.Create<ExamWithPermissionsDto>(ViewSchoolExamDetails);
        StartExamCommand = ReactiveCommand.Create<ExamWithPermissionsDto>(StartExam);
        RetakeExamCommand = ReactiveCommand.Create<ExamWithPermissionsDto>(RetakeExam);
        PracticeExamCommand = ReactiveCommand.Create<ExamWithPermissionsDto>(PracticeExam);

        // 初始化用户权限状态
        UpdateUserPermissions();

        // 监听用户信息更新事件
        if (_authenticationService != null)
        {
            _authenticationService.UserInfoUpdated += OnUserInfoUpdated;
        }

        // 初始加载数据
        _ = Task.Run(RefreshAllAsync);
    }

    #endregion

    #region 方法

    /// <summary>
    /// 创建包含权限信息的考试对象
    /// </summary>
    private async Task<ExamWithPermissionsDto> CreateExamWithPermissionsAsync(StudentExamDto exam)
    {
        ExamWithPermissionsDto examWithPermissions = new()
        {
            Exam = exam
        };

        try
        {
            // 检查用户认证状态
            if (_authenticationService?.CurrentUser != null &&
                int.TryParse(_authenticationService.CurrentUser.Id, out int studentId) && _examAttemptService != null)
            {
                examWithPermissions.AttemptLimit = await _examAttemptService.CheckExamAttemptLimitAsync(exam.Id, studentId);
            }
        }
        catch
        {
            // 提供默认的权限设置，允许用户开始考试
            examWithPermissions.AttemptLimit = new ExamAttemptLimitDto
            {
                ExamId = exam.Id,
                StudentId = 0,
                CanStartExam = true,
                CanRetake = false,
                CanPractice = false,
                TotalAttempts = 0,
                RetakeAttempts = 0,
                PracticeAttempts = 0,
                LimitReason = null
            };
        }

        return examWithPermissions;
    }

    /// <summary>
    /// 刷新全省统考数据
    /// </summary>
    private async Task RefreshProvincialExamsAsync()
    {
        try
        {
            IsLoadingProvincialExams = true;
            ErrorMessage = string.Empty;
            ProvincialCurrentPage = 1;

            ProvincialExamCount = await _studentExamService.GetAvailableExamCountByCategoryAsync(ExamCategory.Provincial);

            List<StudentExamDto> allExams = await _studentExamService.GetAvailableExamsByCategoryAsync(
                ExamCategory.Provincial, ProvincialCurrentPage, PageSize);

            List<StudentExamDto> activeExams = FilterActiveExams(allExams);
            List<StudentExamDto> completedExams = FilterCompletedExams(allExams);

            List<ExamWithPermissionsDto> activeExamsWithPermissions = [];
            foreach (StudentExamDto exam in activeExams)
            {
                ExamWithPermissionsDto examWithPermissions = await CreateExamWithPermissionsAsync(exam);
                activeExamsWithPermissions.Add(examWithPermissions);
            }

            List<ExamWithPermissionsDto> completedExamsWithPermissions = [];
            foreach (StudentExamDto exam in completedExams)
            {
                ExamWithPermissionsDto examWithPermissions = await CreateExamWithPermissionsAsync(exam);
                completedExamsWithPermissions.Add(examWithPermissions);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProvincialExams.Clear();
                CompletedProvincialExams.Clear();

                foreach (ExamWithPermissionsDto examWithPermissions in activeExamsWithPermissions)
                {
                    ProvincialExams.Add(examWithPermissions);
                }

                foreach (ExamWithPermissionsDto examWithPermissions in completedExamsWithPermissions)
                {
                    CompletedProvincialExams.Add(examWithPermissions);
                }
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载全省统考失败：{ex.Message}";
        }
        finally
        {
            IsLoadingProvincialExams = false;
        }
    }

    /// <summary>
    /// 刷新学校统考数据
    /// </summary>
    private async Task RefreshSchoolExamsAsync()
    {
        try
        {
            IsLoadingSchoolExams = true;
            ErrorMessage = string.Empty;
            SchoolCurrentPage = 1;

            SchoolExamCount = await _studentExamService.GetAvailableExamCountByCategoryAsync(ExamCategory.School);

            List<StudentExamDto> allExams = await _studentExamService.GetAvailableExamsByCategoryAsync(
                ExamCategory.School, SchoolCurrentPage, PageSize);

            List<StudentExamDto> activeExams = FilterActiveExams(allExams);
            List<StudentExamDto> completedExams = FilterCompletedExams(allExams);

            List<ExamWithPermissionsDto> activeExamsWithPermissions = [];
            foreach (StudentExamDto exam in activeExams)
            {
                ExamWithPermissionsDto examWithPermissions = await CreateExamWithPermissionsAsync(exam);
                activeExamsWithPermissions.Add(examWithPermissions);
            }

            List<ExamWithPermissionsDto> completedExamsWithPermissions = [];
            foreach (StudentExamDto exam in completedExams)
            {
                ExamWithPermissionsDto examWithPermissions = await CreateExamWithPermissionsAsync(exam);
                completedExamsWithPermissions.Add(examWithPermissions);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SchoolExams.Clear();
                CompletedSchoolExams.Clear();

                foreach (ExamWithPermissionsDto examWithPermissions in activeExamsWithPermissions)
                {
                    SchoolExams.Add(examWithPermissions);
                }

                foreach (ExamWithPermissionsDto examWithPermissions in completedExamsWithPermissions)
                {
                    CompletedSchoolExams.Add(examWithPermissions);
                }
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载学校统考失败：{ex.Message}";
        }
        finally
        {
            IsLoadingSchoolExams = false;
        }
    }

    /// <summary>
    /// 刷新所有数据
    /// </summary>
    private async Task RefreshAllAsync()
    {
        await Task.WhenAll(RefreshProvincialExamsAsync(), RefreshSchoolExamsAsync());

        try
        {
            _ = UpdateUserPermissionsAsync();
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// 加载更多全省统考
    /// </summary>
    private async Task LoadMoreProvincialExamsAsync()
    {
        if (IsLoadingProvincialExams || ProvincialExams.Count >= ProvincialExamCount)
        {
            return;
        }

        try
        {
            IsLoadingProvincialExams = true;
            ProvincialCurrentPage++;

            List<StudentExamDto> exams = await _studentExamService.GetAvailableExamsByCategoryAsync(
                ExamCategory.Provincial, ProvincialCurrentPage, PageSize);

            List<ExamWithPermissionsDto> examsWithPermissions = [];
            foreach (StudentExamDto exam in exams)
            {
                ExamWithPermissionsDto examWithPermissions = await CreateExamWithPermissionsAsync(exam);
                examsWithPermissions.Add(examWithPermissions);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (ExamWithPermissionsDto examWithPermissions in examsWithPermissions)
                {
                    ProvincialExams.Add(examWithPermissions);
                }
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载更多全省统考失败：{ex.Message}";
            ProvincialCurrentPage--; // 回退页码
        }
        finally
        {
            IsLoadingProvincialExams = false;
        }
    }

    /// <summary>
    /// 加载更多学校统考
    /// </summary>
    private async Task LoadMoreSchoolExamsAsync()
    {
        if (IsLoadingSchoolExams || SchoolExams.Count >= SchoolExamCount)
        {
            return;
        }

        try
        {
            IsLoadingSchoolExams = true;
            SchoolCurrentPage++;

            List<StudentExamDto> exams = await _studentExamService.GetAvailableExamsByCategoryAsync(
                ExamCategory.School, SchoolCurrentPage, PageSize);

            List<ExamWithPermissionsDto> examsWithPermissions = [];
            foreach (StudentExamDto exam in exams)
            {
                ExamWithPermissionsDto examWithPermissions = await CreateExamWithPermissionsAsync(exam);
                examsWithPermissions.Add(examWithPermissions);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (ExamWithPermissionsDto examWithPermissions in examsWithPermissions)
                {
                    SchoolExams.Add(examWithPermissions);
                }
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载更多学校统考失败：{ex.Message}";
            SchoolCurrentPage--; // 回退页码
        }
        finally
        {
            IsLoadingSchoolExams = false;
        }
    }

    /// <summary>
    /// 查看全省统考详情
    /// </summary>
    private void ViewProvincialExamDetails(ExamWithPermissionsDto examWithPermissions)
    {
        SelectedProvincialExam = examWithPermissions;
        // TODO: 实现考试详情查看逻辑
    }

    /// <summary>
    /// 查看学校统考详情
    /// </summary>
    private void ViewSchoolExamDetails(ExamWithPermissionsDto examWithPermissions)
    {
        SelectedSchoolExam = examWithPermissions;
        // TODO: 实现考试详情查看逻辑
    }

    /// <summary>
    /// 更新用户权限状态
    /// </summary>
    private void UpdateUserPermissions()
    {
        try
        {
            if (_authenticationService?.CurrentUser != null)
            {
                HasFullAccess = _authenticationService.CurrentUser.HasFullAccess;
                UserPermissionStatus = HasFullAccess ? "拥有完整功能权限" : "权限受限，需要绑定学校";
            }
            else
            {
                HasFullAccess = false;
                UserPermissionStatus = "用户未登录";
            }
        }
        catch
        {
            HasFullAccess = false;
            UserPermissionStatus = "权限状态未知";
        }
    }

    /// <summary>
    /// 异步更新用户权限状态
    /// </summary>
    private async Task UpdateUserPermissionsAsync()
    {
        try
        {
            if (_authenticationService != null)
            {
                _ = await _authenticationService.RefreshUserInfoAsync();
                UpdateUserPermissions();
            }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    private void OnUserInfoUpdated(object? sender, UserInfo? userInfo)
    {
        UpdateUserPermissions();
    }

    /// <summary>
    /// 获取考试状态显示文本
    /// </summary>
    public static string GetExamStatusText(StudentExamDto exam)
    {
        if (exam.StartTime.HasValue && exam.EndTime.HasValue)
        {
            DateTime now = DateTime.Now;
            return now < exam.StartTime.Value ? "即将开始" : now > exam.EndTime.Value ? "联考已结束" : "联考正在进行中";
        }

        return exam.Status switch
        {
            "Published" => "联考正在进行中",
            "InProgress" => "联考正在进行中",
            "Completed" => "联考已结束",
            "Draft" => "即将开始",
            _ => "联考正在进行中"
        };
    }

    /// <summary>
    /// 获取考试时间显示文本
    /// </summary>
    public static string GetExamTimeText(StudentExamDto exam)
    {
        if (exam.StartTime.HasValue && exam.EndTime.HasValue)
        {
            return $"{exam.StartTime.Value:yyyy-MM-dd HH:mm} - {exam.EndTime.Value:yyyy-MM-dd HH:mm}";
        }
        else if (exam.StartTime.HasValue)
        {
            return $"开始时间：{exam.StartTime.Value:yyyy-MM-dd HH:mm}";
        }
        else if (exam.EndTime.HasValue)
        {
            return $"结束时间：{exam.EndTime.Value:yyyy-MM-dd HH:mm}";
        }

        return "时间待定";
    }

    /// <summary>
    /// 过滤进行中的考试
    /// </summary>
    /// <param name="exams">所有考试列表</param>
    /// <returns>进行中的考试列表</returns>
    private static List<StudentExamDto> FilterActiveExams(List<StudentExamDto> exams)
    {
        DateTime now = DateTime.Now;

        List<StudentExamDto> result = exams.Where(exam =>
        {
            if (exam.Status is "Published" or "Scheduled" or "InProgress")
            {
                if (exam.StartTime.HasValue && exam.EndTime.HasValue)
                {
                    bool canParticipate = (exam.Status == "Scheduled" && now <= exam.EndTime.Value) ||
                                          (now >= exam.StartTime.Value && now <= exam.EndTime.Value);
                    return canParticipate;
                }
                return true;
            }
            return false;
        }).ToList();

        return result;
    }

    /// <summary>
    /// 过滤已结束的考试
    /// </summary>
    /// <param name="exams">所有考试列表</param>
    /// <returns>已结束的考试列表</returns>
    private static List<StudentExamDto> FilterCompletedExams(List<StudentExamDto> exams)
    {
        DateTime now = DateTime.Now;

        List<StudentExamDto> result = exams.Where(exam =>
        {
            return exam.Status == "Completed" || exam.EndTime.HasValue && now > exam.EndTime.Value;
        }).ToList();

        return result;
    }

    #endregion

    #region 考试权限检查

    /// <summary>
    /// 检查考试次数限制权限
    /// </summary>
    private async Task<bool> CheckExamAttemptPermissionAsync(StudentExamDto exam, ExamMode mode)
    {
        try
        {
            if (_examAttemptService == null)
            {
                return true;
            }

            if (_authenticationService?.CurrentUser == null)
            {
                ErrorMessage = "用户未登录，无法开始考试";
                return false;
            }

            if (!int.TryParse(_authenticationService.CurrentUser.Id, out int studentId))
            {
                ErrorMessage = "用户ID格式错误，无法开始考试";
                return false;
            }

            ExamAttemptLimitDto limitCheck = await _examAttemptService.CheckExamAttemptLimitAsync(exam.Id, studentId);

            bool canStart = mode switch
            {
                ExamMode.Normal => limitCheck.CanStartExam,
                ExamMode.Retake => limitCheck.CanRetake,
                ExamMode.Practice => limitCheck.CanPractice,
                _ => false
            };

            if (!canStart)
            {
                string modeText = mode switch
                {
                    ExamMode.Normal => "开始考试",
                    ExamMode.Retake => "重新考试",
                    ExamMode.Practice => "练习模式",
                    _ => "考试"
                };

                ErrorMessage = $"无法{modeText}：{limitCheck.LimitReason ?? "权限不足"}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"检查考试权限时发生错误: {ex.Message}";
            return false;
        }
    }

    #endregion

    #region 考试操作方法

    /// <summary>
    /// 开始考试
    /// </summary>
    private async void StartExam(ExamWithPermissionsDto examWithPermissions)
    {
        StudentExamDto exam = examWithPermissions.Exam;

        try
        {
            ExamMode mode = examWithPermissions.GetRecommendedMode();

            if (!await CheckExamAttemptPermissionAsync(exam, mode))
            {
                return;
            }

            NavigateToExam(exam, mode);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"开始考试失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 重新考试（记录分数和排名）
    /// </summary>
    private async void RetakeExam(ExamWithPermissionsDto examWithPermissions)
    {
        StudentExamDto exam = examWithPermissions.Exam;

        try
        {
            if (!await CheckExamAttemptPermissionAsync(exam, ExamMode.Retake))
            {
                return;
            }

            NavigateToExam(exam, ExamMode.Retake);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"重新考试失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 练习模式（不记录分数和排名）
    /// </summary>
    private async void PracticeExam(ExamWithPermissionsDto examWithPermissions)
    {
        StudentExamDto exam = examWithPermissions.Exam;

        try
        {
            if (!await CheckExamAttemptPermissionAsync(exam, ExamMode.Practice))
            {
                return;
            }

            NavigateToExam(exam, ExamMode.Practice);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"练习模式失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 导航到考试界面
    /// </summary>
    private void NavigateToExam(StudentExamDto exam, ExamMode mode)
    {
        try
        {
            if (!HasFullAccess)
            {
                ErrorMessage = "您需要解锁权限才能开始考试。请加入学校组织或联系管理员进行解锁。";
                return;
            }

            StartExamInterfaceAsync(exam, mode);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"启动考试失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 启动考试界面
    /// </summary>
    private void StartExamInterfaceAsync(StudentExamDto exam, ExamMode mode)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow == null ||
            _authenticationService == null)
        {
            throw new InvalidOperationException("无法获取主窗口引用");
        }

        try
        {
            desktop.MainWindow.Hide();

            IBenchSuiteDirectoryService? benchSuiteDirectoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
            ExamToolbarViewModel toolbarViewModel = new(_authenticationService, null, benchSuiteDirectoryService);

            int totalQuestions = exam.Subjects.Sum(s => s.Questions.Count) + exam.Modules.Sum(m => m.Questions.Count);

            ExamType examType = mode switch
            {
                ExamMode.Normal => ExamType.FormalExam,
                ExamMode.Retake => ExamType.FormalExam,
                ExamMode.Practice => ExamType.Practice,
                _ => ExamType.FormalExam
            };

            toolbarViewModel.SetExamInfo(
                examType,
                exam.Id,
                exam.Name,
                totalQuestions,
                exam.DurationMinutes * 60
            );

            ExamToolbarWindow examToolbar = new();
            examToolbar.SetViewModel(toolbarViewModel);

            examToolbar.ExamAutoSubmitted += (sender, e) => OnExamAutoSubmitted(examType, exam.Id);
            examToolbar.ExamManualSubmitted += (sender, e) => OnExamManualSubmitted(examType, exam.Id);
            examToolbar.ViewQuestionsRequested += (sender, e) => OnViewQuestionsRequested(exam);

            examToolbar.Show();

            toolbarViewModel.StartExam();
        }
        catch (Exception ex)
        {
            desktop.MainWindow?.Show();
            throw new Exception($"启动考试界面失败: {ex.Message}", ex);
        }
    }

    #endregion

    #region 考试事件处理

    /// <summary>
    /// 处理考试自动提交事件
    /// </summary>
    private async void OnExamAutoSubmitted(ExamType examType, int examId)
    {
        try
        {
            await HandleExamSubmissionAsync(examType, examId, true);
        }
        catch
        {
            ShowMainWindow();
        }
    }

    /// <summary>
    /// 处理考试手动提交事件
    /// </summary>
    private async void OnExamManualSubmitted(ExamType examType, int examId)
    {
        try
        {
            await HandleExamSubmissionAsync(examType, examId, false);
        }
        catch
        {
            ShowMainWindow();
        }
    }

    /// <summary>
    /// 处理查看题目请求事件
    /// </summary>
    private void OnViewQuestionsRequested(StudentExamDto exam)
    {
        try
        {
            ExamQuestionDetailsViewModel detailsViewModel = new();
            detailsViewModel.SetExamData(exam);

            ExamQuestionDetailsWindow detailsWindow = new()
            {
                DataContext = detailsViewModel
            };

            detailsWindow.Show();
        }
        catch
        {
        }
    }

    /// <summary>
    /// 显示主窗口
    /// </summary>
    private void ShowMainWindow()
    {
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                desktop.MainWindow.Show();
            }
        }
        catch
        {
        }
    }

    #endregion

    #region 析构函数

    /// <summary>
    /// 析构函数，取消事件订阅
    /// </summary>
    ~UnifiedExamViewModel()
    {
        if (_authenticationService != null)
        {
            _authenticationService.UserInfoUpdated -= OnUserInfoUpdated;
        }
    }

    /// <summary>
    /// 处理考试提交并显示结果窗口
    /// </summary>
    private async Task HandleExamSubmissionAsync(ExamType examType, int examId, bool isAutoSubmit)
    {
        bool submitResult = false;
        string errorMessage = "";
        int? actualDurationSeconds = null;
        decimal? score = null;
        decimal? maxScore = null;

        try
        {
            switch (examType)
            {
                case ExamType.FormalExam:
                    if (_enhancedExamToolbarService != null)
                    {
                        Dictionary<ModuleType, ScoringResult>? scoringResults = await _enhancedExamToolbarService.SubmitFormalExamWithResultAsync(examId);
                        submitResult = scoringResults != null && scoringResults.Count > 0;
                        if (scoringResults != null && scoringResults.Count > 0)
                        {
                            // 统一考试模式不需要计算时间，使用默认值
                            actualDurationSeconds = 0;
                            score = scoringResults.Values.Sum(r => r.AchievedScore);
                            maxScore = scoringResults.Values.Sum(r => r.TotalScore);
                            System.Diagnostics.Debug.WriteLine($"UnifiedExamViewModel: 正式考试BenchSuite评分结果 - Score: {score}, MaxScore: {maxScore}");
                        }
                    }
                    break;

                case ExamType.MockExam:
                    if (_enhancedExamToolbarService != null)
                    {
                        Dictionary<ModuleType, ScoringResult>? scoringResults = await _enhancedExamToolbarService.SubmitMockExamAsync(examId, actualDurationSeconds);
                        submitResult = scoringResults != null && scoringResults.Count > 0;
                        if (scoringResults != null && scoringResults.Count > 0)
                        {
                            // 模拟考试不需要重新计算时间
                            score = scoringResults.Values.Sum(r => r.AchievedScore);
                            maxScore = scoringResults.Values.Sum(r => r.TotalScore);
                            System.Diagnostics.Debug.WriteLine($"UnifiedExamViewModel: 模拟考试BenchSuite评分结果 - Score: {score}, MaxScore: {maxScore}");
                        }
                    }
                    break;

                case ExamType.Practice:
                    // 练习模式：仅在本地处理，不向API提交
                    System.Diagnostics.Debug.WriteLine($"UnifiedExamViewModel: 练习模式完成，考试ID: {examId}，不向API提交结果");

                    // 执行本地BenchSuite评分（如果可用）
                    if (_enhancedExamToolbarService != null && _authenticationService?.CurrentUser != null)
                    {
                        if (int.TryParse(_authenticationService.CurrentUser.Id, out int studentId))
                        {
                            try
                            {
                                // 仅进行本地评分，不提交到服务器
                                Dictionary<ModuleType, ScoringResult>? scoringResults =
                                    await _enhancedExamToolbarService.PerformLocalScoringAsync(ExamType.Practice, examId, studentId);

                                if (scoringResults != null && scoringResults.Count > 0)
                                {
                                    // 练习模式不需要计算时间
                                    actualDurationSeconds = 0;
                                    score = scoringResults.Values.Sum(r => r.AchievedScore);
                                    maxScore = scoringResults.Values.Sum(r => r.TotalScore);
                                    System.Diagnostics.Debug.WriteLine($"UnifiedExamViewModel: 练习模式本地评分完成，得分: {score}/{maxScore}");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"UnifiedExamViewModel: 练习模式本地评分失败: {ex.Message}");
                            }
                        }
                    }

                    submitResult = true; // 练习模式总是返回成功，因为不需要实际提交
                    break;

                case ExamType.ComprehensiveTraining:
                    if (_enhancedExamToolbarService != null)
                    {
                        submitResult = await _enhancedExamToolbarService.SubmitComprehensiveTrainingAsync(examId);
                    }
                    break;

                default:
                    errorMessage = $"不支持的考试类型: {examType}";
                    break;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"考试提交异常: {ex.Message}";
            submitResult = false;
        }

        await ShowExamResultAsync(examId, examType, submitResult, actualDurationSeconds, score, maxScore, errorMessage);
    }

    /// <summary>
    /// 显示考试结果窗口
    /// </summary>
    private async Task ShowExamResultAsync(int examId, ExamType examType, bool isSuccessful, int? actualDurationSeconds, decimal? score, decimal? maxScore, string errorMessage = "")
    {
        try
        {
            string examName = "考试";
            try
            {
                StudentExamDto? exam = await _studentExamService.GetExamDetailsAsync(examId);
                if (exam != null)
                {
                    examName = exam.Name;
                }
            }
            catch
            {
            }

            int? durationMinutes = actualDurationSeconds.HasValue ? (actualDurationSeconds.Value / 60) : null;

            System.Diagnostics.Debug.WriteLine($"UnifiedExamViewModel: 显示考试结果 - Score: {score}, MaxScore: {maxScore}, ExamType: {examType}");

            _ = await Views.Dialogs.FullScreenExamResultWindow.ShowFullScreenExamResultAsync(
                examName,
                examType,
                isSuccessful,
                null,
                null,
                durationMinutes,
                score,
                maxScore,
                isSuccessful ? "" : errorMessage,
                isSuccessful ? "考试提交成功" : "考试提交失败",
                true,
                false
            );
        }
        catch
        {
        }
        finally
        {
            ShowMainWindow();
        }
    }

    #endregion
}
