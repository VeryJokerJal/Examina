using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using Avalonia.Controls.ApplicationLifetimes;
using BenchSuite.Models;
using Examina.Extensions;
using Examina.Models;
using Examina.Models.BenchSuite;
using Examina.Models.Exam;
using Examina.Services;
using Examina.ViewModels.Dialogs;
using Examina.Views;
using Examina.Views.Dialogs;
using Examina.Views.Windows;
using ReactiveUI;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 综合训练列表ViewModel
/// </summary>
public class ComprehensiveTrainingListViewModel : ViewModelBase
{
    private readonly IStudentComprehensiveTrainingService _studentComprehensiveTrainingService;
    private readonly IAuthenticationService _authenticationService;
    private readonly EnhancedExamToolbarService? _enhancedExamToolbarService;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private int _totalCount;
    private int _currentPage = 1;
    private bool _hasFullAccess;
    private bool _isUpdatingPermissions = false;
    private DateTime _trainingStartTime;
    private const int PageSize = 20;

    /// <summary>
    /// 综合训练列表
    /// </summary>
    public ObservableCollection<StudentComprehensiveTrainingDto> Trainings { get; } = [];

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
    /// 是否有更多数据
    /// </summary>
    public bool HasMoreData => Trainings.Count < TotalCount;

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
    public string StartButtonText => HasFullAccess ? "开始答题" : "试做";

    /// <summary>
    /// 获取特定训练的按钮文本
    /// </summary>
    public string GetButtonText(StudentComprehensiveTrainingDto training)
    {
        return HasFullAccess ? "开始答题" : training.EnableTrial ? "试做" : "解锁";
    }

    /// <summary>
    /// 检查训练按钮是否可用
    /// </summary>
    public bool CanStartTraining(StudentComprehensiveTrainingDto training)
    {
        // 有权限用户：始终可以开始训练
        // 无权限用户：只有在EnableTrial=true时可以试做，EnableTrial=false时可以点击解锁
        return HasFullAccess || training.EnableTrial;
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
    /// 开始训练命令
    /// </summary>
    public ReactiveCommand<StudentComprehensiveTrainingDto, Unit> StartTrainingCommand { get; }

    public ComprehensiveTrainingListViewModel(
        IStudentComprehensiveTrainingService studentComprehensiveTrainingService,
        IAuthenticationService authenticationService,
        EnhancedExamToolbarService? enhancedExamToolbarService = null)
    {
        _studentComprehensiveTrainingService = studentComprehensiveTrainingService;
        _authenticationService = authenticationService;
        _enhancedExamToolbarService = enhancedExamToolbarService;

        // 创建命令
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        LoadMoreCommand = ReactiveCommand.CreateFromTask(
            LoadMoreAsync,
            this.WhenAnyValue(x => x.HasMoreData, x => x.IsLoading, (hasMore, loading) => hasMore && !loading)
        );
        StartTrainingCommand = ReactiveCommand.CreateFromTask<StudentComprehensiveTrainingDto>(StartTrainingAsync);

        // 初始化用户权限状态
        _ = UpdateUserPermissionsAsync();

        // 监听用户信息更新事件
        _authenticationService.UserInfoUpdated += OnUserInfoUpdated;

        // 初始加载
        _ = RefreshAsync();
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

            // 获取总数
            TotalCount = await _studentComprehensiveTrainingService.GetAvailableTrainingCountAsync();

            // 获取第一页数据
            List<StudentComprehensiveTrainingDto> trainings =
                await _studentComprehensiveTrainingService.GetAvailableTrainingsAsync(CurrentPage, PageSize);

            // 更新UI
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Trainings.Clear();
                foreach (StudentComprehensiveTrainingDto training in trainings)
                {
                    Trainings.Add(training);
                }
            });

            // 数据刷新完成后，强制更新用户权限状态（不影响刷新结果）
            try { _ = UpdateUserPermissionsAsync(); } catch { }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
        }
        catch (Exception)
        {
            ErrorMessage = "加载综合训练列表失败，请稍后重试";
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

            int nextPage = CurrentPage + 1;
            List<StudentComprehensiveTrainingDto> trainings =
                await _studentComprehensiveTrainingService.GetAvailableTrainingsAsync(nextPage, PageSize);

            if (trainings.Count > 0)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (StudentComprehensiveTrainingDto training in trainings)
                    {
                        Trainings.Add(training);
                    }
                });

                CurrentPage = nextPage;
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
        }
        catch (Exception)
        {
            ErrorMessage = "加载更多数据失败，请稍后重试";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 开始训练
    /// </summary>
    private async Task StartTrainingAsync(StudentComprehensiveTrainingDto training)
    {
        try
        {
            // 权限检查逻辑：
            // 1. 有权限用户：始终可以开始训练，不受EnableTrial影响
            // 2. 无权限用户：需要检查EnableTrial状态
            if (!HasFullAccess && !training.EnableTrial)
            {
                await ShowUnlockPromotionWindowAsync();
                return;
            }

            // 检查权限
            bool hasAccess = await _studentComprehensiveTrainingService.HasAccessToTrainingAsync(training.Id);
            if (!hasAccess)
            {
                ErrorMessage = "您没有权限访问此综合训练";
                return;
            }

            // 直接启动综合实训，无需确认对话框
            await StartComprehensiveTrainingAsync(training);
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
        }
        catch (Exception)
        {
            ErrorMessage = "开始训练失败，请稍后重试";
        }
    }

    /// <summary>
    /// 更新用户权限状态
    /// </summary>
    private async Task UpdateUserPermissionsAsync()
    {
        // 防重入机制：如果正在更新权限状态，则跳过
        if (_isUpdatingPermissions)
        {
            return;
        }

        try
        {
            _isUpdatingPermissions = true;

            // 主动刷新用户信息以获取最新状态
            bool refreshSuccess = await _authenticationService.RefreshUserInfoAsync();

            // 刷新成功或失败都以当前信息为准
            UserInfo? currentUser = _authenticationService.CurrentUser;
            HasFullAccess = currentUser?.HasFullAccess ?? false;

            // 通知UI更新按钮文本
            this.RaisePropertyChanged(nameof(StartButtonText));
            this.RaisePropertyChanged(nameof(HasFullAccess));
        }
        catch
        {
            // 异常处理：使用默认值
            HasFullAccess = false;
            this.RaisePropertyChanged(nameof(StartButtonText));
        }
        finally
        {
            _isUpdatingPermissions = false;
        }
    }

    /// <summary>
    /// 显示解锁推广窗口
    /// </summary>
    private async Task ShowUnlockPromotionWindowAsync()
    {
        try
        {
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
            });
        }
        catch
        {
        }
    }

    /// <summary>
    /// 启动综合实训
    /// </summary>
    private async Task StartComprehensiveTrainingAsync(StudentComprehensiveTrainingDto training)
    {
        try
        {
            // 获取训练详情
            StudentComprehensiveTrainingDto? trainingDetails =
                await _studentComprehensiveTrainingService.GetTrainingDetailsAsync(training.Id);
            if (trainingDetails == null)
            {
                ErrorMessage = "获取训练详情失败，请稍后重试";
                return;
            }

            // 标记训练为开始状态（即便失败也继续）
            _ = await _studentComprehensiveTrainingService.StartComprehensiveTrainingAsync(training.Id);

            // 清理考试目录
            IDirectoryCleanupService? directoryCleanupService = AppServiceManager.GetService<IDirectoryCleanupService>();
            if (directoryCleanupService != null)
            {
                DirectoryCleanupResult cleanupResult = await directoryCleanupService.CleanupExamDirectoryAsync();
                if (!cleanupResult.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 目录清理失败: {cleanupResult.ErrorMessage}");
                    // 继续执行，不因清理失败而阻止训练开始
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 目录清理成功，删除文件: {cleanupResult.DeletedFileCount}, 删除目录: {cleanupResult.DeletedDirectoryCount}");
                }
            }

            // 启动训练界面
            await StartTrainingInterfaceAsync(trainingDetails);
        }
        catch
        {
            ErrorMessage = "启动训练失败，请重试";
        }
    }

    /// <summary>
    /// 启动训练界面
    /// </summary>
    private async Task StartTrainingInterfaceAsync(StudentComprehensiveTrainingDto training)
    {
        try
        {
            // 记录训练开始时间
            _trainingStartTime = DateTime.Now;

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                // 文件预下载准备
                bool filesReady =
                    await desktop.MainWindow.PrepareFilesForComprehensiveTrainingAsync(training.Id, training.Name);
                if (!filesReady)
                {
                    ErrorMessage = "文件准备失败，无法开始综合实训。请检查网络连接或联系管理员。";
                    return;
                }

                // 隐藏主窗口
                desktop.MainWindow.Hide();

                // 创建考试工具栏 ViewModel
                IBenchSuiteDirectoryService? benchSuiteDirectoryService =
                    AppServiceManager.GetService<IBenchSuiteDirectoryService>();
                ExamToolbarViewModel toolbarViewModel = new(_authenticationService, null, benchSuiteDirectoryService);

                // 计算总题目数
                int totalQuestions = training.Subjects.Sum(s => s.Questions.Count) +
                                     training.Modules.Sum(m => m.Questions.Count);

                // 设置训练信息
                toolbarViewModel.SetExamInfo(
                    ExamType.ComprehensiveTraining,
                    training.Id,
                    training.Name,
                    totalQuestions,
                    training.DurationMinutes * 60 // 转换为秒
                );

                // 创建考试工具栏窗口并设置 ViewModel
                ExamToolbarWindow examToolbar = new();
                examToolbar.SetViewModel(toolbarViewModel);

                // 订阅训练事件
                examToolbar.ExamAutoSubmitted += OnTrainingAutoSubmitted;
                examToolbar.ExamManualSubmitted += OnTrainingManualSubmitted;
                examToolbar.ViewQuestionsRequested += (sender, e) => OnViewQuestionsRequested(training);
                examToolbar.ViewAnswerAnalysisRequested += (sender, e) => OnViewAnswerAnalysisRequested(training);

                // 显示工具栏窗口
                examToolbar.Show();

                // 开始训练（启动倒计时器并设置状态为进行中）
                toolbarViewModel.StartExam();
            }
            else
            {
                ErrorMessage = "无法启动训练界面，请重试";
            }
        }
        catch
        {
            ErrorMessage = "启动训练界面失败，请重试";
        }
    }

    /// <summary>
    /// 训练自动提交事件处理
    /// </summary>
    private async void OnTrainingAutoSubmitted(object? sender, EventArgs e)
    {
        try
        {
            if (sender is ExamToolbarWindow examToolbar && examToolbar.DataContext is ExamToolbarViewModel viewModel)
            {
                await SubmitTrainingAsync(viewModel.ExamId, viewModel.CurrentExamType, isAutoSubmit: true);
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// 训练手动提交事件处理
    /// </summary>
    private async void OnTrainingManualSubmitted(object? sender, EventArgs e)
    {
        try
        {
            if (sender is ExamToolbarWindow examToolbar && examToolbar.DataContext is ExamToolbarViewModel viewModel)
            {
                await SubmitTrainingAsync(viewModel.ExamId, viewModel.CurrentExamType, isAutoSubmit: false);
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// 提交训练
    /// </summary>
    private async Task SubmitTrainingAsync(int trainingId, ExamType examType, bool isAutoSubmit)
    {
        try
        {
            BenchSuiteScoringResult? scoringResult = null;
            bool submitResult = false;

            // 仅支持综合实训类型
            if (examType == ExamType.ComprehensiveTraining)
            {
                if (_enhancedExamToolbarService != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[调试] 使用EnhancedExamToolbarService提交训练，训练ID: {trainingId}");
                    Dictionary<ModuleType, ScoringResult>? moduleResults = await _enhancedExamToolbarService.SubmitComprehensiveTrainingWithResultAsync(trainingId);

                    System.Diagnostics.Debug.WriteLine($"[调试] EnhancedExamToolbarService返回结果: {(moduleResults != null ? $"{moduleResults.Count} 个模块" : "null")}");

                    if (moduleResults != null)
                    {
                        scoringResult = ConvertModuleResultsToBenchSuiteResult(moduleResults);
                        if (scoringResult != null)
                        {
                            submitResult = true;
                            System.Diagnostics.Debug.WriteLine($"[调试] 转换后的评分结果: 成功={scoringResult.IsSuccess}, 总分={scoringResult.TotalScore}, 得分={scoringResult.AchievedScore}");
                        }
                        else
                        {
                            submitResult = false;
                            System.Diagnostics.Debug.WriteLine("[警告] 模块结果转换失败，无法生成评分结果");
                        }
                    }
                    else
                    {
                        submitResult = false;
                        System.Diagnostics.Debug.WriteLine("[警告] EnhancedExamToolbarService返回null结果");
                    }
                }
                else
                {
                    // 回退到基础提交逻辑，但尝试获取实际用时和BenchSuite评分
                    (CompleteTrainingRequest request, BenchSuiteScoringResult? benchSuiteScoringResult) = await CreateTrainingRequestWithScoringAsync(isAutoSubmit);
                    submitResult = await _studentComprehensiveTrainingService
                        .CompleteComprehensiveTrainingAsync(trainingId, request);

                    // 如果获取到了BenchSuite评分结果，则使用它
                    if (benchSuiteScoringResult != null)
                    {
                        scoringResult = benchSuiteScoringResult;
                    }
                }
            }

            if (submitResult)
            {
                await ShowTrainingResultAsync(trainingId, examType, scoringResult);
            }
            else
            {
                ErrorMessage = "训练提交失败，请稍后重试";
            }
        }
        catch
        {
            ErrorMessage = "训练提交失败，请稍后重试";
        }
    }

    /// <summary>
    /// 创建训练提交请求（包含评分结果）
    /// </summary>
    private async Task<(CompleteTrainingRequest request, BenchSuiteScoringResult? scoringResult)> CreateTrainingRequestWithScoringAsync(bool isAutoSubmit)
    {
        try
        {
            // 尝试从当前活动的工具栏窗口获取实际用时
            int? actualDurationSeconds = null;
            int? currentTrainingId = null;

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                foreach (Avalonia.Controls.Window window in desktop.Windows)
                {
                    if (window is ExamToolbarWindow toolbarWindow &&
                        toolbarWindow.DataContext is ExamToolbarViewModel viewModel &&
                        viewModel.CurrentExamType == ExamType.ComprehensiveTraining)
                    {
                        actualDurationSeconds = viewModel.GetActualDurationSeconds();
                        currentTrainingId = viewModel.ExamId;
                        break;
                    }
                }
            }

            // 尝试进行BenchSuite评分
            BenchSuiteScoringResult? scoringResult = null;
            double? score = null;
            double? maxScore = null;
            string? benchSuiteScoringResultJson = null;

            if (currentTrainingId.HasValue)
            {
                try
                {
                    IBenchSuiteIntegrationService? benchSuiteService = AppServiceManager.GetService<IBenchSuiteIntegrationService>();
                    IBenchSuiteDirectoryService? directoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();

                    if (benchSuiteService != null && directoryService != null)
                    {
                        // 创建BenchSuite评分请求
                        BenchSuiteScoringRequest benchSuiteRequest = new()
                        {
                            ExamId = currentTrainingId.Value,
                            ExamType = ExamType.ComprehensiveTraining,
                            StudentUserId = int.TryParse(_authenticationService.CurrentUser?.Id, out int userId) ? userId : 0,
                            BasePath = directoryService.GetBasePath(),
                            FilePaths = []
                        };

                        // 扫描并添加文件路径
                        ScanAndAddFilePaths(benchSuiteRequest, currentTrainingId.Value);

                        // 转换文件路径字典
                        Dictionary<ModuleType, List<string>> moduleFilePaths = ConvertBenchSuiteFilePathsToModulePaths(benchSuiteRequest.FilePaths);

                        // 调试日志：输出BenchSuite服务调用参数
                        System.Diagnostics.Debug.WriteLine($"[调试] 准备调用BenchSuite服务");
                        System.Diagnostics.Debug.WriteLine($"[调试] 考试类型: {benchSuiteRequest.ExamType}");
                        System.Diagnostics.Debug.WriteLine($"[调试] 考试ID: {benchSuiteRequest.ExamId}");
                        System.Diagnostics.Debug.WriteLine($"[调试] 学生用户ID: {benchSuiteRequest.StudentUserId}");
                        System.Diagnostics.Debug.WriteLine($"[调试] 模块文件路径数量: {moduleFilePaths.Count}");

                        foreach (KeyValuePair<ModuleType, List<string>> kvp in moduleFilePaths)
                        {
                            System.Diagnostics.Debug.WriteLine($"[调试] 模块 {kvp.Key}: {kvp.Value.Count} 个文件");
                            foreach (string filePath in kvp.Value.Take(3)) // 只显示前3个文件
                            {
                                System.Diagnostics.Debug.WriteLine($"[调试]   文件: {filePath}");
                            }
                            if (kvp.Value.Count > 3)
                            {
                                System.Diagnostics.Debug.WriteLine($"[调试]   ... 还有 {kvp.Value.Count - 3} 个文件");
                            }
                        }

                        // 执行BenchSuite评分 - 使用正确的方法签名
                        System.Diagnostics.Debug.WriteLine("[调试] 开始调用BenchSuite.ScoreExamAsync");
                        Dictionary<ModuleType, ScoringResult> moduleResults = await benchSuiteService.ScoreExamAsync(
                            benchSuiteRequest.ExamType,
                            benchSuiteRequest.ExamId,
                            benchSuiteRequest.StudentUserId,
                            moduleFilePaths);

                        System.Diagnostics.Debug.WriteLine($"[调试] BenchSuite服务调用完成，返回 {moduleResults.Count} 个模块结果");

                        // 将模块结果转换为BenchSuiteScoringResult
                        scoringResult = ConvertModuleResultsToBenchSuiteResult(moduleResults);

                        if (scoringResult != null && scoringResult.IsSuccess)
                        {
                            score = scoringResult.AchievedScore;
                            maxScore = scoringResult.TotalScore;

                            // 序列化评分结果
                            benchSuiteScoringResultJson = System.Text.Json.JsonSerializer.Serialize(scoringResult, new System.Text.Json.JsonSerializerOptions
                            {
                                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                                WriteIndented = true
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // BenchSuite评分失败不影响提交，只记录错误
                    System.Diagnostics.Debug.WriteLine($"BenchSuite评分失败: {ex.Message}");
                }
            }

            CompleteTrainingRequest request = new()
            {
                Score = score,
                MaxScore = maxScore,
                DurationSeconds = actualDurationSeconds,
                Notes = isAutoSubmit ? "训练时间到期，自动提交" : "学生手动提交训练",
                BenchSuiteScoringResult = benchSuiteScoringResultJson,
                CompletedAt = DateTime.UtcNow
            };

            return (request, scoringResult);
        }
        catch
        {
            // 异常恢复：返回基础请求
            CompleteTrainingRequest fallbackRequest = new()
            {
                Notes = isAutoSubmit ? "训练时间到期，自动提交（异常恢复）" : "学生手动提交训练（异常恢复）",
                CompletedAt = DateTime.UtcNow
            };
            return (fallbackRequest, null);
        }
    }

    /// <summary>
    /// 创建训练提交请求（兼容性方法）
    /// </summary>
    private async Task<CompleteTrainingRequest> CreateTrainingRequestAsync(bool isAutoSubmit)
    {
        (CompleteTrainingRequest request, BenchSuiteScoringResult? _) = await CreateTrainingRequestWithScoringAsync(isAutoSubmit);
        return request;
    }

    /// <summary>
    /// 扫描并添加文件路径到BenchSuite评分请求
    /// </summary>
    private void ScanAndAddFilePaths(BenchSuiteScoringRequest request, int trainingId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[调试] 开始扫描文件路径，训练ID: {trainingId}");

            IBenchSuiteDirectoryService? directoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
            if (directoryService == null)
            {
                System.Diagnostics.Debug.WriteLine("[错误] IBenchSuiteDirectoryService服务未找到");
                return;
            }

            string basePath = directoryService.GetBasePath();
            System.Diagnostics.Debug.WriteLine($"[调试] BenchSuite基础路径: {basePath}");

            // 获取支持的文件类型
            BenchSuiteFileType[] supportedFileTypes =
            {
                BenchSuiteFileType.Word,
                BenchSuiteFileType.Excel,
                BenchSuiteFileType.CSharp,
                BenchSuiteFileType.Windows
            };

            foreach (BenchSuiteFileType fileType in supportedFileTypes)
            {
                try
                {
                    // 将BenchSuiteFileType转换为ModuleType
                    ModuleType moduleType = ConvertBenchSuiteFileTypeToModuleType(fileType);
                    string directoryPath = directoryService.GetDirectoryPath(moduleType);

                    System.Diagnostics.Debug.WriteLine($"[调试] 扫描文件类型: {fileType} -> 模块类型: {moduleType}");
                    System.Diagnostics.Debug.WriteLine($"[调试] 目录路径: {directoryPath}");
                    System.Diagnostics.Debug.WriteLine($"[调试] 目录是否存在: {Directory.Exists(directoryPath)}");

                    if (Directory.Exists(directoryPath))
                    {
                        List<string> filePaths = [];

                        // 根据文件类型扫描相应的文件
                        string[] extensions = fileType switch
                        {
                            BenchSuiteFileType.Word => new[] { "*.docx", "*.doc" },
                            BenchSuiteFileType.Excel => new[] { "*.xlsx", "*.xls" },
                            BenchSuiteFileType.CSharp => new[] { "*.cs" },
                            BenchSuiteFileType.Windows => new[] { "*.*" }, // Windows操作检测不依赖特定文件
                            _ => new[] { "*.*" }
                        };

                        System.Diagnostics.Debug.WriteLine($"[调试] 扫描扩展名: {string.Join(", ", extensions)}");

                        foreach (string extension in extensions)
                        {
                            string[] files = Directory.GetFiles(directoryPath, extension, SearchOption.AllDirectories);
                            System.Diagnostics.Debug.WriteLine($"[调试] 扩展名 {extension} 找到 {files.Length} 个文件");
                            filePaths.AddRange(files);
                        }

                        System.Diagnostics.Debug.WriteLine($"[调试] {fileType} 总共找到 {filePaths.Count} 个文件");

                        if (filePaths.Count > 0)
                        {
                            request.FilePaths[fileType] = filePaths;
                            System.Diagnostics.Debug.WriteLine($"[调试] 已添加 {fileType} 的 {filePaths.Count} 个文件到请求中");
                            foreach (string filePath in filePaths.Take(5)) // 只显示前5个文件
                            {
                                System.Diagnostics.Debug.WriteLine($"[调试]   文件: {filePath}");
                            }
                            if (filePaths.Count > 5)
                            {
                                System.Diagnostics.Debug.WriteLine($"[调试]   ... 还有 {filePaths.Count - 5} 个文件");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[警告] {fileType} 未找到任何文件");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[警告] {fileType} 对应的目录不存在: {directoryPath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[错误] 扫描{fileType}文件时发生错误: {ex.Message}");
                }
            }

            // 输出扫描结果总结
            System.Diagnostics.Debug.WriteLine($"[调试] 文件扫描完成，总共找到 {request.FilePaths.Count} 种文件类型");
            foreach (KeyValuePair<BenchSuiteFileType, List<string>> kvp in request.FilePaths)
            {
                System.Diagnostics.Debug.WriteLine($"[调试] {kvp.Key}: {kvp.Value.Count} 个文件");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[错误] 扫描文件路径时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 关闭训练并显示主窗口
    /// </summary>
    private void CloseTrainingAndShowMainWindow()
    {
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.Activate();
            }

            _ = RefreshAsync();
        }
        catch
        {
        }
    }

    /// <summary>
    /// 查看题目请求事件处理（统一为ExamQuestionDetailsWindow样式与逻辑）
    /// </summary>
    private void OnViewQuestionsRequested(StudentComprehensiveTrainingDto training)
    {
        try
        {
            // 将综合实训数据转换为通用的StudentExamDto以复用通用窗口
            StudentExamDto examData = ConvertTrainingToStudentExam(training);

            // 创建通用题目详情窗口
            ExamQuestionDetailsViewModel detailsViewModel = new();
            detailsViewModel.SetExamData(examData);

            ExamQuestionDetailsWindow detailsWindow = new()
            {
                DataContext = detailsViewModel
            };

            detailsWindow.Show();
        }
        catch
        {
        }
    }

    /// <summary>
    /// 将综合实训DTO转换为StudentExamDto（用于ExamQuestionDetailsWindow）
    /// </summary>
    private static StudentExamDto ConvertTrainingToStudentExam(StudentComprehensiveTrainingDto training)
    {
        StudentExamDto exam = new()
        {
            Id = training.Id,
            Name = training.Name,
            Description = training.Description,
            ExamType = "ComprehensiveTraining",
            Status = training.Status,
            TotalScore = training.TotalScore,
            DurationMinutes = training.DurationMinutes,
            Subjects = [],
            Modules = []
        };

        // 先按模块转换（保持所有模块，包括无题目的）
        foreach (StudentComprehensiveTrainingModuleDto module in training.Modules)
        {
            StudentModuleDto m = new()
            {
                Id = module.Id,
                Name = module.Name,
                Type = module.Type,
                Description = module.Description ?? string.Empty,
                Score = module.Score,
                Order = module.Order
            };

            if (module.Questions != null && module.Questions.Count > 0)
            {
                foreach (StudentComprehensiveTrainingQuestionDto q in module.Questions)
                {
                    m.Questions.Add(new StudentQuestionDto
                    {
                        Id = q.Id,
                        Title = q.Title,
                        Content = q.Content,
                        QuestionType = q.QuestionType,
                        Score = q.Score,
                        SortOrder = q.SortOrder,
                        IsRequired = q.IsRequired
                    });
                }
            }

            exam.Modules.Add(m);
        }

        // 若没有模块或存在未覆盖的科目，则从Subjects补充为模块
        if ((exam.Modules.Count == 0) && training.Subjects != null && training.Subjects.Count > 0)
        {
            foreach (StudentComprehensiveTrainingSubjectDto s in training.Subjects)
            {
                StudentModuleDto m = new()
                {
                    Id = s.Id,
                    Name = s.SubjectName,
                    Type = s.SubjectType,
                    Description = s.Description ?? string.Empty,
                    Score = s.Score,
                    Order = s.SortOrder
                };

                if (s.Questions != null && s.Questions.Count > 0)
                {
                    foreach (StudentComprehensiveTrainingQuestionDto q in s.Questions)
                    {
                        m.Questions.Add(new StudentQuestionDto
                        {
                            Id = q.Id,
                            Title = q.Title,
                            Content = q.Content,
                            QuestionType = q.QuestionType,
                            Score = q.Score,
                            SortOrder = q.SortOrder,
                            IsRequired = q.IsRequired
                        });
                    }
                }

                exam.Modules.Add(m);
            }
        }

        return exam;
    }

    /// <summary>
    /// 查看答案解析请求事件处理
    /// </summary>
    private void OnViewAnswerAnalysisRequested(StudentComprehensiveTrainingDto training)
    {
        try
        {
            // 创建答案解析窗口
            AnswerAnalysisViewModel analysisViewModel = new();

            // 按模块分组收集题目
            Dictionary<string, List<QuestionItem>> moduleQuestions = [];

            // 从模块中收集题目
            foreach (StudentComprehensiveTrainingModuleDto module in training.Modules)
            {
                string moduleName = module.Name;
                List<QuestionItem> questions = [];

                foreach (StudentComprehensiveTrainingQuestionDto question in module.Questions)
                {
                    questions.Add(new QuestionItem
                    {
                        Id = question.Id,
                        Title = question.Title,
                        Content = question.Content,
                        QuestionType = question.QuestionType,
                        Score = question.Score,
                        SortOrder = question.SortOrder,
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

            // 从科目中收集题目
            foreach (StudentComprehensiveTrainingSubjectDto subject in training.Subjects)
            {
                string subjectName = subject.SubjectName;
                List<QuestionItem> questions = [];

                foreach (StudentComprehensiveTrainingQuestionDto question in subject.Questions)
                {
                    questions.Add(new QuestionItem
                    {
                        Id = question.Id,
                        Title = question.Title,
                        Content = question.Content,
                        QuestionType = question.QuestionType,
                        Score = question.Score,
                        SortOrder = question.SortOrder,
                        ModuleName = subjectName
                    });
                }

                if (questions.Count > 0)
                {
                    // 按排序顺序排列题目
                    questions = [.. questions.OrderBy(q => q.SortOrder)];
                    moduleQuestions[subjectName] = questions;
                }
            }

            analysisViewModel.SetAnswerAnalysisData(training.Name, moduleQuestions);

            AnswerAnalysisWindow analysisWindow = new()
            {
                DataContext = analysisViewModel
            };

            analysisWindow.Show();
        }
        catch
        {
        }
    }

    /// <summary>
    /// 显示训练结果
    /// </summary>
    private async Task ShowTrainingResultAsync(int trainingId, ExamType examType, BenchSuiteScoringResult? scoringResult = null)
    {
        try
        {
            // 获取训练信息
            StudentComprehensiveTrainingDto? training = await GetTrainingByIdAsync(trainingId);
            if (training == null)
            {
                CloseTrainingAndShowMainWindow();
                return;
            }

            // 如果没有传入评分结果，尝试从服务端获取真实的训练完成记录
            if (scoringResult == null)
            {
                scoringResult = await GetTrainingCompletionResultAsync(trainingId);
            }

            // 如果仍然没有评分结果，显示错误信息并返回
            if (scoringResult == null)
            {
                ErrorMessage = "无法获取训练结果数据，请稍后重试";
                CloseTrainingAndShowMainWindow();
                return;
            }

            // 显示详细的训练结果（使用真实的评分结果）
            await ShowDetailedTrainingResultAsync(training.Name, scoringResult);
        }
        catch
        {
            // 如果显示训练结果失败，也要显示主窗口
            ErrorMessage = "显示训练结果失败，请稍后重试";
            CloseTrainingAndShowMainWindow();
        }
    }

    /// <summary>
    /// 根据ID获取训练信息
    /// </summary>
    private async Task<StudentComprehensiveTrainingDto?> GetTrainingByIdAsync(int trainingId)
    {
        try
        {
            return await _studentComprehensiveTrainingService.GetTrainingDetailsAsync(trainingId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取训练完成记录的评分结果
    /// </summary>
    private async Task<BenchSuiteScoringResult?> GetTrainingCompletionResultAsync(int trainingId)
    {
        try
        {
            // 尝试从服务端获取训练完成记录
            // 这里需要调用相应的API来获取已完成的训练记录和评分结果
            // 目前先返回null，表示无法获取到真实数据
            System.Diagnostics.Debug.WriteLine($"尝试获取训练完成记录，训练ID: {trainingId}");

            // TODO: 实现从服务端获取训练完成记录的逻辑
            // 可能需要添加新的API接口来获取已完成训练的详细评分结果

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取训练完成记录失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 显示详细的训练结果
    /// </summary>
    private async Task ShowDetailedTrainingResultAsync(string trainingName, BenchSuiteScoringResult scoringResult)
    {
        try
        {
            // 创建训练结果ViewModel
            TrainingResultViewModel resultViewModel = new();
            resultViewModel.SetTrainingResult(trainingName, scoringResult, _trainingStartTime);

            // 创建训练结果窗口
            TrainingResultWindow resultWindow = new()
            {
                DataContext = resultViewModel
            };

            // 显示结果窗口（非模态，因为主窗口已隐藏）
            resultWindow.Show();

            // 等待窗口关闭
            await resultWindow.WaitForCloseAsync();

            // 窗口关闭后显示主窗口
            CloseTrainingAndShowMainWindow();
        }
        catch
        {
            // 如果显示结果窗口失败，也要显示主窗口
            CloseTrainingAndShowMainWindow();
        }
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    private void OnUserInfoUpdated(object? sender, UserInfo? userInfo)
    {
        _ = UpdateUserPermissionsAsync();
    }

    /// <summary>
    /// 将BenchSuiteFileType转换为ModuleType
    /// </summary>
    private static ModuleType ConvertBenchSuiteFileTypeToModuleType(BenchSuiteFileType fileType)
    {
        return fileType switch
        {
            BenchSuiteFileType.Word => ModuleType.Word,
            BenchSuiteFileType.Excel => ModuleType.Excel,
            BenchSuiteFileType.PowerPoint => ModuleType.PowerPoint,
            BenchSuiteFileType.CSharp => ModuleType.CSharp,
            BenchSuiteFileType.Windows => ModuleType.Windows,
            _ => ModuleType.Windows // 默认返回Windows模块
        };
    }

    /// <summary>
    /// 将ModuleType转换为BenchSuiteFileType
    /// </summary>
    private static BenchSuiteFileType ConvertModuleTypeToBenchSuiteFileType(ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.Word => BenchSuiteFileType.Word,
            ModuleType.Excel => BenchSuiteFileType.Excel,
            ModuleType.PowerPoint => BenchSuiteFileType.PowerPoint,
            ModuleType.CSharp => BenchSuiteFileType.CSharp,
            ModuleType.Windows => BenchSuiteFileType.Windows,
            _ => BenchSuiteFileType.Other
        };
    }

    /// <summary>
    /// 将模块评分结果转换为BenchSuiteScoringResult
    /// </summary>
    private static BenchSuiteScoringResult? ConvertModuleResultsToBenchSuiteResult(Dictionary<ModuleType, ScoringResult> moduleResults)
    {
        // 调试日志：输出moduleResults的内容
        System.Diagnostics.Debug.WriteLine($"[调试] ConvertModuleResultsToBenchSuiteResult: moduleResults.Count = {moduleResults.Count}");
        foreach (KeyValuePair<ModuleType, ScoringResult> kvp in moduleResults)
        {
            System.Diagnostics.Debug.WriteLine($"[调试] 模块: {kvp.Key}, 成功: {kvp.Value.IsSuccess}, 总分: {kvp.Value.TotalScore}, 得分: {kvp.Value.AchievedScore}, 知识点数量: {kvp.Value.KnowledgePointResults.Count}");
        }

        // 处理空集合的情况
        if (moduleResults.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("[警告] moduleResults为空，无法生成评分结果");
            // 返回null，让调用方处理无数据的情况
            return null;
        }

        BenchSuiteScoringResult result = new()
        {
            ModuleResults = moduleResults,
            IsSuccess = moduleResults.Values.Any(r => r.IsSuccess),
            TotalScore = moduleResults.Values.Sum(r => r.TotalScore),
            AchievedScore = moduleResults.Values.Sum(r => r.AchievedScore),
            StartTime = moduleResults.Values.Any() ? moduleResults.Values.Min(r => r.StartTime) : DateTime.Now,
            EndTime = moduleResults.Values.Any() ? moduleResults.Values.Max(r => r.EndTime) : DateTime.Now,
            KnowledgePointResults = [.. moduleResults.Values.SelectMany(r => r.KnowledgePointResults)]
        };

        // 设置错误信息（如果有失败的模块）
        List<string> errorMessages = [.. moduleResults.Values
            .Where(r => !r.IsSuccess && !string.IsNullOrEmpty(r.ErrorMessage))
            .Select(r => r.ErrorMessage!)];

        if (errorMessages.Count > 0)
        {
            result.ErrorMessage = string.Join("; ", errorMessages);
        }

        // 如果没有任何成功的模块，但也没有错误信息，设置默认错误信息
        if (!result.IsSuccess && string.IsNullOrEmpty(result.ErrorMessage))
        {
            result.ErrorMessage = "所有模块评分均失败，未获取到具体错误信息";
        }

        System.Diagnostics.Debug.WriteLine($"[调试] 转换结果: 成功={result.IsSuccess}, 总分={result.TotalScore}, 得分={result.AchievedScore}, 知识点数量={result.KnowledgePointResults.Count}");

        return result;
    }



    /// <summary>
    /// 将BenchSuiteFileType文件路径字典转换为ModuleType文件路径字典
    /// </summary>
    private static Dictionary<ModuleType, List<string>> ConvertBenchSuiteFilePathsToModulePaths(Dictionary<BenchSuiteFileType, List<string>> benchSuiteFilePaths)
    {
        Dictionary<ModuleType, List<string>> moduleFilePaths = [];

        foreach (KeyValuePair<BenchSuiteFileType, List<string>> kvp in benchSuiteFilePaths)
        {
            ModuleType moduleType = ConvertBenchSuiteFileTypeToModuleType(kvp.Key);
            moduleFilePaths[moduleType] = kvp.Value;
        }

        return moduleFilePaths;
    }
}
