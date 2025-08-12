using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ExamLab.Converters;

/// <summary>
/// 布尔值到Visibility的转换器
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            bool isInverse = parameter?.ToString() == "Inverse";

            return isInverse ? boolValue ? Visibility.Collapsed : Visibility.Visible : boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility visibility && visibility == Visibility.Visible;
    }
}
