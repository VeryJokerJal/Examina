using ExaminaWebApplication.Filters;
using ExaminaWebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 用户导入管理控制器
/// </summary>
[RequireLogin]
[Authorize(Roles = "Administrator,Teacher")]
public class UserImportManagementController : Controller
{
    private readonly ILogger<UserImportManagementController> _logger;

    public UserImportManagementController(ILogger<UserImportManagementController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 用户导入主页
    /// </summary>
    /// <returns></returns>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// 非组织用户导入页面
    /// </summary>
    /// <returns></returns>
    public IActionResult ImportNonOrganizationUsers()
    {
        return View();
    }

    /// <summary>
    /// 组织用户导入页面
    /// </summary>
    /// <returns></returns>
    public IActionResult ImportOrganizationUsers()
    {
        return View();
    }
}
