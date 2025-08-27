using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Examina.Models;
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
            if (_enhancedExamToolbarService != null)
            {
                System.Diagnostics.Debug.WriteLine("UnifiedExamViewModel: 成功获取EnhancedExamToolbarService");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("UnifiedExamViewModel: EnhancedExamToolbarService未注册");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UnifiedExamViewModel: 获取EnhancedExamToolbarService失败: {ex.Message}");
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
                int.TryParse(_authenticationService.CurrentUser.Id, out int studentId))
            {
                // 获取考试权限信息
                System.Diagnostics.Debug.WriteLine($"[CreateExamWithPermissionsAsync] 开始检查考试权限，考试ID: {exam.Id}, 学生ID: {studentId}");
                System.Diagnostics.Debug.WriteLine($"[CreateExamWithPermissionsAsync] 考试配置 - AllowRetake: {exam.AllowRetake}, AllowPractice: {exam.AllowPractice}, MaxRetakeCount: {exam.MaxRetakeCount}");

                examWithPermissions.AttemptLimit = await _examAttemptService.CheckExamAttemptLimitAsync(exam.Id, studentId);

                System.Diagnostics.Debug.WriteLine($"[CreateExamWithPermissionsAsync] 权限检查完成 - {exam.Name}: " +
                    $"CanStartExam={examWithPermissions.AttemptLimit.CanStartExam}, " +
                    $"CanRetake={examWithPermissions.AttemptLimit.CanRetake}, " +
                    $"CanPractice={examWithPermissions.AttemptLimit.CanPractice}, " +
                    $"AllowRetake={examWithPermissions.AttemptLimit.AllowRetake}, " +
                    $"AllowPractice={examWithPermissions.AttemptLimit.AllowPractice}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[CreateExamWithPermissionsAsync] 用户未认证，无法检查考试权限");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateExamWithPermissionsAsync] 检查考试权限失败: {ex.Message}");

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

            // 获取总数
            ProvincialExamCount = await _studentExamService.GetAvailableExamCountByCategoryAsync(ExamCategory.Provincial);

            // 获取第一页数据
            List<StudentExamDto> allExams = await _studentExamService.GetAvailableExamsByCategoryAsync(
                ExamCategory.Provincial, ProvincialCurrentPage, PageSize);

            System.Diagnostics.Debug.WriteLine($"[UnifiedExamViewModel] 全省统考原始数据: {allExams.Count} 个考试");
            foreach (StudentExamDto exam in allExams)
            {
                System.Diagnostics.Debug.WriteLine($"[UnifiedExamViewModel] 全省统考: {exam.Name}, 状态: {exam.Status}, 开始: {exam.StartTime}, 结束: {exam.EndTime}");
            }

            // 按状态过滤考试
            List<StudentExamDto> activeExams = FilterActiveExams(allExams);
            List<StudentExamDto> completedExams = FilterCompletedExams(allExams);

            System.Diagnostics.Debug.WriteLine($"[UnifiedExamViewModel] 全省统考过滤结果: 进行中 {activeExams.Count} 个, 已结束 {completedExams.Count} 个");

            // 在后台线程中创建包含权限信息的对象
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

            // 更新UI
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

            System.Diagnostics.Debug.WriteLine($"刷新全省统考成功，共 {ProvincialExamCount} 项，当前显示 {ProvincialExams.Count} 项");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载全省统考失败：{ex.Message}";
            System.Diagnostics.Debug.WriteLine($"刷新全省统考失败: {ex.Message}");
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

            // 获取总数
            SchoolExamCount = await _studentExamService.GetAvailableExamCountByCategoryAsync(ExamCategory.School);

            // 获取第一页数据
            List<StudentExamDto> allExams = await _studentExamService.GetAvailableExamsByCategoryAsync(
                ExamCategory.School, SchoolCurrentPage, PageSize);

            System.Diagnostics.Debug.WriteLine($"[UnifiedExamViewModel] 学校统考原始数据: {allExams.Count} 个考试");
            foreach (StudentExamDto exam in allExams)
            {
                System.Diagnostics.Debug.WriteLine($"[UnifiedExamViewModel] 学校统考: {exam.Name}, 状态: {exam.Status}, 开始: {exam.StartTime}, 结束: {exam.EndTime}");
            }

            // 按状态过滤考试
            List<StudentExamDto> activeExams = FilterActiveExams(allExams);
            List<StudentExamDto> completedExams = FilterCompletedExams(allExams);

            System.Diagnostics.Debug.WriteLine($"[UnifiedExamViewModel] 学校统考过滤结果: 进行中 {activeExams.Count} 个, 已结束 {completedExams.Count} 个");

            // 在后台线程中创建包含权限信息的对象
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

            // 更新UI
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

            System.Diagnostics.Debug.WriteLine($"刷新学校统考成功，共 {SchoolExamCount} 项，当前显示 {SchoolExams.Count} 项");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载学校统考失败：{ex.Message}";
            System.Diagnostics.Debug.WriteLine($"刷新学校统考失败: {ex.Message}");
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

        // 数据刷新完成后，强制更新用户权限状态
        try
        {
            System.Diagnostics.Debug.WriteLine("UnifiedExamViewModel: 刷新完成，开始更新用户权限状态");
            _ = UpdateUserPermissionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UnifiedExamViewModel: 更新用户权限状态失败: {ex.Message}");
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

            // 为每个考试创建包含权限信息的对象
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

            // 为每个考试创建包含权限信息的对象
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
        System.Diagnostics.Debug.WriteLine($"查看全省统考详情: {examWithPermissions.Exam.Name}");
    }

    /// <summary>
    /// 查看学校统考详情
    /// </summary>
    private void ViewSchoolExamDetails(ExamWithPermissionsDto examWithPermissions)
    {
        SelectedSchoolExam = examWithPermissions;
        // TODO: 实现考试详情查看逻辑
        System.Diagnostics.Debug.WriteLine($"查看学校统考详情: {examWithPermissions.Exam.Name}");
    }

    /// <summary>
    /// 更新用户权限状态
    /// </summary>
    private void UpdateUserPermissions()
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

        System.Diagnostics.Debug.WriteLine($"UnifiedExamViewModel: 用户权限状态更新 - HasFullAccess: {HasFullAccess}, Status: {UserPermissionStatus}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UnifiedExamViewModel: 异步更新用户权限状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    private void OnUserInfoUpdated(object? sender, UserInfo? userInfo)
    {
        System.Diagnostics.Debug.WriteLine("UnifiedExamViewModel: 收到用户信息更新事件");
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
        System.Diagnostics.Debug.WriteLine($"[FilterActiveExams] 当前时间: {now:yyyy-MM-dd HH:mm:ss}");

        List<StudentExamDto> result = exams.Where(exam =>
        {
            System.Diagnostics.Debug.WriteLine($"[FilterActiveExams] 检查考试: {exam.Name}");
            System.Diagnostics.Debug.WriteLine($"[FilterActiveExams] - 状态: {exam.Status}");
            System.Diagnostics.Debug.WriteLine($"[FilterActiveExams] - 开始时间: {exam.StartTime}");
            System.Diagnostics.Debug.WriteLine($"[FilterActiveExams] - 结束时间: {exam.EndTime}");

            // 状态为已发布、已安排或进行中
            if (exam.Status is "Published" or "Scheduled" or "InProgress")
            {
                System.Diagnostics.Debug.WriteLine($"[FilterActiveExams] - 状态符合条件");
                // 如果有时间设置，检查是否在时间范围内或即将开始
                if (exam.StartTime.HasValue && exam.EndTime.HasValue)
                {
                    // 对于已安排的考试，如果还没到开始时间，也显示为可参加
                    // 对于已发布和进行中的考试，检查是否在时间范围内
                    bool canParticipate = (exam.Status == "Scheduled" && now <= exam.EndTime.Value) ||
                                         (now >= exam.StartTime.Value && now <= exam.EndTime.Value);
                    System.Diagnostics.Debug.WriteLine($"[FilterActiveExams] - 时间范围检查: {canParticipate}");
                    return canParticipate;
                }
                // 如果没有时间设置，根据状态判断
                System.Diagnostics.Debug.WriteLine($"[FilterActiveExams] - 无时间设置，根据状态判断: true");
                return true;
            }
            System.Diagnostics.Debug.WriteLine($"[FilterActiveExams] - 状态不符合条件: false");
            return false;
        }).ToList();

        System.Diagnostics.Debug.WriteLine($"[FilterActiveExams] 过滤结果: {result.Count} 个进行中的考试");
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
        System.Diagnostics.Debug.WriteLine($"[FilterCompletedExams] 当前时间: {now:yyyy-MM-dd HH:mm:ss}");

        List<StudentExamDto> result = exams.Where(exam =>
        {
            System.Diagnostics.Debug.WriteLine($"[FilterCompletedExams] 检查考试: {exam.Name}");
            System.Diagnostics.Debug.WriteLine($"[FilterCompletedExams] - 状态: {exam.Status}");
            System.Diagnostics.Debug.WriteLine($"[FilterCompletedExams] - 结束时间: {exam.EndTime}");

            // 状态为已完成
            if (exam.Status == "Completed")
            {
                System.Diagnostics.Debug.WriteLine($"[FilterCompletedExams] - 状态为已完成: true");
                return true;
            }
            // 或者当前时间超过结束时间
            if (exam.EndTime.HasValue && now > exam.EndTime.Value)
            {
                System.Diagnostics.Debug.WriteLine($"[FilterCompletedExams] - 时间已过期: true");
                return true;
            }
            System.Diagnostics.Debug.WriteLine($"[FilterCompletedExams] - 不符合已结束条件: false");
            return false;
        }).ToList();

        System.Diagnostics.Debug.WriteLine($"[FilterCompletedExams] 过滤结果: {result.Count} 个已结束的考试");
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
            // 如果没有考试次数限制服务，跳过检查
            if (_examAttemptService == null)
            {
                System.Diagnostics.Debug.WriteLine("[CheckExamAttemptPermissionAsync] 考试次数限制服务未注入，跳过检查");
                return true;
            }

            // 获取当前用户ID
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

            // 检查考试次数限制
            ExamAttemptLimitDto limitCheck = await _examAttemptService.CheckExamAttemptLimitAsync(exam.Id, studentId);

            // 根据考试模式检查权限
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
                System.Diagnostics.Debug.WriteLine($"[CheckExamAttemptPermissionAsync] {modeText}权限检查失败: {limitCheck.LimitReason}");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[CheckExamAttemptPermissionAsync] 考试权限检查通过，模式: {mode}");
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"检查考试权限时发生错误: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[CheckExamAttemptPermissionAsync] 检查考试权限异常: {ex.Message}");
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
        System.Diagnostics.Debug.WriteLine($"[StartExam] 开始考试: {exam.Name} (ID: {exam.Id})");

        try
        {
            // 使用推荐的考试模式
            ExamMode mode = examWithPermissions.GetRecommendedMode();

            // 检查考试次数限制
            if (!await CheckExamAttemptPermissionAsync(exam, mode))
            {
                return; // 错误消息已在CheckExamAttemptPermissionAsync中设置
            }

            // 导航到考试界面
            NavigateToExam(exam, mode);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StartExam] 开始考试失败: {ex.Message}");
            ErrorMessage = $"开始考试失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 重新考试（记录分数和排名）
    /// </summary>
    private async void RetakeExam(ExamWithPermissionsDto examWithPermissions)
    {
        StudentExamDto exam = examWithPermissions.Exam;
        System.Diagnostics.Debug.WriteLine($"[RetakeExam] 重新考试: {exam.Name} (ID: {exam.Id})");

        try
        {
            // 检查考试次数限制
            if (!await CheckExamAttemptPermissionAsync(exam, ExamMode.Retake))
            {
                return; // 错误消息已在CheckExamAttemptPermissionAsync中设置
            }

            // 导航到考试界面，传递考试模式为重考模式（记录分数）
            NavigateToExam(exam, ExamMode.Retake);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RetakeExam] 重新考试失败: {ex.Message}");
            ErrorMessage = $"重新考试失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 练习模式（不记录分数和排名）
    /// </summary>
    private async void PracticeExam(ExamWithPermissionsDto examWithPermissions)
    {
        StudentExamDto exam = examWithPermissions.Exam;
        System.Diagnostics.Debug.WriteLine($"[PracticeExam] 练习模式: {exam.Name} (ID: {exam.Id})");

        try
        {
            // 检查考试次数限制
            if (!await CheckExamAttemptPermissionAsync(exam, ExamMode.Practice))
            {
                return; // 错误消息已在CheckExamAttemptPermissionAsync中设置
            }

            // 导航到考试界面，传递考试模式为练习模式（不记录分数）
            NavigateToExam(exam, ExamMode.Practice);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PracticeExam] 练习模式失败: {ex.Message}");
            ErrorMessage = $"练习模式失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 导航到考试界面
    /// </summary>
    private void NavigateToExam(StudentExamDto exam, ExamMode mode)
    {
        System.Diagnostics.Debug.WriteLine($"[NavigateToExam] 启动考试: {exam.Name}, 模式: {mode}");

        try
        {
            // 检查用户权限
            if (!HasFullAccess)
            {
                ErrorMessage = "您需要解锁权限才能开始考试。请加入学校组织或联系管理员进行解锁。";
                System.Diagnostics.Debug.WriteLine("[NavigateToExam] 用户没有完整权限，无法开始考试");
                return;
            }

            // 启动考试界面
            StartExamInterfaceAsync(exam, mode);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NavigateToExam] 启动考试失败: {ex.Message}");
            ErrorMessage = $"启动考试失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 启动考试界面
    /// </summary>
    private void StartExamInterfaceAsync(StudentExamDto exam, ExamMode mode)
    {
        System.Diagnostics.Debug.WriteLine($"[StartExamInterfaceAsync] 开始启动考试界面: {exam.Name}");

        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow == null ||
            _authenticationService == null)
        {
            throw new InvalidOperationException("无法获取主窗口引用");
        }

        try
        {
            // 隐藏主窗口
            desktop.MainWindow.Hide();
            System.Diagnostics.Debug.WriteLine("[StartExamInterfaceAsync] 主窗口已隐藏");

            // 创建考试工具栏 ViewModel
            IBenchSuiteDirectoryService? benchSuiteDirectoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
            ExamToolbarViewModel toolbarViewModel = new(_authenticationService, null, benchSuiteDirectoryService);

            // 计算总题目数
            int totalQuestions = exam.Subjects.Sum(s => s.Questions.Count) + exam.Modules.Sum(m => m.Questions.Count);

            // 根据考试模式设置考试类型
            ExamType examType = mode switch
            {
                ExamMode.Normal => ExamType.FormalExam,
                ExamMode.Retake => ExamType.FormalExam, // 重考也是正式考试
                ExamMode.Practice => ExamType.MockExam, // 练习模式使用模拟考试类型
                _ => ExamType.FormalExam
            };

            // 设置考试信息
            toolbarViewModel.SetExamInfo(
                examType,
                exam.Id,
                exam.Name,
                totalQuestions,
                exam.DurationMinutes * 60 // 转换为秒
            );

            // 创建考试工具栏窗口并设置 ViewModel
            ExamToolbarWindow examToolbar = new();
            examToolbar.SetViewModel(toolbarViewModel);

            System.Diagnostics.Debug.WriteLine($"[StartExamInterfaceAsync] 考试工具栏已配置 - 考试ID: {exam.Id}, 题目数: {totalQuestions}, 时长: {exam.DurationMinutes}分钟, 模式: {mode}");

            // 订阅考试事件
            examToolbar.ExamAutoSubmitted += (sender, e) => OnExamAutoSubmitted(examType, exam.Id);
            examToolbar.ExamManualSubmitted += (sender, e) => OnExamManualSubmitted(examType, exam.Id);
            examToolbar.ViewQuestionsRequested += (sender, e) => OnViewQuestionsRequested(exam);

            System.Diagnostics.Debug.WriteLine("[StartExamInterfaceAsync] 已订阅考试工具栏事件");

            // 显示工具栏窗口
            examToolbar.Show();
            System.Diagnostics.Debug.WriteLine("[StartExamInterfaceAsync] 考试工具栏窗口已显示");

            // 开始考试（启动倒计时器并设置状态为进行中）
            toolbarViewModel.StartExam();
            System.Diagnostics.Debug.WriteLine($"[StartExamInterfaceAsync] 考试已开始，剩余时间: {toolbarViewModel.RemainingTimeSeconds}秒, 状态: {toolbarViewModel.CurrentExamStatus}");
        }
        catch (Exception ex)
        {
            // 如果启动失败，确保主窗口重新显示
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
        System.Diagnostics.Debug.WriteLine($"[OnExamAutoSubmitted] 考试自动提交: {examType}, ID: {examId}");

        try
        {
            // 执行考试提交并显示结果窗口
            await HandleExamSubmissionAsync(examType, examId, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnExamAutoSubmitted] 处理考试自动提交异常: {ex.Message}");
            // 如果处理失败，至少显示主窗口
            ShowMainWindow();
        }
    }

    /// <summary>
    /// 处理考试手动提交事件
    /// </summary>
    private async void OnExamManualSubmitted(ExamType examType, int examId)
    {
        System.Diagnostics.Debug.WriteLine($"[OnExamManualSubmitted] 考试手动提交: {examType}, ID: {examId}");

        try
        {
            // 执行考试提交并显示结果窗口
            await HandleExamSubmissionAsync(examType, examId, false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnExamManualSubmitted] 处理考试手动提交异常: {ex.Message}");
            // 如果处理失败，至少显示主窗口
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
            System.Diagnostics.Debug.WriteLine($"[OnViewQuestionsRequested] 用户请求查看题目: {exam.Name}");

            // 创建通用题目详情窗口
            ExamQuestionDetailsViewModel detailsViewModel = new();
            detailsViewModel.SetExamData(exam);

            ExamQuestionDetailsWindow detailsWindow = new()
            {
                DataContext = detailsViewModel
            };

            // 显示题目详情窗口
            detailsWindow.Show();
            System.Diagnostics.Debug.WriteLine("[OnViewQuestionsRequested] 题目详情窗口已显示");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnViewQuestionsRequested] 显示题目详情窗口异常: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("[ShowMainWindow] 主窗口已显示");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShowMainWindow] 显示主窗口失败: {ex.Message}");
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
        System.Diagnostics.Debug.WriteLine($"[HandleExamSubmissionAsync] 开始处理考试提交: {examType}, ID: {examId}, 自动提交: {isAutoSubmit}");

        bool submitResult = false;
        string errorMessage = "";
        int? actualDurationSeconds = null;

        try
        {
            // 根据考试类型执行相应的提交逻辑
            switch (examType)
            {
                case ExamType.FormalExam:
                    // 使用EnhancedExamToolbarService进行正式考试提交
                    if (_enhancedExamToolbarService != null)
                    {
                        var scoringResult = await _enhancedExamToolbarService.SubmitFormalExamWithResultAsync(examId);
                        submitResult = scoringResult != null;
                        if (scoringResult != null)
                        {
                            actualDurationSeconds = scoringResult.DurationSeconds;
                        }
                    }
                    break;

                case ExamType.MockExam:
                    // 使用EnhancedExamToolbarService进行模拟考试提交
                    if (_enhancedExamToolbarService != null)
                    {
                        submitResult = await _enhancedExamToolbarService.SubmitMockExamAsync(examId, actualDurationSeconds);
                    }
                    break;

                case ExamType.ComprehensiveTraining:
                    // 使用EnhancedExamToolbarService进行综合训练提交
                    if (_enhancedExamToolbarService != null)
                    {
                        submitResult = await _enhancedExamToolbarService.SubmitComprehensiveTrainingAsync(examId);
                    }
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"[HandleExamSubmissionAsync] 不支持的考试类型: {examType}");
                    errorMessage = $"不支持的考试类型: {examType}";
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"[HandleExamSubmissionAsync] 考试提交结果: {submitResult}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HandleExamSubmissionAsync] 考试提交异常: {ex.Message}");
            errorMessage = $"考试提交异常: {ex.Message}";
            submitResult = false;
        }

        // 显示考试结果窗口
        await ShowExamResultAsync(examId, examType, submitResult, actualDurationSeconds, errorMessage);
    }

    /// <summary>
    /// 显示考试结果窗口
    /// </summary>
    private async Task ShowExamResultAsync(int examId, ExamType examType, bool isSuccessful, int? actualDurationSeconds, string errorMessage = "")
    {
        try
        {
            // 获取考试名称
            string examName = "考试";
            try
            {
                StudentExamDto? exam = await _studentExamService.GetExamDetailsAsync(examId);
                if (exam != null)
                {
                    examName = exam.Name;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExamResultAsync] 获取考试名称失败: {ex.Message}");
            }

            // 转换用时为分钟
            int? durationMinutes = actualDurationSeconds.HasValue ? (actualDurationSeconds.Value / 60) : null;

            System.Diagnostics.Debug.WriteLine($"[ShowExamResultAsync] 准备显示全屏考试结果窗口 - {examName}");

            // 使用全屏考试结果窗口
            await Views.Dialogs.FullScreenExamResultWindow.ShowFullScreenExamResultAsync(
                examName,
                examType,
                isSuccessful,
                null, // startTime
                null, // endTime
                durationMinutes,
                null, // score - 暂时不显示分数
                null, // totalScore
                isSuccessful ? "" : errorMessage,
                isSuccessful ? "考试提交成功" : "考试提交失败",
                true, // showContinue
                false // showClose - 只显示确认按钮
            );

            System.Diagnostics.Debug.WriteLine($"[ShowExamResultAsync] 全屏考试结果窗口已显示并关闭 - {examName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShowExamResultAsync] 显示考试结果窗口异常: {ex.Message}");
        }
        finally
        {
            // 窗口关闭后显示主窗口
            ShowMainWindow();
        }
    }

    #endregion
}
