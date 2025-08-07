using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Excel;

namespace ExaminaWebApplication.Services.Excel;

/// <summary>
/// Excel题目服务类
/// </summary>
public class ExcelQuestionService
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelOperationService _operationService;

    public ExcelQuestionService(ApplicationDbContext context, ExcelOperationService operationService)
    {
        _context = context;
        _operationService = operationService;
    }

    /// <summary>
    /// 创建题目模板
    /// </summary>
    /// <param name="template">题目模板</param>
    /// <returns></returns>
    public async Task<ExcelQuestionTemplate> CreateQuestionTemplateAsync(ExcelQuestionTemplate template)
    {
        // 验证操作点是否存在
        bool operationPointExists = await _context.ExcelOperationPoints
            .AnyAsync(op => op.Id == template.OperationPointId);
        
        if (!operationPointExists)
        {
            throw new ArgumentException($"操作点 ID {template.OperationPointId} 不存在");
        }

        // 验证参数配置是否有效
        ParameterValidationResult validationResult = await ValidateTemplateParametersAsync(template);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"参数配置无效: {string.Join(", ", validationResult.Errors)}");
        }

        template.CreatedAt = DateTime.UtcNow;
        _context.ExcelQuestionTemplates.Add(template);
        await _context.SaveChangesAsync();

        return template;
    }

    /// <summary>
    /// 根据模板生成题目实例
    /// </summary>
    /// <param name="templateId">模板ID</param>
    /// <param name="customParameters">自定义参数（可选）</param>
    /// <returns></returns>
    public async Task<ExcelQuestionInstance> GenerateQuestionInstanceAsync(
        int templateId, 
        Dictionary<string, object?>? customParameters = null)
    {
        ExcelQuestionTemplate? template = await _context.ExcelQuestionTemplates
            .Include(t => t.OperationPoint)
            .ThenInclude(op => op.Parameters)
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            throw new ArgumentException($"题目模板 ID {templateId} 不存在");
        }

        // 解析模板参数配置
        Dictionary<string, object?> templateParameters = JsonSerializer
            .Deserialize<Dictionary<string, object?>>(template.ParameterConfiguration) ?? new();

        // 合并自定义参数
        if (customParameters != null)
        {
            foreach (KeyValuePair<string, object?> kvp in customParameters)
            {
                templateParameters[kvp.Key] = kvp.Value;
            }
        }

        // 生成题目描述
        string questionDescription = GenerateQuestionDescription(template.QuestionTemplate, templateParameters);

        // 创建题目实例
        ExcelQuestionInstance instance = new ExcelQuestionInstance
        {
            TemplateId = templateId,
            QuestionTitle = $"{template.OperationPoint.Name} - {template.TemplateName}",
            QuestionDescription = questionDescription,
            ActualParameters = JsonSerializer.Serialize(templateParameters),
            AnswerValidationRules = GenerateAnswerValidationRules(template.OperationPoint, templateParameters),
            Status = ExcelQuestionStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _context.ExcelQuestionInstances.Add(instance);
        await _context.SaveChangesAsync();

        // 更新模板使用次数
        template.UsageCount++;
        await _context.SaveChangesAsync();

        return instance;
    }

    /// <summary>
    /// 获取题目模板列表
    /// </summary>
    /// <param name="operationPointId">操作点ID（可选）</param>
    /// <param name="difficultyLevel">难度级别（可选）</param>
    /// <returns></returns>
    public async Task<List<ExcelQuestionTemplate>> GetQuestionTemplatesAsync(
        int? operationPointId = null, 
        int? difficultyLevel = null)
    {
        IQueryable<ExcelQuestionTemplate> query = _context.ExcelQuestionTemplates
            .Include(t => t.OperationPoint)
            .Where(t => t.IsEnabled);

        if (operationPointId.HasValue)
        {
            query = query.Where(t => t.OperationPointId == operationPointId.Value);
        }

        if (difficultyLevel.HasValue)
        {
            query = query.Where(t => t.DifficultyLevel == difficultyLevel.Value);
        }

        return await query.OrderBy(t => t.OperationPoint.OperationNumber)
            .ThenBy(t => t.DifficultyLevel)
            .ToListAsync();
    }

    /// <summary>
    /// 获取题目实例列表
    /// </summary>
    /// <param name="status">题目状态（可选）</param>
    /// <param name="createdBy">创建者ID（可选）</param>
    /// <returns></returns>
    public async Task<List<ExcelQuestionInstance>> GetQuestionInstancesAsync(
        ExcelQuestionStatus? status = null, 
        int? createdBy = null)
    {
        IQueryable<ExcelQuestionInstance> query = _context.ExcelQuestionInstances
            .Include(i => i.Template)
            .ThenInclude(t => t.OperationPoint);

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        if (createdBy.HasValue)
        {
            query = query.Where(i => i.CreatedBy == createdBy.Value);
        }

        return await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
    }

    /// <summary>
    /// 批量生成题目实例
    /// </summary>
    /// <param name="templateId">模板ID</param>
    /// <param name="count">生成数量</param>
    /// <param name="parameterVariations">参数变化配置</param>
    /// <returns></returns>
    public async Task<List<ExcelQuestionInstance>> BatchGenerateQuestionInstancesAsync(
        int templateId, 
        int count, 
        Dictionary<string, List<object>>? parameterVariations = null)
    {
        List<ExcelQuestionInstance> instances = new List<ExcelQuestionInstance>();

        for (int i = 0; i < count; i++)
        {
            Dictionary<string, object?>? customParameters = null;
            
            if (parameterVariations != null)
            {
                customParameters = new Dictionary<string, object?>();
                foreach (KeyValuePair<string, List<object>> variation in parameterVariations)
                {
                    // 随机选择一个变化值
                    Random random = new Random();
                    int index = random.Next(variation.Value.Count);
                    customParameters[variation.Key] = variation.Value[index];
                }
            }

            ExcelQuestionInstance instance = await GenerateQuestionInstanceAsync(templateId, customParameters);
            instances.Add(instance);
        }

        return instances;
    }

    /// <summary>
    /// 验证模板参数配置
    /// </summary>
    /// <param name="template">题目模板</param>
    /// <returns></returns>
    private async Task<ParameterValidationResult> ValidateTemplateParametersAsync(ExcelQuestionTemplate template)
    {
        try
        {
            Dictionary<string, object?> parameters = JsonSerializer
                .Deserialize<Dictionary<string, object?>>(template.ParameterConfiguration) ?? new();

            ExcelOperationPoint? operationPoint = await _context.ExcelOperationPoints
                .Include(op => op.Parameters)
                .FirstOrDefaultAsync(op => op.Id == template.OperationPointId);

            if (operationPoint == null)
            {
                return new ParameterValidationResult
                {
                    IsValid = false,
                    Errors = { "操作点不存在" }
                };
            }

            return await _operationService.ValidateOperationParametersAsync(
                operationPoint.OperationNumber, parameters);
        }
        catch (JsonException)
        {
            return new ParameterValidationResult
            {
                IsValid = false,
                Errors = { "参数配置JSON格式无效" }
            };
        }
    }

    /// <summary>
    /// 生成题目描述
    /// </summary>
    /// <param name="template">题目模板</param>
    /// <param name="parameters">参数值</param>
    /// <returns></returns>
    private static string GenerateQuestionDescription(string template, Dictionary<string, object?> parameters)
    {
        string description = template;
        
        foreach (KeyValuePair<string, object?> parameter in parameters)
        {
            string placeholder = $"{{{parameter.Key}}}";
            string value = parameter.Value?.ToString() ?? "";
            description = description.Replace(placeholder, value);
        }

        return description;
    }

    /// <summary>
    /// 生成答案验证规则
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    /// <param name="parameters">参数值</param>
    /// <returns></returns>
    private static string GenerateAnswerValidationRules(ExcelOperationPoint operationPoint, Dictionary<string, object?> parameters)
    {
        AnswerValidationRules rules = new AnswerValidationRules
        {
            OperationType = operationPoint.OperationType,
            TargetType = operationPoint.TargetType.ToString(),
            ExpectedParameters = parameters
        };

        return JsonSerializer.Serialize(rules);
    }
}

/// <summary>
/// 答案验证规则
/// </summary>
public class AnswerValidationRules
{
    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 目标类型
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// 期望的参数值
    /// </summary>
    public Dictionary<string, object?> ExpectedParameters { get; set; } = new();

    /// <summary>
    /// 验证策略（精确匹配、范围匹配等）
    /// </summary>
    public string ValidationStrategy { get; set; } = "ExactMatch";

    /// <summary>
    /// 容错设置
    /// </summary>
    public Dictionary<string, object> ToleranceSettings { get; set; } = new();
}
