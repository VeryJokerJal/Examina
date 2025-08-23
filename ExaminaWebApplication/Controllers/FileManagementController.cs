using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 文件管理控制器
/// </summary>
public class FileManagementController : Controller
{
    private readonly ILogger<FileManagementController> _logger;

    public FileManagementController(ILogger<FileManagementController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 文件管理主页
    /// </summary>
    /// <returns></returns>
    public IActionResult Index()
    {
        return View("~/Views/Shared/FileManagement.cshtml");
    }
}
