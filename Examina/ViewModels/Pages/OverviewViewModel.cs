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
    /// 今日练习次数
    /// </summary>
    [Reactive]
    public int TodayPracticeCount { get; set; } = 0;

    /// <summary>
    /// 总练习次数
    /// </summary>
    [Reactive]
    public int TotalPracticeCount { get; set; } = 0;

    /// <summary>
    /// 最高分数
    /// </summary>
    [Reactive]
    public int HighestScore { get; set; } = 0;

    /// <summary>
    /// 平均分数
    /// </summary>
    [Reactive]
    public double AverageScore { get; set; } = 0.0;

    #endregion

    #region 构造函数

    public OverviewViewModel()
    {
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
        TodayPracticeCount = 3;
        TotalPracticeCount = 25;
        HighestScore = 95;
        AverageScore = 82.5;
    }

    #endregion
}
