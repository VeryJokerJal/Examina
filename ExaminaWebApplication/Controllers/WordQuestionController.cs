using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Services.Word;
using ExaminaWebApplication.Models.Exam;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// Word题目控制器
/// </summary>
[ApiController]
[Route("api/word/question")]
public class WordQuestionController : ControllerBase
{
    private readonly WordQuestionService _wordQuestionService;
    private readonly WordQuestionExcelService _wordQuestionExcelService;
    private readonly ILogger<WordQuestionController> _logger;

    public WordQuestionController(
        WordQuestionService wordQuestionService,
        WordQuestionExcelService wordQuestionExcelService,
        ILogger<WordQuestionController> logger)
    {
        _wordQuestionService = wordQuestionService;
        _wordQuestionExcelService = wordQuestionExcelService;
        _logger = logger;
    }

    /// <summary>
    /// 获取指定科目的所有Word题目
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<List<WordQuestion>>> GetQuestionsBySubjectId([FromQuery] int subjectId)
    {
        try
        {
            if (subjectId <= 0)
            {
                return BadRequest(new { message = "科目ID无效" });
            }

            List<WordQuestion> questions = await _wordQuestionService.GetQuestionsBySubjectIdAsync(subjectId);
            return Ok(questions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Word题目列表失败，科目ID: {SubjectId}", subjectId);
            return StatusCode(500, new { message = "获取题目列表失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 根据ID获取Word题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<WordQuestion>> GetQuestionById(int id)
    {
        try
        {
            WordQuestion? question = await _wordQuestionService.GetQuestionByIdAsync(id);
            if (question == null)
            {
                return NotFound(new { message = "题目不存在" });
            }
            return Ok(question);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Word题目详情失败，ID: {Id}", id);
            return StatusCode(500, new { message = "获取题目详情失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 创建Word题目（支持简化表单：仅要求 + 分值）
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<WordQuestion>> CreateQuestion([FromBody] CreateWordQuestionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 生成标题/描述（若未提供）
            string generatedTitle = request.Title ?? $"Word题目-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            string generatedDescription = request.Description ?? (request.Requirements != null ? $"题目要求：{request.Requirements}" : string.Empty);

            WordQuestion question = new WordQuestion
            {
                SubjectId = request.SubjectId,
                Title = generatedTitle,
                Description = generatedDescription,
                Requirements = request.Requirements,
                TotalScore = request.TotalScore,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            WordQuestion createdQuestion = await _wordQuestionService.CreateQuestionAsync(question);
            return CreatedAtAction(nameof(GetQuestionById), new { id = createdQuestion.Id }, createdQuestion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建Word题目失败");
            return StatusCode(500, new { message = "创建题目失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 更新Word题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <param name="question">题目信息</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateQuestion(int id, [FromBody] WordQuestion question)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != question.Id)
            {
                return BadRequest(new { message = "ID不匹配" });
            }

            bool success = await _wordQuestionService.UpdateQuestionAsync(question);
            if (!success)
            {
                return NotFound(new { message = "题目不存在" });
            }

            return Ok(new { message = "题目更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新Word题目失败，ID: {Id}", id);
            return StatusCode(500, new { message = "更新题目失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 删除Word题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteQuestion(int id)
    {
        try
        {
            bool success = await _wordQuestionService.DeleteQuestionAsync(id);
            if (!success)
            {
                return NotFound(new { message = "题目不存在" });
            }

            return Ok(new { message = "题目删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除Word题目失败，ID: {Id}", id);
            return StatusCode(500, new { message = "删除题目失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 向题目添加操作点
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <param name="operationPoint">操作点信息</param>
    /// <returns></returns>
    [HttpPost("{questionId}/operation-points")]
    public async Task<ActionResult<WordQuestionOperationPoint>> AddOperationPointToQuestion(int questionId, [FromBody] WordQuestionOperationPoint operationPoint)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            WordQuestionOperationPoint createdOperationPoint = await _wordQuestionService.AddOperationPointToQuestionAsync(questionId, operationPoint);
            return Ok(createdOperationPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "向Word题目添加操作点失败，题目ID: {QuestionId}", questionId);
            return StatusCode(500, new { message = "添加操作点失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 更新题目操作点
    /// </summary>
    /// <param name="operationPointId">操作点ID</param>
    /// <param name="operationPoint">操作点信息</param>
    /// <returns></returns>
    [HttpPut("operation-points/{operationPointId}")]
    public async Task<ActionResult> UpdateQuestionOperationPoint(int operationPointId, [FromBody] WordQuestionOperationPoint operationPoint)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (operationPointId != operationPoint.Id)
            {
                return BadRequest(new { message = "ID不匹配" });
            }

            bool success = await _wordQuestionService.UpdateQuestionOperationPointAsync(operationPoint);
            if (!success)
            {
                return NotFound(new { message = "操作点不存在" });
            }

            return Ok(new { message = "操作点更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新Word题目操作点失败，操作点ID: {OperationPointId}", operationPointId);
            return StatusCode(500, new { message = "更新操作点失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 删除题目操作点
    /// </summary>
    /// <param name="operationPointId">操作点ID</param>
    /// <returns></returns>
    [HttpDelete("operation-points/{operationPointId}")]
    public async Task<ActionResult> DeleteQuestionOperationPoint(int operationPointId)
    {
        try
        {
            bool success = await _wordQuestionService.DeleteQuestionOperationPointAsync(operationPointId);
            if (!success)
            {
                return NotFound(new { message = "操作点不存在" });
            }

            return Ok(new { message = "操作点删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除Word题目操作点失败，操作点ID: {OperationPointId}", operationPointId);
            return StatusCode(500, new { message = "删除操作点失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 调整题目操作点顺序
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <param name="operationPointOrders">操作点ID和新顺序的映射</param>
    /// <returns></returns>
    [HttpPost("{questionId}/reorder")]
    public async Task<ActionResult> ReorderQuestionOperationPoints(int questionId, [FromBody] Dictionary<int, int> operationPointOrders)
    {
        try
        {
            if (operationPointOrders == null || !operationPointOrders.Any())
            {
                return BadRequest(new { message = "操作点顺序数据不能为空" });
            }

            bool success = await _wordQuestionService.ReorderQuestionOperationPointsAsync(questionId, operationPointOrders);
            if (!success)
            {
                return BadRequest(new { message = "调整顺序失败" });
            }

            return Ok(new { message = "操作点顺序调整成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调整Word题目操作点顺序失败，题目ID: {QuestionId}", questionId);
            return StatusCode(500, new { message = "调整顺序失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 切换题目状态
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns></returns>
    [HttpPost("{id}/toggle-status")]
    public async Task<ActionResult> ToggleQuestionStatus(int id)
    {
        try
        {
            bool success = await _wordQuestionService.ToggleQuestionStatusAsync(id);
            if (!success)
            {
                return NotFound(new { message = "题目不存在" });
            }

            return Ok(new { message = "状态切换成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换Word题目状态失败，ID: {Id}", id);
            return StatusCode(500, new { message = "状态切换失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取题目统计信息
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetQuestionStatistics([FromQuery] int subjectId)
    {
        try
        {
            if (subjectId <= 0)
            {
                return BadRequest(new { message = "科目ID无效" });
            }

            object statistics = await _wordQuestionService.GetQuestionStatisticsAsync(subjectId);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Word题目统计信息失败，科目ID: {SubjectId}", subjectId);
            return StatusCode(500, new { message = "获取统计信息失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 生成题目描述
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns></returns>
    [HttpGet("{questionId}/description")]
    public async Task<ActionResult<string>> GenerateQuestionDescription(int questionId)
    {
        try
        {
            string description = await _wordQuestionService.GenerateQuestionDescriptionAsync(questionId);
            return Ok(new { description });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成Word题目描述失败，题目ID: {QuestionId}", questionId);
            return StatusCode(500, new { message = "生成题目描述失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 导出Word题目到Excel
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <param name="enabledOnly">是否仅导出启用的题目</param>
    /// <returns>Excel文件</returns>
    [HttpGet("export")]
    public async Task<ActionResult> ExportQuestions([FromQuery] int subjectId, [FromQuery] bool enabledOnly = false)
    {
        try
        {
            if (subjectId <= 0)
            {
                return BadRequest(new { message = "科目ID无效" });
            }

            byte[] excelData = await _wordQuestionExcelService.ExportWordQuestionsToExcelAsync(subjectId, enabledOnly);
            string fileName = $"WordQuestions_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出Word题目失败，科目ID: {SubjectId}", subjectId);
            return StatusCode(500, new { message = "导出失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 下载导入模板
    /// </summary>
    /// <returns>Excel模板文件</returns>
    [HttpGet("import-template")]
    public ActionResult DownloadImportTemplate()
    {
        try
        {
            byte[] templateData = _wordQuestionExcelService.GenerateImportTemplate();
            string fileName = $"WordQuestions_ImportTemplate_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(templateData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成导入模板失败");
            return StatusCode(500, new { message = "生成模板失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 从Excel文件导入Word题目
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <param name="file">Excel文件</param>
    /// <returns>导入结果</returns>
    [HttpPost("import")]
    public async Task<ActionResult> ImportQuestions([FromQuery] int subjectId, IFormFile file)
    {
        try
        {
            if (subjectId <= 0)
            {
                return BadRequest(new { message = "科目ID无效" });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "请选择要导入的Excel文件" });
            }

            // 验证文件类型
            string[] allowedExtensions = [".xlsx", ".xls"];
            string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = "仅支持Excel文件格式（.xlsx, .xls）" });
            }

            // 验证文件大小（限制为10MB）
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { message = "文件大小不能超过10MB" });
            }

            using Stream fileStream = file.OpenReadStream();
            WordQuestionExcelService.ImportResult result = await _wordQuestionExcelService.ImportWordQuestionsFromExcelAsync(fileStream, subjectId);

            return Ok(new
            {
                success = result.SuccessCount > 0,
                successCount = result.SuccessCount,
                failCount = result.FailCount,
                errors = result.Errors,
                importedQuestions = result.ImportedQuestions.Select(q => new
                {
                    q.Id,
                    q.Title,
                    q.TotalScore,
                    q.IsEnabled,
                    OperationPointsCount = q.OperationPoints.Count
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入Word题目失败，科目ID: {SubjectId}", subjectId);
            return StatusCode(500, new { message = "导入失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 预览Excel文件内容（用于导入前确认）
    /// </summary>
    /// <param name="file">Excel文件</param>
    /// <returns>预览数据</returns>
    [HttpPost("preview")]
    public ActionResult PreviewImportFile(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "请选择要预览的Excel文件" });
            }

            // 验证文件类型
            string[] allowedExtensions = [".xlsx", ".xls"];
            string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = "仅支持Excel文件格式（.xlsx, .xls）" });
            }

            // 验证文件大小
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { message = "文件大小不能超过10MB" });
            }

            // 这里可以添加预览逻辑，暂时返回基本信息
            return Ok(new
            {
                fileName = file.FileName,
                fileSize = file.Length,
                message = "文件格式正确，可以进行导入"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预览Excel文件失败");
            return StatusCode(500, new { message = "预览失败，请稍后重试" });
        }
    }
}
