using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Examina.ViewModels.Dialogs;
using System;
using System.ComponentModel;

namespace Examina.Views.Dialogs;

/// <summary>
/// 考试结果显示窗口
/// </summary>
public partial class ExamResultWindow : Window
{
    private ExamResultViewModel? _viewModel;
    private bool _canClose = false;

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public ExamResultWindow()
    {
        InitializeComponent();
        _viewModel = new ExamResultViewModel();
        DataContext = _viewModel;

        SetupWindow();
        SetupCommandSubscriptions();
        System.Diagnostics.Debug.WriteLine("ExamResultWindow: 窗口已初始化");
    }

    /// <summary>
    /// 带ViewModel的构造函数
    /// </summary>
    /// <param name="viewModel">视图模型</param>
    public ExamResultWindow(ExamResultViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        SetupWindow();
        SetupCommandSubscriptions();
        System.Diagnostics.Debug.WriteLine("ExamResultWindow: 窗口已初始化（带ViewModel）");
    }

    /// <summary>
    /// 设置窗口行为
    /// </summary>
    private void SetupWindow()
    {
        // 禁用Alt+F4关闭窗口
        this.Closing += OnWindowClosing;

        // 禁用Escape键关闭窗口
        this.KeyDown += OnKeyDown;

        // 设置为模态对话框行为
        this.Topmost = false;
        this.ShowActivated = true;

        System.Diagnostics.Debug.WriteLine("ExamResultWindow: 窗口行为设置完成");
    }

    /// <summary>
    /// 处理窗口关闭事件
    /// </summary>
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // 只有通过确认按钮才能关闭窗口
        if (!_canClose)
        {
            e.Cancel = true;
            System.Diagnostics.Debug.WriteLine("ExamResultWindow: 阻止窗口关闭，必须通过确认按钮");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("ExamResultWindow: 允许窗口关闭");
        }
    }

    /// <summary>
    /// 处理键盘按键事件
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // 禁用Escape键关闭窗口
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine("ExamResultWindow: 阻止Escape键关闭窗口");
        }
    }

    /// <summary>
    /// 设置命令订阅
    /// </summary>
    private void SetupCommandSubscriptions()
    {
        if (_viewModel != null)
        {
            // 订阅确认命令
            _viewModel.ConfirmCommand.Subscribe(result =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"ExamResultWindow: 确认命令执行，结果: {result}");
                    Close(result);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ExamResultWindow: 确认命令异常: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// 确认按钮点击事件处理器
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("ExamResultWindow: 确认按钮被点击");

            // 允许窗口关闭
            _canClose = true;

            // 关闭窗口并返回true
            Close(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamResultWindow: 确认按钮点击异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 设置考试结果数据
    /// </summary>
    public void SetExamResult(string examName, Models.ExamType examType, bool isSuccessful, 
        DateTime? startTime = null, DateTime? endTime = null, int? durationMinutes = null,
        decimal? score = null, decimal? totalScore = null, string errorMessage = "", string notes = "")
    {
        try
        {
            _viewModel?.SetExamResult(examName, examType, isSuccessful, startTime, endTime, 
                durationMinutes, score, totalScore, errorMessage, notes);
            
            System.Diagnostics.Debug.WriteLine($"ExamResultWindow: 考试结果已设置 - {examName}, 成功: {isSuccessful}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamResultWindow: 设置考试结果异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示考试结果窗口（模态对话框）
    /// </summary>
    public static async Task<bool?> ShowExamResultAsync(Window? owner, string examName, Models.ExamType examType,
        bool isSuccessful, DateTime? startTime = null, DateTime? endTime = null, int? durationMinutes = null,
        decimal? score = null, decimal? totalScore = null, string errorMessage = "", string notes = "")
    {
        try
        {
            ExamResultViewModel viewModel = new();
            viewModel.SetExamResult(examName, examType, isSuccessful, startTime, endTime,
                durationMinutes, score, totalScore, errorMessage, notes);

            ExamResultWindow window = new(viewModel);

            System.Diagnostics.Debug.WriteLine($"ExamResultWindow: 准备显示考试结果模态对话框 - {examName}");

            // 必须作为模态对话框显示，确保用户必须与此窗口交互
            if (owner != null)
            {
                // 显示为模态对话框，阻止与父窗口的交互
                return await window.ShowDialog<bool?>(owner);
            }
            else
            {
                // 如果没有父窗口，仍然显示为模态窗口
                window.WindowState = WindowState.Normal;
                window.Activate();
                window.Show();
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamResultWindow: 显示考试结果窗口异常: {ex.Message}");
            return false;
        }
    }
}
