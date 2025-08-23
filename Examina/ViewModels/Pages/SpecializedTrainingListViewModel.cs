using System.Collections.ObjectModel;
using System.Reactive;
using Examina.Models.SpecializedTraining;
using Examina.Services;
using ReactiveUI;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 专项训练列表ViewModel
/// </summary>
public class SpecializedTrainingListViewModel : ViewModelBase
{
    private readonly IStudentSpecializedTrainingService _studentSpecializedTrainingService;
    private readonly IAuthenticationService _authenticationService;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private int _totalCount;
    private int _currentPage = 1;
    private bool _hasFullAccess;
    private string _searchKeyword = string.Empty;
    private string _selectedModuleType = string.Empty;
    private int _selectedDifficultyLevel = 0;
    private const int PageSize = 20;

    /// <summary>
    /// 专项训练列表
    /// </summary>
    public ObservableCollection<StudentSpecializedTrainingDto> Trainings { get; } = [];

    /// <summary>
    /// 可用的模块类型列表
    /// </summary>
    public ObservableCollection<string> ModuleTypes { get; } = [];

    /// <summary>
    /// 难度等级选项
    /// </summary>
    public ObservableCollection<DifficultyOption> DifficultyOptions { get; } = 
    [
        new() { Level = 0, Text = "全部难度" },
        new() { Level = 1, Text = "入门" },
        new() { Level = 2, Text = "初级" },
        new() { Level = 3, Text = "中级" },
        new() { Level = 4, Text = "高级" },
        new() { Level = 5, Text = "专家" }
    ];

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
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount
    {
        get => _totalCount;
        set => this.RaiseAndSetIfChanged(ref _totalCount, value);
    }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string SearchKeyword
    {
        get => _searchKeyword;
        set => this.RaiseAndSetIfChanged(ref _searchKeyword, value);
    }

    /// <summary>
    /// 选中的模块类型
    /// </summary>
    public string SelectedModuleType
    {
        get => _selectedModuleType;
        set => this.RaiseAndSetIfChanged(ref _selectedModuleType, value);
    }

    /// <summary>
    /// 选中的难度等级
    /// </summary>
    public int SelectedDifficultyLevel
    {
        get => _selectedDifficultyLevel;
        set => this.RaiseAndSetIfChanged(ref _selectedDifficultyLevel, value);
    }

    /// <summary>
    /// 是否有更多数据
    /// </summary>
    public bool HasMoreData => Trainings.Count < TotalCount;

    /// <summary>
    /// 是否有训练数据
    /// </summary>
    public bool HasTrainings => Trainings.Count > 0;

    /// <summary>
    /// 是否显示空状态
    /// </summary>
    public bool ShowEmptyState => !IsLoading && !HasTrainings && string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// 用户是否拥有完整功能权限
    /// </summary>
    public bool HasFullAccess
    {
        get => _hasFullAccess;
        set => this.RaiseAndSetIfChanged(ref _hasFullAccess, value);
    }

    /// <summary>
    /// 开始训练按钮文本
    /// </summary>
    public string StartButtonText => HasFullAccess ? "开始训练" : "解锁";

    /// <summary>
    /// 刷新命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    /// <summary>
    /// 加载更多命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadMoreCommand { get; }

    /// <summary>
    /// 搜索命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SearchCommand { get; }

    /// <summary>
    /// 筛选命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> FilterCommand { get; }

    /// <summary>
    /// 清除筛选命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearFilterCommand { get; }

    /// <summary>
    /// 开始训练命令
    /// </summary>
    public ReactiveCommand<StudentSpecializedTrainingDto, Unit> StartTrainingCommand { get; }

    /// <summary>
    /// 查看详情命令
    /// </summary>
    public ReactiveCommand<StudentSpecializedTrainingDto, Unit> ViewDetailsCommand { get; }

    public SpecializedTrainingListViewModel(
        IStudentSpecializedTrainingService studentSpecializedTrainingService,
        IAuthenticationService authenticationService)
    {
        _studentSpecializedTrainingService = studentSpecializedTrainingService;
        _authenticationService = authenticationService;

        // 初始化命令
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync, this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));
        LoadMoreCommand = ReactiveCommand.CreateFromTask(LoadMoreAsync, this.WhenAnyValue(x => x.IsLoading, x => x.HasMoreData, (loading, hasMore) => !loading && hasMore));
        SearchCommand = ReactiveCommand.CreateFromTask(SearchAsync, this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));
        FilterCommand = ReactiveCommand.CreateFromTask(FilterAsync, this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));
        ClearFilterCommand = ReactiveCommand.CreateFromTask(ClearFilterAsync, this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));
        StartTrainingCommand = ReactiveCommand.CreateFromTask<StudentSpecializedTrainingDto>(StartTrainingAsync, this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));
        ViewDetailsCommand = ReactiveCommand.CreateFromTask<StudentSpecializedTrainingDto>(ViewDetailsAsync, this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));

        // 初始化用户权限状态
        UpdateUserPermissions();

        // 监听用户信息更新事件
        _authenticationService.UserInfoUpdated += OnUserInfoUpdated;

        // 监听属性变化
        this.WhenAnyValue(x => x.Trainings.Count)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(HasTrainings));
                this.RaisePropertyChanged(nameof(ShowEmptyState));
            });

        this.WhenAnyValue(x => x.IsLoading, x => x.ErrorMessage)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(ShowEmptyState)));
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadModuleTypesAsync();
        await RefreshAsync();
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            CurrentPage = 1;
            Trainings.Clear();

            await LoadTrainingsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"刷新失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"刷新专项训练列表失败: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 加载更多数据
    /// </summary>
    private async Task LoadMoreAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            CurrentPage++;

            await LoadTrainingsAsync(append: true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载更多失败: {ex.Message}";
            CurrentPage--; // 回滚页码
            System.Diagnostics.Debug.WriteLine($"加载更多专项训练失败: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 搜索
    /// </summary>
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        Trainings.Clear();
        await LoadTrainingsAsync();
    }

    /// <summary>
    /// 筛选
    /// </summary>
    private async Task FilterAsync()
    {
        CurrentPage = 1;
        Trainings.Clear();
        await LoadTrainingsAsync();
    }

    /// <summary>
    /// 清除筛选
    /// </summary>
    private async Task ClearFilterAsync()
    {
        SearchKeyword = string.Empty;
        SelectedModuleType = string.Empty;
        SelectedDifficultyLevel = 0;
        await RefreshAsync();
    }

    /// <summary>
    /// 开始训练
    /// </summary>
    private async Task StartTrainingAsync(StudentSpecializedTrainingDto training)
    {
        if (!HasFullAccess)
        {
            ErrorMessage = "您需要解锁权限才能开始专项训练。请加入学校组织或联系管理员进行解锁。";
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"快速开始专项训练: {training.Name}");

            // 直接导航到详情页面，在详情页面中开始训练
            DetailViewRequested?.Invoke(training.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"开始训练失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"开始专项训练失败: {ex}");
        }
    }

    /// <summary>
    /// 查看详情
    /// </summary>
    private async Task ViewDetailsAsync(StudentSpecializedTrainingDto training)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"查看专项训练详情: {training.Name}");

            // 触发详情查看事件，由主窗口处理导航
            DetailViewRequested?.Invoke(training.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"查看详情失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"查看专项训练详情失败: {ex}");
        }
    }

    /// <summary>
    /// 详情查看请求事件
    /// </summary>
    public event Action<int>? DetailViewRequested;

    /// <summary>
    /// 加载训练数据
    /// </summary>
    private async Task LoadTrainingsAsync(bool append = false)
    {
        List<StudentSpecializedTrainingDto> trainings;

        // 根据筛选条件选择不同的API
        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            trainings = await _studentSpecializedTrainingService.SearchTrainingsAsync(SearchKeyword, CurrentPage, PageSize);
        }
        else if (!string.IsNullOrWhiteSpace(SelectedModuleType))
        {
            trainings = await _studentSpecializedTrainingService.GetTrainingsByModuleTypeAsync(SelectedModuleType, CurrentPage, PageSize);
        }
        else if (SelectedDifficultyLevel > 0)
        {
            trainings = await _studentSpecializedTrainingService.GetTrainingsByDifficultyAsync(SelectedDifficultyLevel, CurrentPage, PageSize);
        }
        else
        {
            trainings = await _studentSpecializedTrainingService.GetAvailableTrainingsAsync(CurrentPage, PageSize);
        }

        if (!append)
        {
            Trainings.Clear();
        }

        foreach (StudentSpecializedTrainingDto training in trainings)
        {
            Trainings.Add(training);
        }

        // 更新总数（仅在第一页时）
        if (CurrentPage == 1)
        {
            TotalCount = await _studentSpecializedTrainingService.GetAvailableTrainingCountAsync();
        }
    }

    /// <summary>
    /// 加载模块类型
    /// </summary>
    private async Task LoadModuleTypesAsync()
    {
        try
        {
            List<string> moduleTypes = await _studentSpecializedTrainingService.GetAvailableModuleTypesAsync();
            ModuleTypes.Clear();
            ModuleTypes.Add("全部模块"); // 添加默认选项
            foreach (string moduleType in moduleTypes)
            {
                ModuleTypes.Add(moduleType);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载模块类型失败: {ex}");
        }
    }

    /// <summary>
    /// 更新用户权限状态
    /// </summary>
    private void UpdateUserPermissions()
    {
        if (_authenticationService.IsAuthenticated && _authenticationService.CurrentUser != null)
        {
            HasFullAccess = _authenticationService.CurrentUser.HasFullAccess;
        }
        else
        {
            HasFullAccess = false;
        }
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    private void OnUserInfoUpdated()
    {
        UpdateUserPermissions();
    }
}

/// <summary>
/// 难度选项
/// </summary>
public class DifficultyOption
{
    public int Level { get; set; }
    public string Text { get; set; } = string.Empty;
}
