using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Examina.ViewModels.Pages;

namespace Examina.Views.Pages;

public partial class MockExamScoreListView : UserControl
{
    public MockExamScoreListView()
    {
        InitializeComponent();
    }
}

/// <summary>
/// 及格状态到颜色的转换器
/// </summary>
public class PassStatusToColorConverter : IValueConverter
{
    public static readonly PassStatusToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPassed)
        {
            return isPassed ? Brushes.Green : Brushes.Orange;
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 及格状态到文本的转换器
/// </summary>
public class PassStatusToTextConverter : IValueConverter
{
    public static readonly PassStatusToTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPassed)
        {
            return isPassed ? "及格" : "不及格";
        }
        return "未知";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
