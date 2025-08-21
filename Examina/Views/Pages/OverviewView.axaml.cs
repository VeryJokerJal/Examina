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
