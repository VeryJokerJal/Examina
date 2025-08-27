using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Examina.Converters;

/// <summary>
/// 调试用的多值转换器，用于查看绑定值
/// </summary>
public class DebugMultiValueConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2)
        {
            bool hasFullAccess = values[0] is bool access && access;
            bool enableTrial = values[1] is bool trial && trial;
            bool result = hasFullAccess && enableTrial;

            System.Diagnostics.Debug.WriteLine($"[DebugConverter] HasFullAccess: {hasFullAccess}, EnableTrial: {enableTrial}, Result: {result}");

            return result;
        }
        else if (values.Count == 1)
        {
            bool enableTrial = values[0] is bool trial && trial;
            System.Diagnostics.Debug.WriteLine($"[DebugConverter] 仅EnableTrial检查: {enableTrial}");
            return enableTrial;
        }

        System.Diagnostics.Debug.WriteLine($"[DebugConverter] 值不足，values.Count: {values.Count}");
        return false;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
