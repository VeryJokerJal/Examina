using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Examina.Converters;

/// <summary>
/// 训练按钮可用性转换器
/// </summary>
public class TrainingButtonEnabledConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2)
        {
            bool hasFullAccess = values[0] is bool access && access;
            bool enableTrial = values[1] is bool trial && trial;
            
            // 有权限用户：始终可以开始训练
            // 无权限用户：只有在EnableTrial=true时可以试做，EnableTrial=false时可以点击解锁
            bool isEnabled = hasFullAccess || enableTrial;
            
            System.Diagnostics.Debug.WriteLine($"[TrainingButtonEnabledConverter] HasFullAccess: {hasFullAccess}, EnableTrial: {enableTrial}, 按钮可用: {isEnabled}");
            
            return isEnabled;
        }
        
        System.Diagnostics.Debug.WriteLine($"[TrainingButtonEnabledConverter] 值不足，values.Count: {values.Count}");
        return false;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
