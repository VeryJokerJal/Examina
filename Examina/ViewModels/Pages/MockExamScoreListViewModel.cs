using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using ReactiveUI.Fody.Helpers;
using Examina.Models.Api;
using Examina.Services;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 模拟考试成绩列表视图模型
/// </summary>
public class MockExamScoreListViewModel : ViewModelBase
{
    private readonly IStudentMockExamService? _studentMockExamService;

    #region 属性

    /// <summary>
    /// 模拟考试成绩列表
    /// </summary>
    [Reactive]
    public ObservableCollection<MockExamCompletionDto> MockExamScores { get; set; } = [];

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [Reactive]
    public bool IsLoading { get; set; } = false;

    /// <summary>
    /// 是否有错误
    /// </summary>
    [Reactive]
    public bool HasError { get; set; } = false;

    /// <summary>
    /// 错误消息
    /// </summary>
    [Reactive]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 是否有成绩记录
    /// </summary>
    [Reactive]
    public bool HasScores { get; set; } = false;

    /// <summary>
    /// 当前页码
    /// </summary>
    [Reactive]
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// 页大小
    /// </summary>
    [Reactive]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 总记录数
    /// </summary>
    [Reactive]
    public int TotalCount { get; set; } = 0;

    /// <summary>
    /// 是否可以转到上一页
    /// </summary>
    [Reactive]
    public bool CanGoPreviousPage { get; set; } = false;

    /// <summary>
    /// 是否可以转到下一页
    /// </summary>
    [Reactive]
    public bool CanGoNextPage { get; set; } = false;

    #endregion

    #region 命令

    /// <summary>
    /// 刷新命令
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// 上一页命令
    /// </summary>
    public ICommand PreviousPageCommand { get; }

    /// <summary>
    /// 下一页命令
    /// </summary>
    public ICommand NextPageCommand { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    public MockExamScoreListViewModel(IStudentMockExamService? studentMockExamService = null)
    {
        _studentMockExamService = studentMockExamService;

        // 初始化命令
        RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
        PreviousPageCommand = new DelegateCommand(async () => await GoToPreviousPageAsync());
        NextPageCommand = new DelegateCommand(async () => await GoToNextPageAsync());

        // 初始化加载数据
        _ = Task.Run(async () => await RefreshAsync());
    }

    #endregion

    #region 方法

    /// <summary>
    /// 刷新数据
    /// </summary>
    public async Task RefreshAsync()
    {
        await LoadMockExamScoresAsync(CurrentPage);
    }

    /// <summary>
    /// 转到上一页
    /// </summary>
    public async Task GoToPreviousPageAsync()
    {
        if (CanGoPreviousPage)
        {
            CurrentPage--;
            await LoadMockExamScoresAsync(CurrentPage);
        }
    }

    /// <summary>
    /// 转到下一页
    /// </summary>
    public async Task GoToNextPageAsync()
    {
        if (CanGoNextPage)
        {
            CurrentPage++;
            await LoadMockExamScoresAsync(CurrentPage);
        }
    }

    /// <summary>
    /// 加载模拟考试成绩数据
    /// </summary>
    private async Task LoadMockExamScoresAsync(int pageNumber)
    {
        if (_studentMockExamService == null)
        {
            HasError = true;
            ErrorMessage = "服务未初始化，无法加载成绩数据";
            System.Diagnostics.Debug.WriteLine("MockExamScoreListViewModel: 学生模拟考试服务未注入");
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            System.Diagnostics.Debug.WriteLine($"MockExamScoreListViewModel: 开始加载模拟考试成绩，页码: {pageNumber}");

            List<MockExamCompletionDto> scores = await _studentMockExamService.GetMockExamCompletionsAsync(pageNumber, PageSize);

            MockExamScores.Clear();
            foreach (MockExamCompletionDto score in scores)
            {
                MockExamScores.Add(score);
            }

            HasScores = MockExamScores.Count > 0;
            TotalCount = MockExamScores.Count; // 注意：这里只是当前页的数量，实际应该从API返回总数

            // 更新分页状态
            CanGoPreviousPage = CurrentPage > 1;
            CanGoNextPage = MockExamScores.Count >= PageSize; // 简单判断，如果当前页满了就可能有下一页

            System.Diagnostics.Debug.WriteLine($"MockExamScoreListViewModel: 成功加载模拟考试成绩，数量: {MockExamScores.Count}");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"加载成绩数据失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"MockExamScoreListViewModel: 加载模拟考试成绩异常: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}
