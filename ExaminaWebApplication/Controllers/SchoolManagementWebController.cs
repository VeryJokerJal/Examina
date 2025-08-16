using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 学校管理Web控制器（用于返回Views）
/// </summary>
[Authorize(Roles = "Administrator,Teacher")]
public class SchoolManagementWebController : Controller
{
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<SchoolManagementWebController> _logger;

    public SchoolManagementWebController(
        IOrganizationService organizationService,
        ILogger<SchoolManagementWebController> logger)
    {
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// 学校管理首页
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            SchoolManagementViewModel viewModel = new SchoolManagementViewModel();
            
            // 获取学校列表
            List<Models.Organization.Dto.OrganizationDto> schools = await _organizationService.GetSchoolsAsync(false);
            viewModel.Schools = schools;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载学校管理页面失败");
            TempData["ErrorMessage"] = "加载页面失败，请稍后重试";
            return View(new SchoolManagementViewModel());
        }
    }
}
