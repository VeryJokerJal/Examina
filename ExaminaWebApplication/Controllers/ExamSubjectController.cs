using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Models.Exam;
using ExaminaWebApplication.Services.Exam;
using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 试卷科目管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExamSubjectController : ControllerBase
{
    private readonly ExamSubjectService _examSubjectService;
    private readonly ExamService _examService;

    public ExamSubjectController(ExamSubjectService examSubjectService, ExamService examService)
    {
        _examSubjectService = examSubjectService;
        _examService = examService;
    }

    /// <summary>
    /// 获取试卷的所有科目
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <returns></returns>
    [HttpGet("exam/{examId}")]
    public async Task<ActionResult<List<ExamSubject>>> GetExamSubjects(int examId)
    {
        List<ExamSubject> subjects = await _examSubjectService.GetExamSubjectsAsync(examId);
        return Ok(subjects);
    }

    /// <summary>
    /// 根据ID获取科目详情
    /// </summary>
    /// <param name="id">科目ID</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ExamSubject>> GetSubjectById(int id)
    {
        ExamSubject? subject = await _examSubjectService.GetSubjectByIdAsync(id);
        if (subject == null)
        {
            return NotFound($"科目 ID {id} 不存在");
        }
        return Ok(subject);
    }

    /// <summary>
    /// 创建科目
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<ExamSubject>> CreateSubject([FromBody] CreateSubjectRequest request)
    {
        try
        {
            ExamSubject subject = new ExamSubject
            {
                ExamId = request.ExamId,
                SubjectType = request.SubjectType,
                SubjectName = request.SubjectName,
                Description = request.Description,
                Score = request.Score,
                DurationMinutes = request.DurationMinutes,
                IsRequired = request.IsRequired,
                MinScore = request.MinScore,
                Weight = request.Weight,
                SubjectConfig = request.SubjectConfig
            };

            ExamSubject createdSubject = await _examSubjectService.CreateSubjectAsync(subject);
            return CreatedAtAction(nameof(GetSubjectById), new { id = createdSubject.Id }, createdSubject);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 更新科目信息
    /// </summary>
    /// <param name="id">科目ID</param>
    /// <param name="request">更新请求</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ExamSubject>> UpdateSubject(int id, [FromBody] UpdateSubjectRequest request)
    {
        try
        {
            ExamSubject subject = new ExamSubject
            {
                Id = id,
                SubjectName = request.SubjectName,
                Description = request.Description,
                Score = request.Score,
                DurationMinutes = request.DurationMinutes,
                IsRequired = request.IsRequired,
                MinScore = request.MinScore,
                Weight = request.Weight,
                SubjectConfig = request.SubjectConfig
            };

            ExamSubject updatedSubject = await _examSubjectService.UpdateSubjectAsync(subject);
            return Ok(updatedSubject);
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
    /// 删除科目
    /// </summary>
    /// <param name="id">科目ID</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSubject(int id)
    {
        try
        {
            bool deleted = await _examSubjectService.DeleteSubjectAsync(id);
            if (!deleted)
            {
                return NotFound(new { success = false, message = $"科目 ID {id} 不存在" });
            }
            return Ok(new { success = true, message = "科目删除成功" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 切换科目状态
    /// </summary>
    /// <param name="id">科目ID</param>
    /// <returns></returns>
    [HttpPost("{id}/toggle-status")]
    public async Task<ActionResult> ToggleSubjectStatus(int id)
    {
        try
        {
            ExamSubject? subject = await _examSubjectService.GetSubjectByIdAsync(id);
            if (subject == null)
            {
                return NotFound(new { success = false, message = $"科目 ID {id} 不存在" });
            }

            subject.IsEnabled = !subject.IsEnabled;
            await _examSubjectService.UpdateSubjectAsync(subject);

            return Ok(new {
                success = true,
                message = $"科目已{(subject.IsEnabled ? "启用" : "禁用")}",
                isEnabled = subject.IsEnabled
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 调整科目顺序
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <param name="request">顺序调整请求</param>
    /// <returns></returns>
    [HttpPost("exam/{examId}/reorder")]
    public async Task<ActionResult> ReorderSubjects(int examId, [FromBody] ReorderSubjectsRequest request)
    {
        try
        {
            await _examSubjectService.ReorderSubjectsAsync(examId, request.SubjectOrders);
            return Ok(new { success = true, message = "科目顺序调整成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 自动平衡科目分值
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <returns></returns>
    [HttpPost("auto-balance-scores/{examId}")]
    public async Task<ActionResult> AutoBalanceScores(int examId)
    {
        try
        {
            // 获取试卷信息
            var exam = await _examService.GetExamByIdAsync(examId);
            if (exam == null)
            {
                return NotFound(new { success = false, message = $"试卷 ID {examId} 不存在" });
            }

            // 获取启用的科目
            var subjects = await _examSubjectService.GetExamSubjectsAsync(examId);
            var enabledSubjects = subjects.Where(s => s.IsEnabled).ToList();

            if (enabledSubjects.Count == 0)
            {
                return BadRequest(new { success = false, message = "没有启用的科目可以分配分值" });
            }

            // 平均分配分值
            decimal averageScore = exam.TotalScore / enabledSubjects.Count;
            decimal remainder = exam.TotalScore % enabledSubjects.Count;

            for (int i = 0; i < enabledSubjects.Count; i++)
            {
                enabledSubjects[i].Score = averageScore + (i < remainder ? 0.1m : 0);
                await _examSubjectService.UpdateSubjectAsync(enabledSubjects[i]);
            }

            return Ok(new {
                success = true,
                message = $"已为 {enabledSubjects.Count} 个科目自动分配分值",
                totalScore = exam.TotalScore,
                subjectCount = enabledSubjects.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 创建标准五科目结构
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <param name="config">科目配置</param>
    /// <returns></returns>
    [HttpPost("create-standard-subjects/{examId}")]
    public async Task<ActionResult> CreateStandardSubjects(
        int examId,
        [FromBody] StandardSubjectsConfig? config = null)
    {
        try
        {
            List<ExamSubject> subjects = await _examSubjectService.CreateStandardSubjectsAsync(examId, config);
            return Ok(new {
                success = true,
                message = $"成功创建 {subjects.Count} 个标准科目",
                subjects = subjects.Select(s => new { s.Id, s.SubjectName, s.SubjectType, s.Score }).ToList()
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 获取科目统计信息
    /// </summary>
    /// <param name="id">科目ID</param>
    /// <returns></returns>
    [HttpGet("{id}/statistics")]
    public async Task<ActionResult<SubjectStatistics>> GetSubjectStatistics(int id)
    {
        try
        {
            SubjectStatistics statistics = await _examSubjectService.GetSubjectStatisticsAsync(id);
            return Ok(statistics);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }
}

/// <summary>
/// 创建科目请求
/// </summary>
public class CreateSubjectRequest
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    public int ExamId { get; set; }

    /// <summary>
    /// 科目类型
    /// </summary>
    public SubjectType SubjectType { get; set; }

    /// <summary>
    /// 科目名称
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// 科目描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 科目分值
    /// </summary>
    [Range(0.1, 1000.0)]
    public decimal Score { get; set; } = 20.0m;

    /// <summary>
    /// 科目考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 30;

    /// <summary>
    /// 是否必考科目
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 最低分数要求
    /// </summary>
    public decimal? MinScore { get; set; }

    /// <summary>
    /// 科目权重
    /// </summary>
    public decimal Weight { get; set; } = 1.0m;

    /// <summary>
    /// 科目配置
    /// </summary>
    public string? SubjectConfig { get; set; }
}

/// <summary>
/// 更新科目请求
/// </summary>
public class UpdateSubjectRequest
{
    /// <summary>
    /// 科目名称
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// 科目描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 科目分值
    /// </summary>
    [Range(0.1, 1000.0)]
    public decimal Score { get; set; }

    /// <summary>
    /// 科目考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// 是否必考科目
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 最低分数要求
    /// </summary>
    public decimal? MinScore { get; set; }

    /// <summary>
    /// 科目权重
    /// </summary>
    public decimal Weight { get; set; }

    /// <summary>
    /// 科目配置
    /// </summary>
    public string? SubjectConfig { get; set; }
}

/// <summary>
/// 调整科目顺序请求
/// </summary>
public class ReorderSubjectsRequest
{
    /// <summary>
    /// 科目顺序列表
    /// </summary>
    public List<SubjectOrderItem> SubjectOrders { get; set; } = new List<SubjectOrderItem>();
}
