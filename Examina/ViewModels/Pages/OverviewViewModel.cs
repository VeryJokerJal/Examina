using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using ReactiveUI.Fody.Helpers;
using Examina.Models;
using Examina.Services;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 概览页面视图模型
/// </summary>
public class OverviewViewModel : ViewModelBase
{
    private readonly IStudentComprehensiveTrainingService? _comprehensiveTrainingService;

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
        SelectStatisticTypeCommand = new DelegateCommand<object>(SelectStatisticType);
        LoadOverviewData();
        _ = LoadComprehensiveTrainingProgressAsync();
    }

    #endregion

    #region 方法

    /// <summary>
    /// 加载概览数据
    /// </summary>
    private void LoadOverviewData()
    {
        // TODO: 从服务加载实际数据
        MockExamCount = 0;
        ComprehensiveTrainingCount = 5;
        SpecialPracticeCount = 60;
        OnlineExamCount = 0;

        // 加载所有成绩数据
        LoadAllRecords();

        // 根据默认选择筛选显示数据
        FilterRecordsByType();
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
    /// 加载所有成绩数据
    /// </summary>
    private void LoadAllRecords()
    {
        _allRecords.Clear();

        // 综合实训数据
        _allRecords.Add(new TrainingRecord
        {
            Name = "综合实训十二",
            Duration = "1小时23分钟",
            CompletionTime = new DateTime(2025, 7, 28, 17, 23, 13),
            Score = 134,
            Type = StatisticType.ComprehensiveTraining
        });

        _allRecords.Add(new TrainingRecord
        {
            Name = "综合实训五",
            Duration = "1小时23分钟",
            CompletionTime = new DateTime(2025, 7, 29, 17, 23, 13),
            Score = 123,
            Type = StatisticType.ComprehensiveTraining
        });

        _allRecords.Add(new TrainingRecord
        {
            Name = "综合实训六",
            Duration = "1小时23分钟",
            CompletionTime = new DateTime(2025, 7, 30, 17, 23, 13),
            Score = 146,
            Type = StatisticType.ComprehensiveTraining
        });

        _allRecords.Add(new TrainingRecord
        {
            Name = "综合实训三",
            Duration = "1小时23分钟",
            CompletionTime = new DateTime(2025, 7, 31, 17, 23, 13),
            Score = 125,
            Type = StatisticType.ComprehensiveTraining
        });

        // 专项练习数据
        _allRecords.Add(new TrainingRecord
        {
            Name = "Word操作练习",
            Duration = "45分钟",
            CompletionTime = new DateTime(2025, 7, 25, 14, 30, 0),
            Score = 89,
            Type = StatisticType.SpecialPractice
        });

        _allRecords.Add(new TrainingRecord
        {
            Name = "Excel函数练习",
            Duration = "52分钟",
            CompletionTime = new DateTime(2025, 7, 26, 16, 15, 30),
            Score = 92,
            Type = StatisticType.SpecialPractice
        });

        // 模拟考试数据（暂无）
        // 上机统考数据（暂无）
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
