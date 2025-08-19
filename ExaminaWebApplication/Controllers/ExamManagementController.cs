using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Services.ImportedExam;
using ExaminaWebApplication.Models.ImportedExam;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 考试管理控制器
/// </summary>
[Authorize(Policy = "TeacherOrAdminPolicy")]
public class ExamManagementController : Controller
{
    private readonly ExamImportService _examImportService;
    private readonly ILogger<ExamManagementController> _logger;

    public ExamManagementController(
        ExamImportService examImportService,
        ILogger<ExamManagementController> logger)
    {
        _examImportService = examImportService;
        _logger = logger;
    }

    /// <summary>
    /// 考试管理首页
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            List<ImportedExam> exams = await _examImportService.GetImportedExamsAsync(userId);
            return View(exams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试列表失败");
            TempData["ErrorMessage"] = "获取考试列表失败，请稍后重试";
            return View(new List<ImportedExam>());
        }
    }

    /// <summary>
    /// 考试列表页面
    /// </summary>
    public async Task<IActionResult> ExamList()
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            List<ImportedExam> exams = await _examImportService.GetImportedExamsAsync(userId);
            return View(exams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试列表失败");
            TempData["ErrorMessage"] = "获取考试列表失败，请稍后重试";
            return View(new List<ImportedExam>());
        }
    }

    /// <summary>
    /// 考试详情页面
    /// </summary>
    public async Task<IActionResult> ExamDetails(int id)
    {
        try
        {
            ImportedExam? exam = await _examImportService.GetImportedExamDetailsAsync(id);
            if (exam == null)
            {
                TempData["ErrorMessage"] = "考试不存在或已被删除";
                return RedirectToAction(nameof(ExamList));
            }

            // 暂时跳过权限检查，允许查看所有考试
            // string? userId = _userManager.GetUserId(User);
            // if (exam.ImportedBy != userId)
            // {
            //     TempData["ErrorMessage"] = "您没有权限查看此考试";
            //     return RedirectToAction(nameof(ExamList));
            // }

            return View(exam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试详情失败: {ExamId}", id);
            TempData["ErrorMessage"] = "获取考试详情失败，请稍后重试";
            return RedirectToAction(nameof(ExamList));
        }
    }

    /// <summary>
    /// 考试导入页面
    /// </summary>
    public IActionResult ImportExam()
    {
        return View();
    }

    /// <summary>
    /// 处理考试导入
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExam(IFormFile examFile)
    {
        if (examFile == null || examFile.Length == 0)
        {
            TempData["ErrorMessage"] = "请选择要导入的考试文件";
            return View();
        }

        // 验证文件类型
        string fileExtension = Path.GetExtension(examFile.FileName).ToLowerInvariant();
        if (fileExtension != ".json" && fileExtension != ".xml")
        {
            TempData["ErrorMessage"] = "只支持 JSON 和 XML 格式的考试文件";
            return View();
        }

        // 文件大小验证已移除，支持任意大小的考试文件

        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            using Stream fileStream = examFile.OpenReadStream();
            ExamImportResult result = await _examImportService.ImportExamAsync(
                fileStream, examFile.FileName, userId);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = $"考试 '{result.ImportedExamName}' 导入成功！" +
                    $"共导入 {result.TotalSubjects} 个科目，{result.TotalModules} 个模块，{result.TotalQuestions} 道题目";
                
                _logger.LogInformation("用户 {UserId} 成功导入考试: {ExamName} (ID: {ExamId})", 
                    userId, result.ImportedExamName, result.ImportedExamId);

                return RedirectToAction(nameof(ExamDetails), new { id = result.ImportedExamId });
            }
            else
            {
                TempData["ErrorMessage"] = $"导入失败: {result.ErrorMessage}";
                _logger.LogWarning("用户 {UserId} 导入考试失败: {FileName}, 错误: {Error}", 
                    userId, examFile.FileName, result.ErrorMessage);
                return View();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入考试时发生异常: {FileName}", examFile.FileName);
            TempData["ErrorMessage"] = "导入过程中发生错误，请稍后重试";
            return View();
        }
    }

    /// <summary>
    /// 删除导入的考试
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("ExamManagement/DeleteExam")]
    public async Task<IActionResult> DeleteExam(int id)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            bool deleted = await _examImportService.DeleteImportedExamAsync(id, userId);
            if (deleted)
            {
                _logger.LogInformation("用户 {UserId} 删除了考试 ID: {ExamId}", userId, id);
                return Json(new { success = true, message = "考试删除成功" });
            }
            else
            {
                return Json(new { success = false, message = "考试不存在或您没有权限删除" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除考试失败: {ExamId}", id);
            return Json(new { success = false, message = "删除失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 更新考试类型
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <param name="examCategory">考试类型</param>
    /// <returns>更新结果</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateExamCategory(int id, ExamCategory examCategory)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            bool success = await _examImportService.UpdateExamCategoryAsync(id, examCategory, userId);

            if (success)
            {
                string categoryName = examCategory == ExamCategory.Provincial ? "全省统考" : "学校统考";
                return Json(new { success = true, message = $"考试类型已更新为：{categoryName}" });
            }
            else
            {
                return Json(new { success = false, message = "更新失败，考试不存在或您没有权限" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新考试类型失败: {ExamId}", id);
            return Json(new { success = false, message = "更新失败，请稍后重试" });
        }
    }
}
