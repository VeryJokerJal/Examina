using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using Avalonia.Controls.ApplicationLifetimes;
using Examina.Extensions;
using Examina.Models;

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

            // 显示规则说明对话框
            ComprehensiveTrainingRulesViewModel rulesViewModel = new();
            ComprehensiveTrainingRulesDialog dialog = new(rulesViewModel);

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                bool? result = await dialog.ShowDialog<bool?>(desktop.MainWindow);

                if (result == true)
                {
                    // 用户确认开始，启动综合实训
                    await StartComprehensiveTrainingAsync(training);
                }
            }
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
                    scoringResults = await _enhancedExamToolbarService.SubmitComprehensiveTrainingWithResultAsync(trainingId);
                    submitResult = scoringResults != null && scoringResults.Count > 0;
                }
                else
                {
                    // 回退到基础提交逻辑，但尝试获取实际用时和BenchSuite评分
                    CompleteTrainingRequest request = await CreateTrainingRequestAsync(isAutoSubmit);
                    submitResult = await _studentComprehensiveTrainingService
                        .CompleteComprehensiveTrainingAsync(trainingId, request);
                }
            }

            if (submitResult)
            {
                await ShowTrainingResultAsync(trainingId, examType, scoringResults);
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
    /// 创建训练提交请求
    /// </summary>
    private async Task<CompleteTrainingRequest> CreateTrainingRequestAsync(bool isAutoSubmit)
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
            decimal? score = null;
            decimal? maxScore = null;
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
                            FilePaths = new Dictionary<BenchSuiteFileType, List<string>>()
                        };

                        // 扫描并添加文件路径
                        await ScanAndAddFilePathsAsync(benchSuiteRequest, currentTrainingId.Value);

                        // 执行BenchSuite评分
                        scoringResult = await benchSuiteService.ScoreExamAsync(benchSuiteRequest);

                        if (scoringResult.IsSuccess)
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

            return request;
        }
        catch
        {
            // 异常恢复：返回基础请求
            return new CompleteTrainingRequest
            {
                Notes = isAutoSubmit ? "训练时间到期，自动提交（异常恢复）" : "学生手动提交训练（异常恢复）",
                CompletedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 扫描并添加文件路径到BenchSuite评分请求
    /// </summary>
    private async Task ScanAndAddFilePathsAsync(BenchSuiteScoringRequest request, int trainingId)
    {
        try
        {
            IBenchSuiteDirectoryService? directoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
            if (directoryService == null) return;

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
                    string directoryPath = directoryService.GetExamDirectoryPath(ExamType.ComprehensiveTraining, trainingId, fileType);

                    if (Directory.Exists(directoryPath))
                    {
                        List<string> filePaths = new();

                        // 根据文件类型扫描相应的文件
                        string[] extensions = fileType switch
                        {
                            BenchSuiteFileType.Word => new[] { "*.docx", "*.doc" },
                            BenchSuiteFileType.Excel => new[] { "*.xlsx", "*.xls" },
                            BenchSuiteFileType.CSharp => new[] { "*.cs" },
                            BenchSuiteFileType.Windows => new[] { "*.*" }, // Windows操作检测不依赖特定文件
                            _ => new[] { "*.*" }
                        };

                        foreach (string extension in extensions)
                        {
                            string[] files = Directory.GetFiles(directoryPath, extension, SearchOption.AllDirectories);
                            filePaths.AddRange(files);
                        }

                        if (filePaths.Count > 0)
                        {
                            request.FilePaths[fileType] = filePaths;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"扫描{fileType}文件时发生错误: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"扫描文件路径时发生错误: {ex.Message}");
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

            // 收集所有题目的答案解析内容
            List<QuestionItem> questionItems = [];

            // 从模块中收集题目
            foreach (StudentComprehensiveTrainingModuleDto module in training.Modules)
            {
                foreach (StudentComprehensiveTrainingQuestionDto question in module.Questions)
                {
                    questionItems.Add(new QuestionItem
                    {
                        Id = question.Id,
                        Title = question.Title,
                        Content = question.Content,
                        QuestionType = question.QuestionType,
                        Score = question.Score,
                        SortOrder = question.SortOrder
                    });
                }
            }

            // 从科目中收集题目
            foreach (StudentComprehensiveTrainingSubjectDto subject in training.Subjects)
            {
                foreach (StudentComprehensiveTrainingQuestionDto question in subject.Questions)
                {
                    questionItems.Add(new QuestionItem
                    {
                        Id = question.Id,
                        Title = question.Title,
                        Content = question.Content,
                        QuestionType = question.QuestionType,
                        Score = question.Score,
                        SortOrder = question.SortOrder
                    });
                }
            }

            // 按排序顺序排列题目
            questionItems = questionItems.OrderBy(q => q.SortOrder).ToList();

            analysisViewModel.SetAnswerAnalysisData(training.Name, questionItems);

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

            // 如果没有传入评分结果，创建一个基本的失败结果
            scoringResult ??= new BenchSuiteScoringResult
            {
                IsSuccess = false,
                ErrorMessage = "未能获取评分结果",
                TotalScore = 100,
                AchievedScore = 0,
                StartTime = _trainingStartTime,
                EndTime = DateTime.Now
            };

            // 显示详细的训练结果（使用真实或基本的评分结果）
            await ShowDetailedTrainingResultAsync(training.Name, scoringResult);
        }
        catch
        {
            // 如果显示训练结果失败，也要显示主窗口
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
}
