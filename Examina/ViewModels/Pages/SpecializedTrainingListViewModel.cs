using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BenchSuite.Models;
using Examina.Extensions;
using Examina.Models;
using Examina.Models.SpecializedTraining;
using Examina.Services;
using Examina.Views;
using Examina.Views.Windows;
using ReactiveUI;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 专项训练列表ViewModel
/// </summary>
public class SpecializedTrainingListViewModel : ViewModelBase
{
    private readonly IStudentSpecializedTrainingService _studentSpecializedTrainingService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IBenchSuiteIntegrationService? _benchSuiteIntegrationService;
    private readonly IBenchSuiteDirectoryService? _benchSuiteDirectoryService;
    private readonly OpenXmlScoringManager? _openXmlScoringManager;
    private DateTime _trainingStartTime;
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
    public string StartButtonText
    {
        get
        {
            string buttonText = HasFullAccess ? "开始答题" : "试做";
            System.Diagnostics.Debug.WriteLine($"[SpecializedTraining] StartButtonText被访问 - HasFullAccess: {HasFullAccess}, 按钮文本: {buttonText}");
            return buttonText;
        }
    }

    /// <summary>
    /// 获取特定训练的按钮文本
    /// </summary>
    public string GetButtonText(StudentSpecializedTrainingDto training)
    {
        string buttonText = HasFullAccess ? "开始答题" : training.EnableTrial ? "试做" : "解锁";
        System.Diagnostics.Debug.WriteLine($"[SpecializedTraining] GetButtonText - 训练: {training.Name}, HasFullAccess: {HasFullAccess}, EnableTrial: {training.EnableTrial}, 按钮文本: {buttonText}");
        return buttonText;
    }

    /// <summary>
    /// 检查训练按钮是否可用
    /// </summary>
    public bool CanStartTraining(StudentSpecializedTrainingDto training)
    {
        // 有权限用户：始终可以开始训练
        // 无权限用户：只有在EnableTrial=true时可以试做，EnableTrial=false时可以点击解锁
        bool canStart = HasFullAccess || training.EnableTrial;
        System.Diagnostics.Debug.WriteLine($"[SpecializedTraining] CanStartTraining - 训练: {training.Name}, HasFullAccess: {HasFullAccess}, EnableTrial: {training.EnableTrial}, 结果: {canStart}");
        return canStart;
    }

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
        // 添加调试信息来诊断服务注入问题
        System.Diagnostics.Debug.WriteLine($"[SpecializedTrainingListViewModel] 构造函数被调用");
        System.Diagnostics.Debug.WriteLine($"[SpecializedTrainingListViewModel] studentSpecializedTrainingService: {studentSpecializedTrainingService?.GetType().Name ?? "NULL"}");
        System.Diagnostics.Debug.WriteLine($"[SpecializedTrainingListViewModel] authenticationService: {authenticationService?.GetType().Name ?? "NULL"}");

        _studentSpecializedTrainingService = studentSpecializedTrainingService ?? throw new ArgumentNullException(nameof(studentSpecializedTrainingService), "StudentSpecializedTrainingService不能为null");
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService), "AuthenticationService不能为null");

        // 尝试获取BenchSuite服务（可选）
        _benchSuiteIntegrationService = AppServiceManager.GetService<IBenchSuiteIntegrationService>();
        _benchSuiteDirectoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
        _openXmlScoringManager = AppServiceManager.GetService<OpenXmlScoringManager>();

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
        System.Diagnostics.Debug.WriteLine($"[SpecializedTraining] StartTrainingAsync被调用");
        System.Diagnostics.Debug.WriteLine($"[SpecializedTraining] 训练信息: ID={training.Id}, Name={training.Name}");
        System.Diagnostics.Debug.WriteLine($"[SpecializedTraining] 当前用户权限状态: HasFullAccess={HasFullAccess}");
        System.Diagnostics.Debug.WriteLine($"[SpecializedTraining] 训练试做支持状态: EnableTrial={training.EnableTrial}");

        // 权限检查逻辑：
        // 1. 有权限用户：始终可以开始训练，不受EnableTrial影响
        // 2. 无权限用户：需要检查EnableTrial状态
        if (HasFullAccess)
        {
            // 有权限用户，直接开始训练
            System.Diagnostics.Debug.WriteLine("[SpecializedTraining] 用户有完整权限，直接开始训练");
        }
        else
        {
            // 无权限用户，检查试做设置
            if (training.EnableTrial)
            {
                // 支持试做，允许开始
                System.Diagnostics.Debug.WriteLine("[SpecializedTraining] 用户无权限但训练支持试做，允许开始");
            }
            else
            {
                // 不支持试做，显示解锁推广窗口
                System.Diagnostics.Debug.WriteLine("[SpecializedTraining] 用户无权限且训练不支持试做，显示解锁推广窗口");
                await ShowUnlockPromotionWindowAsync();
                return;
            }
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            System.Diagnostics.Debug.WriteLine($"[SpecializedTraining] 权限和试做检查通过，开始专项训练: {training.Name}");

            // 获取包含模块详细信息的训练数据
            System.Diagnostics.Debug.WriteLine($"获取训练详情，训练ID: {training.Id}");
            System.Diagnostics.Debug.WriteLine($"[SpecializedTraining] _studentSpecializedTrainingService状态: {_studentSpecializedTrainingService?.GetType().Name ?? "NULL"}");

            if (_studentSpecializedTrainingService == null)
            {
                ErrorMessage = "专项训练服务未初始化，请重新启动应用程序。";
                System.Diagnostics.Debug.WriteLine("[SpecializedTraining] 错误：_studentSpecializedTrainingService为null");
                return;
            }

            StudentSpecializedTrainingDto? detailedTraining = await _studentSpecializedTrainingService.GetTrainingDetailsAsync(training.Id);

            if (detailedTraining == null)
            {
                ErrorMessage = "无法获取训练详情，请稍后重试。";
                System.Diagnostics.Debug.WriteLine($"获取训练详情失败，训练ID: {training.Id}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"训练详情获取成功，模块数量: {detailedTraining.Modules.Count}, 题目数量: {detailedTraining.Questions.Count}");

            // 启动BenchSuite进行训练，使用包含完整模块信息的数据
            await StartBenchSuiteTrainingAsync(detailedTraining);
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
            // 记录训练开始时间
            _trainingStartTime = DateTime.Now;

            System.Diagnostics.Debug.WriteLine($"启动BenchSuite专项训练: {training.Name}");
            System.Diagnostics.Debug.WriteLine($"模块类型: {training.ModuleType}");
            System.Diagnostics.Debug.WriteLine($"题目数量: {training.QuestionCount}");
            System.Diagnostics.Debug.WriteLine($"预计时长: {training.Duration}分钟");

            // 修复执行顺序：先清理目录，再下载解压文件
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                // 第一步：清理考试目录（在下载文件之前）
                System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 开始清理考试目录");
                IDirectoryCleanupService? directoryCleanupService = AppServiceManager.GetService<IDirectoryCleanupService>();
                if (directoryCleanupService != null)
                {
                    DirectoryCleanupResult cleanupResult = await directoryCleanupService.CleanupExamDirectoryAsync();
                    if (!cleanupResult.IsSuccess)
                    {
                        System.Diagnostics.Debug.WriteLine($"SpecializedTrainingListViewModel: 目录清理失败: {cleanupResult.ErrorMessage}");
                        ErrorMessage = $"目录清理失败，无法开始专项训练: {cleanupResult.ErrorMessage}";
                        return;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"SpecializedTrainingListViewModel: 目录清理成功，删除文件: {cleanupResult.DeletedFileCount}, 删除目录: {cleanupResult.DeletedDirectoryCount}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 目录清理服务不可用，跳过清理步骤");
                }

                // 第二步：文件预下载准备（在目录清理之后）
                System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 开始文件预下载准备");

                bool filesReady = await desktop.MainWindow.PrepareFilesForSpecializedTrainingAsync(training.Id, training.Name);
                if (!filesReady)
                {
                    ErrorMessage = "文件准备失败，无法开始专项训练。请检查网络连接或联系管理员。";
                    System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 文件预下载失败，取消专项训练启动");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 文件预下载完成，继续启动专项训练");

                // 隐藏主窗口
                desktop.MainWindow.Hide();
                System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 主窗口已隐藏");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 无法获取主窗口，跳过文件预下载");
            }

            // 创建考试工具栏ViewModel
            IBenchSuiteDirectoryService? benchSuiteDirectoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
            ExamToolbarViewModel toolbarViewModel = new(_authenticationService, null, benchSuiteDirectoryService);

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
            examToolbar.ViewAnswerAnalysisRequested += (sender, e) => OnViewAnswerAnalysisRequested(training);

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
    /// 处理专项训练提交（包含BenchSuite评分和结果显示）
    /// </summary>
    private async Task SubmitTrainingWithBenchSuiteAsync(int trainingId, bool isAutoSubmit)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"专项训练提交完成，ID: {trainingId}, 自动提交: {isAutoSubmit}");

            // 获取训练信息
            StudentSpecializedTrainingDto? training = await GetTrainingByIdAsync(trainingId);
            if (training == null)
            {
                System.Diagnostics.Debug.WriteLine($"无法获取训练信息，ID: {trainingId}");
                CloseTrainingAndShowMainWindow();
                return;
            }

            // 获取BenchSuite评分结果
            Dictionary<ModuleType, ScoringResult>? scoringResults = await GetBenchSuiteScoringResultAsync(trainingId, training);

            if (scoringResults != null && scoringResults.Count > 0)
            {
                // 显示训练结果窗口，窗口关闭后会自动显示主窗口
                await ShowTrainingResultAsync(training.Name, scoringResults);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("无法获取BenchSuite评分结果或评分失败");
                // 即使评分失败也显示基本结果，窗口关闭后会自动显示主窗口
                ShowBasicTrainingResult(training.Name);
            }

            System.Diagnostics.Debug.WriteLine("专项训练已通过BenchSuite完成");

            // 结果窗口关闭后会自动显示主窗口，这里不需要手动调用
            // CloseTrainingAndShowMainWindow(); // 移除过早的主窗口显示调用
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"处理专项训练提交失败: {ex}");
            ErrorMessage = $"训练提交处理失败: {ex.Message}";

            // 即使出错也要关闭训练并显示主窗口
            CloseTrainingAndShowMainWindow();
        }
    }

    /// <summary>
    /// 根据ID获取训练信息
    /// </summary>
    private async Task<StudentSpecializedTrainingDto?> GetTrainingByIdAsync(int trainingId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[GetTrainingByIdAsync] 开始获取训练详情，ID: {trainingId}");

            StudentSpecializedTrainingDto? training = await _studentSpecializedTrainingService.GetTrainingDetailsAsync(trainingId);

            if (training != null)
            {
                System.Diagnostics.Debug.WriteLine($"[GetTrainingByIdAsync] 训练详情获取成功:");
                System.Diagnostics.Debug.WriteLine($"[GetTrainingByIdAsync]   名称: {training.Name}");
                System.Diagnostics.Debug.WriteLine($"[GetTrainingByIdAsync]   模块类型: {training.ModuleType}");
                System.Diagnostics.Debug.WriteLine($"[GetTrainingByIdAsync]   模块数量: {training.Modules.Count}");
                System.Diagnostics.Debug.WriteLine($"[GetTrainingByIdAsync]   题目数量: {training.Questions.Count}");

                // 详细输出模块信息
                foreach (var module in training.Modules)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetTrainingByIdAsync]   模块: {module.Name} ({module.Type}), 题目数: {module.Questions.Count}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[GetTrainingByIdAsync] 训练详情为空");
            }

            return training;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GetTrainingByIdAsync] 获取训练信息失败，ID: {trainingId}, 错误: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[GetTrainingByIdAsync] 异常详情: {ex}");
            return null;
        }
    }

    /// <summary>
    /// 获取BenchSuite评分结果（集成OpenXML评分）
    /// </summary>
    [Obsolete]
    private async Task<Dictionary<ModuleType, ScoringResult>?> GetBenchSuiteScoringResultAsync(int trainingId, StudentSpecializedTrainingDto training)
    {
        try
        {
            if (_benchSuiteIntegrationService == null || _benchSuiteDirectoryService == null)
            {
                System.Diagnostics.Debug.WriteLine("BenchSuite服务不可用");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[GetBenchSuiteScoringResultAsync] 开始构建文件路径字典，训练ID: {trainingId}");

            // 构建文件路径字典
            Dictionary<ModuleType, List<string>> filePaths = [];

            // 根据训练的模块类型确定文件类型
            ModuleType moduleType = GetModuleTypeFromString(training.ModuleType);
            System.Diagnostics.Debug.WriteLine($"[GetBenchSuiteScoringResultAsync] 训练模块类型: '{training.ModuleType}' -> {moduleType}");

            // 扫描考试目录中的文件
            System.Diagnostics.Debug.WriteLine($"[GetBenchSuiteScoringResultAsync] 开始扫描文件...");
            List<string> moduleFiles = ScanModuleFiles(moduleType, training);
            filePaths[moduleType] = moduleFiles;

            System.Diagnostics.Debug.WriteLine($"[GetBenchSuiteScoringResultAsync] 扫描完成，{moduleType} 模块文件 {moduleFiles.Count} 个");

            if (moduleFiles.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[GetBenchSuiteScoringResultAsync] ⚠️ 警告：未找到任何文件！");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[GetBenchSuiteScoringResultAsync] 文件列表:");
                for (int i = 0; i < moduleFiles.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetBenchSuiteScoringResultAsync]   {i + 1}. {moduleFiles[i]}");
                }
            }

            // 如果有OpenXML评分管理器，优先使用OpenXML评分
            if (_openXmlScoringManager != null && moduleFiles.Count > 0)
            {
                Dictionary<ModuleType, ScoringResult> openXmlResults = await PerformOpenXmlScoringAsync(moduleType, moduleFiles, training);
                if (openXmlResults.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"OpenXML评分完成，模块数量: {openXmlResults.Count}");
                    return openXmlResults;
                }
            }

            // 回退到传统BenchSuite评分
            Dictionary<ModuleType, ScoringResult> results = await _benchSuiteIntegrationService.ScoreExamAsync(
                ExamType.SpecializedTraining,
                trainingId,
                1, // TODO: 从认证服务获取实际用户ID
                filePaths);

            System.Diagnostics.Debug.WriteLine($"BenchSuite评分完成，模块数量: {results.Count}");

            return results;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取BenchSuite评分结果失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 扫描模块文件
    /// </summary>
    private List<string> ScanModuleFiles(ModuleType moduleType, StudentSpecializedTrainingDto training)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 开始扫描，目标模块类型: {moduleType}, 训练ID: {training.Id}, 训练名称: {training.Name}");
            System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 训练模块类型: {training.ModuleType}, 模块数量: {training.Modules.Count}");

            List<string> files = [];

            // 遍历训练的所有模块
            foreach (StudentSpecializedTrainingModuleDto module in training.Modules)
            {
                System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 检查模块: {module.Name}, 类型: {module.Type}, 题目数量: {module.Questions.Count}");

                // 根据模块类型获取对应的ModuleType枚举
                ModuleType currentModuleType = GetModuleTypeFromString(module.Type);
                System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 模块类型转换: '{module.Type}' -> {currentModuleType}");

                // 只处理匹配的模块类型
                if (currentModuleType != moduleType)
                {
                    System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 跳过不匹配的模块类型: {currentModuleType} != {moduleType}");
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 处理匹配的模块: {module.Name}");

                // 遍历模块中的所有题目
                foreach (StudentSpecializedTrainingQuestionDto question in module.Questions)
                {
                    System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 检查题目: ID={question.Id}, 标题='{question.Title}'");

                    string? filePath = null;

                    // 根据模块类型获取相应的文件路径
                    switch (moduleType)
                    {
                        case ModuleType.CSharp:
                            filePath = question.CodeFilePath;
                            System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] C#题目，CodeFilePath: '{filePath}'");
                            break;
                        case ModuleType.Word:
                        case ModuleType.PowerPoint:
                        case ModuleType.Excel:
                            filePath = question.DocumentFilePath;
                            System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] Office题目，DocumentFilePath: '{filePath}'");
                            break;
                        default:
                            System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 不支持的模块类型: {moduleType}");
                            continue;
                    }

                    // 验证文件路径
                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 开始路径转换，原路径: '{filePath}'");

                        // 转换为绝对路径
                        string absolutePath = ConvertToAbsolutePath(filePath, training, moduleType);
                        System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 路径转换完成，绝对路径: '{absolutePath}'");

                        if (File.Exists(absolutePath))
                        {
                            files.Add(absolutePath);
                            System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] ✓ 找到有效文件: {Path.GetFileName(absolutePath)} (原路径: {filePath})");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] ✗ 文件不存在: {absolutePath} (原路径: {filePath})");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 题目文件路径为空，跳过");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 扫描完成，{moduleType} 模块文件 {files.Count} 个");
            if (files.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 文件列表: {string.Join(", ", files.Select(Path.GetFileName))}");
            }

            return files;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 扫描模块文件失败: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ScanModuleFiles] 异常堆栈: {ex.StackTrace}");
            return [];
        }
    }

    /// <summary>
    /// 执行OpenXML评分
    /// </summary>
    [Obsolete]
    private async Task<Dictionary<ModuleType, ScoringResult>> PerformOpenXmlScoringAsync(ModuleType moduleType, List<string> files, StudentSpecializedTrainingDto training)
    {
        Dictionary<ModuleType, ScoringResult> results = [];

        try
        {
            if (_openXmlScoringManager == null || files.Count == 0)
            {
                return results;
            }

            System.Diagnostics.Debug.WriteLine($"开始OpenXML评分，模块类型: {moduleType}, 文件数量: {files.Count}");

            // 创建模拟的试卷模型（基于训练数据）
            ExamModel examModel = CreateExamModelFromTraining(training);

            // 对每个文件进行评分
            List<ScoringResult> fileResults = [];
            foreach (string filePath in files)
            {
                try
                {
                    if (_openXmlScoringManager.IsFileSupported(filePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"正在评分文件: {Path.GetFileName(filePath)}");

                        ScoringResult fileResult = await _openXmlScoringManager.ScoreFileAsync(filePath, examModel);
                        fileResults.Add(fileResult);

                        System.Diagnostics.Debug.WriteLine($"文件评分完成: {Path.GetFileName(filePath)}, 得分: {fileResult.AchievedScore}/{fileResult.TotalScore}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"不支持的文件类型: {Path.GetFileName(filePath)}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"文件评分失败: {Path.GetFileName(filePath)}, 错误: {ex.Message}");
                }
            }

            // 合并评分结果
            if (fileResults.Count > 0)
            {
                ScoringResult combinedResult = CombineScoringResults(fileResults, training.Name);
                results[moduleType] = combinedResult;

                System.Diagnostics.Debug.WriteLine($"OpenXML评分完成，模块: {moduleType}, 总得分: {combinedResult.AchievedScore}/{combinedResult.TotalScore}");
            }

            return results;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenXML评分失败: {ex.Message}");
            return results;
        }
    }

    /// <summary>
    /// 从字符串获取模块类型
    /// </summary>
    private static ModuleType GetModuleTypeFromString(string moduleType)
    {
        return moduleType.ToLower() switch
        {
            "word" => ModuleType.Word,
            "excel" => ModuleType.Excel,
            "powerpoint" => ModuleType.PowerPoint,
            "csharp" => ModuleType.CSharp,
            "windows" => ModuleType.Windows,
            _ => ModuleType.Windows
        };
    }



    /// <summary>
    /// 从训练数据创建试卷模型
    /// </summary>
    [Obsolete]
    private static ExamModel CreateExamModelFromTraining(StudentSpecializedTrainingDto training)
    {
        ExamModel examModel = new()
        {
            Id = training.Id.ToString(),
            Name = training.Name,
            Description = training.Description ?? string.Empty,
            TotalScore = training.TotalScore,
            DurationMinutes = training.Duration,
            Modules = []
        };

        // 从训练模块创建ExamModule和操作点
        foreach (StudentSpecializedTrainingModuleDto module in training.Modules)
        {
            ExamModuleModel examModule = new()
            {
                Id = module.Id.ToString(),
                Name = module.Name,
                Description = module.Description ?? string.Empty,
                Score = module.Score,
                Type = GetModuleTypeFromString(training.ModuleType),
                IsEnabled = module.IsEnabled,
                Order = module.Order,
                Questions = []
            };

            foreach (StudentSpecializedTrainingQuestionDto question in module.Questions)
            {
                QuestionModel questionModel = new()
                {
                    Id = question.Id.ToString(),
                    Title = question.Title,
                    Content = question.Content ?? string.Empty,
                    Score = question.Score,
                    QuestionType = question.QuestionType ?? "检测",
                    Order = question.Order,
                    OperationPoints = []
                };

                // 映射题目中的操作点
                foreach (StudentSpecializedTrainingOperationPointDto operationPointDto in question.OperationPoints)
                {
                    OperationPointModel operationPoint = new()
                    {
                        Id = operationPointDto.Id.ToString(),
                        Name = string.IsNullOrWhiteSpace(operationPointDto.Name) ? $"操作点_{operationPointDto.Id}" : operationPointDto.Name,
                        Description = operationPointDto.Description ?? string.Empty,
                        Score = operationPointDto.Score,
                        Order = operationPointDto.Order,
                        IsEnabled = true,
                        ModuleType = GetModuleTypeFromString(operationPointDto.ModuleType),
                        Parameters = MapOperationPointParameters(operationPointDto.Parameters)
                    };

                    questionModel.OperationPoints.Add(operationPoint);
                }

                // 如果题目没有操作点，创建一个默认操作点
                if (questionModel.OperationPoints.Count == 0)
                {
                    OperationPointModel defaultOperationPoint = new()
                    {
                        Id = $"default_{question.Id}",
                        Name = question.Title,
                        Description = question.Content ?? string.Empty,
                        Score = question.Score,
                        Order = 1,
                        IsEnabled = true,
                        ModuleType = GetModuleTypeFromString(training.ModuleType),
                        Parameters = []
                    };

                    questionModel.OperationPoints.Add(defaultOperationPoint);
                }

                examModule.Questions.Add(questionModel);
            }

            examModel.Modules.Add(examModule);
        }

        // 如果没有模块，从直接题目列表创建默认模块
        if (examModel.Modules.Count == 0)
        {
            ExamModuleModel defaultModule = new()
            {
                Id = "default-module",
                Name = "默认模块",
                Description = "从直接题目列表创建的默认模块",
                Score = training.TotalScore,
                Type = GetModuleTypeFromString(training.ModuleType),
                IsEnabled = true,
                Order = 1,
                Questions = []
            };

            foreach (StudentSpecializedTrainingQuestionDto question in training.Questions)
            {
                QuestionModel questionModel = new()
                {
                    Id = question.Id.ToString(),
                    Title = question.Title,
                    Content = question.Content ?? string.Empty,
                    Score = question.Score,
                    QuestionType = question.QuestionType ?? "检测",
                    Order = question.Order,
                    OperationPoints = []
                };

                // 映射题目中的操作点
                foreach (StudentSpecializedTrainingOperationPointDto operationPointDto in question.OperationPoints)
                {
                    OperationPointModel operationPoint = new()
                    {
                        Id = operationPointDto.Id.ToString(),
                        Name = string.IsNullOrWhiteSpace(operationPointDto.Name) ? $"操作点_{operationPointDto.Id}" : operationPointDto.Name,
                        Description = operationPointDto.Description ?? string.Empty,
                        Score = operationPointDto.Score,
                        Order = operationPointDto.Order,
                        IsEnabled = true,
                        ModuleType = GetModuleTypeFromString(operationPointDto.ModuleType),
                        Parameters = MapOperationPointParameters(operationPointDto.Parameters)
                    };

                    questionModel.OperationPoints.Add(operationPoint);
                }

                // 如果题目没有操作点，创建一个默认操作点
                if (questionModel.OperationPoints.Count == 0)
                {
                    OperationPointModel defaultOperationPoint = new()
                    {
                        Id = $"default_{question.Id}",
                        Name = question.Title,
                        Description = question.Content ?? string.Empty,
                        Score = question.Score,
                        Order = 1,
                        IsEnabled = true,
                        ModuleType = GetModuleTypeFromString(training.ModuleType),
                        Parameters = []
                    };

                    questionModel.OperationPoints.Add(defaultOperationPoint);
                }

                defaultModule.Questions.Add(questionModel);
            }

            examModel.Modules.Add(defaultModule);
        }

        return examModel;
    }

    /// <summary>
    /// 映射操作点参数
    /// </summary>
    private static List<ConfigurationParameterModel> MapOperationPointParameters(IEnumerable<StudentSpecializedTrainingParameterDto> parameters)
    {
        List<ConfigurationParameterModel> configParameters = [];

        foreach (StudentSpecializedTrainingParameterDto paramDto in parameters)
        {
            ConfigurationParameterModel configParam = new()
            {
                Id = paramDto.Id.ToString(),
                Name = paramDto.Name,
                DisplayName = paramDto.Name,
                Value = paramDto.Value ?? paramDto.DefaultValue ?? string.Empty,
                Type = ParseParameterType(paramDto.ParameterType),
                IsRequired = false, // StudentSpecializedTrainingParameterDto没有IsRequired属性，默认为false
                DefaultValue = paramDto.DefaultValue,
                Description = paramDto.Description ?? string.Empty,
                Order = 0, // StudentSpecializedTrainingParameterDto没有Order属性，默认为0
                IsVisible = true
            };

            configParameters.Add(configParam);
        }

        return configParameters;
    }

    /// <summary>
    /// 解析参数类型
    /// </summary>
    private static ParameterType ParseParameterType(string parameterType)
    {
        return parameterType?.ToLower() switch
        {
            "string" or "text" => ParameterType.Text,
            "int" or "integer" or "number" or "double" or "decimal" or "float" => ParameterType.Number,
            "bool" or "boolean" => ParameterType.Boolean,
            "enum" => ParameterType.Enum,
            "color" => ParameterType.Color,
            "file" => ParameterType.File,
            "folder" or "directory" => ParameterType.Folder,
            "path" => ParameterType.Path,
            "multiplechoice" or "multiple_choice" => ParameterType.MultipleChoice,
            "date" or "datetime" => ParameterType.Date,
            _ => ParameterType.Text
        };
    }

    /// <summary>
    /// 合并多个评分结果
    /// </summary>
    private static ScoringResult CombineScoringResults(List<ScoringResult> results, string examName)
    {
        if (results.Count == 0)
        {
            return new ScoringResult
            {
                QuestionTitle = examName,
                TotalScore = 0,
                AchievedScore = 0,
                KnowledgePointResults = [],
                StartTime = DateTime.Now,
                IsSuccess = false,
                ErrorMessage = "没有评分结果"
            };
        }

        if (results.Count == 1)
        {
            return results[0];
        }

        // 合并多个结果
        ScoringResult combined = new()
        {
            QuestionTitle = examName,
            TotalScore = results.Sum(r => r.TotalScore),
            AchievedScore = results.Sum(r => r.AchievedScore),
            KnowledgePointResults = [],
            StartTime = DateTime.Now,
            IsSuccess = results.All(r => r.IsSuccess),
            ErrorMessage = string.Join("; ", results.Where(r => !string.IsNullOrEmpty(r.ErrorMessage)).Select(r => r.ErrorMessage))
        };

        // 合并知识点结果
        foreach (ScoringResult result in results)
        {
            combined.KnowledgePointResults.AddRange(result.KnowledgePointResults);
        }

        return combined;
    }

    /// <summary>
    /// 显示训练结果窗口
    /// </summary>
    private async Task ShowTrainingResultAsync(string trainingName, Dictionary<ModuleType, ScoringResult> scoringResults)
    {
        try
        {
            // 创建训练结果ViewModel
            TrainingResultViewModel resultViewModel = new();
            resultViewModel.SetTrainingResult(trainingName, scoringResults, _trainingStartTime);

            // 创建训练结果窗口
            TrainingResultWindow resultWindow = new()
            {
                DataContext = resultViewModel
            };

            // 显示结果窗口（非模态，因为主窗口已隐藏）
            resultWindow.Show();

            // 等待窗口关闭
            await resultWindow.WaitForCloseAsync();

            System.Diagnostics.Debug.WriteLine("训练结果窗口已显示并关闭");

            // 窗口关闭后显示主窗口
            CloseTrainingAndShowMainWindow();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示训练结果窗口失败: {ex.Message}");
            // 如果显示结果窗口失败，也要显示主窗口
            CloseTrainingAndShowMainWindow();
        }
    }

    /// <summary>
    /// 显示基本训练结果（当BenchSuite评分失败时）
    /// </summary>
    private void ShowBasicTrainingResult(string trainingName)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"BenchSuite评分失败，无法显示详细训练结果: {trainingName}");

            // 不显示硬编码的模拟结果，而是显示错误信息并返回主窗口
            ErrorMessage = "训练评分失败，无法获取详细结果";
            CloseTrainingAndShowMainWindow();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"处理训练结果失败: {ex.Message}");
            CloseTrainingAndShowMainWindow();
        }
    }

    /// <summary>
    /// 关闭训练并显示主窗口
    /// </summary>
    private void CloseTrainingAndShowMainWindow()
    {
        try
        {
            // 显示主窗口
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.Activate();
                System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 主窗口已显示");
            }

            // 刷新训练列表
            _ = RefreshAsync();

            System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 训练列表刷新已启动");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SpecializedTrainingListViewModel: 关闭训练并显示主窗口异常: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"训练数据验证 - 模块数量: {training.Modules.Count}, 题目数量: {training.Questions.Count}");

            // 验证模块数据
            if (training.Modules.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ 警告：训练数据中没有模块信息！");
                ErrorMessage = "该训练没有模块信息，无法显示题目详情。";
                return;
            }

            // 创建题目详情窗口
            QuestionDetailsViewModel detailsViewModel = new();

            // 转换模块数据
            List<ModuleItem> moduleItems = [.. training.Modules.Select(module => new ModuleItem
            {
                Id = module.Id,
                Name = module.Name,
                Description = module.Description,
                Type = module.Type,
                Score = module.Score,
                QuestionCount = module.Questions?.Count ?? 0,
                Order = module.Order,
                IsEnabled = module.IsEnabled
            })];

            System.Diagnostics.Debug.WriteLine($"模块详情:");
            foreach (ModuleItem module in moduleItems)
            {
                System.Diagnostics.Debug.WriteLine($"  - 模块: {module.Name}, 描述: {module.Description}, 题目数: {module.QuestionCount}");
            }

            detailsViewModel.SetQuestionDetailsData(training.Name, moduleItems);

            QuestionDetailsWindow detailsWindow = new()
            {
                DataContext = detailsViewModel
            };

            // 显示题目详情窗口
            detailsWindow.Show();
            System.Diagnostics.Debug.WriteLine("专项训练题目详情窗口已显示");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"查看专项训练题目详情失败: {ex}");
            ErrorMessage = $"显示题目详情失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 处理查看答案解析请求
    /// </summary>
    private void OnViewAnswerAnalysisRequested(StudentSpecializedTrainingDto training)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"查看专项训练答案解析: {training.Name}");

            // 创建答案解析窗口
            AnswerAnalysisViewModel analysisViewModel = new();

            // 按模块分组收集题目
            Dictionary<string, List<QuestionItem>> moduleQuestions = [];

            // 从模块中收集题目
            foreach (StudentSpecializedTrainingModuleDto module in training.Modules)
            {
                string moduleName = module.Name;
                List<QuestionItem> questions = [];

                foreach (StudentSpecializedTrainingQuestionDto question in module.Questions)
                {
                    questions.Add(new QuestionItem
                    {
                        Id = question.Id,
                        Title = question.Title,
                        Content = question.Content, // 题目的content属性作为答案解析
                        QuestionType = question.QuestionType,
                        Score = question.Score,
                        SortOrder = question.Order,
                        ModuleName = moduleName
                    });
                }

                if (questions.Count > 0)
                {
                    // 按排序顺序排列题目
                    questions = [.. questions.OrderBy(q => q.SortOrder)];
                    moduleQuestions[moduleName] = questions;
                }
            }

            // 从直接题目列表中收集题目（如果有的话）
            if (training.Questions.Count > 0)
            {
                string directModuleName = "其他题目";
                List<QuestionItem> questions = [];

                foreach (StudentSpecializedTrainingQuestionDto question in training.Questions)
                {
                    questions.Add(new QuestionItem
                    {
                        Id = question.Id,
                        Title = question.Title,
                        Content = question.Content, // 题目的content属性作为答案解析
                        QuestionType = question.QuestionType,
                        Score = question.Score,
                        SortOrder = question.Order,
                        ModuleName = directModuleName
                    });
                }

                // 按排序顺序排列题目
                questions = [.. questions.OrderBy(q => q.SortOrder)];
                moduleQuestions[directModuleName] = questions;
            }

            analysisViewModel.SetAnswerAnalysisData(training.Name, moduleQuestions);

            AnswerAnalysisWindow analysisWindow = new()
            {
                DataContext = analysisViewModel
            };

            // 显示答案解析窗口
            analysisWindow.Show();
            System.Diagnostics.Debug.WriteLine("专项训练答案解析窗口已显示");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"查看专项训练答案解析失败: {ex}");
        }
    }



    /// <summary>
    /// 加载训练数据
    /// </summary>
    private async Task LoadTrainingsAsync(bool append = false)
    {
        List<StudentSpecializedTrainingDto> trainings = !string.IsNullOrWhiteSpace(SearchKeyword)
            ? await _studentSpecializedTrainingService.SearchTrainingsAsync(SearchKeyword, CurrentPage, PageSize)
            : !string.IsNullOrWhiteSpace(SelectedModuleType)
                ? await _studentSpecializedTrainingService.GetTrainingsByModuleTypeAsync(SelectedModuleType, CurrentPage, PageSize)
                : await _studentSpecializedTrainingService.GetAvailableTrainingsAsync(CurrentPage, PageSize);

        // 根据筛选条件选择不同的API

        if (!append)
        {
            Trainings.Clear();
        }

        foreach (StudentSpecializedTrainingDto training in trainings)
        {
            System.Diagnostics.Debug.WriteLine($"[SpecializedTraining] 加载训练: {training.Name}, EnableTrial: {training.EnableTrial}");
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
    /// 显示解锁推广窗口
    /// </summary>
    private async Task ShowUnlockPromotionWindowAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[SpecializedTraining] 开始显示解锁推广窗口");

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    Avalonia.Controls.Window? mainWindow = desktop.MainWindow;
                    if (mainWindow != null)
                    {
                        UnlockPromotionWindow unlockWindow = new();
                        _ = unlockWindow.ShowDialog(mainWindow);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[SpecializedTraining] 无法获取桌面应用程序生命周期");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SpecializedTraining] 显示解锁推广窗口异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    private void OnUserInfoUpdated(object? sender, UserInfo? userInfo)
    {
        UpdateUserPermissions();
    }

    /// <summary>
    /// 将相对路径转换为绝对路径
    /// </summary>
    /// <param name="filePath">文件路径（可能是相对路径或绝对路径）</param>
    /// <param name="training">专项训练数据</param>
    /// <param name="moduleType">模块类型</param>
    /// <returns>绝对路径</returns>
    private string ConvertToAbsolutePath(string filePath, StudentSpecializedTrainingDto training, ModuleType moduleType)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return filePath;
        }

        // 如果是相对路径，使用正确的路径组合逻辑
        if (_benchSuiteDirectoryService != null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ConvertToAbsolutePath] 开始路径转换，训练ID: {training.Id}, 模块类型: {moduleType}");

                // 使用简化的模块目录路径
                string moduleDirectoryPath = _benchSuiteDirectoryService.GetDirectoryPath(moduleType);

                System.Diagnostics.Debug.WriteLine($"[ConvertToAbsolutePath] 获取模块目录路径成功: '{moduleDirectoryPath}'");

                // 处理以反斜杠开头的路径（如 "\a.docx"）
                string relativePath = filePath.TrimStart('\\', '/');
                string absolutePath = Path.Combine(moduleDirectoryPath, relativePath);

                System.Diagnostics.Debug.WriteLine($"[ConvertToAbsolutePath] 路径转换完成: 原路径='{filePath}', 相对路径='{relativePath}', 模块目录='{moduleDirectoryPath}', 绝对路径='{absolutePath}'");

                return absolutePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConvertToAbsolutePath] 路径转换失败: {ex.Message}，使用基础路径拼接");
                System.Diagnostics.Debug.WriteLine($"[ConvertToAbsolutePath] 异常详情: {ex}");

                // 回退到基础路径拼接
                string basePath = _benchSuiteDirectoryService.GetBasePath();
                string relativePath = filePath.TrimStart('\\', '/');
                string absolutePath = Path.Combine(basePath, relativePath);

                System.Diagnostics.Debug.WriteLine($"[ConvertToAbsolutePath] 回退路径转换: 原路径='{filePath}', 基础路径='{basePath}', 绝对路径='{absolutePath}'");

                return absolutePath;
            }
        }

        // 如果没有directoryService，返回原路径
        System.Diagnostics.Debug.WriteLine($"警告: IBenchSuiteDirectoryService不可用，无法转换路径: {filePath}");
        return filePath;
    }
}


