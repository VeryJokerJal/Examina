using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Examina.Extensions;
using Examina.Models;
using Examina.Models.Api;
using Examina.Models.BenchSuite;
using Examina.Models.Exam;
using Examina.Services;
using Examina.ViewModels.Dialogs;
using Examina.Views.Dialogs;
using Examina.Views;
using ReactiveUI;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 考试列表ViewModel
/// </summary>
public class ExamListViewModel : ViewModelBase
{
    private readonly IStudentExamService _studentExamService;
    private readonly IStudentFormalExamService _studentFormalExamService;
    private readonly IAuthenticationService _authenticationService;
    private readonly EnhancedExamToolbarService? _enhancedExamToolbarService;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private int _totalCount;
    private int _currentPage = 1;
    private bool _hasFullAccess;
    private bool _isUpdatingPermissions = false;
    private const int PageSize = 20;

    /// <summary>
    /// 考试列表
    /// </summary>
    public ObservableCollection<StudentExamDto> Exams { get; } = [];

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
    public bool HasMoreData => Exams.Count < TotalCount;

    /// <summary>
    /// 用户是否拥有完整功能权限
    /// </summary>
    public bool HasFullAccess
    {
        get => _hasFullAccess;
        set => this.RaiseAndSetIfChanged(ref _hasFullAccess, value);
    }

    /// <summary>
    /// 开始考试按钮文本
    /// </summary>
    public string StartButtonText => HasFullAccess ? "开始考试" : "解锁";

    /// <summary>
    /// 刷新命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    /// <summary>
    /// 加载更多命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadMoreCommand { get; }

    /// <summary>
    /// 开始考试命令
    /// </summary>
    public ReactiveCommand<StudentExamDto, Unit> StartExamCommand { get; }


    public ExamListViewModel(IStudentExamService studentExamService, IStudentFormalExamService studentFormalExamService,
        IAuthenticationService authenticationService, EnhancedExamToolbarService? enhancedExamToolbarService = null)
    {
        _studentExamService = studentExamService;
        _studentFormalExamService = studentFormalExamService;
        _authenticationService = authenticationService;
        _enhancedExamToolbarService = enhancedExamToolbarService;

        // 创建命令
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        LoadMoreCommand = ReactiveCommand.CreateFromTask(LoadMoreAsync, this.WhenAnyValue(x => x.HasMoreData, x => x.IsLoading, (hasMore, loading) => hasMore && !loading));
        StartExamCommand = ReactiveCommand.CreateFromTask<StudentExamDto>(StartExamAsync);

        // 初始化用户权限状态
        _ = UpdateUserPermissionsAsync();

        // 监听用户信息更新事件
        _authenticationService.UserInfoUpdated += OnUserInfoUpdated;

        // 初始加载
        _ = RefreshAsync();
    }

    private void OnUserInfoUpdated(object? sender, UserInfo? e)
    {
        _ = UpdateUserPermissionsAsync();
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
            TotalCount = await _studentExamService.GetAvailableExamCountAsync();

            // 获取第一页数据
            List<StudentExamDto> exams = await _studentExamService.GetAvailableExamsAsync(CurrentPage, PageSize);

            // 更新UI
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Exams.Clear();
                foreach (StudentExamDto exam in exams)
                {
                    Exams.Add(exam);
                }
            });

            System.Diagnostics.Debug.WriteLine($"刷新考试列表成功，共 {TotalCount} 项，当前显示 {Exams.Count} 项");

            // 数据刷新完成后，强制更新用户权限状态
            try
            {
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 刷新完成，开始更新用户权限状态");
                _ = UpdateUserPermissionsAsync();
            }
            catch (Exception permissionEx)
            {
                // 权限更新失败不应影响数据刷新的成功状态
                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 权限状态更新失败: {permissionEx.Message}");
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("获取考试列表失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "加载考试列表失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"刷新考试列表失败: {ex.Message}");
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
            List<StudentExamDto> exams = await _studentExamService.GetAvailableExamsAsync(nextPage, PageSize);

            if (exams.Count > 0)
            {
                // 更新UI
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (StudentExamDto exam in exams)
                    {
                        Exams.Add(exam);
                    }
                });

                CurrentPage = nextPage;
                System.Diagnostics.Debug.WriteLine($"加载更多考试成功，当前页: {CurrentPage}，总显示: {Exams.Count} 项");
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("加载更多考试失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "加载更多数据失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"加载更多考试失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 开始考试
    /// </summary>
    private async Task StartExamAsync(StudentExamDto exam)
    {
        try
        {
            if (!HasFullAccess)
            {
                // 用户没有完整权限，显示解锁提示
                ErrorMessage = "您需要解锁权限才能开始考试。请加入学校组织或联系管理员进行解锁。";
                System.Diagnostics.Debug.WriteLine("用户尝试开始考试但没有完整权限");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 准备开始上机统考: {exam.Name} (ID: {exam.Id})");

            // 检查权限
            bool hasAccess = await _studentExamService.HasAccessToExamAsync(exam.Id);
            if (!hasAccess)
            {
                ErrorMessage = "您没有权限访问此考试";
                return;
            }

            // 显示上机统考规则说明对话框
            FormalExamRulesViewModel rulesViewModel = new();
            FormalExamRulesDialog dialog = new(rulesViewModel);

            // 设置对话框的父窗口
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 准备显示上机统考规则对话框");

                bool? result = await dialog.ShowDialog<bool?>(desktop.MainWindow);

                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 对话框返回结果: {result}");

                if (result == true)
                {
                    System.Diagnostics.Debug.WriteLine("ExamListViewModel: 用户确认开始上机统考");
                    // 用户确认开始，启动正式考试
                    await StartFormalExamAsync(exam);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ExamListViewModel: 用户取消了上机统考");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 无法获取主窗口");
                ErrorMessage = "无法显示规则对话框，请重试";
            }
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "认证失败，请重新登录";
            System.Diagnostics.Debug.WriteLine("开始考试失败：用户未认证");
        }
        catch (Exception ex)
        {
            ErrorMessage = "开始考试失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"开始考试失败: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine("ExamListViewModel: 权限状态正在更新中，跳过重复调用");
            return;
        }

        try
        {
            _isUpdatingPermissions = true;
            System.Diagnostics.Debug.WriteLine("ExamListViewModel: 开始更新用户权限状态");

            // 主动刷新用户信息以获取最新状态
            bool refreshSuccess = await _authenticationService.RefreshUserInfoAsync();

            if (refreshSuccess)
            {
                // 刷新成功，获取最新的用户信息
                UserInfo? currentUser = _authenticationService.CurrentUser;
                HasFullAccess = currentUser?.HasFullAccess ?? false;

                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 用户信息刷新成功 - HasFullAccess: {HasFullAccess}");
            }
            else
            {
                // 刷新失败，使用当前缓存的用户信息
                UserInfo? currentUser = _authenticationService.CurrentUser;
                HasFullAccess = currentUser?.HasFullAccess ?? false;

                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 用户信息刷新失败，使用缓存信息 - HasFullAccess: {HasFullAccess}");
            }

            // 通知UI更新按钮文本
            this.RaisePropertyChanged(nameof(StartButtonText));
        }
        catch (Exception ex)
        {
            // 异常处理：使用默认值
            HasFullAccess = false;
            this.RaisePropertyChanged(nameof(StartButtonText));

            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 更新用户权限状态异常: {ex.Message}");
        }
        finally
        {
            _isUpdatingPermissions = false;
            System.Diagnostics.Debug.WriteLine("ExamListViewModel: 权限状态更新完成");
        }
    }

    /// <summary>
    /// 启动正式考试
    /// </summary>
    private async Task StartFormalExamAsync(StudentExamDto exam)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 开始启动正式考试，考试ID: {exam.Id}");

            // 获取考试详情
            StudentExamDto? examDetails = await _studentExamService.GetExamDetailsAsync(exam.Id);
            if (examDetails == null)
            {
                ErrorMessage = "无法获取考试详情，请稍后重试";
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 获取考试详情失败");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 成功获取考试详情 - 名称: {examDetails.Name}, 时长: {examDetails.DurationMinutes}分钟");

            // 文件预下载准备
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 开始文件预下载准备");

                bool filesReady = await desktop.MainWindow.PrepareFilesForOnlineExamAsync(examDetails.Id, examDetails.Name);
                if (!filesReady)
                {
                    ErrorMessage = "文件准备失败，无法开始考试。请检查网络连接或联系管理员。";
                    System.Diagnostics.Debug.WriteLine("ExamListViewModel: 文件预下载失败，取消考试启动");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 文件预下载完成，继续启动考试");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 无法获取主窗口，跳过文件预下载");
            }

            // 启动正式考试界面
            await StartFormalExamInterfaceAsync(examDetails);
        }
        catch (Exception ex)
        {
            ErrorMessage = "启动正式考试失败，请稍后重试";
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 启动正式考试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动正式考试界面
    /// </summary>
    private async Task StartFormalExamInterfaceAsync(StudentExamDto exam)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 准备启动正式考试界面，考试ID: {exam.Id}");

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                // 隐藏主窗口
                desktop.MainWindow.Hide();
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 主窗口已隐藏");

                // 创建考试工具栏 ViewModel
                IBenchSuiteDirectoryService? benchSuiteDirectoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
                ExamToolbarViewModel toolbarViewModel = new(_authenticationService, null, benchSuiteDirectoryService);

                // 计算总题目数
                int totalQuestions = exam.Subjects.Sum(s => s.Questions.Count) + exam.Modules.Sum(m => m.Questions.Count);

                // 设置考试信息
                toolbarViewModel.SetExamInfo(
                    ExamType.FormalExam,
                    exam.Id,
                    exam.Name,
                    totalQuestions,
                    exam.DurationMinutes * 60 // 转换为秒
                );

                // 创建考试工具栏窗口并设置 ViewModel
                ExamToolbarWindow examToolbar = new();
                examToolbar.SetViewModel(toolbarViewModel);

                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 考试工具栏已配置 - 考试ID: {exam.Id}, 题目数: {totalQuestions}, 时长: {exam.DurationMinutes}分钟");

                // 订阅考试事件
                examToolbar.ExamAutoSubmitted += OnExamAutoSubmitted;
                examToolbar.ExamManualSubmitted += OnExamManualSubmitted;
                examToolbar.ViewQuestionsRequested += OnViewQuestionsRequested;

                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 已订阅考试工具栏事件");

                // 显示工具栏窗口
                examToolbar.Show();
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 考试工具栏窗口已显示");

                // 开始考试（启动倒计时器并设置状态为进行中）
                toolbarViewModel.StartExam();
                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 考试已开始，剩余时间: {toolbarViewModel.RemainingTimeSeconds}秒");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 无法获取主窗口，启动考试界面失败");
                ErrorMessage = "无法启动考试界面，请重试";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 启动正式考试界面异常: {ex.Message}");
            ErrorMessage = "启动考试界面失败，请重试";
        }
    }

    /// <summary>
    /// 处理考试自动提交事件
    /// </summary>
    private async void OnExamAutoSubmitted(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("ExamListViewModel: 考试时间到，自动提交");

        try
        {
            // 获取考试工具栏窗口以获取考试信息
            if (sender is ExamToolbarWindow examToolbar && examToolbar.DataContext is ExamToolbarViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 自动提交正式考试，ID: {viewModel.ExamId}, 类型: {viewModel.CurrentExamType}");
                await SubmitFormalExamWithBenchSuiteAsync(viewModel.ExamId, viewModel.CurrentExamType, isAutoSubmit: true, examToolbar);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 无法获取考试工具栏ViewModel，自动提交失败");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 自动提交考试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理考试手动提交事件
    /// </summary>
    private async void OnExamManualSubmitted(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("ExamListViewModel: 学生手动提交考试");

        try
        {
            // 获取考试工具栏窗口以获取考试信息
            if (sender is ExamToolbarWindow examToolbar && examToolbar.DataContext is ExamToolbarViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 手动提交正式考试，ID: {viewModel.ExamId}, 类型: {viewModel.CurrentExamType}");
                await SubmitFormalExamWithBenchSuiteAsync(viewModel.ExamId, viewModel.CurrentExamType, isAutoSubmit: false, examToolbar);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 无法获取考试工具栏ViewModel，手动提交失败");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 手动提交考试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理查看题目请求事件
    /// </summary>
    private void OnViewQuestionsRequested(object? sender, EventArgs e)
    {
        try
        {
            // 获取考试工具栏窗口以获取考试信息
            if (sender is ExamToolbarWindow examToolbar && examToolbar.DataContext is ExamToolbarViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 用户请求查看正式考试题目，考试ID: {viewModel.ExamId}");

                // 查找对应的考试数据
                StudentExamDto? exam = Exams.FirstOrDefault(e => e.Id == viewModel.ExamId);
                if (exam != null)
                {
                    ShowExamQuestionDetails(exam);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 未找到考试ID为 {viewModel.ExamId} 的考试数据");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 无法获取考试工具栏ViewModel，显示题目详情失败");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 显示题目详情异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示考试题目详情
    /// </summary>
    private void ShowExamQuestionDetails(StudentExamDto exam)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 显示考试题目详情 - {exam.Name}");

            // 创建题目详情窗口
            ExamQuestionDetailsViewModel detailsViewModel = new();
            detailsViewModel.SetExamData(exam);

            ExamQuestionDetailsWindow detailsWindow = new()
            {
                DataContext = detailsViewModel
            };

            // 显示题目详情窗口
            detailsWindow.Show();
            System.Diagnostics.Debug.WriteLine("ExamListViewModel: 考试题目详情窗口已显示");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 显示考试题目详情异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 使用BenchSuite评分提交正式考试（异步评分模式）
    /// </summary>
    private async Task SubmitFormalExamWithBenchSuiteAsync(int examId, ExamType examType, bool isAutoSubmit, ExamToolbarWindow examToolbar)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 开始提交正式考试，ID: {examId}, 类型: {examType}, 自动提交: {isAutoSubmit}");

            // 获取实际用时（从考试工具栏）
            int? actualDurationSeconds = GetActualDurationFromToolbar(examToolbar);

            // 先进行基本提交，确保考试状态正确
            bool basicSubmitResult = false;
            string errorMessage = "";

            try
            {
                basicSubmitResult = await _studentFormalExamService.CompleteExamAsync(examId);
                if (!basicSubmitResult)
                {
                    errorMessage = "考试基本提交失败";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 基本提交异常: {ex.Message}");
                errorMessage = $"考试提交异常: {ex.Message}";
                basicSubmitResult = false;
            }

            // 关闭考试工具栏窗口
            CloseExamToolbarWindow(examToolbar);

            // 先显示考试结果窗口（不包含评分，显示计算中状态）
            // 窗口关闭后会自动显示主窗口，这里不需要手动调用
            await ShowExamResultWithAsyncScoringAsync(examId, examType, basicSubmitResult, actualDurationSeconds, errorMessage);

            // 结果窗口关闭后会自动显示主窗口，这里不需要手动调用
            // ShowMainWindowAndRefresh(); // 移除过早的主窗口显示调用
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 提交正式考试异常: {ex.Message}");

            // 确保关闭考试工具栏窗口
            CloseExamToolbarWindow(examToolbar);

            // 显示错误结果，窗口关闭后会自动显示主窗口
            await ShowExamResultAsync(examId, examType, false, null, null, null, $"提交异常: {ex.Message}", "");

            // 传统模式的结果窗口关闭后会自动显示主窗口，这里不需要手动调用
            // ShowMainWindowAndRefresh(); // 移除过早的主窗口显示调用
        }
    }

    /// <summary>
    /// 从考试工具栏获取实际用时
    /// </summary>
    private int? GetActualDurationFromToolbar(ExamToolbarWindow examToolbar)
    {
        try
        {
            if (examToolbar.DataContext is ExamToolbarViewModel viewModel)
            {
                // 使用ExamToolbarViewModel的GetActualDurationSeconds方法
                int actualSeconds = viewModel.GetActualDurationSeconds();

                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 实际用时计算 - 实际用时: {actualSeconds}秒");

                return actualSeconds > 0 ? actualSeconds : null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 获取实际用时异常: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 关闭考试工具栏窗口
    /// </summary>
    private void CloseExamToolbarWindow(ExamToolbarWindow examToolbar)
    {
        try
        {
            examToolbar?.Close();
            System.Diagnostics.Debug.WriteLine("ExamListViewModel: 考试工具栏窗口已关闭");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 关闭考试工具栏窗口异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示考试结果窗口（带异步评分）
    /// </summary>
    private async Task ShowExamResultWithAsyncScoringAsync(int examId, ExamType examType, bool isSuccessful,
        int? actualDurationSeconds, string errorMessage)
    {
        try
        {
            // 获取考试名称
            string examName = "上机统考";
            try
            {
                StudentExamDto? examDetails = await _studentExamService.GetExamDetailsAsync(examId);
                if (examDetails != null)
                {
                    examName = examDetails.Name;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 获取考试名称异常: {ex.Message}");
            }

            // 转换用时为分钟
            int? durationMinutes = actualDurationSeconds.HasValue ? (actualDurationSeconds.Value / 60) : null;

            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 准备显示全屏考试结果窗口（异步评分模式） - {examName}");

            // 创建全屏结果窗口并显示
            Views.Dialogs.FullScreenExamResultWindow resultWindow = Views.Dialogs.FullScreenExamResultWindow.ShowFullScreenExamResult(
                examName,
                examType,
                isSuccessful,
                null, // startTime
                null, // endTime
                durationMinutes,
                null, // score - 初始为空，异步评分后更新
                null, // totalScore
                errorMessage,
                isSuccessful ? "考试提交成功，正在计算成绩..." : "考试提交失败",
                true, // showContinue
                false // showClose - 只显示确认按钮
            );

            // 如果提交成功，开始评分计算
            if (isSuccessful)
            {
                resultWindow.StartScoring();
            }

            // 如果提交成功，在后台开始异步评分
            if (isSuccessful)
            {
                _ = Task.Run(async () => await PerformAsyncScoringAsync(examId, resultWindow));
            }

            // 等待窗口关闭
            await resultWindow.WaitForCloseAsync();

            System.Diagnostics.Debug.WriteLine("ExamListViewModel: 全屏考试结果窗口已显示并关闭（异步评分模式）");

            // 窗口关闭后显示主窗口
            ShowMainWindowAndRefresh();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 显示考试结果异常: {ex.Message}");
            // 如果显示结果窗口失败，也要显示主窗口
            ShowMainWindowAndRefresh();
        }
    }

    /// <summary>
    /// 显示考试结果窗口（传统模式，用于错误情况）
    /// </summary>
    private async Task ShowExamResultAsync(int examId, ExamType examType, bool isSuccessful,
        int? actualDurationSeconds, decimal? score, decimal? totalScore, string errorMessage, string notes)
    {
        try
        {
            // 获取考试名称
            string examName = "上机统考";
            try
            {
                StudentExamDto? examDetails = await _studentExamService.GetExamDetailsAsync(examId);
                if (examDetails != null)
                {
                    examName = examDetails.Name;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 获取考试名称异常: {ex.Message}");
            }

            // 转换用时为分钟
            int? durationMinutes = actualDurationSeconds.HasValue ? (actualDurationSeconds.Value / 60) : null;

            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 准备显示全屏考试结果窗口（传统模式） - {examName}");

            // 使用新的全屏考试结果窗口
            await Views.Dialogs.FullScreenExamResultWindow.ShowFullScreenExamResultAsync(
                examName,
                examType,
                isSuccessful,
                null, // startTime
                null, // endTime
                durationMinutes,
                score,
                totalScore,
                errorMessage,
                notes,
                true, // showContinue
                false // showClose - 只显示确认按钮
            );

            System.Diagnostics.Debug.WriteLine("ExamListViewModel: 全屏考试结果窗口已显示并关闭（传统模式）");

            // 窗口关闭后显示主窗口
            ShowMainWindowAndRefresh();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 显示考试结果窗口异常: {ex.Message}");
            // 如果显示结果窗口失败，也要显示主窗口
            ShowMainWindowAndRefresh();
        }
    }

    /// <summary>
    /// 执行异步评分
    /// </summary>
    private async Task PerformAsyncScoringAsync(int examId, ExamResultViewModel resultViewModel)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 开始异步BenchSuite评分，考试ID: {examId}");

            // 延迟一下，让用户看到"计算中..."状态
            await Task.Delay(1000);

            BenchSuiteScoringResult? scoringResult = null;

            // 使用EnhancedExamToolbarService进行BenchSuite评分
            if (_enhancedExamToolbarService != null)
            {
                try
                {
                    scoringResult = await _enhancedExamToolbarService.SubmitFormalExamWithResultAsync(examId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 异步BenchSuite评分异常: {ex.Message}");
                }
            }

            // 在UI线程上更新结果
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (scoringResult != null && scoringResult.IsSuccess)
                {
                    // 评分成功，更新分数
                    resultViewModel.UpdateScore(scoringResult.AchievedScore, scoringResult.TotalScore, "BenchSuite自动评分完成");
                    System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 异步评分完成，得分: {scoringResult.AchievedScore}");
                }
                else
                {
                    // 评分失败
                    string errorMsg = scoringResult?.ErrorMessage ?? "BenchSuite评分服务不可用";
                    resultViewModel.ScoringFailed($"评分失败: {errorMsg}");
                    System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 异步评分失败: {errorMsg}");
                }
            });

            // 如果评分成功，自动提交成绩到服务器
            if (scoringResult != null && scoringResult.IsSuccess)
            {
                await AutoSubmitScoreAsync(examId, scoringResult);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 异步评分过程异常: {ex.Message}");

            // 在UI线程上更新错误状态
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                resultViewModel.ScoringFailed($"评分过程异常: {ex.Message}");
            });
        }
    }

    /// <summary>
    /// 执行异步评分（全屏窗口版本）
    /// </summary>
    private async Task PerformAsyncScoringAsync(int examId, Views.Dialogs.FullScreenExamResultWindow resultWindow)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 开始异步BenchSuite评分（全屏窗口），考试ID: {examId}");

            // 延迟一下，让用户看到"计算中..."状态
            await Task.Delay(1000);

            BenchSuiteScoringResult? scoringResult = null;

            // 使用EnhancedExamToolbarService进行BenchSuite评分
            if (_enhancedExamToolbarService != null)
            {
                try
                {
                    scoringResult = await _enhancedExamToolbarService.SubmitFormalExamWithResultAsync(examId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 异步BenchSuite评分异常: {ex.Message}");
                }
            }

            // 在UI线程上更新结果
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (scoringResult != null && scoringResult.IsSuccess)
                {
                    // 评分成功，更新分数
                    resultWindow.UpdateScore(scoringResult.AchievedScore, scoringResult.TotalScore, "BenchSuite自动评分完成");
                    System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 异步评分完成，得分: {scoringResult.AchievedScore}");
                }
                else
                {
                    // 评分失败
                    string errorMsg = scoringResult?.ErrorMessage ?? "BenchSuite评分服务不可用";
                    resultWindow.ScoringFailed($"评分失败: {errorMsg}");
                    System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 异步评分失败: {errorMsg}");
                }
            });

            // 如果评分成功，自动提交成绩到服务器
            if (scoringResult != null && scoringResult.IsSuccess)
            {
                await AutoSubmitScoreAsync(examId, scoringResult);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 异步评分过程异常: {ex.Message}");

            // 在UI线程上更新错误状态
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                resultWindow.ScoringFailed($"评分过程异常: {ex.Message}");
            });
        }
    }

    /// <summary>
    /// 自动提交成绩到服务器
    /// </summary>
    private async Task AutoSubmitScoreAsync(int examId, BenchSuiteScoringResult scoringResult)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 开始自动提交成绩到服务器，考试ID: {examId}");

            // 准备成绩提交数据
            SubmitExamScoreRequestDto scoreRequest = new()
            {
                Score = scoringResult.AchievedScore,
                MaxScore = scoringResult.TotalScore,
                DurationSeconds = null,
                Notes = "BenchSuite自动评分完成",
                BenchSuiteScoringResult = System.Text.Json.JsonSerializer.Serialize(scoringResult),
                CompletedAt = DateTime.Now
            };

            // 提交成绩到服务器
            bool submitResult = await _studentFormalExamService.SubmitExamScoreAsync(examId, scoreRequest);

            if (submitResult)
            {
                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 成绩自动提交成功，得分: {scoringResult.AchievedScore}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 成绩自动提交失败");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 自动提交成绩异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示主窗口并刷新数据
    /// </summary>
    private void ShowMainWindowAndRefresh()
    {
        try
        {
            // 显示主窗口
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.Activate();
                System.Diagnostics.Debug.WriteLine("ExamListViewModel: 主窗口已显示");
            }

            // 刷新数据
            _ = UpdateUserPermissionsAsync();

            // 重新加载考试列表
            _ = Task.Run(async () =>
            {
                try
                {
                    await RefreshAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 刷新考试列表异常: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 显示主窗口并刷新数据异常: {ex.Message}");
        }
    }
}
