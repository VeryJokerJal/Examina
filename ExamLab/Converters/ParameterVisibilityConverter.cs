using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace ExamLab.Converters;

/// <summary>
/// 参数可见性转换器 - 基于参数的 IsVisible 属性控制显示
/// </summary>
public class ParameterVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isVisible)
        {
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Visible; // 默认可见
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }

        return true;
    }
}

/// <summary>
/// 参数启用状态转换器 - 基于参数的 IsVisible 属性控制启用状态
/// </summary>
public class ParameterEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isVisible)
        {
            return isVisible;
        }

        return true; // 默认启用
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isEnabled)
        {
            return isEnabled;
        }

        return true;
    }
}
