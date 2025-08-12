using System;
using System.Linq;
using Microsoft.UI.Xaml.Data;
using ExamLab.Models;

namespace ExamLab.Converters;

/// <summary>
/// 字符串格式转换器，用于替代WinUI3中不支持的StringFormat
/// </summary>
public class StringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string format && !string.IsNullOrEmpty(format))
        {
            try
            {
                return string.Format(format, value);
            }
            catch
            {
                return value?.ToString() ?? "";
            }
        }

        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 分值格式转换器
/// </summary>
public class ScoreFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int score)
        {
            return $"分值: {score}";
        }
        return value is double doubleScore ? $"分值: {doubleScore}" : "分值: 0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 顺序格式转换器
/// </summary>
public class OrderFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is int order ? $"顺序: {order}" : "顺序: 0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 题目数量格式转换器
/// </summary>
public class QuestionCountFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is int count ? $"题目: {count}" : "题目: 0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 操作点数量格式转换器
/// </summary>
public class OperationPointCountFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is int count ? $"操作点: {count}" : "操作点: 0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 参数数量格式转换器
/// </summary>
public class ParameterCountFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is int count ? $"参数: {count}" : "参数: 0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 参数数量详细格式转换器（用于详细显示）
/// </summary>
public class ParameterCountDetailFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is int count ? $"参数数量: {count}" : "参数数量: 0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 分值显示转换器（用于预览）
/// </summary>
public class ScoreDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int score)
        {
            return $"分值：{score}分";
        }
        return value is double doubleScore ? $"分值：{doubleScore}分" : "分值：0分";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 模块总分转换器
/// </summary>
public class ModuleTotalScoreConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ExamModule module)
        {
            double totalScore = module.Questions.Sum(q => q.Score);
            return totalScore;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 模块操作点数量转换器
/// </summary>
public class ModuleOperationPointCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ExamModule module)
        {
            int operationPointCount = module.Questions.Sum(q => q.OperationPoints.Count);
            return operationPointCount;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 模块题目数量转换器
/// </summary>
public class ModuleQuestionCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ExamModule module)
        {
            return module.Questions.Count;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
