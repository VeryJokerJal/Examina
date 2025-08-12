using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ExamLab.Converters;

/// <summary>
/// 字符串到Visibility的转换器
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string stringValue)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(stringValue);
            bool isInverse = parameter?.ToString() == "Inverse";

            return isInverse ? 
                (isEmpty ? Visibility.Visible : Visibility.Collapsed) : 
                (isEmpty ? Visibility.Collapsed : Visibility.Visible);
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
