using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 非组织学生管理Web控制器（用于返回Views）
/// </summary>
[Authorize(Roles = "Administrator,Teacher")]
public class NonOrganizationStudentController : Controller
{
    private readonly INonOrganizationStudentService _studentService;
    private readonly ILogger<NonOrganizationStudentController> _logger;

    public NonOrganizationStudentController(
        INonOrganizationStudentService studentService,
        ILogger<NonOrganizationStudentController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    /// <summary>
    /// 非组织学生管理首页
    /// </summary>
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            NonOrganizationStudentViewModel viewModel = new NonOrganizationStudentViewModel
            {
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
            
            // 获取学生列表
            List<Models.Organization.Dto.NonOrganizationStudentDto> students = 
                await _studentService.GetStudentsAsync(false, pageNumber, pageSize);
            viewModel.Students = students;

            // 获取总数
            int totalCount = await _studentService.GetStudentCountAsync(false);
            viewModel.TotalCount = totalCount;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载非组织学生管理页面失败");
            TempData["ErrorMessage"] = "加载页面失败，请稍后重试";
            return View(new NonOrganizationStudentViewModel());
        }
    }
}
