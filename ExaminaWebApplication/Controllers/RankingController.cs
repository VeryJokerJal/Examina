using ExaminaWebApplication.Models.Ranking;
using ExaminaWebApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 排行榜控制器
/// </summary>
public class RankingController : Controller
{
    private readonly RankingService _rankingService;
    private readonly ILogger<RankingController> _logger;

    public RankingController(RankingService rankingService, ILogger<RankingController> logger)
    {
        _rankingService = rankingService;
        _logger = logger;
    }

    /// <summary>
    /// 排行榜首页
    /// </summary>
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// 上机统考排行榜页面
    /// </summary>
    [AllowAnonymous]
    public async Task<IActionResult> ExamRanking(int page = 1, int pageSize = 50)
    {
        try
        {
            RankingQueryDto query = new()
            {
                Type = RankingType.ExamRanking,
                Page = page,
                PageSize = pageSize
            };

            RankingResponseDto ranking = await _rankingService.GetRankingAsync(query);
            return View("RankingView", ranking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上机统考排行榜失败");
            TempData["ErrorMessage"] = "获取排行榜数据失败，请稍后重试";
            return View("RankingView", new RankingResponseDto
            {
                Type = RankingType.ExamRanking,
                TypeName = "上机统考排行榜"
            });
        }
    }

    /// <summary>
    /// 模拟考试排行榜页面
    /// </summary>
    [AllowAnonymous]
    public async Task<IActionResult> MockExamRanking(int page = 1, int pageSize = 50)
    {
        try
        {
            RankingQueryDto query = new()
            {
                Type = RankingType.MockExamRanking,
                Page = page,
                PageSize = pageSize
            };

            RankingResponseDto ranking = await _rankingService.GetRankingAsync(query);
            return View("RankingView", ranking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模拟考试排行榜失败");
            TempData["ErrorMessage"] = "获取排行榜数据失败，请稍后重试";
            return View("RankingView", new RankingResponseDto
            {
                Type = RankingType.MockExamRanking,
                TypeName = "模拟考试排行榜"
            });
        }
    }

    /// <summary>
    /// 综合实训排行榜页面
    /// </summary>
    [AllowAnonymous]
    public async Task<IActionResult> TrainingRanking(int page = 1, int pageSize = 50)
    {
        try
        {
            RankingQueryDto query = new()
            {
                Type = RankingType.TrainingRanking,
                Page = page,
                PageSize = pageSize
            };

            RankingResponseDto ranking = await _rankingService.GetRankingAsync(query);
            return View("RankingView", ranking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取综合实训排行榜失败");
            TempData["ErrorMessage"] = "获取排行榜数据失败，请稍后重试";
            return View("RankingView", new RankingResponseDto
            {
                Type = RankingType.TrainingRanking,
                TypeName = "综合实训排行榜"
            });
        }
    }
}

/// <summary>
/// 排行榜API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RankingApiController : ControllerBase
{
    private readonly RankingService _rankingService;
    private readonly ILogger<RankingApiController> _logger;

    public RankingApiController(RankingService rankingService, ILogger<RankingApiController> logger)
    {
        _rankingService = rankingService;
        _logger = logger;
    }

    /// <summary>
    /// 获取排行榜数据API
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<RankingResponseDto>> GetRanking([FromQuery] RankingQueryDto query)
    {
        try
        {
            RankingResponseDto ranking = await _rankingService.GetRankingAsync(query);
            return Ok(ranking);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "排行榜查询参数无效: {Query}", query);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取排行榜数据失败: {Query}", query);
            return StatusCode(500, new { message = "获取排行榜数据失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取上机统考排行榜API
    /// </summary>
    [HttpGet("exam")]
    [AllowAnonymous]
    public async Task<ActionResult<RankingResponseDto>> GetExamRanking(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? examId = null)
    {
        try
        {
            RankingQueryDto query = new()
            {
                Type = RankingType.ExamRanking,
                Page = page,
                PageSize = pageSize,
                StartDate = startDate,
                EndDate = endDate,
                ExamId = examId
            };

            RankingResponseDto ranking = await _rankingService.GetRankingAsync(query);
            return Ok(ranking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上机统考排行榜失败");
            return StatusCode(500, new { message = "获取排行榜数据失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取模拟考试排行榜API
    /// </summary>
    [HttpGet("mock-exam")]
    [AllowAnonymous]
    public async Task<ActionResult<RankingResponseDto>> GetMockExamRanking(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? examId = null)
    {
        try
        {
            RankingQueryDto query = new()
            {
                Type = RankingType.MockExamRanking,
                Page = page,
                PageSize = pageSize,
                StartDate = startDate,
                EndDate = endDate,
                ExamId = examId
            };

            RankingResponseDto ranking = await _rankingService.GetRankingAsync(query);
            return Ok(ranking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模拟考试排行榜失败");
            return StatusCode(500, new { message = "获取排行榜数据失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取综合实训排行榜API
    /// </summary>
    [HttpGet("training")]
    [AllowAnonymous]
    public async Task<ActionResult<RankingResponseDto>> GetTrainingRanking(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? examId = null)
    {
        try
        {
            RankingQueryDto query = new()
            {
                Type = RankingType.TrainingRanking,
                Page = page,
                PageSize = pageSize,
                StartDate = startDate,
                EndDate = endDate,
                ExamId = examId
            };

            RankingResponseDto ranking = await _rankingService.GetRankingAsync(query);
            return Ok(ranking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取综合实训排行榜失败");
            return StatusCode(500, new { message = "获取排行榜数据失败，请稍后重试" });
        }
    }
}
