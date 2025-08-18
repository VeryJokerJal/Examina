using System.Collections.ObjectModel;
using System.Reactive;
using Examina.Models.Exam;
using Examina.Services;
using ReactiveUI;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 综合训练列表ViewModel
/// </summary>
public class ComprehensiveTrainingListViewModel : ViewModelBase
{
    private readonly IStudentComprehensiveTrainingService _studentComprehensiveTrainingService;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private int _totalCount;
    private int _currentPage = 1;
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
    /// 刷新命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    /// <summary>
    /// 加载更多命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadMoreCommand { get; }

    /// <summary>
    /// 查看详情命令
    /// </summary>
    public ReactiveCommand<StudentComprehensiveTrainingDto, Unit> ViewDetailsCommand { get; }

    public ComprehensiveTrainingListViewModel(IStudentComprehensiveTrainingService studentComprehensiveTrainingService)
    {
        _studentComprehensiveTrainingService = studentComprehensiveTrainingService;

        // 创建命令
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        LoadMoreCommand = ReactiveCommand.CreateFromTask(LoadMoreAsync, this.WhenAnyValue(x => x.HasMoreData, x => x.IsLoading, (hasMore, loading) => hasMore && !loading));
        ViewDetailsCommand = ReactiveCommand.CreateFromTask<StudentComprehensiveTrainingDto>(ViewDetailsAsync);

        // 初始加载
        _ = Task.Run(RefreshAsync);
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
    /// 查看综合训练详情
    /// </summary>
    private async Task ViewDetailsAsync(StudentComprehensiveTrainingDto training)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"查看综合训练详情: {training.Name} (ID: {training.Id})");

            // 检查权限
            bool hasAccess = await _studentComprehensiveTrainingService.HasAccessToTrainingAsync(training.Id);
            if (!hasAccess)
            {
                ErrorMessage = "您没有权限访问此综合训练";
                return;
            }

            // 获取详细信息
            StudentComprehensiveTrainingDto? details = await _studentComprehensiveTrainingService.GetTrainingDetailsAsync(training.Id);
            if (details == null)
            {
                ErrorMessage = "无法获取综合训练详情";
                return;
            }

            // TODO: 导航到综合训练详情页面
            // 这里可以通过导航服务或事件来通知主窗口切换到详情页面
            System.Diagnostics.Debug.WriteLine($"综合训练详情加载成功: {details.Name}，包含 {details.Subjects.Count} 个科目，{details.Modules.Count} 个模块");
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("查看综合训练详情失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "获取综合训练详情失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"查看综合训练详情失败: {ex.Message}");
        }
    }
}
