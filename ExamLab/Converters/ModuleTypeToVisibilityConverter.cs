using System;
using ExamLab.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ExamLab.Converters;

/// <summary>
/// ModuleType到Visibility转换器，用于根据模块类型显示/隐藏UI
/// 参数应为模块类型名称：Windows/PowerPoint/Excel/Word/CSharp
/// </summary>
public class ModuleTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ModuleType moduleType && parameter is string target)
        {
            bool isVisible = target switch
            {
                "Windows" => moduleType == ModuleType.Windows,
                "PowerPoint" => moduleType == ModuleType.PowerPoint,
                "Excel" => moduleType == ModuleType.Excel,
                "Word" => moduleType == ModuleType.Word,
                "CSharp" => moduleType == ModuleType.CSharp,
                _ => false
            };
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

