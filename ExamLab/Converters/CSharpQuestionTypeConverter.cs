using Microsoft.UI.Xaml.Data;
using ExamLab.Models;

namespace ExamLab.Converters;

/// <summary>
/// C#题目类型转换器
/// </summary>
public class CSharpQuestionTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is CSharpQuestionType questionType)
        {
            return questionType switch
            {
                CSharpQuestionType.CodeCompletion => "代码补全",
                CSharpQuestionType.Debugging => "调试纠错",
                CSharpQuestionType.Implementation => "编写实现",
                _ => "代码补全"
            };
        }
        return "代码补全";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string stringValue)
        {
            return stringValue switch
            {
                "代码补全" => CSharpQuestionType.CodeCompletion,
                "调试纠错" => CSharpQuestionType.Debugging,
                "编写实现" => CSharpQuestionType.Implementation,
                _ => CSharpQuestionType.CodeCompletion
            };
        }
        return CSharpQuestionType.CodeCompletion;
    }
}
