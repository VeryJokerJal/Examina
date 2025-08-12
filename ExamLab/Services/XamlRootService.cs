using Microsoft.UI.Xaml;

namespace ExamLab.Services;

/// <summary>
/// XamlRoot管理服务，用于解决WinUI3对话框显示问题
/// </summary>
public static class XamlRootService
{
    private static XamlRoot? _xamlRoot;

    /// <summary>
    /// 设置XamlRoot
    /// </summary>
    /// <param name="xamlRoot">XamlRoot实例</param>
    public static void SetXamlRoot(XamlRoot xamlRoot)
    {
        _xamlRoot = xamlRoot;
    }

    /// <summary>
    /// 获取当前的XamlRoot
    /// </summary>
    /// <returns>XamlRoot实例，如果未设置则返回null</returns>
    public static XamlRoot? GetXamlRoot()
    {
        return _xamlRoot;
    }

    /// <summary>
    /// 检查XamlRoot是否已设置
    /// </summary>
    /// <returns>如果XamlRoot已设置返回true，否则返回false</returns>
    public static bool IsXamlRootSet()
    {
        return _xamlRoot != null;
    }

    /// <summary>
    /// 清除XamlRoot（通常在应用程序关闭时调用）
    /// </summary>
    public static void ClearXamlRoot()
    {
        _xamlRoot = null;
    }
}
