using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;
using Examina.Models;
using Examina.Models.Exam;
using Examina.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 统考ViewModel，支持全省统考和学校统考两个独立列表
/// </summary>
public class UnifiedExamViewModel : ViewModelBase
{
    #region 私有字段

    private readonly IStudentExamService _studentExamService;
    private readonly IAuthenticationService _authenticationService;

    #endregion

    #region 属性

    /// <summary>
    /// 全省统考列表（进行中的考试）
    /// </summary>
    [Reactive] public ObservableCollection<StudentExamDto> ProvincialExams { get; set; } = [];

    /// <summary>
    /// 学校统考列表（进行中的考试）
    /// </summary>
    [Reactive] public ObservableCollection<StudentExamDto> SchoolExams { get; set; } = [];

    /// <summary>
    /// 已结束的全省统考列表
    /// </summary>
    [Reactive] public ObservableCollection<StudentExamDto> CompletedProvincialExams { get; set; } = [];

    /// <summary>
    /// 已结束的学校统考列表
    /// </summary>
    [Reactive] public ObservableCollection<StudentExamDto> CompletedSchoolExams { get; set; } = [];

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
    [Reactive] public StudentExamDto? SelectedProvincialExam { get; set; }

    /// <summary>
    /// 当前选中的学校统考
    /// </summary>
    [Reactive] public StudentExamDto? SelectedSchoolExam { get; set; }

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
    public ReactiveCommand<StudentExamDto, Unit> ViewProvincialExamDetailsCommand { get; }

    /// <summary>
    /// 查看学校统考详情命令
    /// </summary>
    public ReactiveCommand<StudentExamDto, Unit> ViewSchoolExamDetailsCommand { get; }

    #endregion

    #region 构造函数

    public UnifiedExamViewModel(
        IStudentExamService studentExamService,
        IAuthenticationService authenticationService)
    {
        _studentExamService = studentExamService;
        _authenticationService = authenticationService;

        // 初始化命令
        RefreshProvincialExamsCommand = ReactiveCommand.CreateFromTask(RefreshProvincialExamsAsync);
        RefreshSchoolExamsCommand = ReactiveCommand.CreateFromTask(RefreshSchoolExamsAsync);
        RefreshAllCommand = ReactiveCommand.CreateFromTask(RefreshAllAsync);
        LoadMoreProvincialExamsCommand = ReactiveCommand.CreateFromTask(LoadMoreProvincialExamsAsync);
        LoadMoreSchoolExamsCommand = ReactiveCommand.CreateFromTask(LoadMoreSchoolExamsAsync);
        ViewProvincialExamDetailsCommand = ReactiveCommand.Create<StudentExamDto>(ViewProvincialExamDetails);
        ViewSchoolExamDetailsCommand = ReactiveCommand.Create<StudentExamDto>(ViewSchoolExamDetails);

        // 初始化用户权限状态
        UpdateUserPermissions();

        // 监听用户信息更新事件
        if (_authenticationService != null)
        {
            _authenticationService.UserInfoUpdated += OnUserInfoUpdated;
        }

        // 初始加载数据
        _ = Task.Run(async () => await RefreshAllAsync());
    }

    #endregion

    #region 方法

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

            // 更新UI
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProvincialExams.Clear();
                CompletedProvincialExams.Clear();

                foreach (StudentExamDto exam in activeExams)
                {
                    ProvincialExams.Add(exam);
                }

                foreach (StudentExamDto exam in completedExams)
                {
                    CompletedProvincialExams.Add(exam);
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

            // 更新UI
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SchoolExams.Clear();
                CompletedSchoolExams.Clear();

                foreach (StudentExamDto exam in activeExams)
                {
                    SchoolExams.Add(exam);
                }

                foreach (StudentExamDto exam in completedExams)
                {
                    CompletedSchoolExams.Add(exam);
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
            return;

        try
        {
            IsLoadingProvincialExams = true;
            ProvincialCurrentPage++;

            List<StudentExamDto> exams = await _studentExamService.GetAvailableExamsByCategoryAsync(
                ExamCategory.Provincial, ProvincialCurrentPage, PageSize);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (StudentExamDto exam in exams)
                {
                    ProvincialExams.Add(exam);
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
            return;

        try
        {
            IsLoadingSchoolExams = true;
            SchoolCurrentPage++;

            List<StudentExamDto> exams = await _studentExamService.GetAvailableExamsByCategoryAsync(
                ExamCategory.School, SchoolCurrentPage, PageSize);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (StudentExamDto exam in exams)
                {
                    SchoolExams.Add(exam);
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
    private void ViewProvincialExamDetails(StudentExamDto exam)
    {
        SelectedProvincialExam = exam;
        // TODO: 实现考试详情查看逻辑
        System.Diagnostics.Debug.WriteLine($"查看全省统考详情: {exam.Name}");
    }

    /// <summary>
    /// 查看学校统考详情
    /// </summary>
    private void ViewSchoolExamDetails(StudentExamDto exam)
    {
        SelectedSchoolExam = exam;
        // TODO: 实现考试详情查看逻辑
        System.Diagnostics.Debug.WriteLine($"查看学校统考详情: {exam.Name}");
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
                await _authenticationService.RefreshUserInfoAsync();
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
            if (now < exam.StartTime.Value)
            {
                return "即将开始";
            }
            else if (now > exam.EndTime.Value)
            {
                return "联考已结束";
            }
            else
            {
                return "联考正在进行中";
            }
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
            if (exam.Status == "Published" || exam.Status == "Scheduled" || exam.Status == "InProgress")
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

    #endregion
}
