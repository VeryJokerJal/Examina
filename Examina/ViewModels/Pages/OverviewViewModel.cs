using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 概览页面视图模型
/// </summary>
public class OverviewViewModel : ViewModelBase
{
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
        if (parameter is string typeString && Enum.TryParse<StatisticType>(typeString, out var type))
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

        var filteredRecords = _allRecords.Where(r => r.Type == SelectedStatisticType).ToList();

        foreach (var record in filteredRecords)
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
