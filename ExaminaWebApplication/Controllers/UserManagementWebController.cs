using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 用户管理Web控制器（用于返回Views）
/// </summary>
[Authorize(Roles = "Administrator")]
public class UserManagementWebController : Controller
{
    private readonly IUserManagementService _userManagementService;
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<UserManagementWebController> _logger;

    public UserManagementWebController(
        IUserManagementService userManagementService,
        IOrganizationService organizationService,
        ILogger<UserManagementWebController> logger)
    {
        _userManagementService = userManagementService;
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// 用户管理首页
    /// </summary>
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            UserManagementViewModel viewModel = new UserManagementViewModel
            {
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
            
            // 获取用户列表
            List<Models.Organization.Dto.UserDto> users = 
                await _userManagementService.GetUsersAsync(null, false, pageNumber, pageSize);
            viewModel.Users = users;

            // 获取学校列表
            List<Models.Organization.Dto.OrganizationDto> schools = 
                await _organizationService.GetSchoolsAsync(false);
            viewModel.Schools = schools;

            // 获取班级列表
            List<Models.Organization.Dto.OrganizationDto> classes = 
                await _organizationService.GetClassesAsync(false);
            viewModel.Classes = classes;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载用户管理页面失败");
            TempData["ErrorMessage"] = "加载页面失败，请稍后重试";
            return View(new UserManagementViewModel());
        }
    }
}
