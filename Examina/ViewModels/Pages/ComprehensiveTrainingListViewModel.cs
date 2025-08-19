using System.Collections.ObjectModel;
using System.Reactive;
using Examina.Models.Exam;
using Examina.Services;
using Examina.Models;
using ReactiveUI;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 综合训练列表ViewModel
/// </summary>
public class ComprehensiveTrainingListViewModel : ViewModelBase
{
    private readonly IStudentComprehensiveTrainingService _studentComprehensiveTrainingService;
    private readonly IAuthenticationService _authenticationService;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private int _totalCount;
    private int _currentPage = 1;
    private bool _hasFullAccess;
    private bool _isUpdatingPermissions = false;
    private const int PageSize = 20;

    /// <summary>
    /// 综合训练列表
    /// </summary>
    public ObservableCollection<StudentComprehensiveTrainingDto> Trainings { get; } = [];

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
    public bool HasMoreData => Trainings.Count < TotalCount;

    /// <summary>
    /// 用户是否拥有完整功能权限
    /// </summary>
    public bool HasFullAccess
    {
        get => _hasFullAccess;
        set => this.RaiseAndSetIfChanged(ref _hasFullAccess, value);
    }

    /// <summary>
    /// 开始训练按钮文本
    /// </summary>
    public string StartButtonText => HasFullAccess ? "开始训练" : "解锁";

    /// <summary>
    /// 刷新命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    /// <summary>
    /// 加载更多命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadMoreCommand { get; }

    /// <summary>
    /// 开始训练命令
    /// </summary>
    public ReactiveCommand<StudentComprehensiveTrainingDto, Unit> StartTrainingCommand { get; }

    public ComprehensiveTrainingListViewModel(IStudentComprehensiveTrainingService studentComprehensiveTrainingService, IAuthenticationService authenticationService)
    {
        _studentComprehensiveTrainingService = studentComprehensiveTrainingService;
        _authenticationService = authenticationService;

        // 创建命令
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        LoadMoreCommand = ReactiveCommand.CreateFromTask(LoadMoreAsync, this.WhenAnyValue(x => x.HasMoreData, x => x.IsLoading, (hasMore, loading) => hasMore && !loading));
        StartTrainingCommand = ReactiveCommand.CreateFromTask<StudentComprehensiveTrainingDto>(StartTrainingAsync);

        // 初始化用户权限状态
        _ = UpdateUserPermissionsAsync();

        // 监听用户信息更新事件
        _authenticationService.UserInfoUpdated += OnUserInfoUpdated;

        // 初始加载
        _ = RefreshAsync();
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
            TotalCount = await _studentComprehensiveTrainingService.GetAvailableTrainingCountAsync();

            // 获取第一页数据
            List<StudentComprehensiveTrainingDto> trainings = await _studentComprehensiveTrainingService.GetAvailableTrainingsAsync(CurrentPage, PageSize);

            // 更新UI
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Trainings.Clear();
                foreach (StudentComprehensiveTrainingDto training in trainings)
                {
                    Trainings.Add(training);
                }
            });

            System.Diagnostics.Debug.WriteLine($"刷新综合训练列表成功，共 {TotalCount} 项，当前显示 {Trainings.Count} 项");

            // 数据刷新完成后，强制更新用户权限状态
            try
            {
                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 刷新完成，开始更新用户权限状态");
                _ = UpdateUserPermissionsAsync();
            }
            catch (Exception permissionEx)
            {
                // 权限更新失败不应影响数据刷新的成功状态
                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 权限状态更新失败: {permissionEx.Message}");
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("获取综合训练列表失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "加载综合训练列表失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"刷新综合训练列表失败: {ex.Message}");
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
            List<StudentComprehensiveTrainingDto> trainings = await _studentComprehensiveTrainingService.GetAvailableTrainingsAsync(nextPage, PageSize);

            if (trainings.Count > 0)
            {
                // 更新UI
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (StudentComprehensiveTrainingDto training in trainings)
                    {
                        Trainings.Add(training);
                    }
                });

                CurrentPage = nextPage;
                System.Diagnostics.Debug.WriteLine($"加载更多综合训练成功，当前页: {CurrentPage}，总显示: {Trainings.Count} 项");
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("加载更多综合训练失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "加载更多数据失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"加载更多综合训练失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }



    /// <summary>
    /// 开始训练
    /// </summary>
    private async Task StartTrainingAsync(StudentComprehensiveTrainingDto training)
    {
        try
        {
            if (HasFullAccess)
            {
                // 用户有完整权限，开始训练
                System.Diagnostics.Debug.WriteLine($"开始综合训练: {training.Name} (ID: {training.Id})");

                // 检查权限
                bool hasAccess = await _studentComprehensiveTrainingService.HasAccessToTrainingAsync(training.Id);
                if (!hasAccess)
                {
                    ErrorMessage = "您没有权限访问此综合训练";
                    return;
                }

                // TODO: 实现开始训练逻辑
                // 这里应该导航到训练页面或启动训练
                System.Diagnostics.Debug.WriteLine($"综合训练 {training.Name} 已开始");
            }
            else
            {
                // 用户没有完整权限，显示解锁提示
                ErrorMessage = "您需要解锁权限才能开始训练。请加入学校组织或联系管理员进行解锁。";
                System.Diagnostics.Debug.WriteLine("用户尝试开始训练但没有完整权限");
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("开始综合训练失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "开始训练失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"开始综合训练失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新用户权限状态
    /// </summary>
    private async Task UpdateUserPermissionsAsync()
    {
        // 防重入机制：如果正在更新权限状态，则跳过
        if (_isUpdatingPermissions)
        {
            System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 权限状态正在更新中，跳过重复调用");
            return;
        }

        try
        {
            _isUpdatingPermissions = true;
            System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 开始更新用户权限状态");

            // 主动刷新用户信息以获取最新状态
            bool refreshSuccess = await _authenticationService.RefreshUserInfoAsync();

            if (refreshSuccess)
            {
                // 刷新成功，获取最新的用户信息
                UserInfo? currentUser = _authenticationService.CurrentUser;
                HasFullAccess = currentUser?.HasFullAccess ?? false;

                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 用户信息刷新成功 - HasFullAccess: {HasFullAccess}");
            }
            else
            {
                // 刷新失败，使用当前缓存的用户信息
                UserInfo? currentUser = _authenticationService.CurrentUser;
                HasFullAccess = currentUser?.HasFullAccess ?? false;

                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 用户信息刷新失败，使用缓存信息 - HasFullAccess: {HasFullAccess}");
            }

            // 通知UI更新按钮文本
            this.RaisePropertyChanged(nameof(StartButtonText));
        }
        catch (Exception ex)
        {
            // 异常处理：使用默认值
            HasFullAccess = false;
            this.RaisePropertyChanged(nameof(StartButtonText));

            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 更新用户权限状态异常: {ex.Message}");
        }
        finally
        {
            _isUpdatingPermissions = false;
            System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 权限状态更新完成");
        }
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    private void OnUserInfoUpdated(object? sender, UserInfo? userInfo)
    {
        _ = UpdateUserPermissionsAsync();
    }
}
