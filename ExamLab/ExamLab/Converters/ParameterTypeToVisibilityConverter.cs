using System;
using ExamLab.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ExamLab.Converters;

/// <summary>
/// 参数类型到Visibility的转换器
/// </summary>
public class ParameterTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ParameterType parameterType && parameter is string targetTypeString)
        {
            bool matches = targetTypeString switch
            {
                "Text" => parameterType == ParameterType.Text,
                "Number" => parameterType == ParameterType.Number,
                "Boolean" => parameterType == ParameterType.Boolean,
                "Enum" => parameterType == ParameterType.Enum,
                "Color" => parameterType == ParameterType.Color,
                "File" => parameterType == ParameterType.File,
                "MultipleChoice" => parameterType == ParameterType.MultipleChoice,
                _ => false
            };

            return matches ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
