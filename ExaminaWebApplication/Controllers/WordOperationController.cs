using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Services.Word;
using ExaminaWebApplication.Models.Word;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// Word操作点控制器
/// </summary>
[ApiController]
[Route("api/word/operation")]
public class WordOperationController : ControllerBase
{
    private readonly WordOperationService _wordOperationService;
    private readonly ILogger<WordOperationController> _logger;

    public WordOperationController(WordOperationService wordOperationService, ILogger<WordOperationController> logger)
    {
        _wordOperationService = wordOperationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有Word操作点
    /// </summary>
    /// <returns></returns>
    [HttpGet("points")]
    public async Task<ActionResult<List<WordOperationPoint>>> GetAllOperationPoints()
    {
        try
        {
            List<WordOperationPoint> operationPoints = await _wordOperationService.GetAllOperationPointsAsync();
            return Ok(operationPoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Word操作点列表失败");
            return StatusCode(500, new { message = "获取操作点列表失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 根据ID获取Word操作点
    /// </summary>
    /// <param name="id">操作点ID</param>
    /// <returns></returns>
    [HttpGet("points/{id}")]
    public async Task<ActionResult<WordOperationPoint>> GetOperationPointById(int id)
    {
        try
        {
            WordOperationPoint? operationPoint = await _wordOperationService.GetOperationPointByIdAsync(id);
            if (operationPoint == null)
            {
                return NotFound(new { message = "操作点不存在" });
            }
            return Ok(operationPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Word操作点详情失败，ID: {Id}", id);
            return StatusCode(500, new { message = "获取操作点详情失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 根据操作编号获取Word操作点
    /// </summary>
    /// <param name="operationNumber">操作编号</param>
    /// <returns></returns>
    [HttpGet("points/by-number/{operationNumber}")]
    public async Task<ActionResult<WordOperationPoint>> GetOperationPointByNumber(int operationNumber)
    {
        try
        {
            WordOperationPoint? operationPoint = await _wordOperationService.GetOperationPointByNumberAsync(operationNumber);
            if (operationPoint == null)
            {
                return NotFound(new { message = "操作点不存在" });
            }
            return Ok(operationPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据编号获取Word操作点失败，编号: {OperationNumber}", operationNumber);
            return StatusCode(500, new { message = "获取操作点失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 根据类别获取Word操作点
    /// </summary>
    /// <param name="category">操作类别</param>
    /// <returns></returns>
    [HttpGet("points/by-category/{category}")]
    public async Task<ActionResult<List<WordOperationPoint>>> GetOperationPointsByCategory(WordOperationCategory category)
    {
        try
        {
            List<WordOperationPoint> operationPoints = await _wordOperationService.GetOperationPointsByCategoryAsync(category);
            return Ok(operationPoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据类别获取Word操作点失败，类别: {Category}", category);
            return StatusCode(500, new { message = "获取操作点列表失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取所有枚举类型
    /// </summary>
    /// <returns></returns>
    [HttpGet("enum-types")]
    public async Task<ActionResult<List<WordEnumType>>> GetAllEnumTypes()
    {
        try
        {
            List<WordEnumType> enumTypes = await _wordOperationService.GetAllEnumTypesAsync();
            return Ok(enumTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Word枚举类型列表失败");
            return StatusCode(500, new { message = "获取枚举类型列表失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 根据类型名称获取枚举值
    /// </summary>
    /// <param name="typeName">枚举类型名称</param>
    /// <returns></returns>
    [HttpGet("enum-values")]
    public async Task<ActionResult<List<WordEnumValue>>> GetEnumValuesByTypeName([FromQuery] string typeName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return BadRequest(new { message = "枚举类型名称不能为空" });
            }

            List<WordEnumValue> enumValues = await _wordOperationService.GetEnumValuesByTypeNameAsync(typeName);
            return Ok(enumValues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据类型名称获取Word枚举值失败，类型名称: {TypeName}", typeName);
            return StatusCode(500, new { message = "获取枚举值失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 根据类型ID获取枚举值
    /// </summary>
    /// <param name="enumTypeId">枚举类型ID</param>
    /// <returns></returns>
    [HttpGet("enum-values/{enumTypeId}")]
    public async Task<ActionResult<List<WordEnumValue>>> GetEnumValuesByTypeId(int enumTypeId)
    {
        try
        {
            List<WordEnumValue> enumValues = await _wordOperationService.GetEnumValuesByTypeIdAsync(enumTypeId);
            return Ok(enumValues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据类型ID获取Word枚举值失败，类型ID: {EnumTypeId}", enumTypeId);
            return StatusCode(500, new { message = "获取枚举值失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取操作点统计信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetOperationPointStatistics()
    {
        try
        {
            object statistics = await _wordOperationService.GetOperationPointStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Word操作点统计信息失败");
            return StatusCode(500, new { message = "获取统计信息失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 初始化Word操作点数据
    /// </summary>
    /// <returns></returns>
    [HttpPost("initialize")]
    public async Task<ActionResult> InitializeWordOperationData()
    {
        try
        {
            await _wordOperationService.InitializeWordOperationDataAsync();
            return Ok(new { message = "Word操作点数据初始化成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化Word操作点数据失败");
            return StatusCode(500, new { message = "初始化数据失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 强制重新初始化Word操作点数据（包含所有67个操作点）
    /// </summary>
    /// <returns></returns>
    [HttpPost("reinitialize")]
    public async Task<ActionResult> ReinitializeWordOperationData()
    {
        try
        {
            await _wordOperationService.ReinitializeWordOperationDataAsync();
            return Ok(new { message = "Word操作点数据重新初始化成功，已包含所有67个操作点" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新初始化Word操作点数据失败");
            return StatusCode(500, new { message = "重新初始化失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 创建Word操作点
    /// </summary>
    /// <param name="operationPoint">操作点信息</param>
    /// <returns></returns>
    [HttpPost("points")]
    public async Task<ActionResult<WordOperationPoint>> CreateOperationPoint([FromBody] WordOperationPoint operationPoint)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            WordOperationPoint createdOperationPoint = await _wordOperationService.CreateOperationPointAsync(operationPoint);
            return CreatedAtAction(nameof(GetOperationPointById), new { id = createdOperationPoint.Id }, createdOperationPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建Word操作点失败");
            return StatusCode(500, new { message = "创建操作点失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 更新Word操作点
    /// </summary>
    /// <param name="id">操作点ID</param>
    /// <param name="operationPoint">操作点信息</param>
    /// <returns></returns>
    [HttpPut("points/{id}")]
    public async Task<ActionResult> UpdateOperationPoint(int id, [FromBody] WordOperationPoint operationPoint)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != operationPoint.Id)
            {
                return BadRequest(new { message = "ID不匹配" });
            }

            bool success = await _wordOperationService.UpdateOperationPointAsync(operationPoint);
            if (!success)
            {
                return NotFound(new { message = "操作点不存在" });
            }

            return Ok(new { message = "操作点更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新Word操作点失败，ID: {Id}", id);
            return StatusCode(500, new { message = "更新操作点失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 删除Word操作点
    /// </summary>
    /// <param name="id">操作点ID</param>
    /// <returns></returns>
    [HttpDelete("points/{id}")]
    public async Task<ActionResult> DeleteOperationPoint(int id)
    {
        try
        {
            bool success = await _wordOperationService.DeleteOperationPointAsync(id);
            if (!success)
            {
                return NotFound(new { message = "操作点不存在" });
            }

            return Ok(new { message = "操作点删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除Word操作点失败，ID: {Id}", id);
            return StatusCode(500, new { message = "删除操作点失败，请稍后重试" });
        }
    }
}
