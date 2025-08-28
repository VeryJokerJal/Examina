using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Examina.Models;
using Examina.Services;
using Examina.ViewModels;
using Microsoft.Extensions.Logging;

namespace Examina.Views;

/// <summary>
/// 考试专用工具栏窗口组件
/// </summary>
public partial class ExamToolbarWindow : Window, IDisposable
{
    private readonly ScreenReservationService _screenReservationService;
    private readonly ILogger<ExamToolbarWindow> _logger;
    private ExamToolbarViewModel? _viewModel;
    private bool _disposed;

    // 自动收起/展开相关字段
    private bool _isCollapsed = false;
    private double _originalHeight = 60;
    private double _collapsedHeight = 8;
    private System.Timers.Timer? _collapseTimer;
    private System.Timers.Timer? _expandTimer;
    private const double MouseSensorRange = 50; // 鼠标感应范围（像素）
    private const int CollapseDelay = 3000; // 收起延迟（毫秒）
    private const int ExpandDelay = 200; // 展开延迟（毫秒）

    /// <summary>
    /// 考试自动提交事件
    /// </summary>
    public event EventHandler? ExamAutoSubmitted;

    /// <summary>
    /// 考试手动提交事件
    /// </summary>
    public event EventHandler? ExamManualSubmitted;

    /// <summary>
    /// 查看题目请求事件
    /// </summary>
    public event EventHandler? ViewQuestionsRequested;

    /// <summary>
    /// 查看答案解析请求事件
    /// </summary>
    public event EventHandler? ViewAnswerAnalysisRequested;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamToolbarWindow() : this(new ScreenReservationService(), null)
    {
    }

    /// <summary>
    /// 带依赖注入的构造函数
    /// </summary>
    public ExamToolbarWindow(ScreenReservationService screenReservationService, ILogger<ExamToolbarWindow>? logger)
    {
        _screenReservationService = screenReservationService ?? throw new ArgumentNullException(nameof(screenReservationService));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ExamToolbarWindow>.Instance;

        InitializeComponent();
        InitializeWindow();
        SetupEventHandlers();
    }

    /// <summary>
    /// 带ViewModel的构造函数
    /// </summary>
    public ExamToolbarWindow(ExamToolbarViewModel viewModel, ScreenReservationService screenReservationService, ILogger<ExamToolbarWindow>? logger = null)
        : this(screenReservationService, logger)
    {
        SetViewModel(viewModel);
    }

    /// <summary>
    /// 设置ViewModel
    /// </summary>
    public void SetViewModel(ExamToolbarViewModel viewModel)
    {
        // 清理旧的ViewModel事件订阅
        if (_viewModel != null)
        {
            _viewModel.ExamAutoSubmitted -= OnExamAutoSubmitted;
            _viewModel.ExamManualSubmitted -= OnExamManualSubmitted;
            _viewModel.ViewQuestionsRequested -= OnViewQuestionsRequested;
            _viewModel.ViewAnswerAnalysisRequested -= OnViewAnswerAnalysisRequested;
            _viewModel.WindowCloseRequested -= OnWindowCloseRequested;
        }

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        // 订阅新的ViewModel事件
        _viewModel.ExamAutoSubmitted += OnExamAutoSubmitted;
        _viewModel.ExamManualSubmitted += OnExamManualSubmitted;
        _viewModel.ViewQuestionsRequested += OnViewQuestionsRequested;
        _viewModel.ViewAnswerAnalysisRequested += OnViewAnswerAnalysisRequested;
        _viewModel.WindowCloseRequested += OnWindowCloseRequested;

        _logger.LogInformation("ExamToolbarWindow ViewModel已设置 - 剩余时间: {RemainingTime}秒, 格式化时间: {FormattedTime}",
            _viewModel.RemainingTimeSeconds, _viewModel.FormattedRemainingTime);
    }

    /// <summary>
    /// 初始化窗口属性
    /// </summary>
    private void InitializeWindow()
    {
        // 设置窗口基本属性
        SystemDecorations = SystemDecorations.None;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Topmost = true;
        Background = new SolidColorBrush(new Color(128, 60, 60, 60)); // 半透明黑色
        ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaTitleBarHeightHint = -1;
        TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
        CanResize = false;

        _logger.LogInformation("ExamToolbarWindow窗口属性初始化完成");
    }

    /// <summary>
    /// 设置事件处理程序
    /// </summary>
    private void SetupEventHandlers()
    {
        // 防止窗口最小化
        PropertyChanged += ExamToolbarWindow_PropertyChanged;

        // 窗口打开时的处理
        Opened += ExamToolbarWindow_Opened;

        // 窗口关闭时的处理
        Closing += ExamToolbarWindow_Closing;

        // 鼠标事件处理
        PointerEntered += ExamToolbarWindow_PointerEntered;
        PointerExited += ExamToolbarWindow_PointerExited;

        // 初始化定时器
        InitializeTimers();

        _logger.LogInformation("ExamToolbarWindow事件处理程序设置完成");
    }

    /// <summary>
    /// 窗口属性变化事件处理
    /// </summary>
    private void ExamToolbarWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // 防止窗口最小化
        if (e.Property.Name == nameof(WindowState) && WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
            _logger.LogWarning("检测到考试工具栏最小化尝试，已恢复正常状态");
        }
    }

    /// <summary>
    /// 窗口打开事件处理
    /// </summary>
    private void ExamToolbarWindow_Opened(object? sender, EventArgs e)
    {
        // 设置窗口位置到屏幕顶部
        Position = new PixelPoint(0, 0);

        // 设置窗口区域和屏幕预留
        SetupWindowArea();

        _logger.LogInformation("ExamToolbarWindow已打开并设置到屏幕顶部");
    }

    /// <summary>
    /// 窗口关闭事件处理
    /// </summary>
    private void ExamToolbarWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        // 如果考试正在进行中，阻止关闭并触发提交
        if (_viewModel?.CurrentExamStatus is ExamStatus.InProgress or ExamStatus.AboutToEnd)
        {
            e.Cancel = true;
            _logger.LogWarning("检测到考试进行中的窗口关闭尝试，触发自动提交");

            // 触发自动提交
            OnExamAutoSubmitted(this, EventArgs.Empty);
        }

        // 释放资源
        Dispose();
    }

    /// <summary>
    /// 设置窗口区域和屏幕预留
    /// </summary>
    private void SetupWindowArea()
    {
        PixelSize? screenSize = Screens.Primary?.Bounds.Size;

        if (screenSize.HasValue)
        {
            double screenWidth = screenSize.Value.Width;
            double toolbarHeight = 60; // 考试工具栏高度

            Width = screenWidth;
            Height = toolbarHeight;

            // 预留屏幕区域
            bool reservationResult = _screenReservationService.ReserveAreaOnSide((int)toolbarHeight, DockPosition.Top);

            if (!reservationResult)
            {
                _logger.LogWarning("ExamToolbarWindow: 屏幕区域预留失败");
            }
            else
            {
                _logger.LogInformation("ExamToolbarWindow: 屏幕区域预留成功，高度: {Height}px", toolbarHeight);
            }
        }
    }

    /// <summary>
    /// 考试自动提交事件处理
    /// </summary>
    private void OnExamAutoSubmitted(object? sender, EventArgs e)
    {
        _logger.LogWarning("考试时间到，执行自动提交");

        try
        {
            // 触发外部事件（自动提交逻辑已在ViewModel中处理）
            ExamAutoSubmitted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理考试自动提交时发生错误");
        }
    }

    /// <summary>
    /// 考试手动提交事件处理
    /// </summary>
    private void OnExamManualSubmitted(object? sender, EventArgs e)
    {
        _logger.LogInformation("用户手动提交考试");

        try
        {
            // 触发外部事件（提交逻辑已在ViewModel中处理）
            ExamManualSubmitted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理考试手动提交时发生错误");
        }
    }

    /// <summary>
    /// 查看题目请求事件处理
    /// </summary>
    private void OnViewQuestionsRequested(object? sender, EventArgs e)
    {
        _logger.LogInformation("用户请求查看题目");
        ViewQuestionsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 查看答案解析请求事件处理
    /// </summary>
    private void OnViewAnswerAnalysisRequested(object? sender, EventArgs e)
    {
        _logger.LogInformation("用户请求查看答案解析");
        ViewAnswerAnalysisRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 窗口关闭请求事件处理
    /// </summary>
    private void OnWindowCloseRequested(object? sender, EventArgs e)
    {
        _logger.LogInformation("收到窗口关闭请求");

        try
        {
            // 关闭窗口
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关闭窗口时发生错误");
        }
    }



    /// <summary>
    /// 开始考试
    /// </summary>
    public void StartExam(ExamType examType, int examId, string examName, int totalQuestions, int durationSeconds)
    {
        if (_viewModel == null)
        {
            _logger.LogError("无法开始考试：ViewModel未设置");
            return;
        }

        _viewModel.SetExamInfo(examType, examId, examName, totalQuestions, durationSeconds);
        _viewModel.StartCountdown(durationSeconds);

        _logger.LogInformation("考试已开始 - 类型: {ExamType}, ID: {ExamId}, 名称: {ExamName}", examType, examId, examName);
    }

    /// <summary>
    /// 初始化定时器
    /// </summary>
    private void InitializeTimers()
    {
        // 收起定时器
        _collapseTimer = new System.Timers.Timer(CollapseDelay);
        _collapseTimer.Elapsed += (sender, e) =>
        {
            _collapseTimer?.Stop();
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => CollapseToolbar());
        };
        _collapseTimer.AutoReset = false;

        // 展开定时器
        _expandTimer = new System.Timers.Timer(ExpandDelay);
        _expandTimer.Elapsed += (sender, e) =>
        {
            _expandTimer?.Stop();
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ExpandToolbar());
        };
        _expandTimer.AutoReset = false;

        _logger.LogInformation("ExamToolbarWindow定时器初始化完成");
    }

    /// <summary>
    /// 鼠标进入事件处理
    /// </summary>
    private void ExamToolbarWindow_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        _collapseTimer?.Stop();

        if (_isCollapsed)
        {
            _expandTimer?.Start();
        }

        _logger.LogDebug("鼠标进入工具栏区域");
    }

    /// <summary>
    /// 鼠标离开事件处理
    /// </summary>
    private void ExamToolbarWindow_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        _expandTimer?.Stop();

        if (!_isCollapsed)
        {
            _collapseTimer?.Start();
        }

        _logger.LogDebug("鼠标离开工具栏区域");
    }

    /// <summary>
    /// 收起工具栏
    /// </summary>
    private void CollapseToolbar()
    {
        if (_isCollapsed) return;

        _isCollapsed = true;
        Height = _collapsedHeight;
        _logger.LogDebug("工具栏已收起");
    }

    /// <summary>
    /// 展开工具栏
    /// </summary>
    private void ExpandToolbar()
    {
        if (!_isCollapsed) return;

        _isCollapsed = false;
        Height = _originalHeight;
        _logger.LogDebug("工具栏已展开");
    }

    /// <summary>
    /// 停止考试
    /// </summary>
    public void StopExam()
    {
        _viewModel?.StopCountdown();
        _logger.LogInformation("考试已停止");
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // 释放定时器
            _collapseTimer?.Stop();
            _collapseTimer?.Dispose();
            _expandTimer?.Stop();
            _expandTimer?.Dispose();

            // 停止倒计时
            _viewModel?.StopCountdown();

            // 释放屏幕预留区域
            _screenReservationService.Dispose();

            // 清理ViewModel事件订阅
            if (_viewModel != null)
            {
                _viewModel.ExamAutoSubmitted -= OnExamAutoSubmitted;
                _viewModel.ExamManualSubmitted -= OnExamManualSubmitted;
                _viewModel.ViewQuestionsRequested -= OnViewQuestionsRequested;
                _viewModel.WindowCloseRequested -= OnWindowCloseRequested;
                _viewModel.Dispose();
            }

            _disposed = true;
            _logger.LogInformation("ExamToolbarWindow资源已释放");
        }
    }
}
