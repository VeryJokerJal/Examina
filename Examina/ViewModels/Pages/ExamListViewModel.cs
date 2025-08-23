using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Examina.Models;
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
    private readonly IAuthenticationService _authenticationService;
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


    public ExamListViewModel(IStudentExamService studentExamService, IAuthenticationService authenticationService)
    {
        _studentExamService = studentExamService;
        _authenticationService = authenticationService;

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
                ExamToolbarViewModel toolbarViewModel = new(_authenticationService, null);

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
                examToolbar.ExamAutoSubmitted += (sender, e) => OnExamAutoSubmitted(exam);
                examToolbar.ExamManualSubmitted += (sender, e) => OnExamManualSubmitted(exam);
                examToolbar.ViewQuestionsRequested += (sender, e) => OnViewQuestionsRequested(exam);

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
    private void OnExamAutoSubmitted(StudentExamDto exam)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 正式考试自动提交，考试ID: {exam.Id}");
            // TODO: 实现正式考试自动提交逻辑
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 处理考试自动提交异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理考试手动提交事件
    /// </summary>
    private void OnExamManualSubmitted(StudentExamDto exam)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 正式考试手动提交，考试ID: {exam.Id}");
            // TODO: 实现正式考试手动提交逻辑
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 处理考试手动提交异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理查看题目请求事件
    /// </summary>
    private void OnViewQuestionsRequested(StudentExamDto exam)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 用户请求查看正式考试题目，考试ID: {exam.Id}");
            // TODO: 实现正式考试题目详情显示逻辑
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamListViewModel: 显示题目详情异常: {ex.Message}");
        }
    }
}
