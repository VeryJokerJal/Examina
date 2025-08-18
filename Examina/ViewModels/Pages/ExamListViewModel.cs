using System.Collections.ObjectModel;
using ReactiveUI;
using Examina.Models.Exam;
using Examina.Services;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 考试列表ViewModel
/// </summary>
public class ExamListViewModel : ViewModelBase
{
    private readonly IStudentExamService _studentExamService;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private int _totalCount;
    private int _currentPage = 1;
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
    public ReactiveCommand<StudentExamDto, Unit> ViewDetailsCommand { get; }

    public ExamListViewModel(IStudentExamService studentExamService)
    {
        _studentExamService = studentExamService;

        // 创建命令
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        LoadMoreCommand = ReactiveCommand.CreateFromTask(LoadMoreAsync, this.WhenAnyValue(x => x.HasMoreData, x => x.IsLoading, (hasMore, loading) => hasMore && !loading));
        ViewDetailsCommand = ReactiveCommand.CreateFromTask<StudentExamDto>(ViewDetailsAsync);

        // 初始加载
        _ = Task.Run(async () => await RefreshAsync());
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
    /// 查看考试详情
    /// </summary>
    private async Task ViewDetailsAsync(StudentExamDto exam)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"查看考试详情: {exam.Name} (ID: {exam.Id})");

            // 检查权限
            bool hasAccess = await _studentExamService.HasAccessToExamAsync(exam.Id);
            if (!hasAccess)
            {
                ErrorMessage = "您没有权限访问此考试";
                return;
            }

            // 获取详细信息
            StudentExamDto? details = await _studentExamService.GetExamDetailsAsync(exam.Id);
            if (details == null)
            {
                ErrorMessage = "无法获取考试详情";
                return;
            }

            // TODO: 导航到考试详情页面
            // 这里可以通过导航服务或事件来通知主窗口切换到详情页面
            System.Diagnostics.Debug.WriteLine($"考试详情加载成功: {details.Name}，包含 {details.Subjects.Count} 个科目，{details.Modules.Count} 个模块");
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("查看考试详情失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "获取考试详情失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"查看考试详情失败: {ex.Message}");
        }
    }
}
