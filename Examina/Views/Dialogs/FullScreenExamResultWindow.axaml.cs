using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Examina.ViewModels.Dialogs;
using Examina.Models;
using BenchSuite.Models;
using System;
using System.Threading.Tasks;

namespace Examina.Views.Dialogs;

/// <summary>
/// 全屏考试结果显示窗口
/// </summary>
public partial class FullScreenExamResultWindow : Window
{
    private readonly FullScreenExamResultViewModel? _viewModel;
    private bool _canClose = false;
    private TaskCompletionSource<bool>? _closeTaskSource;

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public FullScreenExamResultWindow()
    {
        InitializeComponent();
        
        _viewModel = new FullScreenExamResultViewModel();
        DataContext = _viewModel;

        InitializeWindow();
        SetupEventHandlers();
        
        System.Diagnostics.Debug.WriteLine("FullScreenExamResultWindow: 窗口已初始化");
    }

    /// <summary>
    /// 带ViewModel的构造函数
    /// </summary>
    /// <param name="viewModel">视图模型</param>
    public FullScreenExamResultWindow(FullScreenExamResultViewModel viewModel)
    {
        InitializeComponent();
        
        _viewModel = viewModel;
        DataContext = viewModel;

        InitializeWindow();
        SetupEventHandlers();
        
        System.Diagnostics.Debug.WriteLine("FullScreenExamResultWindow: 窗口已初始化（带ViewModel）");
    }

    /// <summary>
    /// 初始化窗口属性
    /// </summary>
    private void InitializeWindow()
    {
        // 设置窗口基本属性
        WindowState = WindowState.FullScreen;
        SystemDecorations = SystemDecorations.None;
        CanResize = false;
        Topmost = true;
        ShowInTaskbar = false;

        // 设置亚克力效果
        TransparencyLevelHint = new[]
        {
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.Blur,
            WindowTransparencyLevel.Transparent
        };

        // 设置透明背景
        Background = Brushes.Transparent;

        // 扩展客户端区域
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaTitleBarHeightHint = -1;

        System.Diagnostics.Debug.WriteLine("FullScreenExamResultWindow: 窗口属性初始化完成");
    }

    /// <summary>
    /// 设置事件处理器
    /// </summary>
    private void SetupEventHandlers()
    {
        // 禁用Alt+F4关闭窗口
        Closing += OnWindowClosing;

        // 处理键盘事件
        KeyDown += OnKeyDown;

        // 设置命令订阅
        if (_viewModel != null)
        {
            _viewModel.CloseCommand.Subscribe(_ => CloseWindow());
            _viewModel.ContinueCommand.Subscribe(_ => CloseWindow());
        }

        System.Diagnostics.Debug.WriteLine("FullScreenExamResultWindow: 事件处理器设置完成");
    }

    /// <summary>
    /// 窗口关闭事件处理
    /// </summary>
    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_canClose)
        {
            e.Cancel = true;
            System.Diagnostics.Debug.WriteLine("FullScreenExamResultWindow: 阻止窗口关闭");
        }
    }

    /// <summary>
    /// 键盘事件处理
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // 禁用Escape键关闭窗口
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine("FullScreenExamResultWindow: 阻止Escape键关闭窗口");
        }
        // 允许Enter键关闭窗口
        else if (e.Key == Key.Enter)
        {
            CloseWindow();
            e.Handled = true;
        }
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    private void CloseWindow()
    {
        _canClose = true;
        _closeTaskSource?.SetResult(true);
        Close();
        System.Diagnostics.Debug.WriteLine("FullScreenExamResultWindow: 窗口已关闭");
    }

    /// <summary>
    /// 设置考试结果数据
    /// </summary>
    public void SetExamResult(string examName, ExamType examType, bool isSuccessful,
        DateTime? startTime = null, DateTime? endTime = null, int? durationMinutes = null,
        double? score = null, double? totalScore = null, string errorMessage = "", string notes = "",
        bool showContinue = true, bool showClose = true)
    {
        try
        {
            _viewModel?.SetFullScreenExamResult(examName, examType, isSuccessful, startTime, endTime,
                durationMinutes, score, totalScore, errorMessage, notes, showContinue, showClose);

            System.Diagnostics.Debug.WriteLine($"FullScreenExamResultWindow: 考试结果已设置 - {examName}, 成功: {isSuccessful}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FullScreenExamResultWindow: 设置考试结果异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 开始评分计算
    /// </summary>
    public void StartScoring()
    {
        _viewModel?.StartScoring();
    }

    /// <summary>
    /// 更新评分结果
    /// </summary>
    public void UpdateScore(double? score, double? totalScore = null, string notes = "")
    {
        _viewModel?.UpdateScore(score, totalScore, notes);
    }

    /// <summary>
    /// 评分失败
    /// </summary>
    public void ScoringFailed(string errorMessage)
    {
        _viewModel?.ScoringFailed(errorMessage);
    }

    /// <summary>
    /// 设置详细分数信息
    /// </summary>
    public void SetScoreDetail(ExamScoreDetail scoreDetail)
    {
        _viewModel?.SetScoreDetail(scoreDetail);
    }

    /// <summary>
    /// 从BenchSuite评分结果设置详细分数信息
    /// </summary>
    public void SetScoreDetailFromBenchSuite(Dictionary<ModuleType, ScoringResult> benchSuiteResults, double passThreshold = 60)
    {
        _viewModel?.SetScoreDetailFromBenchSuite(benchSuiteResults, passThreshold);
    }

    /// <summary>
    /// 显示全屏考试结果窗口（非阻塞）
    /// </summary>
    public static FullScreenExamResultWindow ShowFullScreenExamResult(string examName, ExamType examType,
        bool isSuccessful, DateTime? startTime = null, DateTime? endTime = null, int? durationMinutes = null,
        double? score = null, double? totalScore = null, string errorMessage = "", string notes = "",
        bool showContinue = true, bool showClose = true)
    {
        try
        {
            FullScreenExamResultViewModel viewModel = new();
            viewModel.SetFullScreenExamResult(examName, examType, isSuccessful, startTime, endTime,
                durationMinutes, score, totalScore, errorMessage, notes, showContinue, showClose);

            FullScreenExamResultWindow window = new(viewModel);
            window.Show();

            System.Diagnostics.Debug.WriteLine($"FullScreenExamResultWindow: 全屏考试结果窗口已显示 - {examName}");
            return window;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FullScreenExamResultWindow: 显示全屏考试结果窗口异常: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 等待窗口关闭
    /// </summary>
    public async Task<bool> WaitForCloseAsync()
    {
        if (_closeTaskSource == null)
        {
            _closeTaskSource = new TaskCompletionSource<bool>();
        }

        return await _closeTaskSource.Task;
    }

    /// <summary>
    /// 显示全屏考试结果窗口并等待关闭（阻塞）
    /// </summary>
    public static async Task<bool> ShowFullScreenExamResultAsync(string examName, ExamType examType,
        bool isSuccessful, DateTime? startTime = null, DateTime? endTime = null, int? durationMinutes = null,
        double? score = null, double? totalScore = null, string errorMessage = "", string notes = "",
        bool showContinue = true, bool showClose = true)
    {
        try
        {
            FullScreenExamResultWindow window = ShowFullScreenExamResult(examName, examType, isSuccessful,
                startTime, endTime, durationMinutes, score, totalScore, errorMessage, notes, showContinue, showClose);

            return await window.WaitForCloseAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FullScreenExamResultWindow: 显示全屏考试结果窗口并等待关闭异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 显示带详细分数信息的全屏考试结果窗口（非阻塞）
    /// </summary>
    public static FullScreenExamResultWindow ShowFullScreenExamResultWithDetail(string examName, ExamType examType,
        bool isSuccessful, ExamScoreDetail? scoreDetail = null, DateTime? startTime = null, DateTime? endTime = null,
        int? durationMinutes = null, string errorMessage = "", string notes = "",
        bool showContinue = true, bool showClose = true)
    {
        try
        {
            FullScreenExamResultViewModel viewModel = new();

            // 设置基本考试结果信息
            double? score = scoreDetail?.AchievedScore;
            double? totalScore = scoreDetail?.TotalScore;

            viewModel.SetFullScreenExamResult(examName, examType, isSuccessful, startTime, endTime,
                durationMinutes, score, totalScore, errorMessage, notes, showContinue, showClose);

            // 设置详细分数信息
            if (scoreDetail != null)
            {
                viewModel.SetScoreDetail(scoreDetail);
            }

            FullScreenExamResultWindow window = new(viewModel);
            window.Show();

            System.Diagnostics.Debug.WriteLine($"FullScreenExamResultWindow: 带详细分数的全屏考试结果窗口已显示 - {examName}");
            return window;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FullScreenExamResultWindow: 显示带详细分数的全屏考试结果窗口异常: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 显示带详细分数信息的全屏考试结果窗口并等待关闭（阻塞）
    /// </summary>
    public static async Task<bool> ShowFullScreenExamResultWithDetailAsync(string examName, ExamType examType,
        bool isSuccessful, ExamScoreDetail? scoreDetail = null, DateTime? startTime = null, DateTime? endTime = null,
        int? durationMinutes = null, string errorMessage = "", string notes = "",
        bool showContinue = true, bool showClose = true)
    {
        try
        {
            FullScreenExamResultWindow window = ShowFullScreenExamResultWithDetail(examName, examType, isSuccessful,
                scoreDetail, startTime, endTime, durationMinutes, errorMessage, notes, showContinue, showClose);

            return await window.WaitForCloseAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FullScreenExamResultWindow: 显示带详细分数的全屏考试结果窗口异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从BenchSuite评分结果显示全屏考试结果窗口（非阻塞）
    /// </summary>
    public static FullScreenExamResultWindow ShowFullScreenExamResultFromBenchSuite(string examName, ExamType examType,
        bool isSuccessful, Dictionary<ModuleType, ScoringResult>? benchSuiteResults = null, DateTime? startTime = null, DateTime? endTime = null,
        int? durationMinutes = null, string errorMessage = "", string notes = "",
        bool showContinue = true, bool showClose = true, double passThreshold = 60)
    {
        try
        {
            FullScreenExamResultViewModel viewModel = new();

            // 设置基本考试结果信息
            double? score = benchSuiteResults?.Values.Sum(r => r.AchievedScore);
            double? totalScore = benchSuiteResults?.Values.Sum(r => r.TotalScore);

            viewModel.SetFullScreenExamResult(examName, examType, isSuccessful, startTime, endTime,
                durationMinutes, score, totalScore, errorMessage, notes, showContinue, showClose);

            // 从BenchSuite结果设置详细分数信息
            if (benchSuiteResults != null)
            {
                viewModel.SetScoreDetailFromBenchSuite(benchSuiteResults, passThreshold);
            }

            FullScreenExamResultWindow window = new(viewModel);
            window.Show();

            System.Diagnostics.Debug.WriteLine($"FullScreenExamResultWindow: 从BenchSuite结果的全屏考试结果窗口已显示 - {examName}");
            return window;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FullScreenExamResultWindow: 显示从BenchSuite结果的全屏考试结果窗口异常: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 从BenchSuite评分结果显示全屏考试结果窗口并等待关闭（阻塞）
    /// </summary>
    public static async Task<bool> ShowFullScreenExamResultFromBenchSuiteAsync(string examName, ExamType examType,
        bool isSuccessful, Dictionary<ModuleType, ScoringResult>? benchSuiteResults = null, DateTime? startTime = null, DateTime? endTime = null,
        int? durationMinutes = null, string errorMessage = "", string notes = "",
        bool showContinue = true, bool showClose = true, double passThreshold = 60)
    {
        try
        {
            FullScreenExamResultWindow window = ShowFullScreenExamResultFromBenchSuite(examName, examType, isSuccessful,
                benchSuiteResults, startTime, endTime, durationMinutes, errorMessage, notes, showContinue, showClose, passThreshold);

            return await window.WaitForCloseAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FullScreenExamResultWindow: 显示从BenchSuite结果的全屏考试结果窗口异常: {ex.Message}");
            return false;
        }
    }
}
