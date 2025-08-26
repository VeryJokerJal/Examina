using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Services.ImportedExam;
using ExaminaWebApplication.Services.Admin;
using ExaminaWebApplication.Models.ImportedExam;
using ExaminaWebApplication.Data;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 考试管理控制器
/// </summary>
[Authorize(Policy = "TeacherOrAdminPolicy")]
public class ExamManagementController : Controller
{
    private readonly ExamImportService _examImportService;
    private readonly IAdminExamManagementService _adminExamService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExamManagementController> _logger;

    public ExamManagementController(
        ExamImportService examImportService,
        IAdminExamManagementService adminExamService,
        ApplicationDbContext context,
        ILogger<ExamManagementController> logger)
    {
        _examImportService = examImportService;
        _adminExamService = adminExamService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 考试管理首页
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            int userId = GetCurrentUserId();
            List<ExaminaWebApplication.Models.Api.Admin.AdminExamDto> exams = await _adminExamService.GetExamsAsync(userId);
            return View(exams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试列表失败");
            TempData["ErrorMessage"] = "获取考试列表失败，请稍后重试";
            return View(new List<ExaminaWebApplication.Models.Api.Admin.AdminExamDto>());
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

    /// <summary>
    /// 考试时间设置页面
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>时间设置页面</returns>
    public async Task<IActionResult> ExamSchedule(int id)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            ImportedExam? exam = await _examImportService.GetImportedExamByIdAsync(id, userId);

            if (exam == null)
            {
                TempData["ErrorMessage"] = "考试不存在或您没有权限访问";
                return RedirectToAction(nameof(Index));
            }

            return View(exam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试时间设置页面失败，考试ID: {ExamId}", id);
            TempData["ErrorMessage"] = "获取考试信息失败，请稍后重试";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// 更新考试时间和状态
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="status">考试状态</param>
    /// <param name="examCategory">考试类型</param>
    /// <returns>更新结果</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateExamSchedule(int id, DateTime startTime, DateTime endTime, string status, ExamCategory examCategory)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以改为从登录用户获取
            int userId = 1; // 使用管理员用户ID

            // 验证时间
            if (endTime <= startTime)
            {
                TempData["ErrorMessage"] = "结束时间必须晚于开始时间";
                return RedirectToAction(nameof(ExamSchedule), new { id });
            }

            // 更新考试时间
            bool timeUpdateSuccess = await _examImportService.UpdateExamScheduleAsync(id, userId, startTime, endTime);
            if (!timeUpdateSuccess)
            {
                TempData["ErrorMessage"] = "更新考试时间失败，考试不存在或您没有权限";
                return RedirectToAction(nameof(ExamSchedule), new { id });
            }

            // 更新考试状态
            bool statusUpdateSuccess = await _examImportService.UpdateExamStatusAsync(id, userId, status);
            if (!statusUpdateSuccess)
            {
                TempData["ErrorMessage"] = "更新考试状态失败";
                return RedirectToAction(nameof(ExamSchedule), new { id });
            }

            // 更新考试类型
            bool categoryUpdateSuccess = await _examImportService.UpdateExamCategoryAsync(id, examCategory, userId);
            if (!categoryUpdateSuccess)
            {
                TempData["ErrorMessage"] = "更新考试类型失败";
                return RedirectToAction(nameof(ExamSchedule), new { id });
            }

            TempData["SuccessMessage"] = "考试设置更新成功！";
            _logger.LogInformation("用户 {UserId} 成功更新考试设置: 考试ID {ExamId}, 开始时间 {StartTime}, 结束时间 {EndTime}, 状态 {Status}, 类型 {Category}",
                userId, id, startTime, endTime, status, examCategory);

            return RedirectToAction(nameof(ExamDetails), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新考试设置失败，考试ID: {ExamId}", id);
            TempData["ErrorMessage"] = "更新考试设置失败，请稍后重试";
            return RedirectToAction(nameof(ExamSchedule), new { id });
        }
    }

    /// <summary>
    /// API测试页面
    /// </summary>
    /// <returns>API测试页面</returns>
    public IActionResult ApiTest()
    {
        return View();
    }

    /// <summary>
    /// 创建测试数据
    /// </summary>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<IActionResult> CreateTestData()
    {
        try
        {
            await ExaminaWebApplication.Data.SeedTestExamData.SeedAsync(_context);
            return Ok(new { message = "测试数据创建成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建测试数据失败");
            return StatusCode(500, new { message = "创建测试数据失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 检查测试数据
    /// </summary>
    [HttpGet("check-test-data")]
    public async Task<IActionResult> CheckTestData()
    {
        try
        {
            TestDataChecker checker = new(_context);
            await checker.CheckTestDataAsync();
            return Ok(new { message = "测试数据检查完成，请查看控制台输出" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查测试数据失败");
            return StatusCode(500, new { message = "检查测试数据失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 重新创建测试数据
    /// </summary>
    [HttpPost("recreate-test-data")]
    public async Task<IActionResult> RecreateTestData()
    {
        try
        {
            TestDataChecker checker = new(_context);
            await checker.RecreateTestDataAsync();
            return Ok(new { message = "测试数据重新创建完成，请查看控制台输出" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新创建测试数据失败");
            return StatusCode(500, new { message = "重新创建测试数据失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 设置考试时间
    /// </summary>
    [HttpPost("set-schedule/{examId}")]
    public async Task<IActionResult> SetExamSchedule(int examId, [FromBody] SetScheduleRequest request)
    {
        try
        {
            int userId = GetCurrentUserId();
            bool success = await _adminExamService.SetExamScheduleAsync(examId, userId, request.StartTime, request.EndTime);

            if (success)
            {
                return Ok(new { message = "考试时间设置成功" });
            }

            return BadRequest(new { message = "考试时间设置失败，请检查权限或考试状态" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置考试时间失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "设置考试时间失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新考试状态
    /// </summary>
    [HttpPost("update-status/{examId}")]
    public async Task<IActionResult> UpdateExamStatus(int examId, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            int userId = GetCurrentUserId();
            bool success = await _adminExamService.UpdateExamStatusAsync(examId, userId, request.Status);

            if (success)
            {
                return Ok(new { message = $"考试状态已更新为: {request.Status}" });
            }

            return BadRequest(new { message = "考试状态更新失败，请检查权限或状态转换规则" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新考试状态失败，考试ID: {ExamId}, 状态: {Status}", examId, request.Status);
            return StatusCode(500, new { message = "更新考试状态失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 发布考试
    /// </summary>
    [HttpPost("publish/{examId}")]
    public async Task<IActionResult> PublishExam(int examId)
    {
        try
        {
            int userId = GetCurrentUserId();
            bool success = await _adminExamService.PublishExamAsync(examId, userId);

            if (success)
            {
                return Ok(new { message = "考试已发布" });
            }

            return BadRequest(new { message = "考试发布失败，请检查权限或考试状态" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布考试失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "发布考试失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 开始考试
    /// </summary>
    [HttpPost("start/{examId}")]
    public async Task<IActionResult> StartExam(int examId)
    {
        try
        {
            int userId = GetCurrentUserId();
            bool success = await _adminExamService.StartExamAsync(examId, userId);

            if (success)
            {
                return Ok(new { message = "考试已开始" });
            }

            return BadRequest(new { message = "考试开始失败，请检查权限或考试状态" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始考试失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "开始考试失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 结束考试
    /// </summary>
    [HttpPost("end/{examId}")]
    public async Task<IActionResult> EndExam(int examId)
    {
        try
        {
            int userId = GetCurrentUserId();
            bool success = await _adminExamService.EndExamAsync(examId, userId);

            if (success)
            {
                return Ok(new { message = "考试已结束" });
            }

            return BadRequest(new { message = "考试结束失败，请检查权限或考试状态" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "结束考试失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "结束考试失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新考试设置（重考和重做）
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateExamSetting(int examId, [FromBody] UpdateExamSettingRequest request)
    {
        try
        {
            int userId = GetCurrentUserId();
            bool success = await _adminExamService.UpdateExamSettingAsync(examId, userId, request.SettingName, request.Value);

            if (success)
            {
                string settingDisplayName = request.SettingName == "AllowRetake" ? "重考设置" : "重做设置";
                string statusText = request.Value ? "启用" : "禁用";
                return Ok(new { message = $"{settingDisplayName}已{statusText}" });
            }

            return BadRequest(new { message = "更新设置失败，请检查权限或考试状态" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新考试设置失败，考试ID: {ExamId}, 设置: {SettingName}, 值: {Value}",
                examId, request.SettingName, request.Value);
            return StatusCode(500, new { message = "更新设置失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private int GetCurrentUserId()
    {
        // 简化实现，实际应该从认证信息中获取
        return 1; // 假设当前用户ID为1
    }
}

/// <summary>
/// 设置考试时间请求
/// </summary>
public class SetScheduleRequest
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

/// <summary>
/// 更新考试状态请求
/// </summary>
public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// 更新考试设置请求
/// </summary>
public class UpdateExamSettingRequest
{
    public string SettingName { get; set; } = string.Empty;
    public bool Value { get; set; }
}
