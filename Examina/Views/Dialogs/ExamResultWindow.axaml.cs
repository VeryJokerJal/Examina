using Avalonia.Controls;
using Avalonia.Interactivity;
using Examina.ViewModels.Dialogs;
using System;

namespace Examina.Views.Dialogs;

/// <summary>
/// 考试结果显示窗口
/// </summary>
public partial class ExamResultWindow : Window
{
    private ExamResultViewModel? _viewModel;

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public ExamResultWindow()
    {
        InitializeComponent();
        _viewModel = new ExamResultViewModel();
        DataContext = _viewModel;

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

        SetupCommandSubscriptions();
        System.Diagnostics.Debug.WriteLine("ExamResultWindow: 窗口已初始化（带ViewModel）");
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
    /// 显示考试结果窗口
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

            System.Diagnostics.Debug.WriteLine($"ExamResultWindow: 准备显示考试结果窗口 - {examName}");

            if (owner != null)
            {
                return await window.ShowDialog<bool?>(owner);
            }
            else
            {
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
