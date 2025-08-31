using Avalonia.Controls;
using Avalonia.Interactivity;
using Examina.ViewModels;
using System.Threading.Tasks;

namespace Examina.Views;

/// <summary>
/// 训练结果窗口
/// </summary>
public partial class TrainingResultWindow : Window
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public TrainingResultWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 查看详情按钮点击事件
    /// </summary>
    private void ViewDetailsButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is TrainingResultViewModel viewModel)
            {
                // 显示详细信息对话框
                string detailsMessage = BuildDetailsMessage(viewModel);

                // 在控制台输出详细信息（调试用）
                System.Diagnostics.Debug.WriteLine("=== 训练结果详情 ===");
                System.Diagnostics.Debug.WriteLine(detailsMessage);
                System.Diagnostics.Debug.WriteLine("==================");

                // TODO: 这里可以实现更复杂的详情窗口
                // 目前先在调试输出中显示详细信息
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示训练结果详情失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 构建详细信息消息
    /// </summary>
    private static string BuildDetailsMessage(TrainingResultViewModel viewModel)
    {
        StringBuilder sb = new();

        sb.AppendLine($"训练名称: {viewModel.TrainingName}");
        sb.AppendLine($"完成时间: {viewModel.CompletionTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"训练耗时: {viewModel.Duration:hh\\:mm\\:ss}");
        sb.AppendLine($"总分: {viewModel.TotalScore:F1}");
        sb.AppendLine($"得分: {viewModel.AchievedScore:F1}");
        sb.AppendLine($"得分率: {viewModel.ScoreRate:F1}%");
        sb.AppendLine($"成绩等级: {viewModel.Grade}");
        sb.AppendLine();

        sb.AppendLine("模块详情:");
        foreach (ModuleResultItem module in viewModel.ModuleResults)
        {
            sb.AppendLine($"  {module.ModuleName}: {module.AchievedScore:F1}/{module.TotalScore:F1} ({module.ScoreRate:F1}%)");
            if (!string.IsNullOrEmpty(module.ErrorMessage))
            {
                sb.AppendLine($"    错误: {module.ErrorMessage}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("题目详情:");
        foreach (QuestionResultItem question in viewModel.QuestionResults)
        {
            sb.AppendLine($"  {question.StatusIcon} {question.QuestionTitle}: {question.AchievedScore:F1}/{question.TotalScore:F1}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// 等待窗口关闭
    /// </summary>
    public Task WaitForCloseAsync()
    {
        TaskCompletionSource<bool> tcs = new();

        Closed += (sender, e) => tcs.SetResult(true);

        return tcs.Task;
    }
}
