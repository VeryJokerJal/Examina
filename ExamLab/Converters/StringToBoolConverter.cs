using System;
using Microsoft.UI.Xaml.Data;

namespace ExamLab.Converters;

/// <summary>
/// 字符串到布尔值的转换器
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string stringValue)
        {
            return stringValue.ToLower() switch
            {
                "true" or "1" or "是" or "yes" => true,
                "false" or "0" or "否" or "no" => false,
                _ => false
            };
        }

        if (value is bool boolValue)
        {
            return boolValue;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue.ToString().ToLower();
        }

        return "false";
    }
}
