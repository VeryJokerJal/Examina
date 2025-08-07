using System.ComponentModel.DataAnnotations;
using ExaminaWebApplication.Models.Exam;
using ExaminaWebApplication.Services.Exam;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 试卷管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExamController : ControllerBase
{
    private readonly ExamService _examService;
    private readonly ExamSubjectService _examSubjectService;
    private readonly ExamQuestionService _examQuestionService;

    public ExamController(
        ExamService examService,
        ExamSubjectService examSubjectService,
        ExamQuestionService examQuestionService)
    {
        _examService = examService;
        _examSubjectService = examSubjectService;
        _examQuestionService = examQuestionService;
    }

    /// <summary>
    /// 获取所有试卷
    /// </summary>
    /// <param name="includeDetails">是否包含详细信息</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<List<Models.Exam.Exam>>> GetAllExams([FromQuery] bool includeDetails = false)
    {
        List<Models.Exam.Exam> exams = await _examService.GetAllExamsAsync(includeDetails);
        return Ok(exams);
    }

    /// <summary>
    /// 根据状态获取试卷
    /// </summary>
    /// <param name="status">试卷状态</param>
    /// <param name="includeDetails">是否包含详细信息</param>
    /// <returns></returns>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<List<Models.Exam.Exam>>> GetExamsByStatus(
        ExamStatus status,
        [FromQuery] bool includeDetails = false)
    {
        List<Models.Exam.Exam> exams = await _examService.GetExamsByStatusAsync(status, includeDetails);
        return Ok(exams);
    }

    /// <summary>
    /// 根据创建者获取试卷
    /// </summary>
    /// <param name="createdBy">创建者ID</param>
    /// <param name="includeDetails">是否包含详细信息</param>
    /// <returns></returns>
    [HttpGet("creator/{createdBy}")]
    public async Task<ActionResult<List<Models.Exam.Exam>>> GetExamsByCreator(
        int createdBy,
        [FromQuery] bool includeDetails = false)
    {
        List<Models.Exam.Exam> exams = await _examService.GetExamsByCreatorAsync(createdBy, includeDetails);
        return Ok(exams);
    }

    /// <summary>
    /// 根据ID获取试卷详情
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Models.Exam.Exam>> GetExamById(int id)
    {
        Models.Exam.Exam? exam = await _examService.GetExamByIdAsync(id);
        return exam == null ? (ActionResult<Exam>)NotFound($"试卷 ID {id} 不存在") : (ActionResult<Exam>)Ok(exam);
    }

    /// <summary>
    /// 创建新试卷
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<Models.Exam.Exam>> CreateExam([FromBody] CreateExamRequest request)
    {
        try
        {
            Models.Exam.Exam exam = new()
            {
                Name = request.Name,
                Description = request.Description,
                ExamType = request.ExamType,
                TotalScore = request.TotalScore,
                DurationMinutes = request.DurationMinutes,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                AllowRetake = request.AllowRetake,
                MaxRetakeCount = request.MaxRetakeCount,
                PassingScore = request.PassingScore,
                RandomizeQuestions = request.RandomizeQuestions,
                ShowScore = request.ShowScore,
                ShowAnswers = request.ShowAnswers,
                CreatedBy = request.CreatedBy,
                Tags = request.Tags,
                ExtendedConfig = request.ExtendedConfig
            };

            Models.Exam.Exam createdExam = await _examService.CreateExamAsync(exam);

            // 如果请求创建标准科目结构
            if (request.CreateStandardSubjects)
            {
                _ = await _examSubjectService.CreateStandardSubjectsAsync(createdExam.Id, request.SubjectsConfig);
            }

            return CreatedAtAction(nameof(GetExamById), new { id = createdExam.Id }, createdExam);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 更新试卷信息
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <param name="request">更新请求</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<Models.Exam.Exam>> UpdateExam(int id, [FromBody] UpdateExamRequest request)
    {
        try
        {
            Models.Exam.Exam exam = new()
            {
                Id = id,
                Name = request.Name,
                Description = request.Description,
                ExamType = request.ExamType,
                TotalScore = request.TotalScore,
                DurationMinutes = request.DurationMinutes,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                AllowRetake = request.AllowRetake,
                MaxRetakeCount = request.MaxRetakeCount,
                PassingScore = request.PassingScore,
                RandomizeQuestions = request.RandomizeQuestions,
                ShowScore = request.ShowScore,
                ShowAnswers = request.ShowAnswers,
                Tags = request.Tags,
                ExtendedConfig = request.ExtendedConfig
            };

            Models.Exam.Exam updatedExam = await _examService.UpdateExamAsync(exam);
            return Ok(updatedExam);
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
    /// 删除试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteExam(int id)
    {
        try
        {
            bool deleted = await _examService.DeleteExamAsync(id);
            return !deleted ? NotFound($"试卷 ID {id} 不存在") : NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 发布试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <param name="request">发布请求</param>
    /// <returns></returns>
    [HttpPost("{id}/publish")]
    public async Task<ActionResult<Models.Exam.Exam>> PublishExam(int id, [FromBody] PublishExamRequest request)
    {
        try
        {
            Models.Exam.Exam publishedExam = await _examService.PublishExamAsync(id, request.PublishedBy);
            return Ok(publishedExam);
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
    /// 验证试卷是否可以发布
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns></returns>
    [HttpPost("{id}/validate")]
    public async Task<ActionResult<ExamValidationResult>> ValidateExam(int id)
    {
        Models.Exam.Exam? exam = await _examService.GetExamByIdAsync(id);
        if (exam == null)
        {
            return NotFound($"试卷 ID {id} 不存在");
        }

        ExamValidationResult result = await _examService.ValidateExamForPublishAsync(exam);
        return Ok(result);
    }

    /// <summary>
    /// 获取试卷统计信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<ExamStatistics>> GetExamStatistics()
    {
        ExamStatistics statistics = await _examService.GetExamStatisticsAsync();
        return Ok(statistics);
    }

    /// <summary>
    /// 导出试卷到Excel文件
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>Excel文件</returns>
    [HttpGet("{id}/export")]
    public async Task<ActionResult> ExportExam(int id)
    {
        try
        {
            Models.Exam.Exam? exam = await _examService.GetExamByIdAsync(id);
            if (exam == null)
            {
                return NotFound($"试卷 ID {id} 不存在");
            }

            byte[] excelData = await _examService.ExportExamToExcelAsync(id);
            string fileName = $"试卷_{exam.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "导出失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 复制试卷
    /// </summary>
    /// <param name="id">源试卷ID</param>
    /// <param name="request">复制请求</param>
    /// <returns></returns>
    [HttpPost("{id}/copy")]
    public async Task<ActionResult<Models.Exam.Exam>> CopyExam(int id, [FromBody] CopyExamRequest request)
    {
        try
        {
            Models.Exam.Exam? sourceExam = await _examService.GetExamByIdAsync(id);
            if (sourceExam == null)
            {
                return NotFound($"源试卷 ID {id} 不存在");
            }

            // 创建新试卷
            Models.Exam.Exam newExam = new()
            {
                Name = request.NewName,
                Description = sourceExam.Description,
                ExamType = sourceExam.ExamType,
                TotalScore = sourceExam.TotalScore,
                DurationMinutes = sourceExam.DurationMinutes,
                AllowRetake = sourceExam.AllowRetake,
                MaxRetakeCount = sourceExam.MaxRetakeCount,
                PassingScore = sourceExam.PassingScore,
                RandomizeQuestions = sourceExam.RandomizeQuestions,
                ShowScore = sourceExam.ShowScore,
                ShowAnswers = sourceExam.ShowAnswers,
                CreatedBy = request.CreatedBy,
                Tags = sourceExam.Tags,
                ExtendedConfig = sourceExam.ExtendedConfig
            };

            Models.Exam.Exam createdExam = await _examService.CreateExamAsync(newExam);

            // TODO: 复制科目和题目（需要在后续实现）

            return CreatedAtAction(nameof(GetExamById), new { id = createdExam.Id }, createdExam);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

/// <summary>
/// 创建试卷请求
/// </summary>
public class CreateExamRequest
{
    /// <summary>
    /// 试卷名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 试卷类型
    /// </summary>
    public ExamType ExamType { get; set; } = ExamType.UnifiedExam;

    /// <summary>
    /// 总分
    /// </summary>
    [Range(0.1, 9999.99)]
    public decimal TotalScore { get; set; } = 100.0m;

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 考试开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重考
    /// </summary>
    public bool AllowRetake { get; set; } = false;

    /// <summary>
    /// 最大重考次数
    /// </summary>
    public int MaxRetakeCount { get; set; } = 0;

    /// <summary>
    /// 及格分数
    /// </summary>
    [Range(0.1, 9999.99)]
    public decimal PassingScore { get; set; } = 60.0m;

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 是否显示分数
    /// </summary>
    public bool ShowScore { get; set; } = true;

    /// <summary>
    /// 是否显示答案
    /// </summary>
    public bool ShowAnswers { get; set; } = false;

    /// <summary>
    /// 创建者ID
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// 试卷标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 扩展配置
    /// </summary>
    public string? ExtendedConfig { get; set; }

    /// <summary>
    /// 是否创建标准科目结构
    /// </summary>
    public bool CreateStandardSubjects { get; set; } = true;

    /// <summary>
    /// 科目配置
    /// </summary>
    public StandardSubjectsConfig? SubjectsConfig { get; set; }
}

/// <summary>
/// 更新试卷请求
/// </summary>
public class UpdateExamRequest
{
    /// <summary>
    /// 试卷名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 试卷类型
    /// </summary>
    public ExamType ExamType { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    public int TotalScore { get; set; }

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// 考试开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重考
    /// </summary>
    public bool AllowRetake { get; set; }

    /// <summary>
    /// 最大重考次数
    /// </summary>
    public int MaxRetakeCount { get; set; }

    /// <summary>
    /// 及格分数
    /// </summary>
    public int PassingScore { get; set; }

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 是否显示分数
    /// </summary>
    public bool ShowScore { get; set; } = true;

    /// <summary>
    /// 是否显示答案
    /// </summary>
    public bool ShowAnswers { get; set; } = false;

    /// <summary>
    /// 试卷标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 扩展配置
    /// </summary>
    public string? ExtendedConfig { get; set; }
}

/// <summary>
/// 发布试卷请求
/// </summary>
public class PublishExamRequest
{
    /// <summary>
    /// 发布者ID
    /// </summary>
    public int PublishedBy { get; set; }
}

/// <summary>
/// 复制试卷请求
/// </summary>
public class CopyExamRequest
{
    /// <summary>
    /// 新试卷名称
    /// </summary>
    public string NewName { get; set; } = string.Empty;

    /// <summary>
    /// 创建者ID
    /// </summary>
    public int CreatedBy { get; set; }
}
