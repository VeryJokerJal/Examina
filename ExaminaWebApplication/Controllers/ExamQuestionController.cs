using System.ComponentModel.DataAnnotations;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Exam;
using ExaminaWebApplication.Services.Exam;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 试卷题目管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExamQuestionController : ControllerBase
{
    private readonly ExamQuestionService _examQuestionService;
    private readonly ApplicationDbContext _context;

    public ExamQuestionController(ExamQuestionService examQuestionService, ApplicationDbContext context)
    {
        _examQuestionService = examQuestionService;
        _context = context;
    }

    /// <summary>
    /// 获取试卷的所有题目
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <returns></returns>
    [HttpGet("exam/{examId}")]
    public async Task<ActionResult<List<ExamQuestion>>> GetExamQuestions(int examId)
    {
        List<ExamQuestion> questions = await _examQuestionService.GetExamQuestionsAsync(examId);
        return Ok(questions);
    }

    /// <summary>
    /// 获取科目的所有题目
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    [HttpGet("subject/{subjectId}")]
    public async Task<ActionResult<List<ExamQuestion>>> GetSubjectQuestions(int subjectId)
    {
        List<ExamQuestion> questions = await _examQuestionService.GetSubjectQuestionsAsync(subjectId);
        return Ok(questions);
    }

    /// <summary>
    /// 根据ID获取题目详情
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ExamQuestion>> GetQuestionById(int id)
    {
        ExamQuestion? question = await _examQuestionService.GetQuestionByIdAsync(id);
        return question == null ? (ActionResult<ExamQuestion>)NotFound($"题目 ID {id} 不存在") : (ActionResult<ExamQuestion>)Ok(question);
    }

    /// <summary>
    /// 从Excel操作点创建题目
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns></returns>
    [HttpPost("excel-operation")]
    public async Task<ActionResult<ExamQuestion>> CreateQuestionFromExcelOperation([FromBody] CreateExcelQuestionRequest request)
    {
        try
        {
            ExamQuestion question = await _examQuestionService.CreateQuestionFromExcelOperationAsync(request);
            return CreatedAtAction(nameof(GetQuestionById), new { id = question.Id }, question);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 从Excel题目模板创建题目
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns></returns>
    [HttpPost("excel-template")]
    public async Task<ActionResult<ExamQuestion>> CreateQuestionFromExcelTemplate([FromBody] CreateExcelTemplateQuestionRequest request)
    {
        try
        {
            ExamQuestion question = await _examQuestionService.CreateQuestionFromExcelTemplateAsync(request);
            return CreatedAtAction(nameof(GetQuestionById), new { id = question.Id }, question);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 更新题目信息
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <param name="request">更新请求</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ExamQuestion>> UpdateQuestion(int id, [FromBody] UpdateQuestionRequest request)
    {
        try
        {
            ExamQuestion question = new()
            {
                Id = id,
                Title = request.Title,
                Content = request.Content,
                Score = request.Score,
                DifficultyLevel = request.DifficultyLevel,
                EstimatedMinutes = request.EstimatedMinutes,
                IsRequired = request.IsRequired,
                QuestionConfig = request.QuestionConfig,
                AnswerValidationRules = request.AnswerValidationRules,
                StandardAnswer = request.StandardAnswer,
                ScoringRules = request.ScoringRules,
                Tags = request.Tags,
                Remarks = request.Remarks
            };

            ExamQuestion updatedQuestion = await _examQuestionService.UpdateQuestionAsync(question);
            return Ok(updatedQuestion);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 删除题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteQuestion(int id)
    {
        try
        {
            bool deleted = await _examQuestionService.DeleteQuestionAsync(id);
            return !deleted ? NotFound($"题目 ID {id} 不存在") : NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 调整题目顺序
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <param name="request">顺序调整请求</param>
    /// <returns></returns>
    [HttpPost("subject/{subjectId}/reorder")]
    public async Task<ActionResult> ReorderQuestions(int subjectId, [FromBody] ReorderQuestionsRequest request)
    {
        await _examQuestionService.ReorderQuestionsAsync(subjectId, request.QuestionOrders);
        return Ok();
    }

    /// <summary>
    /// 批量从Excel操作点创建题目
    /// </summary>
    /// <param name="request">批量创建请求</param>
    /// <returns></returns>
    [HttpPost("batch-excel-operations")]
    public async Task<ActionResult<List<ExamQuestion>>> BatchCreateQuestionsFromExcelOperations([FromBody] BatchCreateExcelQuestionsRequest request)
    {
        try
        {
            List<ExamQuestion> questions = [];

            foreach (CreateExcelQuestionRequest questionRequest in request.Questions)
            {
                ExamQuestion question = await _examQuestionService.CreateQuestionFromExcelOperationAsync(questionRequest);
                questions.Add(question);
            }

            return Ok(questions);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 预览Excel操作点题目
    /// </summary>
    /// <param name="request">预览请求</param>
    /// <returns></returns>
    [HttpPost("preview-excel-operation")]
    public ActionResult<object> PreviewExcelOperationQuestion([FromBody] PreviewExcelQuestionRequest request)
    {
        try
        {
            // 这里可以调用Excel操作服务来生成预览内容
            // 暂时返回基本信息
            object preview = new
            {
                request.OperationNumber,
                request.Parameters,
                EstimatedContent = $"Excel操作点 {request.OperationNumber} 的题目预览",
                EstimatedDifficulty = request.DifficultyLevel,
                EstimatedScore = request.Score
            };

            return Ok(preview);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 批量更新题目分值
    /// </summary>
    /// <param name="request">批量更新分值请求</param>
    /// <returns></returns>
    [HttpPost("batch-update-score")]
    public async Task<ActionResult> BatchUpdateScore([FromBody] BatchUpdateScoreRequest request)
    {
        try
        {
            if (request.QuestionIds == null || request.QuestionIds.Count == 0)
            {
                return BadRequest(new { success = false, message = "请选择要更新的题目" });
            }

            if (request.NewScore < 0.1m || request.NewScore > 100.0m)
            {
                return BadRequest(new { success = false, message = "分值范围应在0.1-100.0之间" });
            }

            // 获取要更新的题目
            List<ExamQuestion> questions = await _context.ExamQuestions
                .Where(q => request.QuestionIds.Contains(q.Id))
                .ToListAsync();

            if (questions.Count == 0)
            {
                return BadRequest(new { success = false, message = "未找到要更新的题目" });
            }

            // 检查题目所属试卷的状态
            var examIds = questions.Select(q => q.ExamId).Distinct().ToList();
            var exams = await _context.Exams
                .Where(e => examIds.Contains(e.Id))
                .ToListAsync();

            var publishedExams = exams.Where(e => e.Status == ExamStatus.Published || e.Status == ExamStatus.InProgress).ToList();
            if (publishedExams.Any())
            {
                return BadRequest(new { success = false, message = "已发布或进行中的试卷题目不能修改分值" });
            }

            // 批量更新分值
            foreach (var question in questions)
            {
                question.Score = request.NewScore;
                question.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"成功更新 {questions.Count} 道题目的分值" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "批量更新分值失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 批量启用题目
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns></returns>
    [HttpPost("batch-enable")]
    public async Task<ActionResult> BatchEnableQuestions([FromBody] BatchOperationRequest request)
    {
        try
        {
            if (request.QuestionIds == null || request.QuestionIds.Count == 0)
            {
                return BadRequest(new { success = false, message = "请选择要启用的题目" });
            }

            // 获取要更新的题目
            List<ExamQuestion> questions = await _context.ExamQuestions
                .Where(q => request.QuestionIds.Contains(q.Id))
                .ToListAsync();

            if (questions.Count == 0)
            {
                return BadRequest(new { success = false, message = "未找到要启用的题目" });
            }

            // 检查题目所属试卷的状态
            var examIds = questions.Select(q => q.ExamId).Distinct().ToList();
            var exams = await _context.Exams
                .Where(e => examIds.Contains(e.Id))
                .ToListAsync();

            var publishedExams = exams.Where(e => e.Status == ExamStatus.Published || e.Status == ExamStatus.InProgress).ToList();
            if (publishedExams.Any())
            {
                return BadRequest(new { success = false, message = "已发布或进行中的试卷题目不能修改状态" });
            }

            // 批量启用题目
            foreach (var question in questions)
            {
                question.IsEnabled = true;
                question.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"成功启用 {questions.Count} 道题目" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "批量启用失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 批量禁用题目
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns></returns>
    [HttpPost("batch-disable")]
    public async Task<ActionResult> BatchDisableQuestions([FromBody] BatchOperationRequest request)
    {
        try
        {
            if (request.QuestionIds == null || request.QuestionIds.Count == 0)
            {
                return BadRequest(new { success = false, message = "请选择要禁用的题目" });
            }

            // 获取要更新的题目
            List<ExamQuestion> questions = await _context.ExamQuestions
                .Where(q => request.QuestionIds.Contains(q.Id))
                .ToListAsync();

            if (questions.Count == 0)
            {
                return BadRequest(new { success = false, message = "未找到要禁用的题目" });
            }

            // 检查题目所属试卷的状态
            var examIds = questions.Select(q => q.ExamId).Distinct().ToList();
            var exams = await _context.Exams
                .Where(e => examIds.Contains(e.Id))
                .ToListAsync();

            var publishedExams = exams.Where(e => e.Status == ExamStatus.Published || e.Status == ExamStatus.InProgress).ToList();
            if (publishedExams.Any())
            {
                return BadRequest(new { success = false, message = "已发布或进行中的试卷题目不能修改状态" });
            }

            // 批量禁用题目
            foreach (var question in questions)
            {
                question.IsEnabled = false;
                question.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"成功禁用 {questions.Count} 道题目" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "批量禁用失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 批量删除题目
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns></returns>
    [HttpPost("batch-delete")]
    public async Task<ActionResult> BatchDeleteQuestions([FromBody] BatchOperationRequest request)
    {
        try
        {
            if (request.QuestionIds == null || request.QuestionIds.Count == 0)
            {
                return BadRequest(new { success = false, message = "请选择要删除的题目" });
            }

            // 获取要删除的题目
            List<ExamQuestion> questions = await _context.ExamQuestions
                .Where(q => request.QuestionIds.Contains(q.Id))
                .ToListAsync();

            if (questions.Count == 0)
            {
                return BadRequest(new { success = false, message = "未找到要删除的题目" });
            }

            // 检查题目所属试卷的状态
            var examIds = questions.Select(q => q.ExamId).Distinct().ToList();
            var exams = await _context.Exams
                .Where(e => examIds.Contains(e.Id))
                .ToListAsync();

            var publishedExams = exams.Where(e => e.Status == ExamStatus.Published || e.Status == ExamStatus.InProgress).ToList();
            if (publishedExams.Any())
            {
                return BadRequest(new { success = false, message = "已发布或进行中的试卷题目不能删除" });
            }

            // 批量删除题目
            _context.ExamQuestions.RemoveRange(questions);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"成功删除 {questions.Count} 道题目" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "批量删除失败，请稍后重试" });
        }
    }
}

/// <summary>
/// 更新题目请求
/// </summary>
public class UpdateQuestionRequest
{
    /// <summary>
    /// 题目标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值
    /// </summary>
    [Range(0.1, 100.0)]
    public decimal Score { get; set; }

    /// <summary>
    /// 难度级别
    /// </summary>
    public int DifficultyLevel { get; set; }

    /// <summary>
    /// 预计完成时间
    /// </summary>
    public int EstimatedMinutes { get; set; }

    /// <summary>
    /// 是否必答
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 题目配置
    /// </summary>
    public string? QuestionConfig { get; set; }

    /// <summary>
    /// 答案验证规则
    /// </summary>
    public string? AnswerValidationRules { get; set; }

    /// <summary>
    /// 标准答案
    /// </summary>
    public string? StandardAnswer { get; set; }

    /// <summary>
    /// 评分规则
    /// </summary>
    public string? ScoringRules { get; set; }

    /// <summary>
    /// 题目标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 题目备注
    /// </summary>
    public string? Remarks { get; set; }
}

/// <summary>
/// 调整题目顺序请求
/// </summary>
public class ReorderQuestionsRequest
{
    /// <summary>
    /// 题目顺序列表
    /// </summary>
    public List<QuestionOrderItem> QuestionOrders { get; set; } = [];
}

/// <summary>
/// 批量创建Excel题目请求
/// </summary>
public class BatchCreateExcelQuestionsRequest
{
    /// <summary>
    /// 题目列表
    /// </summary>
    public List<CreateExcelQuestionRequest> Questions { get; set; } = [];
}

/// <summary>
/// 预览Excel题目请求
/// </summary>
public class PreviewExcelQuestionRequest
{
    /// <summary>
    /// Excel操作点编号
    /// </summary>
    public int OperationNumber { get; set; }

    /// <summary>
    /// 题目参数
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = [];

    /// <summary>
    /// 题目分值
    /// </summary>
    [Range(0.1, 100.0)]
    public decimal Score { get; set; } = 10.0m;

    /// <summary>
    /// 难度级别
    /// </summary>
    public int DifficultyLevel { get; set; } = 1;
}

/// <summary>
/// 批量更新分值请求
/// </summary>
public class BatchUpdateScoreRequest
{
    /// <summary>
    /// 题目ID列表
    /// </summary>
    public List<int> QuestionIds { get; set; } = new();

    /// <summary>
    /// 新的分值
    /// </summary>
    [Range(0.1, 100.0)]
    public decimal NewScore { get; set; }
}

/// <summary>
/// 批量操作请求
/// </summary>
public class BatchOperationRequest
{
    /// <summary>
    /// 题目ID列表
    /// </summary>
    public List<int> QuestionIds { get; set; } = new();
}
