using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Examina.Models.MockExam;
using Examina.Services;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 模拟考试列表视图模型
/// </summary>
public class MockExamListViewModel : ViewModelBase
{
    private readonly IStudentMockExamService _mockExamService;

    private bool _isLoading;
    private string? _errorMessage;
    private int _totalCount;
    private bool _hasMoreData;
    private int _currentPage = 1;
    private const int PageSize = 20;

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
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    /// <summary>
    /// 模拟考试总数
    /// </summary>
    public int TotalCount
    {
        get => _totalCount;
        set => this.RaiseAndSetIfChanged(ref _totalCount, value);
    }

    /// <summary>
    /// 是否有更多数据
    /// </summary>
    public bool HasMoreData
    {
        get => _hasMoreData;
        set => this.RaiseAndSetIfChanged(ref _hasMoreData, value);
    }

    /// <summary>
    /// 模拟考试列表
    /// </summary>
    public ObservableCollection<StudentMockExamDto> MockExams { get; } = [];

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
    public ReactiveCommand<StudentMockExamDto, Unit> ViewDetailsCommand { get; }

    /// <summary>
    /// 开始考试命令
    /// </summary>
    public ReactiveCommand<StudentMockExamDto, Unit> StartExamCommand { get; }

    /// <summary>
    /// 删除考试命令
    /// </summary>
    public ReactiveCommand<StudentMockExamDto, Unit> DeleteExamCommand { get; }

    /// <summary>
    /// 创建模拟考试命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> CreateMockExamCommand { get; }

    public MockExamListViewModel(IStudentMockExamService mockExamService)
    {
        _mockExamService = mockExamService;

        // 初始化命令
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        LoadMoreCommand = ReactiveCommand.CreateFromTask(LoadMoreAsync, this.WhenAnyValue(x => x.HasMoreData));
        ViewDetailsCommand = ReactiveCommand.CreateFromTask<StudentMockExamDto>(ViewDetailsAsync);
        StartExamCommand = ReactiveCommand.CreateFromTask<StudentMockExamDto>(StartExamAsync);
        DeleteExamCommand = ReactiveCommand.CreateFromTask<StudentMockExamDto>(DeleteExamAsync);
        CreateMockExamCommand = ReactiveCommand.Create(CreateMockExam);

        // 自动加载数据
        _ = LoadInitialDataAsync();
    }

    /// <summary>
    /// 加载初始数据
    /// </summary>
    private async Task LoadInitialDataAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 开始加载初始数据");

            // 加载总数
            TotalCount = await _mockExamService.GetStudentMockExamCountAsync();
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 模拟考试总数: {TotalCount}");

            // 加载第一页数据
            _currentPage = 1;
            List<StudentMockExamDto> mockExams = await _mockExamService.GetStudentMockExamsAsync(_currentPage, PageSize);

            MockExams.Clear();
            foreach (StudentMockExamDto mockExam in mockExams)
            {
                MockExams.Add(mockExam);
            }

            // 检查是否有更多数据
            HasMoreData = mockExams.Count == PageSize && MockExams.Count < TotalCount;

            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 加载完成，当前显示 {MockExams.Count} 项，总共 {TotalCount} 项");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 加载初始数据异常: {ex.Message}");
            ErrorMessage = "加载模拟考试列表失败，请稍后重试";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    private async Task RefreshAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 开始刷新数据");
            await LoadInitialDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 刷新数据异常: {ex.Message}");
            ErrorMessage = "刷新失败，请稍后重试";
        }
    }

    /// <summary>
    /// 加载更多数据
    /// </summary>
    private async Task LoadMoreAsync()
    {
        try
        {
            if (!HasMoreData || IsLoading)
            {
                return;
            }

            IsLoading = true;
            ErrorMessage = null;

            _currentPage++;
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 加载第 {_currentPage} 页数据");

            List<StudentMockExamDto> mockExams = await _mockExamService.GetStudentMockExamsAsync(_currentPage, PageSize);

            foreach (StudentMockExamDto mockExam in mockExams)
            {
                MockExams.Add(mockExam);
            }

            // 检查是否还有更多数据
            HasMoreData = mockExams.Count == PageSize && MockExams.Count < TotalCount;

            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 加载更多完成，当前显示 {MockExams.Count} 项");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 加载更多数据异常: {ex.Message}");
            ErrorMessage = "加载更多数据失败，请稍后重试";
            _currentPage--; // 回退页码
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 查看详情
    /// </summary>
    private async Task ViewDetailsAsync(StudentMockExamDto mockExam)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 查看模拟考试详情，ID: {mockExam.Id}");

            StudentMockExamDto? details = await _mockExamService.GetMockExamDetailsAsync(mockExam.Id);
            if (details != null)
            {
                // TODO: 导航到详情页面或显示详情对话框
                System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 成功获取模拟考试详情，题目数量: {details.Questions.Count}");
            }
            else
            {
                ErrorMessage = "获取模拟考试详情失败";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 查看详情异常: {ex.Message}");
            ErrorMessage = "查看详情失败，请稍后重试";
        }
    }

    /// <summary>
    /// 开始考试
    /// </summary>
    private async Task StartExamAsync(StudentMockExamDto mockExam)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 开始模拟考试，ID: {mockExam.Id}");

            bool success = await _mockExamService.StartMockExamAsync(mockExam.Id);
            if (success)
            {
                // 更新本地状态
                mockExam.Status = "InProgress";
                mockExam.StartedAt = DateTime.UtcNow;

                // TODO: 导航到考试页面
                System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 成功开始模拟考试");
            }
            else
            {
                ErrorMessage = "开始模拟考试失败";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 开始考试异常: {ex.Message}");
            ErrorMessage = "开始考试失败，请稍后重试";
        }
    }

    /// <summary>
    /// 删除考试
    /// </summary>
    private async Task DeleteExamAsync(StudentMockExamDto mockExam)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 删除模拟考试，ID: {mockExam.Id}");

            bool success = await _mockExamService.DeleteMockExamAsync(mockExam.Id);
            if (success)
            {
                MockExams.Remove(mockExam);
                TotalCount--;
                System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 成功删除模拟考试");
            }
            else
            {
                ErrorMessage = "删除模拟考试失败";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 删除考试异常: {ex.Message}");
            ErrorMessage = "删除考试失败，请稍后重试";
        }
    }

    /// <summary>
    /// 创建模拟考试
    /// </summary>
    private void CreateMockExam()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 创建模拟考试");
            // TODO: 导航到创建模拟考试页面或显示创建对话框
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 创建模拟考试异常: {ex.Message}");
            ErrorMessage = "创建模拟考试失败，请稍后重试";
        }
    }
}
