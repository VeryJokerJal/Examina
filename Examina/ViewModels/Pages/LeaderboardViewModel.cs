using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Examina.Models.Ranking;
using Examina.Services;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 排行榜页面视图模型
/// </summary>
public class LeaderboardViewModel : ViewModelBase
{
    private readonly RankingService? _rankingService;
    private readonly ILogger<LeaderboardViewModel>? _logger;

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

    /// <summary>
    /// 错误消息
    /// </summary>
    [Reactive]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 试卷筛选列表
    /// </summary>
    public ObservableCollection<ExamFilterItem> ExamFilters { get; } = [];

    /// <summary>
    /// 选中的试卷筛选项
    /// </summary>
    [Reactive]
    public ExamFilterItem? SelectedExamFilter { get; set; }

    /// <summary>
    /// 是否显示试卷筛选器（模拟考试排行榜不显示）
    /// </summary>
    [Reactive]
    public bool ShowExamFilter { get; set; } = true;

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

    /// <summary>
    /// 切换试卷筛选命令
    /// </summary>
    public ICommand SwitchExamFilterCommand { get; }

    #endregion

    #region 构造函数

    public LeaderboardViewModel()
    {
        RefreshLeaderboardCommand = new DelegateCommand(RefreshLeaderboard);
        SwitchLeaderboardTypeCommand = new DelegateCommand<LeaderboardTypeItem>(SwitchLeaderboardType);
        SwitchExamFilterCommand = new DelegateCommand<ExamFilterItem>(SwitchExamFilter);

        InitializeLeaderboardTypes();
        InitializeExamFilters();

        // 监听排行榜类型变化
        _ = this.WhenAnyValue(x => x.SelectedLeaderboardType)
            .Where(type => type != null)
            .Subscribe(type => OnLeaderboardTypeChanged(type!));

        // 监听试卷筛选变化
        _ = this.WhenAnyValue(x => x.SelectedExamFilter)
            .Where(filter => filter != null)
            .Subscribe(filter => OnExamFilterChanged(filter!));

        // 不在构造函数中自动加载数据，等待设置排行榜类型后再加载
    }

    public LeaderboardViewModel(RankingService rankingService, ILogger<LeaderboardViewModel> logger) : this()
    {
        _rankingService = rankingService;
        _logger = logger;
    }

    public LeaderboardViewModel(RankingService rankingService, ILogger<LeaderboardViewModel> logger, string? rankingTypeId) : this(rankingService, logger)
    {
        if (!string.IsNullOrEmpty(rankingTypeId))
        {
            SetRankingType(rankingTypeId);
        }
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
    /// 初始化试卷筛选列表
    /// </summary>
    private void InitializeExamFilters()
    {
        ExamFilters.Clear();

        // 添加"全部试卷"选项
        ExamFilters.Add(new ExamFilterItem
        {
            ExamId = null,
            ExamName = "全部试卷",
            DisplayName = "全部试卷"
        });

        // 默认选择"全部试卷"
        SelectedExamFilter = ExamFilters.FirstOrDefault();
    }

    /// <summary>
    /// 加载排行榜数据
    /// </summary>
    private async void LoadLeaderboardData()
    {
        IsLoading = true;
        LeaderboardData.Clear();
        ErrorMessage = null;

        try
        {
            if (_rankingService != null && SelectedLeaderboardType != null)
            {
                _logger?.LogInformation("开始加载排行榜数据，类型: {Type}, 试卷筛选: {ExamFilter}",
                    SelectedLeaderboardType.Id, SelectedExamFilter?.DisplayName ?? "无");

                // 根据选中的排行榜类型获取数据
                RankingType rankingType = SelectedLeaderboardType.Id switch
                {
                    "exam-ranking" => RankingType.ExamRanking,
                    "mock-exam-ranking" => RankingType.MockExamRanking,
                    "training-ranking" => RankingType.TrainingRanking,
                    _ => RankingType.ExamRanking
                };

                // 获取试卷筛选ID（null表示全部试卷）
                int? examId = SelectedExamFilter?.ExamId;

                RankingResponseDto? response = await _rankingService.GetRankingByTypeAsync(rankingType, examId, 1, 50);

                if (response != null && response.Entries.Any())
                {
                    foreach (RankingEntryDto entry in response.Entries)
                    {
                        LeaderboardData.Add(new LeaderboardEntry
                        {
                            Rank = entry.Rank,
                            Username = entry.Username,
                            Score = (int)entry.Score,
                            CompletionTime = TimeSpan.FromSeconds(entry.DurationSeconds),
                            CompletionDate = entry.CompletedAt
                        });
                    }

                    _logger?.LogInformation("成功加载排行榜数据，记录数: {Count}", response.Entries.Count);
                }
                else
                {
                    _logger?.LogWarning("未获取到排行榜数据");
                    ErrorMessage = "暂无排行榜数据";
                }
            }
            else
            {
                // 如果没有服务注入，使用模拟数据
                _logger?.LogWarning("排行榜服务未注入，使用模拟数据");

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
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "加载排行榜数据时发生异常");
            ErrorMessage = "加载排行榜数据失败，请稍后重试";
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

    /// <summary>
    /// 切换试卷筛选
    /// </summary>
    private void SwitchExamFilter(ExamFilterItem? examFilter)
    {
        if (examFilter == null)
        {
            return;
        }

        SelectedExamFilter = examFilter;
        LoadLeaderboardData();
    }

    /// <summary>
    /// 排行榜类型变化处理
    /// </summary>
    private void OnLeaderboardTypeChanged(LeaderboardTypeItem leaderboardType)
    {
        // 更新试卷筛选器的显示状态
        ShowExamFilter = leaderboardType.Id != "mock-exam-ranking";

        // 如果是模拟考试排行榜，重置筛选器为"全部试卷"
        if (!ShowExamFilter)
        {
            SelectedExamFilter = ExamFilters.FirstOrDefault();
        }

        // 加载对应类型的试卷列表
        _ = LoadExamFiltersAsync(leaderboardType.Id);
    }

    /// <summary>
    /// 试卷筛选变化处理
    /// </summary>
    private void OnExamFilterChanged(ExamFilterItem examFilter)
    {
        // 当筛选条件变化时，重新加载排行榜数据
        LoadLeaderboardData();
    }

    /// <summary>
    /// 异步加载试卷筛选列表
    /// </summary>
    private async Task LoadExamFiltersAsync(string rankingTypeId)
    {
        try
        {
            _logger?.LogInformation("开始加载试卷筛选列表，排行榜类型: {RankingTypeId}", rankingTypeId);

            // 保存当前选中的筛选项
            ExamFilterItem? currentFilter = SelectedExamFilter;

            // 清空现有筛选列表
            ExamFilters.Clear();

            // 添加"全部试卷"选项
            ExamFilters.Add(new ExamFilterItem
            {
                ExamId = null,
                ExamName = "全部试卷",
                DisplayName = "全部试卷"
            });

            // 根据排行榜类型加载对应的试卷列表
            // 这里暂时使用模拟数据，实际应该调用相应的服务获取试卷列表
            await Task.Run(() =>
            {
                for (int i = 1; i <= 10; i++)
                {
                    ExamFilters.Add(new ExamFilterItem
                    {
                        ExamId = i,
                        ExamName = $"试卷{i:D2}",
                        DisplayName = $"试卷{i:D2}"
                    });
                }
            });

            // 恢复之前的选择，如果不存在则选择"全部试卷"
            SelectedExamFilter = ExamFilters.FirstOrDefault(f => f.ExamId == currentFilter?.ExamId)
                               ?? ExamFilters.FirstOrDefault();

            _logger?.LogInformation("试卷筛选列表加载完成，共 {Count} 个选项", ExamFilters.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "加载试卷筛选列表时发生异常");
        }
    }

    /// <summary>
    /// 设置排行榜类型并加载数据
    /// </summary>
    /// <param name="rankingTypeId">排行榜类型ID</param>
    public void SetRankingType(string rankingTypeId)
    {
        _logger?.LogInformation("设置排行榜类型: {RankingTypeId}", rankingTypeId);

        // 根据类型ID找到对应的排行榜类型项
        LeaderboardTypeItem? targetType = LeaderboardTypes.FirstOrDefault(t => t.Id == rankingTypeId);

        if (targetType != null)
        {
            SelectedLeaderboardType = targetType;

            // 更新页面标题
            PageTitle = targetType.Name;

            // 加载对应类型的数据
            LoadLeaderboardData();
        }
        else
        {
            _logger?.LogWarning("未找到排行榜类型: {RankingTypeId}", rankingTypeId);
            // 如果没找到，默认选择第一个类型
            SelectedLeaderboardType = LeaderboardTypes.FirstOrDefault();
            if (SelectedLeaderboardType != null)
            {
                PageTitle = SelectedLeaderboardType.Name;
                LoadLeaderboardData();
            }
        }
    }

    /// <summary>
    /// 手动触发数据加载（用于初始化时没有自动加载的情况）
    /// </summary>
    public void LoadInitialData()
    {
        if (SelectedLeaderboardType != null)
        {
            LoadLeaderboardData();
        }
        else
        {
            // 如果没有选中的类型，选择第一个并加载
            SelectedLeaderboardType = LeaderboardTypes.FirstOrDefault();
            if (SelectedLeaderboardType != null)
            {
                PageTitle = SelectedLeaderboardType.Name;
                LoadLeaderboardData();
            }
        }
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

/// <summary>
/// 试卷筛选项
/// </summary>
public class ExamFilterItem
{
    /// <summary>
    /// 试卷ID（null表示"全部试卷"）
    /// </summary>
    public int? ExamId { get; set; }

    /// <summary>
    /// 试卷名称
    /// </summary>
    public string ExamName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
