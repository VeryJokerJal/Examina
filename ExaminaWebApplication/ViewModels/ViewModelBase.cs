using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExaminaWebApplication.ViewModels;

/// <summary>
/// 所有ViewModel的基类，提供INotifyPropertyChanged的基础功能
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <summary>
    /// 属性更改事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 触发属性更改通知
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 设置属性值并触发更改通知
    /// </summary>
    /// <typeparam name="T">属性类型</typeparam>
    /// <param name="field">字段引用</param>
    /// <param name="value">新值</param>
    /// <param name="propertyName">属性名称</param>
    /// <returns>是否发生了更改</returns>
    protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
