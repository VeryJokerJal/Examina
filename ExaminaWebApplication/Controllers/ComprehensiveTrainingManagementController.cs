using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Services.ImportedComprehensiveTraining;
using ExaminaWebApplication.Services.ImportedSpecializedTraining;
using ExaminaWebApplication.Models.ImportedComprehensiveTraining;
using ExaminaWebApplication.Models.ImportedSpecializedTraining;
using ImportedComprehensiveTrainingEntity = ExaminaWebApplication.Models.ImportedComprehensiveTraining.ImportedComprehensiveTraining;
using ImportedSpecializedTrainingEntity = ExaminaWebApplication.Models.ImportedSpecializedTraining.ImportedSpecializedTraining;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 综合训练管理控制器
/// </summary>
[Authorize(Policy = "TeacherOrAdminPolicy")]
public class ComprehensiveTrainingManagementController : Controller
{
    private readonly ComprehensiveTrainingImportService _comprehensiveTrainingImportService;
    private readonly SpecializedTrainingImportService _specializedTrainingImportService;
    private readonly EnhancedComprehensiveTrainingService _enhancedComprehensiveTrainingService;
    private readonly ILogger<ComprehensiveTrainingManagementController> _logger;

    public ComprehensiveTrainingManagementController(
        ComprehensiveTrainingImportService comprehensiveTrainingImportService,
        SpecializedTrainingImportService specializedTrainingImportService,
        EnhancedComprehensiveTrainingService enhancedComprehensiveTrainingService,
        ILogger<ComprehensiveTrainingManagementController> logger)
    {
        _comprehensiveTrainingImportService = comprehensiveTrainingImportService;
        _specializedTrainingImportService = specializedTrainingImportService;
        _enhancedComprehensiveTrainingService = enhancedComprehensiveTrainingService;
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
    /// 处理综合训练导入（增强版：同时导入到综合训练和模拟考试系统）
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

            // 使用增强的服务进行双重模式导入
            EnhancedImportResult result = await _enhancedComprehensiveTrainingService.ImportWithDualModeAsync(
                fileStream, comprehensiveTrainingFile.FileName, userId);

            if (result.IsSuccess)
            {
                string successMessage = $"综合训练 '{result.ComprehensiveTrainingResult?.ImportedComprehensiveTrainingName}' 导入成功！" +
                    $"共导入 {result.ComprehensiveTrainingResult?.TotalSubjects} 个科目，{result.ComprehensiveTrainingResult?.TotalModules} 个模块，{result.ComprehensiveTrainingResult?.TotalQuestions} 道题目";

                if (result.MockExamImportSuccess)
                {
                    successMessage += "，题目已同时导入到模拟考试系统，可用于模拟考试";
                }
                else if (!string.IsNullOrEmpty(result.WarningMessage))
                {
                    successMessage += $"。注意：{result.WarningMessage}";
                }

                TempData["SuccessMessage"] = successMessage;

                _logger.LogInformation("用户 {UserId} 成功进行双重模式导入: {ComprehensiveTrainingName} (ID: {ComprehensiveTrainingId})，模拟考试导入：{MockExamSuccess}",
                    userId, result.ComprehensiveTrainingResult?.ImportedComprehensiveTrainingName,
                    result.ComprehensiveTrainingResult?.ImportedComprehensiveTrainingId, result.MockExamImportSuccess);

                return RedirectToAction(nameof(ComprehensiveTrainingDetails), new { id = result.ComprehensiveTrainingResult?.ImportedComprehensiveTrainingId });
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                _logger.LogWarning("用户 {UserId} 双重模式导入失败: {ErrorMessage}", userId, result.ErrorMessage);
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
    /// 删除综合训练（增强版：同时删除相关的模拟考试数据）
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComprehensiveTraining(int id)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            // 使用增强的服务进行级联删除
            EnhancedDeleteResult result = await _enhancedComprehensiveTrainingService.DeleteWithCascadeAsync(id, userId);

            if (result.IsSuccess)
            {
                string successMessage = "综合训练删除成功";
                if (result.DeletedMockExamQuestions > 0)
                {
                    successMessage += $"，同时删除了 {result.DeletedMockExamQuestions} 个相关的模拟考试记录";
                }
                TempData["SuccessMessage"] = successMessage;

                _logger.LogInformation("用户 {UserId} 成功进行级联删除: 综合训练ID {ComprehensiveTrainingId}，删除模拟考试记录 {MockExamCount} 个",
                    userId, id, result.DeletedMockExamQuestions);
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "综合训练不存在或您没有权限删除";
                _logger.LogWarning("用户 {UserId} 级联删除失败: {ErrorMessage}", userId, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "级联删除综合训练失败: {ComprehensiveTrainingId}", id);
            TempData["ErrorMessage"] = "删除综合训练时发生错误，请稍后重试";
        }

        return RedirectToAction(nameof(ComprehensiveTrainingList));
    }

    /// <summary>
    /// 更新综合训练的试用设置
    /// </summary>
    /// <param name="id">综合训练ID</param>
    /// <param name="enableTrial">是否启用试用</param>
    /// <returns>更新结果</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTrialSetting(int id, bool enableTrial)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            bool success = await _comprehensiveTrainingImportService.UpdateTrialSettingAsync(id, enableTrial, userId);

            if (success)
            {
                return Json(new { success = true, message = $"试用设置已{(enableTrial ? "启用" : "禁用")}" });
            }
            else
            {
                return Json(new { success = false, message = "更新失败，综合训练不存在或您没有权限" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新综合训练试用设置失败: {ComprehensiveTrainingId}", id);
            return Json(new { success = false, message = "更新失败，请稍后重试" });
        }
    }

    #region 专项训练管理

    /// <summary>
    /// 专项训练管理页面
    /// </summary>
    public async Task<IActionResult> SpecializedTraining()
    {
        try
        {
            // 管理员可以查看所有专项训练，不限制用户
            List<ImportedSpecializedTrainingEntity> specializedTrainings = await _specializedTrainingImportService.GetImportedSpecializedTrainingsAsync();
            return View(specializedTrainings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取专项训练列表失败");
            TempData["ErrorMessage"] = "获取专项训练列表失败，请稍后重试";
            return View(new List<ImportedSpecializedTrainingEntity>());
        }
    }

    /// <summary>
    /// 专项训练导入页面
    /// </summary>
    public IActionResult ImportSpecializedTraining()
    {
        return View();
    }

    /// <summary>
    /// 处理专项训练文件上传和导入
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ImportSpecializedTraining(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "请选择要导入的文件";
            return View();
        }

        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "只支持 JSON 格式的文件";
            return View();
        }

        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            using Stream fileStream = file.OpenReadStream();
            SpecializedTrainingImportResult result = await _specializedTrainingImportService.ImportSpecializedTrainingAsync(
                fileStream, file.FileName, userId);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = $"专项训练导入成功！训练名称：{result.ImportedSpecializedTrainingName}";
                return RedirectToAction(nameof(SpecializedTraining));
            }
            else
            {
                TempData["ErrorMessage"] = $"导入失败：{result.ErrorMessage}";
                return View();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "专项训练导入过程发生异常");
            TempData["ErrorMessage"] = "导入过程发生异常，请稍后重试";
            return View();
        }
    }

    /// <summary>
    /// 专项训练详情页面
    /// </summary>
    public async Task<IActionResult> SpecializedTrainingDetails(int id)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            ImportedSpecializedTrainingEntity? specializedTraining = await _specializedTrainingImportService.GetSpecializedTrainingByIdAsync(id, userId);
            if (specializedTraining == null)
            {
                TempData["ErrorMessage"] = "专项训练不存在或您没有权限查看";
                return RedirectToAction(nameof(SpecializedTraining));
            }

            return View(specializedTraining);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取专项训练详情失败: {Id}", id);
            TempData["ErrorMessage"] = "获取专项训练详情失败，请稍后重试";
            return RedirectToAction(nameof(SpecializedTraining));
        }
    }

    /// <summary>
    /// 删除专项训练
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteSpecializedTraining(int id)
    {
        try
        {
            _logger.LogInformation("收到删除专项训练请求，训练ID: {TrainingId}", id);

            // 先检查专项训练是否存在
            var allTrainings = await _specializedTrainingImportService.GetImportedSpecializedTrainingsAsync();
            _logger.LogInformation("数据库中所有专项训练: {Trainings}",
                string.Join(", ", allTrainings.Select(t => $"ID:{t.Id},Name:{t.Name},ImportedBy:{t.ImportedBy}")));

            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            bool success = await _specializedTrainingImportService.DeleteSpecializedTrainingAsync(id, userId);

            _logger.LogInformation("删除专项训练结果，训练ID: {TrainingId}, 成功: {Success}", id, success);

            if (success)
            {
                return Json(new { success = true, message = "专项训练删除成功" });
            }
            else
            {
                return Json(new { success = false, message = "删除失败，专项训练不存在或您没有权限" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除专项训练失败: {Id}", id);
            return Json(new { success = false, message = "删除失败，请稍后重试" });
        }
    }

    #endregion

    #region API端点

    /// <summary>
    /// 更新综合实训名称
    /// </summary>
    [HttpPut("api/comprehensive-training/{id}/name")]
    public async Task<IActionResult> UpdateComprehensiveTrainingName(int id, [FromBody] ExaminaWebApplication.Models.Api.Admin.UpdateComprehensiveTrainingNameRequestDto request)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            // 验证请求数据
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new ExaminaWebApplication.Models.Api.Admin.UpdateComprehensiveTrainingNameResponseDto
                {
                    Success = false,
                    Message = "综合实训名称不能为空"
                });
            }

            bool success = await _comprehensiveTrainingImportService.UpdateComprehensiveTrainingNameAsync(
                id, userId, request.Name);

            if (!success)
            {
                _logger.LogWarning("更新综合实训名称失败，用户ID: {UserId}, 训练ID: {TrainingId}, 新名称: {NewName}",
                    userId, id, request.Name);
                return BadRequest(new ExaminaWebApplication.Models.Api.Admin.UpdateComprehensiveTrainingNameResponseDto
                {
                    Success = false,
                    Message = "更新综合实训名称失败，训练不存在、您无权限操作、名称已存在或包含非法字符"
                });
            }

            _logger.LogInformation("更新综合实训名称成功，用户ID: {UserId}, 训练ID: {TrainingId}, 新名称: {NewName}",
                userId, id, request.Name);

            return Ok(new ExaminaWebApplication.Models.Api.Admin.UpdateComprehensiveTrainingNameResponseDto
            {
                Success = true,
                Message = "综合实训名称更新成功",
                UpdatedName = request.Name.Trim(),
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新综合实训名称失败，训练ID: {TrainingId}", id);
            return StatusCode(500, new ExaminaWebApplication.Models.Api.Admin.UpdateComprehensiveTrainingNameResponseDto
            {
                Success = false,
                Message = "更新综合实训名称失败，服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 更新专项训练名称
    /// </summary>
    [HttpPut("api/specialized-training/{id}/name")]
    public async Task<IActionResult> UpdateSpecializedTrainingName(int id, [FromBody] ExaminaWebApplication.Models.Api.Admin.UpdateSpecializedTrainingNameRequestDto request)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            // 验证请求数据
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new ExaminaWebApplication.Models.Api.Admin.UpdateSpecializedTrainingNameResponseDto
                {
                    Success = false,
                    Message = "专项训练名称不能为空"
                });
            }

            bool success = await _specializedTrainingImportService.UpdateSpecializedTrainingNameAsync(
                id, userId, request.Name);

            if (!success)
            {
                _logger.LogWarning("更新专项训练名称失败，用户ID: {UserId}, 训练ID: {TrainingId}, 新名称: {NewName}",
                    userId, id, request.Name);
                return BadRequest(new ExaminaWebApplication.Models.Api.Admin.UpdateSpecializedTrainingNameResponseDto
                {
                    Success = false,
                    Message = "更新专项训练名称失败，训练不存在、您无权限操作、名称已存在或包含非法字符"
                });
            }

            _logger.LogInformation("更新专项训练名称成功，用户ID: {UserId}, 训练ID: {TrainingId}, 新名称: {NewName}",
                userId, id, request.Name);

            return Ok(new ExaminaWebApplication.Models.Api.Admin.UpdateSpecializedTrainingNameResponseDto
            {
                Success = true,
                Message = "专项训练名称更新成功",
                UpdatedName = request.Name.Trim(),
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新专项训练名称失败，训练ID: {TrainingId}", id);
            return StatusCode(500, new ExaminaWebApplication.Models.Api.Admin.UpdateSpecializedTrainingNameResponseDto
            {
                Success = false,
                Message = "更新专项训练名称失败，服务器内部错误"
            });
        }
    }

    #endregion
}
