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
    private readonly IStudentComprehensiveTrainingService? _comprehensiveTrainingService;
    private readonly IStudentExamService? _studentExamService;
    private readonly IStudentMockExamService? _studentMockExamService;

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
    /// 排序类型列表
    /// </summary>
    public ObservableCollection<SortTypeItem> SortTypes { get; } = [];

    /// <summary>
    /// 选中的排序类型
    /// </summary>
    [Reactive]
    public SortTypeItem? SelectedSortType { get; set; }

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

    #endregion

    #region 构造函数

    public LeaderboardViewModel()
    {
        // 添加调试日志
        System.Diagnostics.Debug.WriteLine("LeaderboardViewModel: 无参构造函数调用");

        RefreshLeaderboardCommand = new DelegateCommand(RefreshLeaderboard);
        SwitchLeaderboardTypeCommand = new DelegateCommand<LeaderboardTypeItem>(SwitchLeaderboardType);
        SwitchExamFilterCommand = new DelegateCommand<ExamFilterItem>(SwitchExamFilter);
        SwitchSortTypeCommand = new DelegateCommand<SortTypeItem>(SwitchSortType);

        InitializeLeaderboardTypes();
        InitializeExamFilters();
        InitializeSortTypes();

        // 监听排行榜类型变化（按ID去重，防止相同类型重复触发）
        _ = this.WhenAnyValue(x => x.SelectedLeaderboardType)
            .Where(type => type != null)
            .DistinctUntilChanged(type => type!.Id)
            .Subscribe(type => OnLeaderboardTypeChanged(type!));

        // 监听试卷筛选变化
        _ = this.WhenAnyValue(x => x.SelectedExamFilter)
            .Where(filter => filter != null)
            .Subscribe(filter => OnExamFilterChanged(filter!));

        // 监听排序类型变化
        _ = this.WhenAnyValue(x => x.SelectedSortType)
            .Where(sortType => sortType != null)
            .Subscribe(sortType => OnSortTypeChanged(sortType!));

        // 不在构造函数中自动加载数据，等待设置排行榜类型后再加载
    }

    public LeaderboardViewModel(RankingService rankingService, ILogger<LeaderboardViewModel> logger, IStudentComprehensiveTrainingService comprehensiveTrainingService, IStudentExamService studentExamService, IStudentMockExamService? studentMockExamService = null) : this()
    {
        _rankingService = rankingService;
        _logger = logger;
        _comprehensiveTrainingService = comprehensiveTrainingService;
        _studentExamService = studentExamService;
        _studentMockExamService = studentMockExamService;

        // 添加调试日志
        System.Diagnostics.Debug.WriteLine($"LeaderboardViewModel: 依赖注入构造函数调用");
        System.Diagnostics.Debug.WriteLine($"  - RankingService: {(_rankingService != null ? "已注入" : "null")}");
        System.Diagnostics.Debug.WriteLine($"  - Logger: {(_logger != null ? "已注入" : "null")}");
        System.Diagnostics.Debug.WriteLine($"  - ComprehensiveTrainingService: {(_comprehensiveTrainingService != null ? "已注入" : "null")}");
        System.Diagnostics.Debug.WriteLine($"  - StudentExamService: {(_studentExamService != null ? "已注入" : "null")}");
        System.Diagnostics.Debug.WriteLine($"  - StudentMockExamService: {(_studentMockExamService != null ? "已注入" : "null")}");
    }

    public LeaderboardViewModel(RankingService rankingService, ILogger<LeaderboardViewModel> logger, IStudentComprehensiveTrainingService comprehensiveTrainingService, IStudentExamService studentExamService, string? rankingTypeId, IStudentMockExamService? studentMockExamService = null) : this(rankingService, logger, comprehensiveTrainingService, studentExamService, studentMockExamService)
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
    /// 初始化排序类型列表
    /// </summary>
    private void InitializeSortTypes()
    {
        SortTypes.Clear();

        SortTypes.Add(new SortTypeItem
        {
            Id = "score",
            Name = "按分数排序",
            Description = "按考试分数从高到低排序",
            Icon = "🏆"
        });

        SortTypes.Add(new SortTypeItem
        {
            Id = "school",
            Name = "按学校排序",
            Description = "按学校名称排序",
            Icon = "🏫"
        });

        SortTypes.Add(new SortTypeItem
        {
            Id = "class",
            Name = "按班级排序",
            Description = "按班级名称排序",
            Icon = "👥"
        });

        SortTypes.Add(new SortTypeItem
        {
            Id = "time",
            Name = "按时间排序",
            Description = "按完成时间排序",
            Icon = "⏰"
        });

        // 默认选择按分数排序
        SelectedSortType = SortTypes.FirstOrDefault();
    }

    /// <summary>
    /// 加载排行榜数据
    /// </summary>
    private async void LoadLeaderboardData()
    {
        IsLoading = true;
        ErrorMessage = null;

        // 添加服务状态调试日志
        System.Diagnostics.Debug.WriteLine("LeaderboardViewModel.LoadLeaderboardData: 开始加载数据");
        System.Diagnostics.Debug.WriteLine($"  - RankingService状态: {(_rankingService != null ? "可用" : "null")}");
        System.Diagnostics.Debug.WriteLine($"  - SelectedLeaderboardType: {SelectedLeaderboardType?.Id ?? "null"}");

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
                        CompletionDate = DateTime.Now.AddDays(-i),
                        SchoolName = $"学校{(i % 3) + 1}",
                        ClassName = $"班级{(i % 5) + 1}"
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

            // 数据加载完成后应用排序
            if (LeaderboardData.Any() && SelectedSortType != null)
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

        // 仅设置类型，数据加载由 OnLeaderboardTypeChanged 和筛选器变化统一触发
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
    /// 切换排序类型
    /// </summary>
    private void SwitchSortType(SortTypeItem? sortType)
    {
        if (sortType == null)
        {
            return;
        }

        SelectedSortType = sortType;
        ApplySorting();
    }

    /// <summary>
    /// 排行榜类型变化处理
    /// </summary>
    private void OnLeaderboardTypeChanged(LeaderboardTypeItem leaderboardType)
    {
        _logger?.LogInformation("排行榜类型变化: {Type}", leaderboardType.Id);

        // 更新试卷筛选器的显示状态
        ShowExamFilter = leaderboardType.Id != "mock-exam-ranking";

        // 如果是模拟考试排行榜，重置筛选器为"全部试卷"
        if (!ShowExamFilter)
        {
            // 临时禁用筛选器变化监听，避免重复加载数据
            ExamFilterItem? currentFilter = SelectedExamFilter;
            SelectedExamFilter = ExamFilters.FirstOrDefault();

            // 如果筛选器没有实际变化，手动触发数据加载
            if (currentFilter == SelectedExamFilter)
            {
                LoadLeaderboardData();
            }
        }
        else
        {
            // 对于其他类型，直接加载数据
            LoadLeaderboardData();
        }

        // 加载对应类型的试卷列表（异步，不阻塞当前操作）
        _ = LoadExamFiltersAsync(leaderboardType.Id);
    }

    /// <summary>
    /// 试卷筛选变化处理
    /// </summary>
    private void OnExamFilterChanged(ExamFilterItem examFilter)
    {
        _logger?.LogInformation("试卷筛选变化: {Filter}", examFilter?.DisplayName ?? "null");

        // 当筛选条件变化时，重新加载排行榜数据
        // 但要避免在初始化过程中重复加载
        if (!IsLoading)
        {
            LoadLeaderboardData();
        }
    }

    /// <summary>
    /// 排序类型变化处理
    /// </summary>
    private void OnSortTypeChanged(SortTypeItem sortType)
    {
        _logger?.LogInformation("排序类型变化: {SortType}", sortType?.Name ?? "null");

        // 当排序类型变化时，重新应用排序
        if (!IsLoading && LeaderboardData.Any())
        {
            ApplySorting();
        }
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
            if (rankingTypeId == "training-ranking" && _comprehensiveTrainingService != null)
            {
                try
                {
                    // 获取综合实训列表
                    List<Models.Exam.StudentComprehensiveTrainingDto> trainings = await _comprehensiveTrainingService.GetAvailableTrainingsAsync(1, 100);

                    foreach (Models.Exam.StudentComprehensiveTrainingDto training in trainings)
                    {
                        ExamFilters.Add(new ExamFilterItem
                        {
                            ExamId = training.Id,
                            ExamName = training.Name,
                            DisplayName = training.Name
                        });
                    }

                    _logger?.LogInformation("成功加载 {Count} 个综合实训试卷", trainings.Count);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "加载综合实训试卷列表失败");

                    // 如果加载失败，使用模拟数据作为备用
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
                    // 获取正式考试列表
                    List<Models.Exam.StudentExamDto> exams = await _studentExamService.GetAvailableExamsAsync(1, 100);

                    foreach (Models.Exam.StudentExamDto exam in exams)
                    {
                        ExamFilters.Add(new ExamFilterItem
                        {
                            ExamId = exam.Id,
                            ExamName = exam.Name,
                            DisplayName = exam.Name
                        });
                    }

                    _logger?.LogInformation("成功加载 {Count} 个正式考试试卷", exams.Count);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "加载正式考试试卷列表失败");

                    // 如果加载失败，使用模拟数据作为备用
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
                    // 获取模拟考试列表
                    List<Models.MockExam.StudentMockExamDto> mockExams = await _studentMockExamService.GetStudentMockExamsAsync(1, 100);

                    foreach (Models.MockExam.StudentMockExamDto mockExam in mockExams)
                    {
                        ExamFilters.Add(new ExamFilterItem
                        {
                            ExamId = mockExam.Id,
                            ExamName = mockExam.Name,
                            DisplayName = mockExam.Name
                        });
                    }

                    _logger?.LogInformation("成功加载 {Count} 个模拟考试试卷", mockExams.Count);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "加载模拟考试试卷列表失败");

                    // 如果加载失败，使用模拟数据作为备用
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
                // 如果没有对应的服务，使用模拟数据作为备用
                _logger?.LogWarning("未找到对应的服务，使用模拟数据，排行榜类型: {RankingTypeId}", rankingTypeId);

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
            // 避免重复触发：仅当新旧不同才更新
            if (SelectedLeaderboardType?.Id != targetType.Id)
            {
                SelectedLeaderboardType = targetType;
                PageTitle = targetType.Name;
            }
        }
        else
        {
            _logger?.LogWarning("未找到排行榜类型: {RankingTypeId}", rankingTypeId);
            // 如果没找到，默认选择第一个类型
            var first = LeaderboardTypes.FirstOrDefault();
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
            // 如果没有选中的类型，选择第一个并加载
            SelectedLeaderboardType = LeaderboardTypes.FirstOrDefault();
            if (SelectedLeaderboardType != null)
            {
                PageTitle = SelectedLeaderboardType.Name;
                LoadLeaderboardData();
            }
        }
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    private void ApplySorting()
    {
        if (SelectedSortType == null || !LeaderboardData.Any())
        {
            return;
        }

        try
        {
            _logger?.LogInformation("应用排序: {SortType}", SelectedSortType.Id);

            List<LeaderboardEntry> sortedData = SelectedSortType.Id switch
            {
                "score" => LeaderboardData.OrderByDescending(x => x.Score)
                                         .ThenBy(x => x.CompletionTime)
                                         .ThenBy(x => x.CompletionDate)
                                         .ToList(),
                "school" => LeaderboardData.OrderBy(x => x.SchoolName)
                                          .ThenBy(x => x.ClassName)
                                          .ThenByDescending(x => x.Score)
                                          .ToList(),
                "class" => LeaderboardData.OrderBy(x => x.ClassName)
                                         .ThenBy(x => x.SchoolName)
                                         .ThenByDescending(x => x.Score)
                                         .ToList(),
                "time" => LeaderboardData.OrderBy(x => x.CompletionDate)
                                        .ThenByDescending(x => x.Score)
                                        .ToList(),
                _ => LeaderboardData.OrderByDescending(x => x.Score)
                                   .ThenBy(x => x.CompletionTime)
                                   .ToList()
            };

            // 重新分配排名
            for (int i = 0; i < sortedData.Count; i++)
            {
                sortedData[i].Rank = i + 1;
            }

            // 更新集合
            LeaderboardData.Clear();
            foreach (LeaderboardEntry entry in sortedData)
            {
                LeaderboardData.Add(entry);
            }

            _logger?.LogInformation("排序完成，共 {Count} 条记录", sortedData.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "应用排序时发生异常");
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

    /// <summary>
    /// 学校名称
    /// </summary>
    public string SchoolName { get; set; } = string.Empty;

    /// <summary>
    /// 班级名称
    /// </summary>
    public string ClassName { get; set; } = string.Empty;
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

/// <summary>
/// 排序类型项
/// </summary>
public class SortTypeItem
{
    /// <summary>
    /// 排序类型ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 排序类型名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 排序类型描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string Icon { get; set; } = string.Empty;
}
