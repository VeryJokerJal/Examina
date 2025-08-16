using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 班级管理Web控制器（用于返回Views）
/// </summary>
[Authorize(Roles = "Administrator,Teacher")]
public class ClassManagementWebController : Controller
{
    private readonly IOrganizationService _organizationService;
    private readonly IInvitationCodeService _invitationCodeService;
    private readonly ILogger<ClassManagementWebController> _logger;

    public ClassManagementWebController(
        IOrganizationService organizationService,
        IInvitationCodeService invitationCodeService,
        ILogger<ClassManagementWebController> logger)
    {
        _organizationService = organizationService;
        _invitationCodeService = invitationCodeService;
        _logger = logger;
    }

    /// <summary>
    /// 班级管理首页
    /// </summary>
    public async Task<IActionResult> Index(int? schoolId = null)
    {
        try
        {
            ClassManagementViewModel viewModel = new ClassManagementViewModel();
            
            // 获取学校列表
            List<Models.Organization.Dto.OrganizationDto> schools = await _organizationService.GetSchoolsAsync(false);
            viewModel.Schools = schools;

            // 获取班级列表
            List<Models.Organization.Dto.OrganizationDto> classes;
            if (schoolId.HasValue)
            {
                classes = await _organizationService.GetClassesBySchoolAsync(schoolId.Value, false);
                viewModel.SelectedSchool = schools.FirstOrDefault(s => s.Id == schoolId.Value);
            }
            else
            {
                classes = await _organizationService.GetClassesAsync(false);
            }
            viewModel.Classes = classes;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载班级管理页面失败");
            TempData["ErrorMessage"] = "加载页面失败，请稍后重试";
            return View(new ClassManagementViewModel());
        }
    }
}
