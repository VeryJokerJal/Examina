using System;
using Microsoft.UI.Xaml.Data;

namespace ExamLab.Converters;

/// <summary>
/// 字符串到整数的转换器
/// </summary>
public class StringToIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string stringValue && int.TryParse(stringValue, out int result))
        {
            return result;
        }

        if (value is int intValue)
        {
            return intValue;
        }

        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString() ?? "0";
    }
}
