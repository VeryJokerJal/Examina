using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Examina.Extensions;
using Examina.Models;
using Examina.Models.SpecializedTraining;
using Examina.Services;
using Examina.Views;
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

        // 初始化用户权限状态
        UpdateUserPermissions();

        // 监听用户信息更新事件
        _authenticationService.UserInfoUpdated += OnUserInfoUpdated;

        _ = InitializeAsync();

        // 监听属性变化
        _ = this.WhenAnyValue(x => x.Trainings.Count)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(HasTrainings));
                this.RaisePropertyChanged(nameof(ShowEmptyState));
            });

        _ = this.WhenAnyValue(x => x.IsLoading, x => x.ErrorMessage)
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
            IsLoading = true;
            ErrorMessage = string.Empty;

            System.Diagnostics.Debug.WriteLine($"开始专项训练: {training.Name}");

            // 直接启动BenchSuite进行训练，无需API调用
            await StartBenchSuiteTrainingAsync(training);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"开始训练失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"开始专项训练失败: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 启动BenchSuite训练
    /// </summary>
    private async Task StartBenchSuiteTrainingAsync(StudentSpecializedTrainingDto training)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"启动BenchSuite专项训练: {training.Name}");
            System.Diagnostics.Debug.WriteLine($"模块类型: {training.ModuleType}");
            System.Diagnostics.Debug.WriteLine($"题目数量: {training.QuestionCount}");
            System.Diagnostics.Debug.WriteLine($"预计时长: {training.Duration}分钟");

            // 文件预下载准备
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 开始文件预下载准备");

                bool filesReady = await desktop.MainWindow.PrepareFilesForSpecializedTrainingAsync(training.Id, training.Name);
                if (!filesReady)
                {
                    ErrorMessage = "文件准备失败，无法开始专项训练。请检查网络连接或联系管理员。";
                    System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 文件预下载失败，取消专项训练启动");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 文件预下载完成，继续启动专项训练");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 无法获取主窗口，跳过文件预下载");
            }

            // 创建考试工具栏ViewModel
            ExamToolbarViewModel toolbarViewModel = new();

            // 设置考试信息
            toolbarViewModel.SetExamInfo(
                ExamType.SpecializedTraining,
                training.Id,
                training.Name,
                training.QuestionCount,
                training.Duration * 60 // 转换为秒
            );

            // 启动考试（切换到进行中状态并开始计时）
            toolbarViewModel.StartExam();
            System.Diagnostics.Debug.WriteLine($"专项训练已启动 - 状态: {toolbarViewModel.CurrentExamStatus}");

            // 创建考试工具栏窗口
            ExamToolbarWindow examToolbar = new();
            examToolbar.SetViewModel(toolbarViewModel);

            System.Diagnostics.Debug.WriteLine($"专项训练工具栏已配置 - 训练ID: {training.Id}, 题目数: {training.QuestionCount}, 时长: {training.Duration}分钟");

            // 订阅考试事件
            examToolbar.ExamAutoSubmitted += (sender, e) => OnTrainingAutoSubmitted(sender, e, training.Id);
            examToolbar.ExamManualSubmitted += (sender, e) => OnTrainingManualSubmitted(sender, e, training.Id);
            examToolbar.ViewQuestionsRequested += (sender, e) => OnViewQuestionsRequested(training);

            System.Diagnostics.Debug.WriteLine("已订阅专项训练工具栏事件");

            // 显示工具栏窗口
            examToolbar.Show();
            System.Diagnostics.Debug.WriteLine("专项训练工具栏窗口已显示");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"启动BenchSuite训练失败: {ex}");
            throw;
        }
    }

    /// <summary>
    /// 处理训练自动提交事件
    /// </summary>
    private async void OnTrainingAutoSubmitted(object? sender, EventArgs e, int trainingId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"专项训练自动提交，ID: {trainingId}");
            await SubmitTrainingWithBenchSuiteAsync(trainingId, isAutoSubmit: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"处理专项训练自动提交事件失败: {ex}");
        }
    }

    /// <summary>
    /// 处理训练手动提交事件
    /// </summary>
    private async void OnTrainingManualSubmitted(object? sender, EventArgs e, int trainingId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"专项训练手动提交，ID: {trainingId}");
            await SubmitTrainingWithBenchSuiteAsync(trainingId, isAutoSubmit: false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"处理专项训练手动提交事件失败: {ex}");
        }
    }

    /// <summary>
    /// 处理专项训练提交（简化版本，无API调用）
    /// </summary>
    private async Task SubmitTrainingWithBenchSuiteAsync(int trainingId, bool isAutoSubmit)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"专项训练提交完成，ID: {trainingId}, 自动提交: {isAutoSubmit}");

            // 专项训练提交后直接完成，无需API调用
            // BenchSuite会处理实际的评分和结果记录

            System.Diagnostics.Debug.WriteLine("专项训练已通过BenchSuite完成");

            // 可选：刷新训练列表
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"处理专项训练提交失败: {ex}");
            ErrorMessage = $"训练提交处理失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 处理查看题目详情请求
    /// </summary>
    private void OnViewQuestionsRequested(StudentSpecializedTrainingDto training)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"查看专项训练题目详情: {training.Name}");
            // TODO: 实现题目详情窗口
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"查看专项训练题目详情失败: {ex}");
        }
    }



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
        else
        {
            trainings = !string.IsNullOrWhiteSpace(SelectedModuleType)
                ? await _studentSpecializedTrainingService.GetTrainingsByModuleTypeAsync(SelectedModuleType, CurrentPage, PageSize)
                : await _studentSpecializedTrainingService.GetAvailableTrainingsAsync(CurrentPage, PageSize);
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
        HasFullAccess = _authenticationService.IsAuthenticated && _authenticationService.CurrentUser != null && _authenticationService.CurrentUser.HasFullAccess;
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    private void OnUserInfoUpdated(object? sender, UserInfo? userInfo)
    {
        UpdateUserPermissions();
    }
}


