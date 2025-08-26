using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Examina.ViewModels;
using Microsoft.Extensions.Logging;

namespace Examina.Converters;

/// <summary>
/// 布尔值到时间颜色画刷转换器
/// </summary>
public class BoolToTimeBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUrgent)
        {
            var brush = isUrgent
                ? new SolidColorBrush(Color.Parse("#FFFF4444")) // 紧急状态：红色
                : new SolidColorBrush(Color.Parse("#FFFFFF"));   // 正常状态：白色（可见）

            // 调试输出
            System.Diagnostics.Debug.WriteLine($"BoolToTimeBrushConverter: IsUrgent={isUrgent}, Color={brush.Color}");
            return brush;
        }

        var defaultBrush = new SolidColorBrush(Color.Parse("#FFFFFF")); // 默认：白色
        System.Diagnostics.Debug.WriteLine($"BoolToTimeBrushConverter: 默认颜色={defaultBrush.Color}");
        return defaultBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 考试状态到字符串转换器
/// </summary>
public class ExamStatusToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ExamStatus status)
        {
            return status switch
            {
                ExamStatus.Preparing => "准备中",
                ExamStatus.InProgress => "考试进行中",
                ExamStatus.AboutToEnd => "即将结束",
                ExamStatus.Ended => "考试已结束",
                ExamStatus.Submitted => "已提交",
                _ => "未知状态"
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
/// 考试状态到颜色画刷转换器
/// </summary>
public class ExamStatusToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ExamStatus status)
        {
            // 使用更适合主题的颜色，保持语义化
            return status switch
            {
                ExamStatus.Preparing => new SolidColorBrush(Color.Parse("#FF9E9E9E")), // 灰色 - 准备中
                ExamStatus.InProgress => new SolidColorBrush(Color.Parse("#FF4CAF50")), // 绿色 - 进行中
                ExamStatus.AboutToEnd => new SolidColorBrush(Color.Parse("#FFFF9800")), // 橙色 - 即将结束
                ExamStatus.Ended => new SolidColorBrush(Color.Parse("#FFF44336")), // 红色 - 已结束
                ExamStatus.Submitted => new SolidColorBrush(Color.Parse("#FF607D8B")), // 蓝灰色 - 已提交
                _ => new SolidColorBrush(Color.Parse("#FF9E9E9E"))
            };
        }
        return new SolidColorBrush(Color.Parse("#FF9E9E9E"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值到网络图标转换器
/// </summary>
public class BoolToNetworkIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected 
                ? "M1,9L3,9L3,11L1,11L1,9M5,6L7,6L7,11L5,11L5,6M9,3L11,3L11,11L9,11L9,3M13,0L15,0L15,11L13,11L13,0M17,6L19,6L19,11L17,11L17,6M21,9L23,9L23,11L21,11L21,9Z"
                : "M12,2C13.11,2 14,2.9 14,4C14,5.11 13.11,6 12,6C10.89,6 10,5.11 10,4C10,2.9 10.89,2 12,2M21,9V7L15,1L9,7V9H21M15,11H9V13H15V11M15,15H9V17H15V15M15,19H9V21H15V19Z";
        }
        return "M12,2C13.11,2 14,2.9 14,4C14,5.11 13.11,6 12,6C10.89,6 10,5.11 10,4C10,2.9 10.89,2 12,2M21,9V7L15,1L9,7V9H21M15,11H9V13H15V11M15,15H9V17H15V15M15,19H9V21H15V19Z";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值到网络颜色画刷转换器
/// </summary>
public class BoolToNetworkBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            // 使用更适合主题的颜色
            return isConnected
                ? new SolidColorBrush(Color.Parse("#FF4CAF50")) // 绿色 - 已连接
                : new SolidColorBrush(Color.Parse("#FFF44336")); // 红色 - 未连接
        }
        return new SolidColorBrush(Color.Parse("#FFF44336"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值到提交按钮文本转换器
/// </summary>
public class BoolToSubmitTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSubmitting)
        {
            return isSubmitting ? "提交中..." : "提交考试";
        }
        return "提交考试";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 考试是否可以开始转换器
/// </summary>
public class ExamCanStartConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Examina.Models.Exam.StudentExamDto exam)
        {
            // 考试可以开始的条件：状态为Published或InProgress，且在时间范围内
            bool timeValid = exam.StartTime.HasValue && exam.EndTime.HasValue &&
                           DateTime.Now >= exam.StartTime.Value && DateTime.Now <= exam.EndTime.Value;

            bool statusValid = exam.Status == "Published" || exam.Status == "InProgress";

            // 简化逻辑：只要时间和状态有效就显示按钮，具体的权限检查由ViewModel处理
            bool canStart = statusValid && timeValid;

            System.Diagnostics.Debug.WriteLine($"[ExamCanStartConverter] {exam.Name}: Status={exam.Status}, TimeValid={timeValid}, CanStart={canStart}");
            System.Diagnostics.Debug.WriteLine($"  StartTime={exam.StartTime}, EndTime={exam.EndTime}, Now={DateTime.Now}");
            return canStart;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 考试已完成且无重考/重做选项转换器
/// </summary>
public class ExamCompletedNoOptionsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Examina.Models.Exam.StudentExamDto exam)
        {
            // 显示"联考已完成"的条件：考试已结束且不允许重考和重做
            bool showCompleted = exam.Status == "Completed" && !exam.AllowRetake && !exam.AllowPractice;

            System.Diagnostics.Debug.WriteLine($"[ExamCompletedNoOptionsConverter] {exam.Name}: Status={exam.Status}, AllowRetake={exam.AllowRetake}, AllowPractice={exam.AllowPractice}, ShowCompleted={showCompleted}");
            return showCompleted;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
