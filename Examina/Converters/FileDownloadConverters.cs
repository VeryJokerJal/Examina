using System.Globalization;
using Avalonia.Data.Converters;
using Examina.Models.FileDownload;

namespace Examina.Converters;

/// <summary>
/// 枚举到显示名称转换器
/// </summary>
public class EnumToDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FileDownloadTaskType taskType)
        {
            return taskType switch
            {
                FileDownloadTaskType.MockExam => "模拟考试",
                FileDownloadTaskType.OnlineExam => "上机统考",
                FileDownloadTaskType.ComprehensiveTraining => "综合实训",
                FileDownloadTaskType.SpecializedTraining => "专项训练",
                _ => value.ToString()
            };
        }

        if (value is FileDownloadStatus status)
        {
            return status switch
            {
                FileDownloadStatus.Pending => "等待中",
                FileDownloadStatus.Downloading => "下载中",
                FileDownloadStatus.Downloaded => "已下载",
                FileDownloadStatus.Extracting => "解压中",
                FileDownloadStatus.Completed => "已完成",
                FileDownloadStatus.Failed => "失败",
                FileDownloadStatus.Cancelled => "已取消",
                _ => value.ToString()
            };
        }

        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 文件状态到CSS类转换器
/// </summary>
public class FileStatusToClassConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FileDownloadStatus status)
        {
            return status switch
            {
                FileDownloadStatus.Pending => "status-pending",
                FileDownloadStatus.Downloading => "status-downloading",
                FileDownloadStatus.Downloaded => "status-completed",
                FileDownloadStatus.Extracting => "status-downloading",
                FileDownloadStatus.Completed => "status-completed",
                FileDownloadStatus.Failed => "status-failed",
                FileDownloadStatus.Cancelled => "status-failed",
                _ => "status-pending"
            };
        }

        return "status-pending";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 大于零转换器
/// </summary>
public class GreaterThanZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > 0;
        }

        if (value is double doubleValue)
        {
            return doubleValue > 0;
        }

        if (value is long longValue)
        {
            return longValue > 0;
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值反转转换器
/// </summary>
public class BooleanInverseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return false;
    }
}

/// <summary>
/// 文件大小格式化转换器
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return FormatFileSize(bytes);
        }

        if (value is int intBytes)
        {
            return FormatFileSize(intBytes);
        }

        return "0 B";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// 进度百分比转换器
/// </summary>
public class ProgressPercentageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            return $"{progress:F1}%";
        }

        return "0.0%";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
