using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Examina.Extensions;
using Examina.Models;
using Examina.Models.Exam;
using Examina.Models.MockExam;
using Examina.Services;
using Examina.ViewModels.Dialogs;
using Examina.Views;
using Examina.Views.Dialogs;
using ReactiveUI;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 模拟考试视图模型
/// </summary>
public class MockExamViewModel : ViewModelBase
{
    private readonly IStudentMockExamService _mockExamService;
    private readonly IAuthenticationService _authenticationService;
    private readonly EnhancedExamToolbarService? _enhancedExamToolbarService;

    private bool _isLoading;
    private string? _errorMessage;
    private bool _hasFullAccess;
    private bool _isUpdatingPermissions = false;

    /// <summary>
    /// 概览页面刷新请求事件
    /// </summary>
    public static event EventHandler? OverviewPageRefreshRequested;

    /// <summary>
    /// 排行榜页面刷新请求事件
    /// </summary>
    public static event EventHandler? LeaderboardPageRefreshRequested;

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
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    /// <summary>
    /// 是否有完整访问权限
    /// </summary>
    public bool HasFullAccess
    {
        get => _hasFullAccess;
        set => this.RaiseAndSetIfChanged(ref _hasFullAccess, value);
    }

    /// <summary>
    /// 开始按钮文本
    /// </summary>
    public string StartButtonText => HasFullAccess ? "开始模拟考试" : "解锁";

    /// <summary>
    /// 开始模拟考试命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartMockExamCommand { get; }

    public MockExamViewModel(IStudentMockExamService mockExamService, IAuthenticationService authenticationService)
    {
        _mockExamService = mockExamService;
        _authenticationService = authenticationService;

        // 尝试获取EnhancedExamToolbarService（可选依赖）
        try
        {
            _enhancedExamToolbarService = AppServiceManager.GetService<EnhancedExamToolbarService>();
            if (_enhancedExamToolbarService != null)
            {
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 成功获取EnhancedExamToolbarService");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: EnhancedExamToolbarService不可用，将使用原有提交逻辑");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 获取EnhancedExamToolbarService失败: {ex.Message}");
            _enhancedExamToolbarService = null;
        }

        // 初始化命令
        StartMockExamCommand = ReactiveCommand.CreateFromTask(StartMockExamAsync, this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));

        // 初始化用户权限状态
        UpdateUserPermissions();

        // 监听用户信息更新事件
        _authenticationService.UserInfoUpdated += OnUserInfoUpdated;
    }

    /// <summary>
    /// 开始模拟考试
    /// </summary>
    private async Task StartMockExamAsync()
    {
        try
        {
            if (!HasFullAccess)
            {
                // 用户没有完整权限，显示解锁提示
                ErrorMessage = "您需要解锁权限才能开始模拟考试。请加入学校组织或联系管理员进行解锁。";
                System.Diagnostics.Debug.WriteLine("用户尝试开始模拟考试但没有完整权限");
                return;
            }

            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 准备开始模拟考试");

            // 显示规则说明对话框
            MockExamRulesViewModel rulesViewModel = new();
            MockExamRulesDialog dialog = new(rulesViewModel);

            // 设置对话框的父窗口
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 准备显示规则对话框");

                bool? result = await dialog.ShowDialog<bool?>(desktop.MainWindow);

                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 对话框返回结果: {result}");

                if (result == true)
                {
                    System.Diagnostics.Debug.WriteLine("MockExamViewModel: 用户确认开始模拟考试");
                    // 用户确认开始，调用快速开始API
                    await QuickStartMockExamAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MockExamViewModel: 用户取消了模拟考试");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 无法获取主窗口");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 开始模拟考试异常: {ex.Message}");
            ErrorMessage = "启动模拟考试失败，请重试";
        }
    }

    /// <summary>
    /// 快速开始模拟考试
    /// </summary>
    private async Task QuickStartMockExamAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 开始调用快速开始模拟考试API");

            // 调用新的API获取模块化的模拟考试数据
            MockExamComprehensiveTrainingDto? mockExam = await _mockExamService.QuickStartMockExamComprehensiveTrainingAsync();
            if (mockExam != null)
            {
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 成功生成模拟考试，ID: {mockExam.Id}");
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 模拟考试包含 {mockExam.Modules.Count} 个模块");

                // 记录模块信息
                foreach (MockExamModuleDto module in mockExam.Modules)
                {
                    System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 模块 {module.Name} ({module.Type})，包含 {module.Questions.Count} 道题目，描述: {module.Description}");
                }

                // 启动模拟考试界面
                await StartMockExamInterfaceAsync(mockExam);
            }
            else
            {
                ErrorMessage = "生成模拟考试失败，请检查题库或稍后重试";
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 快速开始模拟考试失败");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 快速开始模拟考试异常: {ex.Message}");
            ErrorMessage = "生成模拟考试失败，请重试";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 更新用户权限状态
    /// </summary>
    private void UpdateUserPermissions()
    {
        if (_isUpdatingPermissions)
        {
            return;
        }

        try
        {
            _isUpdatingPermissions = true;
            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 开始更新用户权限状态");

            UserInfo? currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                // 检查用户是否有完整权限
                HasFullAccess = currentUser.HasFullAccess;
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 用户权限状态 - HasFullAccess: {HasFullAccess}");
            }
            else
            {
                HasFullAccess = false;
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 当前用户为空，设置为无权限");
            }

            // 通知UI更新按钮文本
            this.RaisePropertyChanged(nameof(StartButtonText));
        }
        catch (Exception ex)
        {
            // 异常处理：使用默认值
            HasFullAccess = false;
            this.RaisePropertyChanged(nameof(StartButtonText));

            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 更新用户权限状态异常: {ex.Message}");
        }
        finally
        {
            _isUpdatingPermissions = false;
            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 权限状态更新完成");
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
    /// 启动模拟考试界面
    /// </summary>
    private async Task StartMockExamInterfaceAsync(MockExamComprehensiveTrainingDto mockExam)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 开始启动模拟考试界面");

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                // 文件预下载准备
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 开始文件预下载准备");

                bool filesReady = await desktop.MainWindow.PrepareFilesForMockExamAsync(mockExam.Id, mockExam.Name);
                if (!filesReady)
                {
                    ErrorMessage = "文件准备失败，无法开始模拟考试。请检查网络连接或联系管理员。";
                    System.Diagnostics.Debug.WriteLine("MockExamViewModel: 文件预下载失败，取消模拟考试启动");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 文件预下载完成，继续启动模拟考试");

                // 隐藏主窗口
                desktop.MainWindow.Hide();
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 主窗口已隐藏");

                // 创建考试工具栏 ViewModel
                IBenchSuiteDirectoryService? benchSuiteDirectoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
                ExamToolbarViewModel toolbarViewModel = new(_authenticationService, null, benchSuiteDirectoryService);

                // 计算总题目数
                int totalQuestions = mockExam.Modules.Sum(m => m.Questions.Count);

                // 设置考试信息
                toolbarViewModel.SetExamInfo(
                    ExamType.MockExam,
                    mockExam.Id,
                    mockExam.Name,
                    totalQuestions,
                    mockExam.DurationMinutes * 60 // 转换为秒
                );

                // 设置模块信息用于题目详情显示
                await SetModuleInformationAsync(toolbarViewModel, mockExam);

                // 创建考试工具栏窗口并设置 ViewModel
                ExamToolbarWindow examToolbar = new();
                examToolbar.SetViewModel(toolbarViewModel);

                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 考试工具栏已配置 - 考试ID: {mockExam.Id}, 题目数: {totalQuestions}, 时长: {mockExam.DurationMinutes}分钟");

                // 订阅考试事件 - 这是关键的事件订阅
                examToolbar.ExamAutoSubmitted += OnExamAutoSubmitted;
                examToolbar.ExamManualSubmitted += OnExamManualSubmitted;
                examToolbar.ViewQuestionsRequested += (sender, e) => OnViewQuestionsRequested(mockExam);

                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 已订阅考试工具栏事件");

                // 显示工具栏窗口
                examToolbar.Show();
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 考试工具栏窗口已显示");

                // 开始考试（启动倒计时器并设置状态为进行中）
                toolbarViewModel.StartExam();
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 考试已开始，剩余时间: {toolbarViewModel.RemainingTimeSeconds}秒, 格式化时间: {toolbarViewModel.FormattedRemainingTime}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 无法获取主窗口，启动考试界面失败");
                ErrorMessage = "无法启动考试界面，请重试";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 启动模拟考试界面异常: {ex.Message}");
            ErrorMessage = "启动考试界面失败，请重试";
        }
    }

    /// <summary>
    /// 设置模块信息用于题目详情显示
    /// </summary>
    private async Task SetModuleInformationAsync(ExamToolbarViewModel toolbarViewModel, MockExamComprehensiveTrainingDto mockExam)
    {
        try
        {
            // 设置学生信息
            UserInfo? currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                toolbarViewModel.StudentName = currentUser.RealName ?? "未知学生";
            }

            // 记录模块详细信息
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 设置模块信息，共 {mockExam.Modules.Count} 个模块");

            foreach (MockExamModuleDto module in mockExam.Modules)
            {
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 模块 {module.Name} - {module.Description}");
                foreach (MockExamQuestionDto question in module.Questions)
                {
                    System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 题目 {question.Id} - {question.Title}");
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 设置模块信息异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理考试自动提交事件
    /// </summary>
    private async void OnExamAutoSubmitted(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MockExamViewModel: 考试时间到，自动提交");

        try
        {
            // 获取考试工具栏窗口以获取考试信息
            if (sender is ExamToolbarWindow examToolbar && examToolbar.DataContext is ExamToolbarViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 自动提交考试，ID: {viewModel.ExamId}, 类型: {viewModel.CurrentExamType}");
                await SubmitExamWithBenchSuiteAsync(viewModel.ExamId, viewModel.CurrentExamType, isAutoSubmit: true);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 无法获取考试工具栏ViewModel，自动提交失败");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 自动提交考试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理考试手动提交事件
    /// </summary>
    private async void OnExamManualSubmitted(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MockExamViewModel: 学生手动提交考试");

        try
        {
            // 获取考试工具栏窗口以获取考试信息
            if (sender is ExamToolbarWindow examToolbar && examToolbar.DataContext is ExamToolbarViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 手动提交考试，ID: {viewModel.ExamId}, 类型: {viewModel.CurrentExamType}");
                await SubmitExamWithBenchSuiteAsync(viewModel.ExamId, viewModel.CurrentExamType, isAutoSubmit: false);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 无法获取考试工具栏ViewModel，手动提交失败");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 手动提交考试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理查看题目请求事件
    /// </summary>
    private void OnViewQuestionsRequested(MockExamComprehensiveTrainingDto mockExam)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 用户请求查看题目");

            // 将MockExamComprehensiveTrainingDto转换为StudentExamDto
            StudentExamDto examData = ConvertMockExamToStudentExam(mockExam);

            // 创建通用题目详情窗口
            ExamQuestionDetailsViewModel detailsViewModel = new();
            detailsViewModel.SetExamData(examData);

            ExamQuestionDetailsWindow detailsWindow = new()
            {
                DataContext = detailsViewModel
            };

            // 显示题目详情窗口
            detailsWindow.Show();
            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 题目详情窗口已显示");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 显示题目详情窗口异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 将MockExamComprehensiveTrainingDto转换为StudentExamDto
    /// </summary>
    private StudentExamDto ConvertMockExamToStudentExam(MockExamComprehensiveTrainingDto mockExam)
    {
        System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 开始转换模拟考试数据 - {mockExam.Name}");
        System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 原始模块数量: {mockExam.Modules.Count}");

        // 打印每个模块的详细信息
        for (int i = 0; i < mockExam.Modules.Count; i++)
        {
            MockExamModuleDto module = mockExam.Modules[i];
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 模块 {i + 1} - ID: {module.Id}, 名称: {module.Name}, 类型: {module.Type}, 题目数: {module.Questions?.Count ?? 0}");
        }

        StudentExamDto studentExam = new()
        {
            Id = mockExam.Id,
            Name = mockExam.Name,
            Description = mockExam.Description,
            ExamType = "MockExam",
            Status = mockExam.Status,
            TotalScore = (int)mockExam.TotalScore,
            DurationMinutes = mockExam.DurationMinutes
        };

        // 转换模块
        foreach (MockExamModuleDto mockModule in mockExam.Modules)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 正在转换模块 - {mockModule.Name}");

            StudentModuleDto studentModule = new()
            {
                Id = mockModule.Id,
                Name = mockModule.Name,
                Type = mockModule.Type,
                Description = mockModule.Description ?? string.Empty,
                Score = (int)mockModule.Score,
                Order = mockModule.Order
            };

            // 转换题目（如果存在）
            if (mockModule.Questions != null)
            {
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 模块 {mockModule.Name} 包含 {mockModule.Questions.Count} 个题目");
                foreach (MockExamQuestionDto mockQuestion in mockModule.Questions)
                {
                    StudentQuestionDto studentQuestion = new()
                    {
                        Id = mockQuestion.Id,
                        Title = mockQuestion.Title,
                        Content = mockQuestion.Content,
                        QuestionType = mockModule.Type, // 使用模块类型作为题目类型
                        Score = (int)mockQuestion.Score,
                        SortOrder = mockQuestion.SortOrder,
                        IsRequired = mockQuestion.IsRequired
                    };
                    studentModule.Questions.Add(studentQuestion);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 模块 {mockModule.Name} 没有题目");
            }

            // 无论模块是否包含题目，都添加到列表中
            studentExam.Modules.Add(studentModule);
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 已添加模块 {mockModule.Name} 到转换结果");
        }

        // 兼容：合并科目为模块显示，补充未出现在Modules中的模块
        if (mockExam.Subjects != null && mockExam.Subjects.Count > 0)
        {
            foreach (MockExamSubjectDto subject in mockExam.Subjects)
            {
                bool exists = studentExam.Modules.Any(m => string.Equals(m.Name, subject.SubjectName, StringComparison.OrdinalIgnoreCase)
                                                        && string.Equals(m.Type, subject.SubjectType, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    StudentModuleDto moduleFromSubject = new()
                    {
                        Id = subject.Id,
                        Name = subject.SubjectName,
                        Type = subject.SubjectType,
                        Description = subject.Description ?? string.Empty,
                        Score = (int)subject.Score,
                        Order = subject.SortOrder
                    };

                    if (subject.Questions != null)
                    {
                        foreach (MockExamQuestionDto q in subject.Questions)
                        {
                            StudentQuestionDto mapped = new()
                            {
                                Id = q.Id,
                                Title = q.Title,
                                Content = q.Content,
                                QuestionType = subject.SubjectType,
                                Score = (int)q.Score,
                                SortOrder = q.SortOrder,
                                IsRequired = q.IsRequired
                            };
                            moduleFromSubject.Questions.Add(mapped);
                        }
                    }

                    studentExam.Modules.Add(moduleFromSubject);
                    System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 从科目补充模块 {moduleFromSubject.Name}");
                }
            }
        }

        // 保底：确保标准模块都存在（即使没有题目也显示在左侧列表）
        var expectedModules = new[]
        {
            new { Type = "word", Name = "Word操作", Order = 1, Description = "Word 文档编辑与排版" },
            new { Type = "excel", Name = "Excel操作", Order = 2, Description = "Excel 数据处理与分析" },
            new { Type = "ppt", Name = "PowerPoint操作", Order = 3, Description = "PowerPoint 演示文稿制作" }
        };

        foreach (var em in expectedModules)
        {
            bool existsByType = studentExam.Modules.Any(m => string.Equals(m.Type, em.Type, StringComparison.OrdinalIgnoreCase));
            if (!existsByType)
            {
                StudentModuleDto placeholder = new()
                {
                    Id = 0,
                    Name = em.Name,
                    Type = em.Type,
                    Description = em.Description,
                    Score = 0,
                    Order = em.Order
                };
                studentExam.Modules.Add(placeholder);
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 添加占位模块 {placeholder.Name}（无题目）");
            }
        }

        // 排序：按Order升序，再按Name
        studentExam.Modules = new System.Collections.ObjectModel.ObservableCollection<StudentModuleDto>(
            studentExam.Modules.OrderBy(m => m.Order).ThenBy(m => m.Name)
        );



        System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 转换完成 - 模块数: {studentExam.Modules.Count}, 总题目数: {studentExam.Modules.Sum(m => m.Questions.Count)}");
        return studentExam;
    }

    /// <summary>
    /// 使用BenchSuite评分提交考试
    /// </summary>
    private async Task SubmitExamWithBenchSuiteAsync(int examId, ExamType examType, bool isAutoSubmit)
    {
        // 获取实际用时（从考试工具栏）
        int? actualDurationSeconds = GetActualDurationFromToolbar();

        try
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 开始提交考试，ID: {examId}, 类型: {examType}, 自动提交: {isAutoSubmit}");

            bool submitResult = false;

            // 优先使用EnhancedExamToolbarService进行BenchSuite集成提交
            if (_enhancedExamToolbarService != null)
            {
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 使用EnhancedExamToolbarService进行BenchSuite集成提交，实际用时: {actualDurationSeconds}秒");

                switch (examType)
                {
                    case ExamType.MockExam:
                        submitResult = await _enhancedExamToolbarService.SubmitMockExamAsync(examId, actualDurationSeconds);
                        break;
                    case ExamType.FormalExam:
                        submitResult = await _enhancedExamToolbarService.SubmitFormalExamAsync(examId);
                        break;
                    case ExamType.ComprehensiveTraining:
                        submitResult = await _enhancedExamToolbarService.SubmitComprehensiveTrainingAsync(examId);
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"MockExamViewModel: EnhancedExamToolbarService不支持的考试类型: {examType}");
                        break;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: EnhancedExamToolbarService不可用，使用原有提交逻辑");

                // 回退到原有的提交方法
                switch (examType)
                {
                    case ExamType.MockExam:
                        // 使用现有的模拟考试服务提交
                        MockExamSubmissionResponseDto? submitResponse = await _mockExamService.SubmitMockExamAsync(examId, actualDurationSeconds);
                        if (submitResponse != null)
                        {
                            submitResult = submitResponse.Success;
                            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 模拟考试提交响应 - 成功: {submitResponse.Success}, 时间状态: {submitResponse.TimeStatusDescription}, 客户端用时: {actualDurationSeconds}秒");

                            if (!submitResponse.Success)
                            {
                                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 模拟考试提交失败 - 状态: {submitResponse.Status}, 消息: {submitResponse.Message}");

                                // 如果是权限问题，记录更多信息
                                if (submitResponse.Status == "Unauthorized")
                                {
                                    System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 权限验证失败，考试ID: {examId}");

                                    // 获取当前用户信息用于调试
                                    var currentUser = _authenticationService.CurrentUser;
                                    if (currentUser != null)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 当前用户信息 - ID: {currentUser.Id}, 用户名: {currentUser.Username}, 权限: {currentUser.HasFullAccess}");
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("MockExamViewModel: 当前用户信息为空");
                                    }
                                }
                            }

                            if (submitResponse.ActualDurationMinutes.HasValue)
                            {
                                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 考试实际用时: {submitResponse.ActualDurationMinutes}分钟");
                            }
                        }
                        else
                        {
                            submitResult = false;
                            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 模拟考试提交响应为空");
                        }
                        break;

                    case ExamType.FormalExam:
                        // 正式考试提交（需要实现）
                        System.Diagnostics.Debug.WriteLine("MockExamViewModel: 正式考试提交功能待实现");
                        submitResult = true; // 临时返回成功
                        break;

                    case ExamType.ComprehensiveTraining:
                        // 综合实训提交（需要实现）
                        System.Diagnostics.Debug.WriteLine("MockExamViewModel: 综合实训提交功能待实现");
                        submitResult = true; // 临时返回成功
                        break;

                    default:
                        System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 不支持的考试类型: {examType}");
                        break;
                }
            }

            if (submitResult)
            {
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 考试提交成功，ID: {examId}");

                // 显示考试结果窗口，窗口关闭后会自动显示主窗口
                await ShowExamResultAsync(examId, examType, true, actualDurationSeconds);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 考试提交失败，ID: {examId}");

                // 显示失败结果窗口，窗口关闭后会自动显示主窗口
                await ShowExamResultAsync(examId, examType, false, actualDurationSeconds);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 提交考试异常: {ex.Message}");
            // 如果提交过程异常，显示错误结果窗口
            await ShowExamResultAsync(examId, examType, false, actualDurationSeconds);
        }
    }

    /// <summary>
    /// 关闭考试并显示主窗口
    /// </summary>
    private void CloseExamAndShowMainWindow()
    {
        try
        {
            // 显示主窗口
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.Activate();
                System.Diagnostics.Debug.WriteLine("MockExamViewModel: 主窗口已显示");
            }

            // 刷新数据
            UpdateUserPermissions();

            // 通知首页刷新统计数据
            _ = Task.Run(async () =>
            {
                try
                {
                    await NotifyOverviewPageRefreshAsync();
                    await NotifyLeaderboardPageRefreshAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 通知页面刷新异常: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 关闭考试并显示主窗口异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 从考试工具栏获取实际用时
    /// </summary>
    private int? GetActualDurationFromToolbar()
    {
        try
        {
            // 查找当前活动的ExamToolbarWindow
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                foreach (Avalonia.Controls.Window window in desktop.Windows)
                {
                    if (window is ExamToolbarWindow toolbarWindow &&
                        toolbarWindow.DataContext is ExamToolbarViewModel viewModel &&
                        viewModel.CurrentExamType == ExamType.MockExam)
                    {
                        int actualDurationSeconds = viewModel.GetActualDurationSeconds();
                        System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 从工具栏获取实际用时: {actualDurationSeconds}秒");
                        return actualDurationSeconds;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 无法找到考试工具栏窗口，无法获取实际用时");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 获取实际用时异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 通知概览页面刷新数据
    /// </summary>
    private async Task NotifyOverviewPageRefreshAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 开始通知概览页面刷新数据");

            // 延迟一下确保数据库操作完成
            await Task.Delay(1000);

            // 发送刷新概览页面的消息
            // 这里可以使用消息总线或事件聚合器，暂时使用简单的方式
            OverviewPageRefreshRequested?.Invoke(this, EventArgs.Empty);

            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 概览页面刷新通知已发送");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 通知概览页面刷新异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 通知排行榜页面刷新数据
    /// </summary>
    private async Task NotifyLeaderboardPageRefreshAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 开始通知排行榜页面刷新数据");

            // 延迟一下确保数据库操作完成
            await Task.Delay(500);

            // 发送刷新排行榜页面的消息
            LeaderboardPageRefreshRequested?.Invoke(this, EventArgs.Empty);

            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 排行榜页面刷新通知已发送");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 通知排行榜页面刷新异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示考试结果窗口
    /// </summary>
    private async Task ShowExamResultAsync(int examId, ExamType examType, bool isSuccessful, int? actualDurationSeconds)
    {
        try
        {
            // 获取考试名称
            string examName = "模拟考试";

            // 转换用时为分钟
            int? durationMinutes = actualDurationSeconds.HasValue ? (actualDurationSeconds.Value / 60) : null;

            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 准备显示全屏考试结果窗口 - {examName}");

            // 使用新的全屏考试结果窗口
            await Views.Dialogs.FullScreenExamResultWindow.ShowFullScreenExamResultAsync(
                examName,
                examType,
                isSuccessful,
                null, // startTime
                null, // endTime
                durationMinutes,
                null, // score - 模拟考试暂时不显示分数
                null, // totalScore
                isSuccessful ? "" : "考试提交失败",
                isSuccessful ? "模拟考试提交成功" : "请检查网络连接或联系管理员",
                true, // showContinue
                false // showClose - 只显示确认按钮
            );

            System.Diagnostics.Debug.WriteLine("MockExamViewModel: 全屏考试结果窗口已显示并关闭");

            // 窗口关闭后显示主窗口
            CloseExamAndShowMainWindow();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamViewModel: 显示全屏考试结果窗口失败: {ex.Message}");
            // 如果显示结果窗口失败，也要显示主窗口
            CloseExamAndShowMainWindow();
        }
    }
}