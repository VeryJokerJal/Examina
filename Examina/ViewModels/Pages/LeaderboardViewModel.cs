using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 排行榜页面视图模型
/// </summary>
public class LeaderboardViewModel : ViewModelBase
{
    #region 属性

    /// <summary>
    /// 页面标题
    /// </summary>
    [Reactive]
    public string PageTitle { get; set; } = "排行榜";

    /// <summary>
    /// 排行榜类型列表
    /// </summary>
    public ObservableCollection<LeaderboardTypeItem> LeaderboardTypes { get; } = [];

    /// <summary>
    /// 选中的排行榜类型
    /// </summary>
    [Reactive]
    public LeaderboardTypeItem? SelectedLeaderboardType { get; set; }

    /// <summary>
    /// 排行榜数据
    /// </summary>
    public ObservableCollection<LeaderboardEntry> LeaderboardData { get; } = [];

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [Reactive]
    public bool IsLoading { get; set; } = false;

    #endregion

    #region 命令

    /// <summary>
    /// 刷新排行榜命令
    /// </summary>
    public ICommand RefreshLeaderboardCommand { get; }

    /// <summary>
    /// 切换排行榜类型命令
    /// </summary>
    public ICommand SwitchLeaderboardTypeCommand { get; }

    #endregion

    #region 构造函数

    public LeaderboardViewModel()
    {
        RefreshLeaderboardCommand = new DelegateCommand(RefreshLeaderboard);
        SwitchLeaderboardTypeCommand = new DelegateCommand<LeaderboardTypeItem>(SwitchLeaderboardType);

        InitializeLeaderboardTypes();
        LoadLeaderboardData();
    }

    #endregion

    #region 方法

    /// <summary>
    /// 初始化排行榜类型
    /// </summary>
    private void InitializeLeaderboardTypes()
    {
        LeaderboardTypes.Clear();

        LeaderboardTypes.Add(new LeaderboardTypeItem
        {
            Id = "exam-ranking",
            Name = "上机统考排行",
            Description = "正式考试成绩排行榜",
            Icon = "🏆"
        });

        LeaderboardTypes.Add(new LeaderboardTypeItem
        {
            Id = "mock-exam-ranking",
            Name = "模拟考试排行",
            Description = "模拟考试成绩排行榜",
            Icon = "📊"
        });

        LeaderboardTypes.Add(new LeaderboardTypeItem
        {
            Id = "training-ranking",
            Name = "综合实训排行",
            Description = "综合实训成绩排行榜",
            Icon = "🎯"
        });

        SelectedLeaderboardType = LeaderboardTypes.FirstOrDefault();
    }

    /// <summary>
    /// 加载排行榜数据
    /// </summary>
    private async void LoadLeaderboardData()
    {
        IsLoading = true;
        LeaderboardData.Clear();

        try
        {
            // TODO: 从服务加载实际排行榜数据
            await Task.Delay(1000); // 模拟网络延迟

            // 模拟数据
            for (int i = 1; i <= 10; i++)
            {
                LeaderboardData.Add(new LeaderboardEntry
                {
                    Rank = i,
                    Username = $"用户{i:D3}",
                    Score = 100 - (i * 2),
                    CompletionTime = TimeSpan.FromMinutes(30 + (i * 2)),
                    CompletionDate = DateTime.Now.AddDays(-i)
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 刷新排行榜
    /// </summary>
    private void RefreshLeaderboard()
    {
        LoadLeaderboardData();
    }

    /// <summary>
    /// 切换排行榜类型
    /// </summary>
    private void SwitchLeaderboardType(LeaderboardTypeItem? leaderboardType)
    {
        if (leaderboardType == null)
        {
            return;
        }

        SelectedLeaderboardType = leaderboardType;
        LoadLeaderboardData();
    }

    #endregion
}

/// <summary>
/// 排行榜类型项目
/// </summary>
public class LeaderboardTypeItem
{
    /// <summary>
    /// 排行榜类型ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 排行榜类型名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 排行榜类型描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// 排行榜条目
/// </summary>
public class LeaderboardEntry
{
    /// <summary>
    /// 排名
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 分数
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public TimeSpan CompletionTime { get; set; }

    /// <summary>
    /// 完成日期
    /// </summary>
    public DateTime CompletionDate { get; set; }
}
