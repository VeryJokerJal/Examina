using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Examina.ViewModels.Pages;

namespace Examina.Views.Pages;

public partial class OverviewView : UserControl
{
    public OverviewView()
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
                viewModel.LastSubmissionMessage = "请输入有效的练习ID";
                viewModel.LastSubmissionSuccess = false;
                return;
            }

            decimal? score = decimal.TryParse(PracticeScoreTextBox.Text, out decimal s) ? s : null;
            decimal? maxScore = decimal.TryParse(PracticeMaxScoreTextBox.Text, out decimal ms) ? ms : null;
            int? duration = int.TryParse(PracticeDurationTextBox.Text, out int d) ? d : null;

            // 提交成绩
            bool success = await viewModel.SubmitSpecialPracticeScoreAsync(practiceId, score, maxScore, duration, "首页快速提交");

            System.Diagnostics.Debug.WriteLine($"专项练习成绩提交结果: {success}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"提交专项练习成绩异常: {ex.Message}");
            if (DataContext is OverviewViewModel vm)
            {
                vm.LastSubmissionMessage = $"提交时发生错误: {ex.Message}";
                vm.LastSubmissionSuccess = false;
            }
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
                viewModel.LastSubmissionMessage = "请输入有效的训练ID";
                viewModel.LastSubmissionSuccess = false;
                return;
            }

            decimal? score = decimal.TryParse(TrainingScoreTextBox.Text, out decimal s) ? s : null;
            decimal? maxScore = decimal.TryParse(TrainingMaxScoreTextBox.Text, out decimal ms) ? ms : null;
            int? duration = int.TryParse(TrainingDurationTextBox.Text, out int d) ? d : null;

            // 提交成绩
            bool success = await viewModel.SubmitComprehensiveTrainingScoreAsync(trainingId, score, maxScore, duration, "首页快速提交");

            System.Diagnostics.Debug.WriteLine($"综合训练成绩提交结果: {success}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"提交综合训练成绩异常: {ex.Message}");
            if (DataContext is OverviewViewModel vm)
            {
                vm.LastSubmissionMessage = $"提交时发生错误: {ex.Message}";
                vm.LastSubmissionSuccess = false;
            }
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
    /// 刷新成绩记录
    /// </summary>
    private async void RefreshRecords_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OverviewViewModel viewModel)
            return;

        try
        {
            await viewModel.RefreshRecordsAsync();
            System.Diagnostics.Debug.WriteLine("成绩记录刷新完成");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"刷新成绩记录异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试API连接
    /// </summary>
    private async void TestApiConnection_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OverviewViewModel viewModel)
            return;

        try
        {
            await viewModel.TestApiConnectionAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API连接测试异常: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
        }
    }
}

/// <summary>
/// 统计类型到显示名称的转换器
/// </summary>
public class StatisticTypeToDisplayNameConverter : IValueConverter
{
    public static readonly StatisticTypeToDisplayNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is StatisticType statisticType
            ? statisticType switch
            {
                StatisticType.MockExam => "模拟考试",
                StatisticType.ComprehensiveTraining => "综合实训",
                StatisticType.SpecialPractice => "专项练习",
                StatisticType.OnlineExam => "上机统考",
                _ => "未知类型"
            }
            : "未知类型";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 统计类型到背景色的转换器
/// </summary>
public class StatisticTypeToBackgroundConverter : IValueConverter
{
    public static readonly StatisticTypeToBackgroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is StatisticType selectedType && parameter is string targetTypeString)
        {
            if (Enum.TryParse<StatisticType>(targetTypeString, out StatisticType parsedTargetType))
            {
                // 只有选中状态才显示背景色，未选中状态返回透明
                if (selectedType == parsedTargetType)
                {
                    // 获取选中状态的背景色资源键
                    string selectedKey = parsedTargetType switch
                    {
                        StatisticType.MockExam => "MockExamSelectedBrush",
                        StatisticType.ComprehensiveTraining => "ComprehensiveTrainingSelectedBrush",
                        StatisticType.SpecialPractice => "SpecialPracticeSelectedBrush",
                        StatisticType.OnlineExam => "OnlineExamSelectedBrush",
                        _ => ""
                    };

                    if (!string.IsNullOrEmpty(selectedKey) &&
                        Application.Current?.TryGetResource(selectedKey, null, out object? brush) == true &&
                        brush is IBrush colorBrush)
                    {
                        return colorBrush;
                    }
                }

                // 未选中状态：返回透明
                return Brushes.Transparent;
            }
        }

        // 备用方案：返回透明
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
