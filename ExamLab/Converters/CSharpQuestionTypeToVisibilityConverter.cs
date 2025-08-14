using System;
using ExamLab.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ExamLab.Converters;

/// <summary>
/// C#题目类型到可见性转换器
/// </summary>
public class CSharpQuestionTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is CSharpQuestionType questionType && parameter is string target
            ? target switch
            {
                "CodeCompletion" => questionType == CSharpQuestionType.CodeCompletion ? Visibility.Visible : Visibility.Collapsed,
                "Debugging" => questionType == CSharpQuestionType.Debugging ? Visibility.Visible : Visibility.Collapsed,
                "Implementation" => questionType == CSharpQuestionType.Implementation ? Visibility.Visible : Visibility.Collapsed,
                _ => Visibility.Collapsed
            }
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
