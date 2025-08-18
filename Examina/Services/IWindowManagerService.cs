using Examina.ViewModels;

namespace Examina.Services;

/// <summary>
/// 窗口管理服务接口
/// </summary>
public interface IWindowManagerService
{
    /// <summary>
    /// 导航到登录窗口
    /// </summary>
    void NavigateToLogin();

    /// <summary>
    /// 导航到主窗口
    /// </summary>
    void NavigateToMain();

    /// <summary>
    /// 导航到用户信息完善窗口
    /// </summary>
    void NavigateToUserInfoCompletion();

    /// <summary>
    /// 导航到加载窗口
    /// </summary>
    void NavigateToLoading();

    /// <summary>
    /// 关闭当前窗口
    /// </summary>
    void CloseCurrentWindow();

    /// <summary>
    /// 退出应用程序
    /// </summary>
    void ExitApplication();
}
