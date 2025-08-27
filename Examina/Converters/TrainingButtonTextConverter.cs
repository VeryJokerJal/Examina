using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Examina.Converters;

/// <summary>
/// 训练按钮文本转换器
/// </summary>
public class TrainingButtonTextConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2)
        {
            bool hasFullAccess = values[0] is bool access && access;
            bool enableTrial = values[1] is bool trial && trial;
            
            string buttonText;
            if (hasFullAccess)
            {
                buttonText = "开始答题";
            }
            else
            {
                buttonText = enableTrial ? "试做" : "解锁";
            }
            
            System.Diagnostics.Debug.WriteLine($"[TrainingButtonTextConverter] HasFullAccess: {hasFullAccess}, EnableTrial: {enableTrial}, 按钮文本: {buttonText}");
            
            return buttonText;
        }
        
        System.Diagnostics.Debug.WriteLine($"[TrainingButtonTextConverter] 值不足，values.Count: {values.Count}");
        return "开始答题";
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
