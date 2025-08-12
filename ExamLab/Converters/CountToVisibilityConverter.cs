using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ExamLab.Converters;

/// <summary>
/// 数量到Visibility的转换器
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
        {
            string? parameterString = parameter?.ToString();
            bool isZeroParameter = parameterString == "Zero";

            if (isZeroParameter)
            {
                // 当参数为"Zero"时，count为0时显示，否则隐藏
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                // 默认行为：count大于0时显示，否则隐藏
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
