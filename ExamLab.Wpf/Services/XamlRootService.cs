namespace ExamLab.Services;

/// <summary>
/// XamlRoot管理服务的WPF版本（空实现，因为WPF不需要XamlRoot）
/// </summary>
public static class XamlRootService
{
    /// <summary>
    /// 设置XamlRoot（WPF版本中为空实现）
    /// </summary>
    /// <param name="xamlRoot">XamlRoot实例（在WPF中忽略）</param>
    public static void SetXamlRoot(object? xamlRoot)
    {
        // WPF 不需要 XamlRoot，空实现
    }

    /// <summary>
    /// 获取当前的XamlRoot（WPF版本中返回null）
    /// </summary>
    /// <returns>始终返回null</returns>
    public static object? GetXamlRoot()
    {
        return null;
    }

    /// <summary>
    /// 检查XamlRoot是否已设置（WPF版本中始终返回true）
    /// </summary>
    /// <returns>始终返回true</returns>
    public static bool IsXamlRootSet()
    {
        return true;
    }

    /// <summary>
    /// 清除XamlRoot（WPF版本中为空实现）
    /// </summary>
    public static void ClearXamlRoot()
    {
        // WPF 不需要 XamlRoot，空实现
    }
}
