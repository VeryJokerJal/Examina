using System;
using ExamLab.Models;
using Microsoft.UI.Xaml.Data;

namespace ExamLab.Converters;

/// <summary>
/// C#题目类型转换器
/// </summary>
public class CSharpQuestionTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is CSharpQuestionType questionType
            ? questionType switch
            {
                CSharpQuestionType.CodeCompletion => "代码补全",
                CSharpQuestionType.Debugging => "调试纠错",
                CSharpQuestionType.Implementation => "编写实现",
                _ => "代码补全"
            }
            : "代码补全";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is string stringValue
            ? stringValue switch
            {
                "代码补全" => CSharpQuestionType.CodeCompletion,
                "调试纠错" => CSharpQuestionType.Debugging,
                "编写实现" => CSharpQuestionType.Implementation,
                _ => CSharpQuestionType.CodeCompletion
            }
            : CSharpQuestionType.CodeCompletion;
    }
}
