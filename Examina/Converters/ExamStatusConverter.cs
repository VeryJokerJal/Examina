using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Examina.Models.Exam;

namespace Examina.Converters;

/// <summary>
/// 考试状态转换器
/// </summary>
public class ExamStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is StudentExamDto exam)
        {
            if (exam.StartTime.HasValue && exam.EndTime.HasValue)
            {
                DateTime now = DateTime.Now;
                if (now < exam.StartTime.Value)
                {
                    return "即将开始";
                }
                else if (now > exam.EndTime.Value)
                {
                    return "联考已结束";
                }
                else
                {
                    return "联考正在进行中";
                }
            }

            return exam.Status switch
            {
                "Published" => "联考正在进行中",
                "InProgress" => "联考正在进行中",
                "Completed" => "联考已结束",
                "Draft" => "即将开始",
                _ => "联考正在进行中"
            };
        }

        return "未知状态";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 考试按钮文本转换器
/// </summary>
public class ExamButtonTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is StudentExamDto exam)
        {
            if (exam.StartTime.HasValue && exam.EndTime.HasValue)
            {
                DateTime now = DateTime.Now;
                if (now < exam.StartTime.Value)
                {
                    return "即将开始";
                }
                else if (now > exam.EndTime.Value)
                {
                    return "查看结果";
                }
                else
                {
                    return "开始考试";
                }
            }

            return exam.Status switch
            {
                "Published" => "开始考试",
                "InProgress" => "开始考试",
                "Completed" => "查看结果",
                "Draft" => "即将开始",
                _ => "开始考试"
            };
        }

        return "开始考试";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 考试按钮启用状态转换器
/// </summary>
public class ExamButtonEnabledConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is StudentExamDto exam)
        {
            if (exam.StartTime.HasValue && exam.EndTime.HasValue)
            {
                DateTime now = DateTime.Now;
                // 只有在考试进行中时才能开始考试
                return now >= exam.StartTime.Value && now <= exam.EndTime.Value;
            }

            return exam.Status switch
            {
                "Published" => true,
                "InProgress" => true,
                "Completed" => false,
                "Draft" => false,
                _ => true
            };
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
