using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 班级成员管理Web控制器
/// </summary>
[Authorize(Roles = "Administrator,Teacher")]
public class ClassMembersController : Controller
{
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<ClassMembersController> _logger;

    public ClassMembersController(
        IOrganizationService organizationService,
        ILogger<ClassMembersController> logger)
    {
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// 班级成员管理首页
    /// </summary>
    /// <param name="classId">班级ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    public async Task<IActionResult> Index(int classId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            // 验证班级ID
            if (classId <= 0)
            {
                _logger.LogWarning("无效的班级ID: {ClassId}", classId);
                return BadRequest("无效的班级ID");
            }

            // 获取班级信息
            Models.Organization.Dto.OrganizationDto? classInfo = 
                await _organizationService.GetOrganizationByIdAsync(classId);
            
            if (classInfo == null)
            {
                _logger.LogWarning("班级不存在: {ClassId}", classId);
                return NotFound("班级不存在");
            }

            // 创建ViewModel
            ClassMembersViewModel viewModel = new ClassMembersViewModel
            {
                ClassId = classId,
                ClassInfo = classInfo,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };

            // 获取班级成员列表
            List<Models.Organization.Dto.StudentOrganizationDto> members =
                await _organizationService.GetOrganizationMembersAsync(classId, false);
            viewModel.Members = members;

            _logger.LogInformation("班级成员管理页面加载成功: {ClassId}, 成员数量: {MemberCount}", 
                classId, members.Count);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载班级成员管理页面失败: {ClassId}", classId);
            return StatusCode(500, "加载页面失败");
        }
    }
}
