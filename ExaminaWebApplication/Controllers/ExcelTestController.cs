using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Services.Excel;
using ExaminaWebApplication.Models.Excel;
// using ExaminaWebApplication.Tests;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// Excel数据库设计测试控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExcelTestController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelOperationService _operationService;
    private readonly ExcelQuestionService _questionService;

    public ExcelTestController(
        ApplicationDbContext context,
        ExcelOperationService operationService,
        ExcelQuestionService questionService)
    {
        _context = context;
        _operationService = operationService;
        _questionService = questionService;
    }

    /// <summary>
    /// 获取Excel操作统计信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<ExcelOperationStatistics>> GetStatistics()
    {
        ExcelOperationStatistics statistics = await _operationService.GetOperationStatisticsAsync();
        return Ok(statistics);
    }

    /// <summary>
    /// 获取所有操作点
    /// </summary>
    /// <returns></returns>
    [HttpGet("operation-points")]
    public async Task<ActionResult<List<ExcelOperationPoint>>> GetAllOperationPoints()
    {
        List<ExcelOperationPoint> operationPoints = await _operationService.GetAllOperationPointsAsync();
        return Ok(operationPoints);
    }

    /// <summary>
    /// 根据分类获取操作点
    /// </summary>
    /// <param name="category">操作分类</param>
    /// <returns></returns>
    [HttpGet("operation-points/category/{category}")]
    public async Task<ActionResult<List<ExcelOperationPoint>>> GetOperationPointsByCategory(ExcelOperationCategory category)
    {
        List<ExcelOperationPoint> operationPoints = await _operationService.GetOperationPointsByCategoryAsync(category);
        return Ok(operationPoints);
    }

    /// <summary>
    /// 获取指定操作点的详细信息
    /// </summary>
    /// <param name="operationNumber">操作点编号</param>
    /// <returns></returns>
    [HttpGet("operation-points/{operationNumber}")]
    public async Task<ActionResult<ExcelOperationPoint>> GetOperationPointByNumber(int operationNumber)
    {
        ExcelOperationPoint? operationPoint = await _operationService.GetOperationPointByNumberAsync(operationNumber);
        if (operationPoint == null)
        {
            return NotFound($"操作点 {operationNumber} 不存在");
        }
        return Ok(operationPoint);
    }

    /// <summary>
    /// 获取所有枚举类型
    /// </summary>
    /// <returns></returns>
    [HttpGet("enum-types")]
    public async Task<ActionResult<List<ExcelEnumType>>> GetAllEnumTypes()
    {
        List<ExcelEnumType> enumTypes = await _operationService.GetAllEnumTypesAsync();
        return Ok(enumTypes);
    }

    /// <summary>
    /// 根据类型名称获取枚举值
    /// </summary>
    /// <param name="typeName">枚举类型名称</param>
    /// <returns></returns>
    [HttpGet("enum-values/{typeName}")]
    public async Task<ActionResult<List<ExcelEnumValue>>> GetEnumValuesByTypeName(string typeName)
    {
        List<ExcelEnumValue> enumValues = await _operationService.GetEnumValuesByTypeNameAsync(typeName);
        return Ok(enumValues);
    }

    /// <summary>
    /// 验证操作点参数
    /// </summary>
    /// <param name="request">验证请求</param>
    /// <returns></returns>
    [HttpPost("validate-parameters")]
    public async Task<ActionResult<ParameterValidationResult>> ValidateParameters([FromBody] ValidateParametersRequest request)
    {
        ParameterValidationResult result = await _operationService.ValidateOperationParametersAsync(
            request.OperationNumber, request.ParameterValues);
        return Ok(result);
    }

    /// <summary>
    /// 获取题目模板列表
    /// </summary>
    /// <param name="operationPointId">操作点ID（可选）</param>
    /// <param name="difficultyLevel">难度级别（可选）</param>
    /// <returns></returns>
    [HttpGet("question-templates")]
    public async Task<ActionResult<List<ExcelQuestionTemplate>>> GetQuestionTemplates(
        [FromQuery] int? operationPointId = null, 
        [FromQuery] int? difficultyLevel = null)
    {
        List<ExcelQuestionTemplate> templates = await _questionService.GetQuestionTemplatesAsync(operationPointId, difficultyLevel);
        return Ok(templates);
    }

    /// <summary>
    /// 创建题目模板
    /// </summary>
    /// <param name="template">题目模板</param>
    /// <returns></returns>
    [HttpPost("question-templates")]
    public async Task<ActionResult<ExcelQuestionTemplate>> CreateQuestionTemplate([FromBody] ExcelQuestionTemplate template)
    {
        try
        {
            ExcelQuestionTemplate createdTemplate = await _questionService.CreateQuestionTemplateAsync(template);
            return CreatedAtAction(nameof(GetQuestionTemplates), new { id = createdTemplate.Id }, createdTemplate);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 生成题目实例
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns></returns>
    [HttpPost("question-instances/generate")]
    public async Task<ActionResult<ExcelQuestionInstance>> GenerateQuestionInstance([FromBody] GenerateQuestionRequest request)
    {
        try
        {
            ExcelQuestionInstance instance = await _questionService.GenerateQuestionInstanceAsync(
                request.TemplateId, request.CustomParameters);
            return Ok(instance);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 批量生成题目实例
    /// </summary>
    /// <param name="request">批量生成请求</param>
    /// <returns></returns>
    [HttpPost("question-instances/batch-generate")]
    public async Task<ActionResult<List<ExcelQuestionInstance>>> BatchGenerateQuestionInstances([FromBody] BatchGenerateQuestionRequest request)
    {
        try
        {
            List<ExcelQuestionInstance> instances = await _questionService.BatchGenerateQuestionInstancesAsync(
                request.TemplateId, request.Count, request.ParameterVariations);
            return Ok(instances);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 获取题目实例列表
    /// </summary>
    /// <param name="status">题目状态（可选）</param>
    /// <param name="createdBy">创建者ID（可选）</param>
    /// <returns></returns>
    [HttpGet("question-instances")]
    public async Task<ActionResult<List<ExcelQuestionInstance>>> GetQuestionInstances(
        [FromQuery] ExcelQuestionStatus? status = null, 
        [FromQuery] int? createdBy = null)
    {
        List<ExcelQuestionInstance> instances = await _questionService.GetQuestionInstancesAsync(status, createdBy);
        return Ok(instances);
    }

    /// <summary>
    /// 测试基础操作点配置
    /// </summary>
    /// <returns></returns>
    [HttpGet("test/basic-operations")]
    public async Task<ActionResult<object>> TestBasicOperations()
    {
        List<ExcelOperationPoint> basicOperations = await _operationService
            .GetOperationPointsByCategoryAsync(ExcelOperationCategory.BasicOperation);

        object result = new
        {
            Count = basicOperations.Count,
            Operations = basicOperations.Select(op => new
            {
                op.OperationNumber,
                op.Name,
                op.Description,
                ParameterCount = op.Parameters.Count,
                Parameters = op.Parameters.OrderBy(p => p.ParameterOrder).Select(p => new
                {
                    p.ParameterName,
                    p.DataType,
                    p.IsRequired,
                    p.ExampleValue,
                    EnumType = p.EnumType?.TypeName
                })
            }).OrderBy(op => op.OperationNumber)
        };

        return Ok(result);
    }

    /// <summary>
    /// 测试枚举值配置
    /// </summary>
    /// <returns></returns>
    [HttpGet("test/enum-values")]
    public async Task<ActionResult<object>> TestEnumValues()
    {
        List<ExcelEnumType> enumTypes = await _operationService.GetAllEnumTypesAsync();

        object result = new
        {
            Count = enumTypes.Count,
            EnumTypes = enumTypes.Select(et => new
            {
                et.TypeName,
                et.Category,
                et.Description,
                ValueCount = et.EnumValues.Count,
                Values = et.EnumValues.OrderBy(ev => ev.SortOrder).Select(ev => new
                {
                    ev.EnumKey,
                    ev.EnumValue,
                    ev.DisplayName,
                    ev.Description,
                    ev.IsDefault
                })
            })
        };

        return Ok(result);
    }

    /// <summary>
    /// 运行数据库设计完整性测试
    /// </summary>
    /// <returns></returns>
    [HttpGet("run-tests")]
    public Task<ActionResult<object>> RunDatabaseTests()
    {
        // TODO: 实现测试功能
        object result = new
        {
            TestCount = 0,
            PassedCount = 0,
            FailedCount = 0,
            Message = "测试功能暂未实现"
        };

        return Task.FromResult<ActionResult<object>>(Ok(result));
    }
}

/// <summary>
/// 验证参数请求
/// </summary>
public class ValidateParametersRequest
{
    /// <summary>
    /// 操作点编号
    /// </summary>
    public int OperationNumber { get; set; }

    /// <summary>
    /// 参数值字典
    /// </summary>
    public Dictionary<string, object?> ParameterValues { get; set; } = new();
}

/// <summary>
/// 生成题目请求
/// </summary>
public class GenerateQuestionRequest
{
    /// <summary>
    /// 模板ID
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// 自定义参数
    /// </summary>
    public Dictionary<string, object?>? CustomParameters { get; set; }
}

/// <summary>
/// 批量生成题目请求
/// </summary>
public class BatchGenerateQuestionRequest
{
    /// <summary>
    /// 模板ID
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// 生成数量
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 参数变化配置
    /// </summary>
    public Dictionary<string, List<object>>? ParameterVariations { get; set; }
}
