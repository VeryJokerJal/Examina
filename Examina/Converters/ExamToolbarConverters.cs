using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Examina.ViewModels;

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
            return isUrgent ? new SolidColorBrush(Color.Parse("#FFFF4444")) : new SolidColorBrush(Color.Parse("#FFFFFF"));
        }
        return new SolidColorBrush(Color.Parse("#FFFFFF"));
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
                ExamStatus.InProgress => "进行中",
                ExamStatus.AboutToEnd => "即将结束",
                ExamStatus.Ended => "已结束",
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
            return status switch
            {
                ExamStatus.Preparing => new SolidColorBrush(Color.Parse("#FFCCCCCC")),
                ExamStatus.InProgress => new SolidColorBrush(Color.Parse("#FF28A745")),
                ExamStatus.AboutToEnd => new SolidColorBrush(Color.Parse("#FFFFC107")),
                ExamStatus.Ended => new SolidColorBrush(Color.Parse("#FFFF4444")),
                ExamStatus.Submitted => new SolidColorBrush(Color.Parse("#FF6C757D")),
                _ => new SolidColorBrush(Color.Parse("#FFCCCCCC"))
            };
        }
        return new SolidColorBrush(Color.Parse("#FFCCCCCC"));
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
            return isConnected 
                ? new SolidColorBrush(Color.Parse("#FF28A745")) 
                : new SolidColorBrush(Color.Parse("#FFFF4444"));
        }
        return new SolidColorBrush(Color.Parse("#FFFF4444"));
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
