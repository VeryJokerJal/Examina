using System;
using Microsoft.UI.Xaml.Data;

namespace ExamLab.Converters;

/// <summary>
/// Null到Boolean的转换器 - 用于控制按钮的启用状态
/// </summary>
public class NullToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isInverse = parameter?.ToString() == "Inverse";
        bool isNull = value == null;

        // 默认：非null时返回true（启用），null时返回false（禁用）
        // Inverse：null时返回true（启用），非null时返回false（禁用）
        return isInverse ? isNull : !isNull;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
