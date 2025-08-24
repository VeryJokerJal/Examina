using System.Collections.ObjectModel;
using System.Reactive;
using Examina.Extensions;
using Examina.Models.BenchSuite;
using Examina.Models.Exam;
using Examina.Services;
using Examina.Models;
using Examina.ViewModels.Dialogs;
using Examina.Views.Dialogs;
using Examina.Views;
using Avalonia.Controls.ApplicationLifetimes;
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
    /// 开始训练命令
    /// </summary>
    public ReactiveCommand<StudentComprehensiveTrainingDto, Unit> StartTrainingCommand { get; }

    public ComprehensiveTrainingListViewModel(IStudentComprehensiveTrainingService studentComprehensiveTrainingService, IAuthenticationService authenticationService, EnhancedExamToolbarService? enhancedExamToolbarService = null)
    {
        _studentComprehensiveTrainingService = studentComprehensiveTrainingService;
        _authenticationService = authenticationService;
        _enhancedExamToolbarService = enhancedExamToolbarService;

        // 创建命令
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        LoadMoreCommand = ReactiveCommand.CreateFromTask(LoadMoreAsync, this.WhenAnyValue(x => x.HasMoreData, x => x.IsLoading, (hasMore, loading) => hasMore && !loading));
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
            List<StudentComprehensiveTrainingDto> trainings = await _studentComprehensiveTrainingService.GetAvailableTrainingsAsync(CurrentPage, PageSize);

            // 更新UI
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Trainings.Clear();
                foreach (StudentComprehensiveTrainingDto training in trainings)
                {
                    Trainings.Add(training);
                }
            });

            System.Diagnostics.Debug.WriteLine($"刷新综合训练列表成功，共 {TotalCount} 项，当前显示 {Trainings.Count} 项");

            // 数据刷新完成后，强制更新用户权限状态
            try
            {
                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 刷新完成，开始更新用户权限状态");
                _ = UpdateUserPermissionsAsync();
            }
            catch (Exception permissionEx)
            {
                // 权限更新失败不应影响数据刷新的成功状态
                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 权限状态更新失败: {permissionEx.Message}");
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("获取综合训练列表失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "加载综合训练列表失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"刷新综合训练列表失败: {ex.Message}");
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
            List<StudentComprehensiveTrainingDto> trainings = await _studentComprehensiveTrainingService.GetAvailableTrainingsAsync(nextPage, PageSize);

            if (trainings.Count > 0)
            {
                // 更新UI
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (StudentComprehensiveTrainingDto training in trainings)
                    {
                        Trainings.Add(training);
                    }
                });

                CurrentPage = nextPage;
                System.Diagnostics.Debug.WriteLine($"加载更多综合训练成功，当前页: {CurrentPage}，总显示: {Trainings.Count} 项");
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("加载更多综合训练失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "加载更多数据失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"加载更多综合训练失败: {ex.Message}");
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
            if (HasFullAccess)
            {
                // 用户有完整权限，开始训练
                System.Diagnostics.Debug.WriteLine($"开始综合训练: {training.Name} (ID: {training.Id})");

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

                // 设置对话框的父窗口
                if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow != null)
                {
                    System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 准备显示规则对话框");

                    bool? result = await dialog.ShowDialog<bool?>(desktop.MainWindow);

                    System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 对话框返回结果: {result}");

                    if (result == true)
                    {
                        System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 用户确认开始综合实训");
                        // 用户确认开始，启动综合实训
                        await StartComprehensiveTrainingAsync(training);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 用户取消了综合实训");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 无法获取主窗口");
                }
            }
            else
            {
                // 用户没有完整权限，显示解锁提示
                ErrorMessage = "您需要解锁权限才能开始训练。请加入学校组织或联系管理员进行解锁。";
                System.Diagnostics.Debug.WriteLine("用户尝试开始训练但没有完整权限");
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("开始综合训练失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "开始训练失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"开始综合训练失败: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 权限状态正在更新中，跳过重复调用");
            return;
        }

        try
        {
            _isUpdatingPermissions = true;
            System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 开始更新用户权限状态");

            // 主动刷新用户信息以获取最新状态
            bool refreshSuccess = await _authenticationService.RefreshUserInfoAsync();

            if (refreshSuccess)
            {
                // 刷新成功，获取最新的用户信息
                UserInfo? currentUser = _authenticationService.CurrentUser;
                HasFullAccess = currentUser?.HasFullAccess ?? false;

                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 用户信息刷新成功 - HasFullAccess: {HasFullAccess}");
            }
            else
            {
                // 刷新失败，使用当前缓存的用户信息
                UserInfo? currentUser = _authenticationService.CurrentUser;
                HasFullAccess = currentUser?.HasFullAccess ?? false;

                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 用户信息刷新失败，使用缓存信息 - HasFullAccess: {HasFullAccess}");
            }

            // 通知UI更新按钮文本
            this.RaisePropertyChanged(nameof(StartButtonText));
        }
        catch (Exception ex)
        {
            // 异常处理：使用默认值
            HasFullAccess = false;
            this.RaisePropertyChanged(nameof(StartButtonText));

            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 更新用户权限状态异常: {ex.Message}");
        }
        finally
        {
            _isUpdatingPermissions = false;
            System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 权限状态更新完成");
        }
    }

    /// <summary>
    /// 启动综合实训
    /// </summary>
    private async Task StartComprehensiveTrainingAsync(StudentComprehensiveTrainingDto training)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 准备启动综合实训 - ID: {training.Id}, 名称: {training.Name}");

            // 获取训练详情
            StudentComprehensiveTrainingDto? trainingDetails = await _studentComprehensiveTrainingService.GetTrainingDetailsAsync(training.Id);
            if (trainingDetails == null)
            {
                ErrorMessage = "获取训练详情失败，请稍后重试";
                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 获取训练详情失败");
                return;
            }

            // 标记训练为开始状态
            bool startResult = await _studentComprehensiveTrainingService.StartComprehensiveTrainingAsync(training.Id);
            if (!startResult)
            {
                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 标记训练开始状态失败，但继续启动工具栏");
            }

            // 启动训练界面
            await StartTrainingInterfaceAsync(trainingDetails);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 启动综合实训异常: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 开始文件预下载准备");

                bool filesReady = await desktop.MainWindow.PrepareFilesForComprehensiveTrainingAsync(training.Id, training.Name);
                if (!filesReady)
                {
                    ErrorMessage = "文件准备失败，无法开始综合实训。请检查网络连接或联系管理员。";
                    System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 文件预下载失败，取消综合实训启动");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 文件预下载完成，继续启动综合实训");

                // 隐藏主窗口
                desktop.MainWindow.Hide();
                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 主窗口已隐藏");

                // 创建考试工具栏 ViewModel
                IBenchSuiteDirectoryService? benchSuiteDirectoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
                ExamToolbarViewModel toolbarViewModel = new(_authenticationService, null, benchSuiteDirectoryService);

                // 计算总题目数
                int totalQuestions = training.Subjects.Sum(s => s.Questions.Count) + training.Modules.Sum(m => m.Questions.Count);

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

                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 训练工具栏已配置 - 训练ID: {training.Id}, 题目数: {totalQuestions}, 时长: {training.DurationMinutes}分钟");

                // 订阅训练事件
                examToolbar.ExamAutoSubmitted += OnTrainingAutoSubmitted;
                examToolbar.ExamManualSubmitted += OnTrainingManualSubmitted;
                examToolbar.ViewQuestionsRequested += (sender, e) => OnViewQuestionsRequested(training);
                examToolbar.ViewAnswerAnalysisRequested += (sender, e) => OnViewAnswerAnalysisRequested(training);

                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 已订阅训练工具栏事件");

                // 显示工具栏窗口
                examToolbar.Show();
                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 训练工具栏窗口已显示");

                // 开始训练（启动倒计时器并设置状态为进行中）
                toolbarViewModel.StartExam();
                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 训练已开始，剩余时间: {toolbarViewModel.RemainingTimeSeconds}秒, 格式化时间: {toolbarViewModel.FormattedRemainingTime}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 无法获取主窗口，启动训练界面失败");
                ErrorMessage = "无法启动训练界面，请重试";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 启动训练界面异常: {ex.Message}");
            ErrorMessage = "启动训练界面失败，请重试";
        }
    }

    /// <summary>
    /// 训练自动提交事件处理
    /// </summary>
    private async void OnTrainingAutoSubmitted(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 训练时间到，自动提交");

        try
        {
            // 获取训练工具栏窗口以获取训练信息
            if (sender is ExamToolbarWindow examToolbar && examToolbar.DataContext is ExamToolbarViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 自动提交训练，ID: {viewModel.ExamId}, 类型: {viewModel.CurrentExamType}");
                await SubmitTrainingAsync(viewModel.ExamId, viewModel.CurrentExamType, isAutoSubmit: true);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 无法获取训练工具栏ViewModel，自动提交失败");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 自动提交训练异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 训练手动提交事件处理
    /// </summary>
    private async void OnTrainingManualSubmitted(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 学生手动提交训练");

        try
        {
            // 获取训练工具栏窗口以获取训练信息
            if (sender is ExamToolbarWindow examToolbar && examToolbar.DataContext is ExamToolbarViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 手动提交训练，ID: {viewModel.ExamId}, 类型: {viewModel.CurrentExamType}");
                await SubmitTrainingAsync(viewModel.ExamId, viewModel.CurrentExamType, isAutoSubmit: false);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 无法获取训练工具栏ViewModel，手动提交失败");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 手动提交训练异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 提交训练
    /// </summary>
    private async Task SubmitTrainingAsync(int trainingId, ExamType examType, bool isAutoSubmit)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 开始提交训练，ID: {trainingId}, 类型: {examType}, 自动提交: {isAutoSubmit}");

            BenchSuiteScoringResult? scoringResult = null;
            bool submitResult = false;

            // 确保是综合实训类型
            if (examType == ExamType.ComprehensiveTraining)
            {
                // 优先使用EnhancedExamToolbarService进行BenchSuite集成提交
                if (_enhancedExamToolbarService != null)
                {
                    System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 使用EnhancedExamToolbarService进行BenchSuite集成提交");
                    scoringResult = await _enhancedExamToolbarService.SubmitComprehensiveTrainingWithResultAsync(trainingId);
                    submitResult = scoringResult != null;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: EnhancedExamToolbarService不可用，使用基础提交逻辑");

                    // 回退到基础提交逻辑，但尝试获取实际用时
                    CompleteTrainingRequest request = await CreateTrainingRequestAsync(isAutoSubmit);
                    submitResult = await _studentComprehensiveTrainingService.CompleteComprehensiveTrainingAsync(trainingId, request);
                }

                if (submitResult)
                {
                    System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 综合实训提交成功，ID: {trainingId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 综合实训提交失败，ID: {trainingId}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 不支持的训练类型: {examType}");
            }

            if (submitResult)
            {
                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 训练提交成功，ID: {trainingId}");

                // 获取训练信息并显示结果（传递真实的评分结果）
                await ShowTrainingResultAsync(trainingId, examType, scoringResult);

                // 关闭训练工具栏窗口并显示主窗口
                CloseTrainingAndShowMainWindow();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 训练提交失败，ID: {trainingId}");
                ErrorMessage = "训练提交失败，请稍后重试";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 提交训练异常: {ex.Message}");
            ErrorMessage = "训练提交失败，请稍后重试";
        }
    }

    /// <summary>
    /// 创建训练提交请求
    /// </summary>
    private Task<CompleteTrainingRequest> CreateTrainingRequestAsync(bool isAutoSubmit)
    {
        try
        {
            // 尝试从当前活动的工具栏窗口获取实际用时
            int? actualDurationSeconds = null;

            // 查找当前活动的ExamToolbarWindow
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                foreach (Avalonia.Controls.Window window in desktop.Windows)
                {
                    if (window is ExamToolbarWindow toolbarWindow &&
                        toolbarWindow.DataContext is ExamToolbarViewModel viewModel &&
                        viewModel.CurrentExamType == ExamType.ComprehensiveTraining)
                    {
                        actualDurationSeconds = viewModel.GetActualDurationSeconds();
                        System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 获取到实际用时: {actualDurationSeconds}秒");
                        break;
                    }
                }
            }

            if (!actualDurationSeconds.HasValue)
            {
                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 无法获取实际用时，使用默认值");
            }

            CompleteTrainingRequest request = new()
            {
                Score = null, // 基础提交不包含评分
                MaxScore = null,
                DurationSeconds = actualDurationSeconds,
                Notes = isAutoSubmit ? "训练时间到期，自动提交" : "学生手动提交训练",
                CompletedAt = DateTime.UtcNow // 记录精确的提交时间（UTC）
            };

            return Task.FromResult(request);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 创建训练请求异常: {ex.Message}");

            // 返回基础请求
            return Task.FromResult(new CompleteTrainingRequest
            {
                Notes = isAutoSubmit ? "训练时间到期，自动提交（异常恢复）" : "学生手动提交训练（异常恢复）",
                CompletedAt = DateTime.UtcNow // 即使异常恢复也要记录提交时间
            });
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
                System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 主窗口已显示");
            }

            // 刷新训练列表
            _ = RefreshAsync();

            System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 训练列表刷新已启动");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 关闭训练并显示主窗口异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 查看题目请求事件处理
    /// </summary>
    private void OnViewQuestionsRequested(StudentComprehensiveTrainingDto training)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 查看题目请求 - 训练: {training.Name}");

            // 创建题目详情窗口
            QuestionDetailsViewModel detailsViewModel = new();

            // 转换模块数据
            List<ModuleItem> moduleItems = training.Modules.Select(module => new ModuleItem
            {
                Id = module.Id,
                Name = module.Name,
                Description = module.Description,
                Type = module.Type,
                Score = module.Score,
                QuestionCount = module.Questions?.Count ?? 0,
                Order = module.Order,
                IsEnabled = true
            }).ToList();

            detailsViewModel.SetQuestionDetailsData(training.Name, moduleItems);

            QuestionDetailsWindow detailsWindow = new()
            {
                DataContext = detailsViewModel
            };

            // 显示题目详情窗口
            detailsWindow.Show();
            System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 题目详情窗口已显示");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 查看题目详情失败: {ex}");
        }
    }

    /// <summary>
    /// 查看答案解析请求事件处理
    /// </summary>
    private void OnViewAnswerAnalysisRequested(StudentComprehensiveTrainingDto training)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 查看答案解析请求 - 训练: {training.Name}");

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
                        Content = question.Content, // 题目的content属性作为答案解析
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
                        Content = question.Content, // 题目的content属性作为答案解析
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

            // 显示答案解析窗口
            analysisWindow.Show();
            System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingListViewModel: 答案解析窗口已显示");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ComprehensiveTrainingListViewModel: 查看答案解析失败: {ex}");
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
                System.Diagnostics.Debug.WriteLine($"无法获取综合训练信息，ID: {trainingId}");
                return;
            }

            // 如果没有传入评分结果，创建一个基本的失败结果
            if (scoringResult == null)
            {
                System.Diagnostics.Debug.WriteLine("未获取到真实评分结果，创建基本结果");
                scoringResult = new BenchSuiteScoringResult
                {
                    IsSuccess = false,
                    ErrorMessage = "未能获取评分结果",
                    TotalScore = 100,
                    AchievedScore = 0,
                    StartTime = _trainingStartTime,
                    EndTime = DateTime.Now
                };
            }

            // 显示详细的训练结果（使用真实或基本的评分结果）
            await ShowDetailedTrainingResultAsync(training.Name, scoringResult);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示综合训练结果失败: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取综合训练信息失败，ID: {trainingId}, 错误: {ex.Message}");
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

            System.Diagnostics.Debug.WriteLine("综合训练结果窗口已显示并关闭");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示详细训练结果失败: {ex.Message}");
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
