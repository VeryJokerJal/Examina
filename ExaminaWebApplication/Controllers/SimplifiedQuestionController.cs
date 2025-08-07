using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Models.Exam;
using ExaminaWebApplication.Services.Exam;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 简化题目控制器 - 处理新的简化题目创建流程
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SimplifiedQuestionController : ControllerBase
{
    private readonly SimplifiedQuestionService _simplifiedQuestionService;
    private readonly ExcelImportExportService _excelImportExportService;
    private readonly ILogger<SimplifiedQuestionController> _logger;

    public SimplifiedQuestionController(
        SimplifiedQuestionService simplifiedQuestionService,
        ExcelImportExportService excelImportExportService,
        ILogger<SimplifiedQuestionController> logger)
    {
        _simplifiedQuestionService = simplifiedQuestionService;
        _excelImportExportService = excelImportExportService;
        _logger = logger;
    }

    /// <summary>
    /// 创建简化题目
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>创建的题目</returns>
    [HttpPost]
    public async Task<ActionResult<SimplifiedQuestionResponse>> CreateQuestion([FromBody] CreateSimplifiedQuestionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            SimplifiedQuestionResponse question = await _simplifiedQuestionService.CreateSimplifiedQuestionAsync(request);
            return Ok(question);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "创建简化题目参数错误");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建简化题目失败");
            return StatusCode(500, new { message = "创建题目失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取简化题目列表
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns>题目列表</returns>
    [HttpGet]
    public async Task<ActionResult<List<SimplifiedQuestionResponse>>> GetQuestions([FromQuery] int subjectId)
    {
        try
        {
            if (subjectId <= 0)
            {
                return BadRequest(new { message = "科目ID无效" });
            }

            List<SimplifiedQuestionResponse> questions = await _simplifiedQuestionService.GetSimplifiedQuestionsAsync(subjectId);
            return Ok(questions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取简化题目列表失败");
            return StatusCode(500, new { message = "获取题目列表失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 根据科目ID获取简化题目列表（路径参数版本）
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns>题目列表</returns>
    [HttpGet("subject/{subjectId}")]
    public async Task<ActionResult<List<SimplifiedQuestionResponse>>> GetQuestionsBySubject(int subjectId)
    {
        try
        {
            if (subjectId <= 0)
            {
                return BadRequest(new { message = "科目ID无效" });
            }

            List<SimplifiedQuestionResponse> questions = await _simplifiedQuestionService.GetSimplifiedQuestionsAsync(subjectId);
            return Ok(questions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取简化题目列表失败");
            return StatusCode(500, new { message = "获取题目列表失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取简化题目详情
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>题目详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<SimplifiedQuestionResponse>> GetQuestion(int id)
    {
        try
        {
            SimplifiedQuestionResponse? question = await _simplifiedQuestionService.GetSimplifiedQuestionAsync(id);
            
            if (question == null)
            {
                return NotFound(new { message = "题目不存在" });
            }

            return Ok(question);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取简化题目详情失败");
            return StatusCode(500, new { message = "获取题目详情失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 更新简化题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新后的题目</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<SimplifiedQuestionResponse>> UpdateQuestion(int id, [FromBody] CreateSimplifiedQuestionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            SimplifiedQuestionResponse? question = await _simplifiedQuestionService.UpdateSimplifiedQuestionAsync(id, request);
            
            if (question == null)
            {
                return NotFound(new { message = "题目不存在" });
            }

            return Ok(question);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "更新简化题目参数错误");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新简化题目失败");
            return StatusCode(500, new { message = "更新题目失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 切换简化题目状态
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>切换结果</returns>
    [HttpPost("{id}/toggle-status")]
    public async Task<ActionResult> ToggleQuestionStatus(int id)
    {
        try
        {
            bool success = await _simplifiedQuestionService.ToggleQuestionStatusAsync(id);

            if (!success)
            {
                return NotFound(new { message = "题目不存在" });
            }

            return Ok(new { message = "状态切换成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换简化题目状态失败");
            return StatusCode(500, new { message = "状态切换失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 删除简化题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteQuestion(int id)
    {
        try
        {
            bool success = await _simplifiedQuestionService.DeleteSimplifiedQuestionAsync(id);

            if (!success)
            {
                return NotFound(new { message = "题目不存在" });
            }

            return Ok(new { message = "题目删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除简化题目失败");
            return StatusCode(500, new { message = "删除题目失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取操作类型列表
    /// </summary>
    /// <param name="subjectType">科目类型</param>
    /// <returns>操作类型列表</returns>
    [HttpGet("operation-types")]
    public ActionResult<object> GetOperationTypes([FromQuery] int subjectType)
    {
        try
        {
            object operationTypes = subjectType switch
            {
                1 => new[] // Excel
                {
                    new { value = "BasicOperation", label = "基础操作" },
                    new { value = "DataListOperation", label = "数据清单操作" },
                    new { value = "ChartOperation", label = "图表操作" }
                },
                4 => new[] // Windows
                {
                    new { value = "Create", label = "创建操作" },
                    new { value = "Copy", label = "复制操作" },
                    new { value = "Move", label = "移动操作" },
                    new { value = "Delete", label = "删除操作" },
                    new { value = "Rename", label = "重命名操作" },
                    new { value = "CreateShortcut", label = "创建快捷方式" },
                    new { value = "ModifyProperties", label = "属性修改" },
                    new { value = "CopyAndRename", label = "复制并重命名" }
                },
                _ => new object[] { }
            };

            return Ok(operationTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取操作类型列表失败");
            return StatusCode(500, new { message = "获取操作类型失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 导入Windows题目
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <param name="file">Excel文件</param>
    /// <returns>导入结果</returns>
    [HttpPost("import")]
    public async Task<ActionResult> ImportQuestions([FromQuery] int subjectId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "请选择要导入的Excel文件" });
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "仅支持.xlsx格式的Excel文件" });
            }

            using var stream = file.OpenReadStream();
            var result = await _excelImportExportService.ImportWindowsQuestionsFromExcelAsync(stream, subjectId);

            return Ok(new
            {
                message = "导入完成",
                successCount = result.SuccessCount,
                failCount = result.FailCount,
                errors = result.Errors,
                importedQuestions = result.ImportedQuestions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入题目失败");
            return StatusCode(500, new { message = "导入失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 导出Windows题目
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <param name="enabledOnly">是否仅导出启用的题目</param>
    /// <returns>Excel文件</returns>
    [HttpGet("export")]
    public async Task<ActionResult> ExportQuestions([FromQuery] int subjectId, [FromQuery] bool enabledOnly = false)
    {
        try
        {
            var excelData = await _excelImportExportService.ExportWindowsQuestionsToExcelAsync(subjectId, enabledOnly);
            var fileName = $"Windows题目_科目{subjectId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出题目失败");
            return StatusCode(500, new { message = "导出失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 下载导入模板
    /// </summary>
    /// <returns>模板Excel文件</returns>
    [HttpGet("template")]
    public ActionResult DownloadTemplate()
    {
        try
        {
            var templateData = _excelImportExportService.GenerateImportTemplate();
            var fileName = $"Windows题目导入模板_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(templateData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成模板失败");
            return StatusCode(500, new { message = "生成模板失败，请稍后重试" });
        }
    }
}
