using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Examina.Models;
using Examina.Models.Enums;
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
    private readonly ILogger<LeaderboardViewModel>? _logger; // 不再使用，仅保留签名兼容
    private readonly IStudentComprehensiveTrainingService? _comprehensiveTrainingService;
    private readonly IStudentExamService? _studentExamService;
    private readonly IStudentMockExamService? _studentMockExamService;

    /// <summary>
    /// 标记是否已经完成初始化，避免重复初始化
    /// </summary>
    private readonly bool _isInitialized = false;

    /// <summary>
    /// 抑制自动加载与事件响应，确保依赖注入完成后再进行数据加载
    /// </summary>
    private readonly bool _suppressAutoLoad = false;

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

    /// <summary>
    /// 排序选项列表
    /// </summary>
    public ObservableCollection<SortOptionItem> SortOptions { get; } = [];

    /// <summary>
    /// 选中的排序选项
    /// </summary>
    [Reactive]
    public SortOption SelectedSortOption { get; set; } = SortOption.RankAscending;

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

    /// <summary>
    /// 切换排序类型命令
    /// </summary>
    public ICommand SwitchSortTypeCommand { get; }

    /// <summary>
    /// 排序命令
    /// </summary>
    public ICommand SortCommand { get; }

    #endregion

    #region 构造函数

    public LeaderboardViewModel()
    {
        RefreshLeaderboardCommand = new DelegateCommand(RefreshLeaderboard);
        SwitchLeaderboardTypeCommand = new DelegateCommand<LeaderboardTypeItem>(SwitchLeaderboardType);
        SwitchExamFilterCommand = new DelegateCommand<ExamFilterItem>(SwitchExamFilter);
        SwitchSortTypeCommand = new DelegateCommand<SortTypeItem>(SwitchSortType);
        SortCommand = new DelegateCommand<SortOption>(ApplySortOption);

        InitializeLeaderboardTypes();
        InitializeExamFilters();
        InitializeSortOptions();

        _ = this.WhenAnyValue(x => x.SelectedLeaderboardType)
            .Where(type => type != null)
            .DistinctUntilChanged(type => type!.Id)
            .Subscribe(type =>
            {
                if (_suppressAutoLoad)
                {
                    return;
                }

                OnLeaderboardTypeChanged(type!);
            });

        _ = this.WhenAnyValue(x => x.SelectedExamFilter)
            .Where(filter => filter != null)
            .Subscribe(filter =>
            {
                if (_suppressAutoLoad)
                {
                    return;
                }

                OnExamFilterChanged(filter!);
            });

        _ = this.WhenAnyValue(x => x.SelectedSortOption)
            .Subscribe(sortOption => ApplySorting());
    }

    public LeaderboardViewModel(
        RankingService rankingService,
        ILogger<LeaderboardViewModel>? logger,
        IStudentComprehensiveTrainingService comprehensiveTrainingService,
        IStudentExamService studentExamService,
        IStudentMockExamService? studentMockExamService = null)
    {
        _rankingService = rankingService;
        _logger = logger; // 不再使用，仅保留签名兼容
        _comprehensiveTrainingService = comprehensiveTrainingService;
        _studentExamService = studentExamService;
        _studentMockExamService = studentMockExamService;

        RefreshLeaderboardCommand = new DelegateCommand(RefreshLeaderboard);
        SwitchLeaderboardTypeCommand = new DelegateCommand<LeaderboardTypeItem>(SwitchLeaderboardType);
        SwitchExamFilterCommand = new DelegateCommand<ExamFilterItem>(SwitchExamFilter);
        SwitchSortTypeCommand = new DelegateCommand<SortTypeItem>(SwitchSortType);
        SortCommand = new DelegateCommand<SortOption>(ApplySortOption);

        InitializeLeaderboardTypes();
        InitializeExamFilters();
        InitializeSortOptions();

        _ = this.WhenAnyValue(x => x.SelectedLeaderboardType)
            .Where(type => type != null)
            .DistinctUntilChanged(type => type!.Id)
            .Subscribe(type => OnLeaderboardTypeChanged(type!));

        _ = this.WhenAnyValue(x => x.SelectedExamFilter)
            .Where(filter => filter != null)
            .Subscribe(filter => OnExamFilterChanged(filter!));

        _ = this.WhenAnyValue(x => x.SelectedSortOption)
            .Subscribe(sortOption => ApplySorting());
    }

    public LeaderboardViewModel(
        RankingService rankingService,
        ILogger<LeaderboardViewModel>? logger,
        IStudentComprehensiveTrainingService comprehensiveTrainingService,
        IStudentExamService studentExamService,
        string? rankingTypeId,
        IStudentMockExamService? studentMockExamService = null)
        : this(rankingService, logger, comprehensiveTrainingService, studentExamService, studentMockExamService)
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
        if (_isInitialized && LeaderboardTypes.Count > 0)
        {
            return;
        }

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
    }

    /// <summary>
    /// 初始化试卷筛选列表
    /// </summary>
    private void InitializeExamFilters()
    {
        ExamFilters.Clear();

        ExamFilters.Add(new ExamFilterItem
        {
            ExamId = null,
            ExamName = "全部试卷",
            DisplayName = "全部试卷"
        });

        SelectedExamFilter = ExamFilters.FirstOrDefault();
    }

    /// <summary>
    /// 初始化排序选项列表
    /// </summary>
    private void InitializeSortOptions()
    {
        SortOptions.Clear();
        SortOptions.Add(new SortOptionItem(SortOption.RankAscending, true)); // 默认选中
        SortOptions.Add(new SortOptionItem(SortOption.ScoreDescending));
        SortOptions.Add(new SortOptionItem(SortOption.ScoreAscending));
        SortOptions.Add(new SortOptionItem(SortOption.TimeLatest));
        SortOptions.Add(new SortOptionItem(SortOption.TimeEarliest));
        SortOptions.Add(new SortOptionItem(SortOption.NameAscending));
        SortOptions.Add(new SortOptionItem(SortOption.NameDescending));
    }

    /// <summary>
    /// 加载排行榜数据
    /// </summary>
    private async void LoadLeaderboardData()
    {
        if (_suppressAutoLoad)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            if (_rankingService != null && SelectedLeaderboardType != null)
            {
                RankingType rankingType = SelectedLeaderboardType.Id switch
                {
                    "exam-ranking" => RankingType.ExamRanking,
                    "mock-exam-ranking" => RankingType.MockExamRanking,
                    "training-ranking" => RankingType.TrainingRanking,
                    _ => RankingType.ExamRanking
                };

                int? examId = SelectedExamFilter?.ExamId;

                RankingResponseDto? response =
                    await _rankingService.GetRankingByTypeAsync(rankingType, examId, 1, 50);

                LeaderboardData.Clear();
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
                            CompletionDate = entry.CompletedAt,
                            SchoolName = entry.SchoolName ?? "未知学校",
                            ClassName = entry.ClassName ?? "未知班级"
                        });
                    }
                }
                else
                {
                    ErrorMessage = "暂无排行榜数据";
                }
            }
            else
            {
                // 没有服务时使用模拟数据
                for (int i = 1; i <= 10; i++)
                {
                    LeaderboardData.Add(new LeaderboardEntry
                    {
                        Rank = i,
                        Username = $"用户{i:D3}",
                        Score = 100 - (i * 2),
                        CompletionTime = TimeSpan.FromMinutes(30 + (i * 2)),
                        CompletionDate = DateTime.Now.AddDays(-i),
                        SchoolName = $"学校{(i % 3) + 1}",
                        ClassName = $"班级{(i % 5) + 1}"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载排行榜数据时发生异常: {ex}");
            ErrorMessage = "加载排行榜数据失败，请稍后重试";
        }
        finally
        {
            IsLoading = false;

            if (LeaderboardData.Any())
            {
                ApplySorting();
            }
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
    /// 切换排序类型（向后兼容性方法）
    /// </summary>
    private void SwitchSortType(SortTypeItem? sortType)
    {
        if (sortType == null)
        {
            return;
        }

        // 映射旧的排序类型到新的排序选项
        SortOption newSortOption = sortType.Id switch
        {
            "score" => SortOption.ScoreDescending,
            "school" => SortOption.NameAscending, // 学校排序映射到用户名排序
            "class" => SortOption.NameAscending,  // 班级排序映射到用户名排序
            "time" => SortOption.TimeLatest,
            _ => SortOption.RankAscending
        };

        ApplySortOption(newSortOption);
    }

    /// <summary>
    /// 排行榜类型变化处理
    /// </summary>
    private void OnLeaderboardTypeChanged(LeaderboardTypeItem leaderboardType)
    {
        ShowExamFilter = leaderboardType.Id is not "mock-exam-ranking";

        bool needLoad = true;

        if (!ShowExamFilter)
        {
            ExamFilterItem? currentFilter = SelectedExamFilter;
            ExamFilterItem? defaultFilter = ExamFilters.FirstOrDefault();
            if (currentFilter?.ExamId != defaultFilter?.ExamId)
            {
                SelectedExamFilter = defaultFilter;
                needLoad = false; // SelectedExamFilter 变化会触发加载
            }
        }

        if (needLoad)
        {
            LoadLeaderboardData();
        }

        _ = LoadExamFiltersAsync(leaderboardType.Id);
    }

    /// <summary>
    /// 试卷筛选变化处理
    /// </summary>
    private void OnExamFilterChanged(ExamFilterItem examFilter)
    {
        if (!IsLoading)
        {
            LoadLeaderboardData();
        }
    }

    /// <summary>
    /// 排序类型变化处理（向后兼容性方法）
    /// </summary>
    private void OnSortTypeChanged(SortTypeItem sortType)
    {
        if (!IsLoading && LeaderboardData.Any())
        {
            SwitchSortType(sortType);
        }
    }

    /// <summary>
    /// 异步加载试卷筛选列表
    /// </summary>
    private async Task LoadExamFiltersAsync(string rankingTypeId)
    {
        try
        {
            ExamFilterItem? currentFilter = SelectedExamFilter;

            ExamFilters.Clear();

            ExamFilters.Add(new ExamFilterItem
            {
                ExamId = null,
                ExamName = "全部试卷",
                DisplayName = "全部试卷"
            });

            if (rankingTypeId == "training-ranking" && _comprehensiveTrainingService != null)
            {
                try
                {
                    List<Models.Exam.StudentComprehensiveTrainingDto> trainings =
                        await _comprehensiveTrainingService.GetAvailableTrainingsAsync(1, 100);

                    foreach (Models.Exam.StudentComprehensiveTrainingDto training in trainings)
                    {
                        ExamFilters.Add(new ExamFilterItem
                        {
                            ExamId = training.Id,
                            ExamName = training.Name,
                            DisplayName = training.Name
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载综合实训试卷列表失败: {ex}");
                    for (int i = 1; i <= 10; i++)
                    {
                        ExamFilters.Add(new ExamFilterItem
                        {
                            ExamId = i,
                            ExamName = $"试卷{i:D2}",
                            DisplayName = $"试卷{i:D2}"
                        });
                    }
                }
            }
            else if (rankingTypeId == "exam-ranking" && _studentExamService != null)
            {
                try
                {
                    List<Models.Exam.StudentExamDto> exams =
                        await _studentExamService.GetAvailableExamsAsync(1, 100);

                    foreach (Models.Exam.StudentExamDto exam in exams)
                    {
                        ExamFilters.Add(new ExamFilterItem
                        {
                            ExamId = exam.Id,
                            ExamName = exam.Name,
                            DisplayName = exam.Name
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载正式考试试卷列表失败: {ex}");
                    for (int i = 1; i <= 10; i++)
                    {
                        ExamFilters.Add(new ExamFilterItem
                        {
                            ExamId = i,
                            ExamName = $"试卷{i:D2}",
                            DisplayName = $"试卷{i:D2}"
                        });
                    }
                }
            }
            else if (rankingTypeId == "mock-exam-ranking" && _studentMockExamService != null)
            {
                try
                {
                    List<Models.MockExam.StudentMockExamDto> mockExams =
                        await _studentMockExamService.GetStudentMockExamsAsync(1, 100);

                    foreach (Models.MockExam.StudentMockExamDto mockExam in mockExams)
                    {
                        ExamFilters.Add(new ExamFilterItem
                        {
                            ExamId = mockExam.Id,
                            ExamName = mockExam.Name,
                            DisplayName = mockExam.Name
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载模拟考试试卷列表失败: {ex}");
                    for (int i = 1; i <= 10; i++)
                    {
                        ExamFilters.Add(new ExamFilterItem
                        {
                            ExamId = i,
                            ExamName = $"模拟考试{i:D2}",
                            DisplayName = $"模拟考试{i:D2}"
                        });
                    }
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    string examPrefix = rankingTypeId switch
                    {
                        "mock-exam-ranking" => "模拟考试",
                        "exam-ranking" => "正式考试",
                        _ => "试卷"
                    };

                    for (int i = 1; i <= 10; i++)
                    {
                        ExamFilters.Add(new ExamFilterItem
                        {
                            ExamId = i,
                            ExamName = $"{examPrefix}{i:D2}",
                            DisplayName = $"{examPrefix}{i:D2}"
                        });
                    }
                });
            }

            SelectedExamFilter = ExamFilters.FirstOrDefault(f => f.ExamId == currentFilter?.ExamId)
                               ?? ExamFilters.FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载试卷筛选列表时发生异常: {ex}");
        }
    }

    /// <summary>
    /// 设置排行榜类型并加载数据
    /// </summary>
    /// <param name="rankingTypeId">排行榜类型ID</param>
    public void SetRankingType(string rankingTypeId)
    {
        LeaderboardTypeItem? targetType = LeaderboardTypes.FirstOrDefault(t => t.Id == rankingTypeId);

        if (targetType != null)
        {
            if (SelectedLeaderboardType?.Id != targetType.Id)
            {
                SelectedLeaderboardType = targetType;
                PageTitle = targetType.Name;
            }
        }
        else
        {
            LeaderboardTypeItem? first = LeaderboardTypes.FirstOrDefault();
            if (first != null && SelectedLeaderboardType?.Id != first.Id)
            {
                SelectedLeaderboardType = first;
                PageTitle = first.Name;
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
            SelectedLeaderboardType = LeaderboardTypes.FirstOrDefault();
            if (SelectedLeaderboardType != null)
            {
                PageTitle = SelectedLeaderboardType.Name;
                LoadLeaderboardData();
            }
        }
    }

    /// <summary>
    /// 应用排序选项
    /// </summary>
    /// <param name="sortOption">排序选项</param>
    private void ApplySortOption(SortOption sortOption)
    {
        SelectedSortOption = sortOption;

        // 更新排序选项的选中状态
        foreach (SortOptionItem item in SortOptions)
        {
            item.IsSelected = item.Value == sortOption;
        }
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    private void ApplySorting()
    {
        if (!LeaderboardData.Any())
        {
            return;
        }

        try
        {
            List<LeaderboardEntry> sortedData = SelectedSortOption switch
            {
                SortOption.RankAscending => [.. LeaderboardData.OrderBy(x => x.Rank)],
                SortOption.ScoreDescending => [.. LeaderboardData.OrderByDescending(x => x.Score)
                                                           .ThenBy(x => x.CompletionTime)
                                                           .ThenBy(x => x.CompletionDate)],
                SortOption.ScoreAscending => [.. LeaderboardData.OrderBy(x => x.Score)
                                                          .ThenByDescending(x => x.CompletionTime)
                                                          .ThenByDescending(x => x.CompletionDate)],
                SortOption.TimeLatest => [.. LeaderboardData.OrderByDescending(x => x.CompletionDate)
                                                      .ThenByDescending(x => x.Score)],
                SortOption.TimeEarliest => [.. LeaderboardData.OrderBy(x => x.CompletionDate)
                                                        .ThenByDescending(x => x.Score)],
                SortOption.NameAscending => [.. LeaderboardData.OrderBy(x => x.Username)
                                                         .ThenByDescending(x => x.Score)],
                SortOption.NameDescending => [.. LeaderboardData.OrderByDescending(x => x.Username)
                                                          .ThenByDescending(x => x.Score)],
                _ => [.. LeaderboardData.OrderBy(x => x.Rank)]
            };

            // 只有在非排名排序时才重新计算排名
            if (SelectedSortOption != SortOption.RankAscending)
            {
                for (int i = 0; i < sortedData.Count; i++)
                {
                    sortedData[i].Rank = i + 1;
                }
            }

            LeaderboardData.Clear();
            foreach (LeaderboardEntry entry in sortedData)
            {
                LeaderboardData.Add(entry);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"应用排序时发生异常: {ex}");
        }
    }

    #endregion
}

/// <summary>
/// 排行榜类型项目
/// </summary>
public class LeaderboardTypeItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// 排行榜条目
/// </summary>
public class LeaderboardEntry
{
    public int Rank { get; set; }
    public string Username { get; set; } = string.Empty;
    public double Score { get; set; }
    public TimeSpan CompletionTime { get; set; }
    public DateTime CompletionDate { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
}

/// <summary>
/// 试卷筛选项
/// </summary>
public class ExamFilterItem
{
    public int? ExamId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// 排序类型项
/// </summary>
public class SortTypeItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
