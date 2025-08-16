using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Services.ImportedComprehensiveTraining;
using ExaminaWebApplication.Models.ImportedComprehensiveTraining;
using ImportedComprehensiveTrainingEntity = ExaminaWebApplication.Models.ImportedComprehensiveTraining.ImportedComprehensiveTraining;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 综合训练管理控制器
/// </summary>
[Authorize(Policy = "TeacherOrAdminPolicy")]
public class ComprehensiveTrainingManagementController : Controller
{
    private readonly ComprehensiveTrainingImportService _comprehensiveTrainingImportService;
    private readonly ILogger<ComprehensiveTrainingManagementController> _logger;

    public ComprehensiveTrainingManagementController(
        ComprehensiveTrainingImportService comprehensiveTrainingImportService,
        ILogger<ComprehensiveTrainingManagementController> logger)
    {
        _comprehensiveTrainingImportService = comprehensiveTrainingImportService;
        _logger = logger;
    }

    /// <summary>
    /// 综合训练管理首页
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            List<ImportedComprehensiveTrainingEntity> comprehensiveTrainings = await _comprehensiveTrainingImportService.GetImportedComprehensiveTrainingsAsync(userId);
            return View(comprehensiveTrainings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取综合训练列表失败");
            TempData["ErrorMessage"] = "获取综合训练列表失败，请稍后重试";
            return View(new List<ImportedComprehensiveTrainingEntity>());
        }
    }

    /// <summary>
    /// 综合训练列表页面
    /// </summary>
    public async Task<IActionResult> ComprehensiveTrainingList()
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            List<ImportedComprehensiveTrainingEntity> comprehensiveTrainings = await _comprehensiveTrainingImportService.GetImportedComprehensiveTrainingsAsync(userId);
            return View(comprehensiveTrainings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取综合训练列表失败");
            TempData["ErrorMessage"] = "获取综合训练列表失败，请稍后重试";
            return View(new List<ImportedComprehensiveTrainingEntity>());
        }
    }

    /// <summary>
    /// 综合训练详情页面
    /// </summary>
    public async Task<IActionResult> ComprehensiveTrainingDetails(int id)
    {
        try
        {
            ImportedComprehensiveTrainingEntity? comprehensiveTraining = await _comprehensiveTrainingImportService.GetImportedComprehensiveTrainingDetailsAsync(id);
            if (comprehensiveTraining == null)
            {
                TempData["ErrorMessage"] = "综合训练不存在或已被删除";
                return RedirectToAction(nameof(ComprehensiveTrainingList));
            }

            // 暂时跳过权限检查，允许查看所有综合训练
            // string? userId = _userManager.GetUserId(User);
            // if (comprehensiveTraining.ImportedBy != userId)
            // {
            //     TempData["ErrorMessage"] = "您没有权限查看此综合训练";
            //     return RedirectToAction(nameof(ComprehensiveTrainingList));
            // }

            return View(comprehensiveTraining);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取综合训练详情失败: {ComprehensiveTrainingId}", id);
            TempData["ErrorMessage"] = "获取综合训练详情失败，请稍后重试";
            return RedirectToAction(nameof(ComprehensiveTrainingList));
        }
    }

    /// <summary>
    /// 导入综合训练页面
    /// </summary>
    [HttpGet]
    public IActionResult ImportComprehensiveTraining()
    {
        return View();
    }

    /// <summary>
    /// 处理综合训练导入
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportComprehensiveTraining(IFormFile comprehensiveTrainingFile)
    {
        if (comprehensiveTrainingFile == null || comprehensiveTrainingFile.Length == 0)
        {
            TempData["ErrorMessage"] = "请选择要导入的综合训练文件";
            return View();
        }

        // 验证文件类型
        string fileExtension = Path.GetExtension(comprehensiveTrainingFile.FileName).ToLowerInvariant();
        if (fileExtension != ".json" && fileExtension != ".xml")
        {
            TempData["ErrorMessage"] = "只支持 JSON 和 XML 格式的综合训练文件";
            return View();
        }

        // 验证文件大小（限制为10MB）
        if (comprehensiveTrainingFile.Length > 10 * 1024 * 1024)
        {
            TempData["ErrorMessage"] = "文件大小不能超过 10MB";
            return View();
        }

        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            using Stream fileStream = comprehensiveTrainingFile.OpenReadStream();
            ComprehensiveTrainingImportResult result = await _comprehensiveTrainingImportService.ImportComprehensiveTrainingAsync(
                fileStream, comprehensiveTrainingFile.FileName, userId);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = $"综合训练 '{result.ImportedComprehensiveTrainingName}' 导入成功！" +
                    $"共导入 {result.TotalSubjects} 个科目，{result.TotalModules} 个模块，{result.TotalQuestions} 道题目";
                
                _logger.LogInformation("用户 {UserId} 成功导入综合训练: {ComprehensiveTrainingName} (ID: {ComprehensiveTrainingId})", 
                    userId, result.ImportedComprehensiveTrainingName, result.ImportedComprehensiveTrainingId);

                return RedirectToAction(nameof(ComprehensiveTrainingDetails), new { id = result.ImportedComprehensiveTrainingId });
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                _logger.LogWarning("用户 {UserId} 导入综合训练失败: {ErrorMessage}", userId, result.ErrorMessage);
                return View();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入综合训练时发生异常: {FileName}", comprehensiveTrainingFile.FileName);
            TempData["ErrorMessage"] = "导入过程中发生错误，请稍后重试";
            return View();
        }
    }

    /// <summary>
    /// 删除综合训练
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComprehensiveTraining(int id)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            bool success = await _comprehensiveTrainingImportService.DeleteImportedComprehensiveTrainingAsync(id, userId);
            
            if (success)
            {
                TempData["SuccessMessage"] = "综合训练删除成功";
            }
            else
            {
                TempData["ErrorMessage"] = "综合训练不存在或您没有权限删除";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除综合训练失败: {ComprehensiveTrainingId}", id);
            TempData["ErrorMessage"] = "删除综合训练时发生错误，请稍后重试";
        }

        return RedirectToAction(nameof(ComprehensiveTrainingList));
    }
}
