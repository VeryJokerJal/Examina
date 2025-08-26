using System.Globalization;
using Avalonia.Data.Converters;
using Examina.Models.Exam;

namespace Examina.Converters;

/// <summary>
/// 考试权限按钮文本转换器
/// </summary>
public class ExamPermissionButtonTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ExamWithPermissionsDto examWithPermissions)
        {
            string buttonText = examWithPermissions.PrimaryButtonText;
            System.Diagnostics.Debug.WriteLine($"[ExamPermissionButtonTextConverter] {examWithPermissions.Exam.Name}: ButtonText={buttonText}");
            return buttonText;
        }
        return "开始考试";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 考试权限按钮启用状态转换器
/// </summary>
public class ExamPermissionButtonEnabledConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ExamWithPermissionsDto examWithPermissions)
        {
            bool isEnabled = examWithPermissions.IsPrimaryButtonEnabled;
            System.Diagnostics.Debug.WriteLine($"[ExamPermissionButtonEnabledConverter] {examWithPermissions.Exam.Name}: IsEnabled={isEnabled}");
            return isEnabled;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 考试权限按钮可见性转换器
/// </summary>
public class ExamPermissionButtonVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ExamWithPermissionsDto examWithPermissions)
        {
            bool isVisible = examWithPermissions.IsPrimaryButtonVisible;
            System.Diagnostics.Debug.WriteLine($"[ExamPermissionButtonVisibilityConverter] {examWithPermissions.Exam.Name}: IsVisible={isVisible}");
            return isVisible;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 考试状态消息转换器
/// </summary>
public class ExamStatusMessageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ExamWithPermissionsDto examWithPermissions)
        {
            string statusMessage = examWithPermissions.StatusMessage;
            System.Diagnostics.Debug.WriteLine($"[ExamStatusMessageConverter] {examWithPermissions.Exam.Name}: StatusMessage={statusMessage}");
            return statusMessage;
        }
        return "状态未知";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 考试次数统计文本转换器
/// </summary>
public class ExamAttemptCountTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ExamWithPermissionsDto examWithPermissions)
        {
            string attemptCountText = examWithPermissions.AttemptCountText;
            System.Diagnostics.Debug.WriteLine($"[ExamAttemptCountTextConverter] {examWithPermissions.Exam.Name}: AttemptCountText={attemptCountText}");
            return attemptCountText;
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 考试权限状态到颜色转换器
/// </summary>
public class ExamPermissionStatusColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ExamWithPermissionsDto examWithPermissions)
        {
            // 根据考试状态返回不同的颜色
            if (!examWithPermissions.IsPrimaryButtonVisible)
            {
                return "#FF9E9E9E"; // 灰色 - 不可用
            }

            if (!examWithPermissions.IsPrimaryButtonEnabled)
            {
                return "#FFFF5722"; // 红色 - 无权限
            }

            if (examWithPermissions.AttemptLimit?.HasCompletedFirstAttempt == true)
            {
                return "#FFFF9800"; // 橙色 - 重考/练习
            }

            return "#FF4CAF50"; // 绿色 - 可以开始
        }
        return "#FF9E9E9E";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
