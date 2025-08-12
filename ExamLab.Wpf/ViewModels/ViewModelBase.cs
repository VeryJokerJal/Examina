using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// 所有ViewModel的基类，提供ReactiveUI的基础功能
/// </summary>
public abstract class ViewModelBase : ReactiveObject
{
    /// <summary>
    /// 标题属性，用于显示在界面上
    /// </summary>
    [Reactive] public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [Reactive] public bool IsLoading { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [Reactive] public string? ErrorMessage { get; set; }

    /// <summary>
    /// 是否有错误
    /// </summary>
    [Reactive] public bool HasError { get; set; }

    /// <summary>
    /// 清除错误信息
    /// </summary>
    protected void ClearError()
    {
        ErrorMessage = null;
        HasError = false;
    }

    /// <summary>
    /// 设置错误信息
    /// </summary>
    /// <param name="message">错误消息</param>
    protected void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}
