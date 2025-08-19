using System.Collections.ObjectModel;
using System.Reactive;
using Examina.Models;
using Examina.Models.Exam;
using Examina.Services;
using ReactiveUI;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 考试列表ViewModel
/// </summary>
public class ExamListViewModel : ViewModelBase
{
    private readonly IStudentExamService _studentExamService;
    private readonly IAuthenticationService _authenticationService;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private int _totalCount;
    private int _currentPage = 1;
    private bool _hasFullAccess;
    private const int PageSize = 20;

    /// <summary>
    /// 考试列表
    /// </summary>
    public ObservableCollection<StudentExamDto> Exams { get; } = [];

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount
    {
        get => _totalCount;
        set => this.RaiseAndSetIfChanged(ref _totalCount, value);
    }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    /// <summary>
    /// 是否有更多数据
    /// </summary>
    public bool HasMoreData => Exams.Count < TotalCount;

    /// <summary>
    /// 用户是否拥有完整功能权限
    /// </summary>
    public bool HasFullAccess
    {
        get => _hasFullAccess;
        set => this.RaiseAndSetIfChanged(ref _hasFullAccess, value);
    }

    /// <summary>
    /// 开始考试按钮文本
    /// </summary>
    public string StartButtonText => HasFullAccess ? "开始考试" : "解锁";

    /// <summary>
    /// 刷新命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    /// <summary>
    /// 加载更多命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadMoreCommand { get; }

    /// <summary>
    /// 开始考试命令
    /// </summary>
    public ReactiveCommand<StudentExamDto, Unit> StartExamCommand { get; }


    public ExamListViewModel(IStudentExamService studentExamService, IAuthenticationService authenticationService)
    {
        _studentExamService = studentExamService;
        _authenticationService = authenticationService;

        // 创建命令
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        LoadMoreCommand = ReactiveCommand.CreateFromTask(LoadMoreAsync, this.WhenAnyValue(x => x.HasMoreData, x => x.IsLoading, (hasMore, loading) => hasMore && !loading));
        StartExamCommand = ReactiveCommand.CreateFromTask<StudentExamDto>(StartExamAsync);

        // 初始化用户权限状态
        _ = Task.Run(UpdateUserPermissionsAsync);

        // 监听用户信息更新事件
        _authenticationService.UserInfoUpdated += OnUserInfoUpdated;

        // 初始加载
        _ = Task.Run(RefreshAsync);
    }

    private void OnUserInfoUpdated(object? sender, UserInfo? e)
    {
        _ = Task.Run(UpdateUserPermissionsAsync);
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            CurrentPage = 1;

            // 获取总数
            TotalCount = await _studentExamService.GetAvailableExamCountAsync();

            // 获取第一页数据
            List<StudentExamDto> exams = await _studentExamService.GetAvailableExamsAsync(CurrentPage, PageSize);

            // 更新UI
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Exams.Clear();
                foreach (StudentExamDto exam in exams)
                {
                    Exams.Add(exam);
                }
            });

            System.Diagnostics.Debug.WriteLine($"刷新考试列表成功，共 {TotalCount} 项，当前显示 {Exams.Count} 项");
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("获取考试列表失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "加载考试列表失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"刷新考试列表失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 加载更多数据
    /// </summary>
    private async Task LoadMoreAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            int nextPage = CurrentPage + 1;
            List<StudentExamDto> exams = await _studentExamService.GetAvailableExamsAsync(nextPage, PageSize);

            if (exams.Count > 0)
            {
                // 更新UI
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (StudentExamDto exam in exams)
                    {
                        Exams.Add(exam);
                    }
                });

                CurrentPage = nextPage;
                System.Diagnostics.Debug.WriteLine($"加载更多考试成功，当前页: {CurrentPage}，总显示: {Exams.Count} 项");
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("加载更多考试失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "加载更多数据失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"加载更多考试失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 开始考试
    /// </summary>
    private async Task StartExamAsync(StudentExamDto exam)
    {
        try
        {
            if (HasFullAccess)
            {
                // 用户有完整权限，开始考试
                System.Diagnostics.Debug.WriteLine($"开始考试: {exam.Name} (ID: {exam.Id})");

                // 检查权限
                bool hasAccess = await _studentExamService.HasAccessToExamAsync(exam.Id);
                if (!hasAccess)
                {
                    ErrorMessage = "您没有权限访问此考试";
                    return;
                }

                // TODO: 实现开始考试逻辑
                // 这里应该导航到考试页面或启动考试
                System.Diagnostics.Debug.WriteLine($"考试 {exam.Name} 已开始");
            }
            else
            {
                // 用户没有完整权限，显示解锁提示
                ErrorMessage = "您需要解锁权限才能开始考试。请加入学校组织或联系管理员进行解锁。";
                System.Diagnostics.Debug.WriteLine("用户尝试开始考试但没有完整权限");
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("开始考试失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "开始考试失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"开始考试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新用户权限状态
    /// </summary>
    private async Task UpdateUserPermissionsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("ExamListViewModel: 开始更新用户权限状态");

            // 主动刷新用户信息以获取最新状态
            bool refreshSuccess = await _authenticationService.RefreshUserInfoAsync();

            if (refreshSuccess)
            {
                // 刷新成功，获取最新的用户信息
                UserInfo? currentUser = _authenticationService.CurrentUser;
                HasFullAccess = currentUser?.HasFullAccess ?? false;

                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 用户信息刷新成功 - HasFullAccess: {HasFullAccess}");
            }
            else
            {
                // 刷新失败，使用当前缓存的用户信息
                UserInfo? currentUser = _authenticationService.CurrentUser;
                HasFullAccess = currentUser?.HasFullAccess ?? false;

                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 用户信息刷新失败，使用缓存信息 - HasFullAccess: {HasFullAccess}");
            }

            // 通知UI更新按钮文本
            this.RaisePropertyChanged(nameof(StartButtonText));
        }
        catch (Exception ex)
        {
            // 异常处理：使用默认值
            HasFullAccess = false;
            this.RaisePropertyChanged(nameof(StartButtonText));

            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 更新用户权限状态异常: {ex.Message}");
        }
    }
}
