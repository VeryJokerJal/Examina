using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using ReactiveUI.Fody.Helpers;
using Examina.Models;
using Examina.Models.Api;
using Examina.Services;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 概览页面视图模型
/// </summary>
public class OverviewViewModel : ViewModelBase
{
    private readonly IStudentComprehensiveTrainingService? _comprehensiveTrainingService;
    private readonly IStudentExamService? _studentExamService;
    private readonly IStudentMockExamService? _studentMockExamService;

    #region 属性

    /// <summary>
    /// 页面标题
    /// </summary>
    [Reactive]
    public string PageTitle { get; set; } = "概览";

    /// <summary>
    /// 欢迎消息
    /// </summary>
    [Reactive]
    public string WelcomeMessage { get; set; } = "欢迎使用Examina考试练习系统";

    /// <summary>
    /// 模拟考试次数
    /// </summary>
    [Reactive]
    public int MockExamCount { get; set; } = 0;

    /// <summary>
    /// 是否有模拟考试记录
    /// </summary>
    [Reactive]
    public bool HasMockExamRecords { get; set; } = false;

    /// <summary>
    /// 综合实训次数
    /// </summary>
    [Reactive]
    public int ComprehensiveTrainingCount { get; set; } = 0;

    /// <summary>
    /// 专项练习次数
    /// </summary>
    [Reactive]
    public int SpecialPracticeCount { get; set; } = 0;

    /// <summary>
    /// 上机统考次数
    /// </summary>
    [Reactive]
    public int OnlineExamCount { get; set; } = 0;

    /// <summary>
    /// 当前选中的统计类型
    /// </summary>
    [Reactive]
    public StatisticType SelectedStatisticType { get; set; } = StatisticType.ComprehensiveTraining;

    /// <summary>
    /// 当前显示的成绩列表
    /// </summary>
    [Reactive]
    public ObservableCollection<TrainingRecord> DisplayedRecords { get; set; } = [];

    /// <summary>
    /// 所有成绩记录（用于筛选）
    /// </summary>
    private readonly ObservableCollection<TrainingRecord> _allRecords = [];

    /// <summary>
    /// 综合实训总数
    /// </summary>
    [Reactive]
    public int ComprehensiveTrainingTotalCount { get; set; } = 0;

    /// <summary>
    /// 综合实训已完成数量
    /// </summary>
    [Reactive]
    public int ComprehensiveTrainingCompletedCount { get; set; } = 0;

    /// <summary>
    /// 综合实训完成百分比
    /// </summary>
    [Reactive]
    public double ComprehensiveTrainingCompletionPercentage { get; set; } = 0;

    /// <summary>
    /// 综合实训进度文本
    /// </summary>
    [Reactive]
    public string ComprehensiveTrainingProgressText { get; set; } = "0/0";

    /// <summary>
    /// 是否正在加载综合实训进度
    /// </summary>
    [Reactive]
    public bool IsLoadingComprehensiveTrainingProgress { get; set; } = false;

    /// <summary>
    /// 专项练习总数
    /// </summary>
    [Reactive]
    public int SpecialPracticeTotalCount { get; set; } = 0;

    /// <summary>
    /// 专项练习已完成数量
    /// </summary>
    [Reactive]
    public int SpecialPracticeCompletedCount { get; set; } = 0;

    /// <summary>
    /// 专项练习完成百分比
    /// </summary>
    [Reactive]
    public double SpecialPracticeCompletionPercentage { get; set; } = 0;

    /// <summary>
    /// 专项练习进度文本
    /// </summary>
    [Reactive]
    public string SpecialPracticeProgressText { get; set; } = "0/0";

    /// <summary>
    /// 是否正在加载专项练习进度
    /// </summary>
    [Reactive]
    public bool IsLoadingSpecialPracticeProgress { get; set; } = false;



    /// <summary>
    /// 是否正在加载成绩记录
    /// </summary>
    [Reactive]
    public bool IsLoadingRecords { get; set; } = false;

    #endregion

    #region 命令

    /// <summary>
    /// 选择统计类型命令
    /// </summary>
    public ICommand SelectStatisticTypeCommand { get; }

    #endregion

    #region 构造函数

    public OverviewViewModel()
    {
        SelectStatisticTypeCommand = new DelegateCommand<object>(SelectStatisticType);
        LoadOverviewData();
    }

    public OverviewViewModel(IStudentComprehensiveTrainingService comprehensiveTrainingService)
    {
        _comprehensiveTrainingService = comprehensiveTrainingService;
        _studentExamService = null;
        SelectStatisticTypeCommand = new DelegateCommand<object>(SelectStatisticType);
        LoadOverviewData();
        _ = LoadComprehensiveTrainingProgressAsync();
        _ = LoadSpecialPracticeProgressAsync();
    }

    public OverviewViewModel(IStudentComprehensiveTrainingService comprehensiveTrainingService, IStudentExamService studentExamService)
    {
        _comprehensiveTrainingService = comprehensiveTrainingService;
        _studentExamService = studentExamService;
        _studentMockExamService = null;
        SelectStatisticTypeCommand = new DelegateCommand<object>(SelectStatisticType);
        LoadOverviewData();
        _ = LoadComprehensiveTrainingProgressAsync();
        _ = LoadSpecialPracticeProgressAsync();
    }

    public OverviewViewModel(IStudentComprehensiveTrainingService comprehensiveTrainingService, IStudentExamService studentExamService, IStudentMockExamService studentMockExamService)
    {
        _comprehensiveTrainingService = comprehensiveTrainingService;
        _studentExamService = studentExamService;
        _studentMockExamService = studentMockExamService;
        SelectStatisticTypeCommand = new DelegateCommand<object>(SelectStatisticType);
        LoadOverviewData();
        _ = LoadComprehensiveTrainingProgressAsync();
        _ = LoadSpecialPracticeProgressAsync();
    }

    #endregion

    #region 方法

    /// <summary>
    /// 加载概览数据
    /// </summary>
    private void LoadOverviewData()
    {
        // 设置默认值
        ComprehensiveTrainingCount = 5;
        SpecialPracticeCount = 60;
        OnlineExamCount = 0;

        // 异步加载实际数据
        _ = Task.Run(async () =>
        {
            await LoadMockExamCountAsync();
            await LoadAllRecordsAsync();

            // 在UI线程上更新显示数据
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                FilterRecordsByType();
            });
        });
    }

    /// <summary>
    /// 加载模拟考试数量
    /// </summary>
    private async Task LoadMockExamCountAsync()
    {
        try
        {
            if (_studentMockExamService != null)
            {
                int completedCount = await _studentMockExamService.GetCompletedMockExamCountAsync();

                // 在UI线程上更新属性
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    MockExamCount = completedCount;
                    System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 已完成模拟考试数量: {completedCount}");
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("OverviewViewModel: 模拟考试服务未注入，使用默认值");
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    MockExamCount = 0;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 加载模拟考试数量异常: {ex.Message}");
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                MockExamCount = 0;
            });
        }
    }

    /// <summary>
    /// 加载综合实训进度数据
    /// </summary>
    private async Task LoadComprehensiveTrainingProgressAsync()
    {
        if (_comprehensiveTrainingService == null)
        {
            System.Diagnostics.Debug.WriteLine("OverviewViewModel: 综合实训服务未注入，跳过进度加载");
            return;
        }

        try
        {
            IsLoadingComprehensiveTrainingProgress = true;
            System.Diagnostics.Debug.WriteLine("OverviewViewModel: 开始加载综合实训进度");

            ComprehensiveTrainingProgressDto progress = await _comprehensiveTrainingService.GetTrainingProgressAsync();

            ComprehensiveTrainingTotalCount = progress.TotalCount;
            ComprehensiveTrainingCompletedCount = progress.CompletedCount;
            ComprehensiveTrainingCompletionPercentage = progress.CompletionPercentage;
            ComprehensiveTrainingProgressText = $"{progress.CompletedCount}/{progress.TotalCount}";

            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 综合实训进度加载成功 - 总数: {progress.TotalCount}, 完成: {progress.CompletedCount}, 百分比: {progress.CompletionPercentage}%");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 加载综合实训进度失败: {ex.Message}");

            // 设置默认值
            ComprehensiveTrainingTotalCount = 0;
            ComprehensiveTrainingCompletedCount = 0;
            ComprehensiveTrainingCompletionPercentage = 0;
            ComprehensiveTrainingProgressText = "0/0";
        }
        finally
        {
            IsLoadingComprehensiveTrainingProgress = false;
        }
    }

    /// <summary>
    /// 加载专项练习进度数据
    /// </summary>
    private async Task LoadSpecialPracticeProgressAsync()
    {
        if (_studentExamService == null)
        {
            System.Diagnostics.Debug.WriteLine("OverviewViewModel: 学生考试服务未注入，跳过专项练习进度加载");
            return;
        }

        try
        {
            IsLoadingSpecialPracticeProgress = true;
            System.Diagnostics.Debug.WriteLine("OverviewViewModel: 开始加载专项练习进度");

            SpecialPracticeProgressDto progress = await _studentExamService.GetSpecialPracticeProgressAsync();

            SpecialPracticeTotalCount = progress.TotalCount;
            SpecialPracticeCompletedCount = progress.CompletedCount;
            SpecialPracticeCompletionPercentage = progress.CompletionPercentage;
            SpecialPracticeProgressText = $"{progress.CompletedCount}/{progress.TotalCount}";

            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 专项练习进度加载成功 - 总数: {progress.TotalCount}, 完成: {progress.CompletedCount}, 百分比: {progress.CompletionPercentage}%");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 加载专项练习进度失败: {ex.Message}");

            // 设置默认值
            SpecialPracticeTotalCount = 0;
            SpecialPracticeCompletedCount = 0;
            SpecialPracticeCompletionPercentage = 0;
            SpecialPracticeProgressText = "0/0";
        }
        finally
        {
            IsLoadingSpecialPracticeProgress = false;
        }
    }



    /// <summary>
    /// 刷新综合训练进度
    /// </summary>
    public async Task RefreshComprehensiveTrainingProgressAsync()
    {
        System.Diagnostics.Debug.WriteLine("OverviewViewModel: 开始刷新综合训练进度");
        await LoadComprehensiveTrainingProgressAsync();
    }

    /// <summary>
    /// 刷新专项练习进度
    /// </summary>
    public async Task RefreshSpecialPracticeProgressAsync()
    {
        System.Diagnostics.Debug.WriteLine("OverviewViewModel: 开始刷新专项练习进度");
        await LoadSpecialPracticeProgressAsync();
    }

    /// <summary>
    /// 刷新成绩记录
    /// </summary>
    public async Task RefreshRecordsAsync()
    {
        System.Diagnostics.Debug.WriteLine("OverviewViewModel: 开始刷新成绩记录");

        IsLoadingRecords = true;

        try
        {
            await LoadAllRecordsAsync();
            FilterRecordsByType();
            System.Diagnostics.Debug.WriteLine("OverviewViewModel: 成绩记录刷新完成");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 刷新成绩记录异常: {ex.Message}");
        }
        finally
        {
            IsLoadingRecords = false;
        }
    }

    /// <summary>
    /// 刷新统计数据
    /// </summary>
    public async Task RefreshStatisticsAsync()
    {
        System.Diagnostics.Debug.WriteLine("OverviewViewModel: 开始刷新统计数据");

        try
        {
            await LoadMockExamCountAsync();
            System.Diagnostics.Debug.WriteLine("OverviewViewModel: 统计数据刷新完成");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 刷新统计数据异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 刷新所有数据
    /// </summary>
    public async Task RefreshAllDataAsync()
    {
        System.Diagnostics.Debug.WriteLine("OverviewViewModel: 开始刷新所有数据");

        try
        {
            await LoadMockExamCountAsync();
            await RefreshRecordsAsync();
            System.Diagnostics.Debug.WriteLine("OverviewViewModel: 所有数据刷新完成");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 刷新所有数据异常: {ex.Message}");
        }
    }







    /// <summary>
    /// 加载所有成绩数据
    /// </summary>
    private async Task LoadAllRecordsAsync()
    {
        _allRecords.Clear();

        try
        {
            // 加载综合训练完成记录
            await LoadComprehensiveTrainingRecordsAsync();

            // 加载专项练习完成记录
            await LoadSpecialPracticeRecordsAsync();

            // 加载模拟考试完成记录
            await LoadMockExamRecordsAsync();

            // 上机统考数据（暂无API）

            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 成功加载 {_allRecords.Count} 条成绩记录");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 加载成绩记录异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载综合训练完成记录
    /// </summary>
    private async Task LoadComprehensiveTrainingRecordsAsync()
    {
        if (_comprehensiveTrainingService == null)
        {
            System.Diagnostics.Debug.WriteLine("OverviewViewModel: 综合训练服务未注入，无法加载记录");
            return;
        }

        try
        {
            List<ComprehensiveTrainingCompletionDto> completions = await _comprehensiveTrainingService.GetComprehensiveTrainingCompletionsAsync(1, 50);

            foreach (ComprehensiveTrainingCompletionDto completion in completions)
            {
                if (completion.Status == ComprehensiveTrainingCompletionStatus.Completed)
                {
                    _allRecords.Add(new TrainingRecord
                    {
                        Name = completion.TrainingName,
                        Duration = completion.DurationText,
                        CompletionTime = completion.CompletedAt ?? completion.UpdatedAt,
                        Score = (int)(completion.Score ?? 0),
                        Type = StatisticType.ComprehensiveTraining
                    });
                }
            }

            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 加载了 {completions.Count} 条综合训练记录");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 加载综合训练记录异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载专项练习完成记录
    /// </summary>
    private async Task LoadSpecialPracticeRecordsAsync()
    {
        if (_studentExamService == null)
        {
            System.Diagnostics.Debug.WriteLine("OverviewViewModel: 学生考试服务未注入，无法加载记录");
            return;
        }

        try
        {
            List<SpecialPracticeCompletionDto> completions = await _studentExamService.GetSpecialPracticeCompletionsAsync(1, 50);

            foreach (SpecialPracticeCompletionDto completion in completions)
            {
                if (completion.Status == SpecialPracticeCompletionStatus.Completed)
                {
                    _allRecords.Add(new TrainingRecord
                    {
                        Name = completion.PracticeName,
                        Duration = completion.DurationText,
                        CompletionTime = completion.CompletedAt ?? completion.UpdatedAt,
                        Score = (int)(completion.Score ?? 0),
                        Type = StatisticType.SpecialPractice
                    });
                }
            }

            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 加载了 {completions.Count} 条专项练习记录");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 加载专项练习记录异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 选择统计类型
    /// </summary>
    private void SelectStatisticType(object? parameter)
    {
        if (parameter is string typeString && Enum.TryParse<StatisticType>(typeString, out StatisticType type))
        {
            SelectedStatisticType = type;
            FilterRecordsByType();
        }
    }

    /// <summary>
    /// 加载模拟考试完成记录
    /// </summary>
    private async Task LoadMockExamRecordsAsync()
    {
        if (_studentMockExamService == null)
        {
            System.Diagnostics.Debug.WriteLine("OverviewViewModel: 模拟考试服务为null，跳过加载模拟考试记录");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine("OverviewViewModel: 开始加载模拟考试完成记录");

            List<MockExamCompletionDto> completions = await _studentMockExamService.GetMockExamCompletionsAsync(1, 50);

            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 获取到 {completions.Count} 条模拟考试完成记录");

            int completedCount = 0;
            foreach (MockExamCompletionDto completion in completions)
            {
                if (completion.Status == MockExamCompletionStatus.Completed)
                {
                    _allRecords.Add(new TrainingRecord
                    {
                        Name = completion.MockExamName,
                        Duration = completion.DurationText ?? "未知",
                        CompletionTime = completion.CompletedAt ?? completion.CreatedAt,
                        Score = (int)(completion.Score ?? 0),
                        Type = StatisticType.MockExam,
                        FormattedCompletionTime = completion.FormattedCompletedAt
                    });
                    completedCount++;
                }
            }

            // 更新是否有模拟考试记录的状态
            HasMockExamRecords = completedCount > 0;

            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 成功加载 {completedCount} 条模拟考试记录");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OverviewViewModel: 加载模拟考试记录异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据类型筛选记录
    /// </summary>
    private void FilterRecordsByType()
    {
        DisplayedRecords.Clear();

        List<TrainingRecord> filteredRecords = _allRecords.Where(r => r.Type == SelectedStatisticType).ToList();

        foreach (TrainingRecord? record in filteredRecords)
        {
            DisplayedRecords.Add(record);
        }
    }

    #endregion
}

/// <summary>
/// 统计类型枚举
/// </summary>
public enum StatisticType
{
    /// <summary>
    /// 模拟考试
    /// </summary>
    MockExam,

    /// <summary>
    /// 综合实训
    /// </summary>
    ComprehensiveTraining,

    /// <summary>
    /// 专项练习
    /// </summary>
    SpecialPractice,

    /// <summary>
    /// 上机统考
    /// </summary>
    OnlineExam
}

/// <summary>
/// 实训记录
/// </summary>
public class TrainingRecord
{
    /// <summary>
    /// 实训名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 用时
    /// </summary>
    public string Duration { get; set; } = string.Empty;

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime CompletionTime { get; set; }

    /// <summary>
    /// 得分
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 记录类型
    /// </summary>
    public StatisticType Type { get; set; }

    /// <summary>
    /// 格式化的完成时间
    /// </summary>
    public string FormattedCompletionTime => CompletionTime.ToString("yyyy年MM月dd日 HH时mm分ss秒");
}
