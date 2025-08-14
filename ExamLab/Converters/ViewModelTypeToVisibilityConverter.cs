using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using ExamLab.ViewModels;

namespace ExamLab.Converters;

/// <summary>
/// ViewModel类型到Visibility的转换器
/// 用于根据当前ViewModel类型控制界面元素的可见性
/// </summary>
public class ViewModelTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string targetViewModelType)
        {
            bool matches = targetViewModelType switch
            {
                "CSharpModuleViewModel" => value is CSharpModuleViewModel,
                "PowerPointModuleViewModel" => value is PowerPointModuleViewModel,
                "ExcelModuleViewModel" => value is ExcelModuleViewModel,
                "WordModuleViewModel" => value is WordModuleViewModel,
                "WindowsModuleViewModel" => value is WindowsModuleViewModel,
                _ => false
            };

            return matches ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
