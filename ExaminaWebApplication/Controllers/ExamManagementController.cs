using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Services.ImportedExam;
using ExaminaWebApplication.Models.ImportedExam;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 考试管理控制器
/// </summary>
[Authorize]
public class ExamManagementController : Controller
{
    private readonly ExamImportService _examImportService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<ExamManagementController> _logger;

    public ExamManagementController(
        ExamImportService examImportService,
        UserManager<IdentityUser> userManager,
        ILogger<ExamManagementController> logger)
    {
        _examImportService = examImportService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// 考试管理首页
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            string? userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

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
            string? userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

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

            string? userId = _userManager.GetUserId(User);
            if (exam.ImportedBy != userId)
            {
                TempData["ErrorMessage"] = "您没有权限查看此考试";
                return RedirectToAction(nameof(ExamList));
            }

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

        // 验证文件大小（限制为10MB）
        if (examFile.Length > 10 * 1024 * 1024)
        {
            TempData["ErrorMessage"] = "文件大小不能超过 10MB";
            return View();
        }

        try
        {
            string? userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

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
    public async Task<IActionResult> DeleteExam(int id)
    {
        try
        {
            string? userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "用户未登录" });
            }

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
    /// 获取考试统计信息
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetExamStatistics()
    {
        try
        {
            string? userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "用户未登录" });
            }

            List<ImportedExam> exams = await _examImportService.GetImportedExamsAsync(userId);
            
            object statistics = new
            {
                totalExams = exams.Count,
                totalSubjects = exams.Sum(e => e.Subjects.Count),
                totalModules = exams.Sum(e => e.Modules.Count),
                recentImports = exams.OrderByDescending(e => e.ImportedAt)
                    .Take(5)
                    .Select(e => new
                    {
                        id = e.Id,
                        name = e.Name,
                        importedAt = e.ImportedAt.ToString("yyyy-MM-dd HH:mm"),
                        status = e.ImportStatus
                    })
            };

            return Json(new { success = true, data = statistics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试统计信息失败");
            return Json(new { success = false, message = "获取统计信息失败" });
        }
    }
}
