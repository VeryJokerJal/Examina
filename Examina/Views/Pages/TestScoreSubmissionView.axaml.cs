using Avalonia.Controls;
using Avalonia.Interactivity;
using Examina.ViewModels.Pages;

namespace Examina.Views.Pages;

/// <summary>
/// 成绩提交功能测试页面
/// </summary>
public partial class TestScoreSubmissionView : UserControl
{
    public TestScoreSubmissionView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 提交专项练习成绩
    /// </summary>
    private async void SubmitSpecialPracticeScore_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OverviewViewModel viewModel)
            return;

        try
        {
            // 获取输入值
            if (!int.TryParse(PracticeIdTextBox.Text, out int practiceId))
            {
                // 显示错误消息
                return;
            }

            decimal? score = decimal.TryParse(PracticeScoreTextBox.Text, out decimal s) ? s : null;
            decimal? maxScore = decimal.TryParse(PracticeMaxScoreTextBox.Text, out decimal ms) ? ms : null;
            int? duration = int.TryParse(PracticeDurationTextBox.Text, out int d) ? d : null;
            string? notes = string.IsNullOrWhiteSpace(PracticeNotesTextBox.Text) ? null : PracticeNotesTextBox.Text;

            // 提交成绩
            bool success = await viewModel.SubmitSpecialPracticeScoreAsync(practiceId, score, maxScore, duration, notes);
            
            System.Diagnostics.Debug.WriteLine($"专项练习成绩提交结果: {success}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"提交专项练习成绩异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 提交综合训练成绩
    /// </summary>
    private async void SubmitComprehensiveTrainingScore_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OverviewViewModel viewModel)
            return;

        try
        {
            // 获取输入值
            if (!int.TryParse(TrainingIdTextBox.Text, out int trainingId))
            {
                // 显示错误消息
                return;
            }

            decimal? score = decimal.TryParse(TrainingScoreTextBox.Text, out decimal s) ? s : null;
            decimal? maxScore = decimal.TryParse(TrainingMaxScoreTextBox.Text, out decimal ms) ? ms : null;
            int? duration = int.TryParse(TrainingDurationTextBox.Text, out int d) ? d : null;
            string? notes = string.IsNullOrWhiteSpace(TrainingNotesTextBox.Text) ? null : TrainingNotesTextBox.Text;

            // 提交成绩
            bool success = await viewModel.SubmitComprehensiveTrainingScoreAsync(trainingId, score, maxScore, duration, notes);
            
            System.Diagnostics.Debug.WriteLine($"综合训练成绩提交结果: {success}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"提交综合训练成绩异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 刷新所有进度
    /// </summary>
    private async void RefreshAllProgress_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OverviewViewModel viewModel)
            return;

        try
        {
            await viewModel.RefreshAllProgressAsync();
            System.Diagnostics.Debug.WriteLine("所有进度刷新完成");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"刷新所有进度异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 刷新专项练习进度
    /// </summary>
    private async void RefreshSpecialPracticeProgress_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OverviewViewModel viewModel)
            return;

        try
        {
            await viewModel.RefreshSpecialPracticeProgressAsync();
            System.Diagnostics.Debug.WriteLine("专项练习进度刷新完成");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"刷新专项练习进度异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 刷新综合训练进度
    /// </summary>
    private async void RefreshComprehensiveTrainingProgress_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OverviewViewModel viewModel)
            return;

        try
        {
            await viewModel.RefreshComprehensiveTrainingProgressAsync();
            System.Diagnostics.Debug.WriteLine("综合训练进度刷新完成");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"刷新综合训练进度异常: {ex.Message}");
        }
    }
}
