using System;
using Microsoft.UI.Xaml.Data;

namespace ExamLab.Converters;

/// <summary>
/// 布尔值到透明度转换器
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    /// <summary>
    /// 将布尔值转换为透明度
    /// </summary>
    /// <param name="value">布尔值</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="parameter">参数</param>
    /// <param name="language">语言</param>
    /// <returns>透明度值</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            // 如果参数是"Inverse"，则反转逻辑
            bool inverse = parameter?.ToString() == "Inverse";
            
            if (inverse)
            {
                return boolValue ? 0.0 : 1.0;
            }
            else
            {
                return boolValue ? 1.0 : 0.0;
            }
        }
        
        return 0.0;
    }

    /// <summary>
    /// 将透明度转换回布尔值
    /// </summary>
    /// <param name="value">透明度值</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="parameter">参数</param>
    /// <param name="language">语言</param>
    /// <returns>布尔值</returns>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is double opacity)
        {
            // 如果参数是"Inverse"，则反转逻辑
            bool inverse = parameter?.ToString() == "Inverse";
            
            if (inverse)
            {
                return opacity < 0.5;
            }
            else
            {
                return opacity >= 0.5;
            }
        }
        
        return false;
    }
}
