using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Examina.Converters;

/// <summary>
/// 整数到布尔值转换器
/// </summary>
public class IntToBoolConverter : IValueConverter
{
    /// <summary>
    /// 单例实例
    /// </summary>
    public static readonly IntToBoolConverter Instance = new();

    /// <summary>
    /// 将整数转换为布尔值
    /// </summary>
    /// <param name="value">输入值</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="parameter">参数</param>
    /// <param name="culture">文化信息</param>
    /// <returns>转换结果</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > 0;
        }

        if (value is long longValue)
        {
            return longValue > 0;
        }

        if (value is double decimalValue)
        {
            return decimalValue > 0;
        }

        if (value is double doubleValue)
        {
            return doubleValue > 0;
        }

        if (value is float floatValue)
        {
            return floatValue > 0;
        }

        return false;
    }

    /// <summary>
    /// 将布尔值转换为整数
    /// </summary>
    /// <param name="value">输入值</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="parameter">参数</param>
    /// <param name="culture">文化信息</param>
    /// <returns>转换结果</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? 1 : 0;
        }

        return 0;
    }
}
