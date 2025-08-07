using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Excel;

namespace ExaminaWebApplication.Services.Excel;

/// <summary>
/// Excel操作服务类
/// </summary>
public class ExcelOperationService
{
    private readonly ApplicationDbContext _context;

    public ExcelOperationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取所有Excel操作点
    /// </summary>
    /// <returns></returns>
    public async Task<List<ExcelOperationPoint>> GetAllOperationPointsAsync()
    {
        return await _context.ExcelOperationPoints
            .Include(op => op.Parameters)
            .ThenInclude(p => p.EnumType)
            .ThenInclude(et => et!.EnumValues)
            .OrderBy(op => op.OperationNumber)
            .ToListAsync();
    }

    /// <summary>
    /// 根据分类获取操作点
    /// </summary>
    /// <param name="category">操作分类</param>
    /// <returns></returns>
    public async Task<List<ExcelOperationPoint>> GetOperationPointsByCategoryAsync(ExcelOperationCategory category)
    {
        return await _context.ExcelOperationPoints
            .Include(op => op.Parameters)
            .ThenInclude(p => p.EnumType)
            .ThenInclude(et => et!.EnumValues)
            .Where(op => op.Category == category)
            .OrderBy(op => op.OperationNumber)
            .ToListAsync();
    }

    /// <summary>
    /// 获取指定操作点的详细信息
    /// </summary>
    /// <param name="operationNumber">操作点编号</param>
    /// <returns></returns>
    public async Task<ExcelOperationPoint?> GetOperationPointByNumberAsync(int operationNumber)
    {
        return await _context.ExcelOperationPoints
            .Include(op => op.Parameters.OrderBy(p => p.ParameterOrder))
            .ThenInclude(p => p.EnumType)
            .ThenInclude(et => et!.EnumValues.OrderBy(ev => ev.SortOrder))
            .FirstOrDefaultAsync(op => op.OperationNumber == operationNumber);
    }

    /// <summary>
    /// 获取所有枚举类型
    /// </summary>
    /// <returns></returns>
    public async Task<List<ExcelEnumType>> GetAllEnumTypesAsync()
    {
        return await _context.ExcelEnumTypes
            .Include(et => et.EnumValues.OrderBy(ev => ev.SortOrder))
            .OrderBy(et => et.Category)
            .ThenBy(et => et.TypeName)
            .ToListAsync();
    }

    /// <summary>
    /// 根据类型名称获取枚举值
    /// </summary>
    /// <param name="typeName">枚举类型名称</param>
    /// <returns></returns>
    public async Task<List<ExcelEnumValue>> GetEnumValuesByTypeNameAsync(string typeName)
    {
        return await _context.ExcelEnumValues
            .Include(ev => ev.EnumType)
            .Where(ev => ev.EnumType.TypeName == typeName)
            .OrderBy(ev => ev.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 验证操作点参数配置
    /// </summary>
    /// <param name="operationNumber">操作点编号</param>
    /// <param name="parameterValues">参数值字典</param>
    /// <returns></returns>
    public async Task<ParameterValidationResult> ValidateOperationParametersAsync(
        int operationNumber, 
        Dictionary<string, object?> parameterValues)
    {
        ExcelOperationPoint? operationPoint = await GetOperationPointByNumberAsync(operationNumber);
        if (operationPoint == null)
        {
            return new ParameterValidationResult
            {
                IsValid = false,
                Errors = { $"操作点 {operationNumber} 不存在" }
            };
        }

        List<ExcelParameterConfigurationBase> configurations = new List<ExcelParameterConfigurationBase>();
        
        foreach (ExcelOperationParameter parameter in operationPoint.Parameters)
        {
            object? value = parameterValues.ContainsKey(parameter.ParameterName) 
                ? parameterValues[parameter.ParameterName] 
                : null;
            
            ExcelParameterConfigurationBase config = ExcelParameterConfigurationManager
                .CreateParameterConfiguration(parameter, value);
            configurations.Add(config);
        }

        return ExcelParameterConfigurationManager.ValidateParameters(configurations);
    }

    /// <summary>
    /// 获取操作点统计信息
    /// </summary>
    /// <returns></returns>
    public async Task<ExcelOperationStatistics> GetOperationStatisticsAsync()
    {
        int totalOperationPoints = await _context.ExcelOperationPoints.CountAsync();
        int basicOperations = await _context.ExcelOperationPoints
            .CountAsync(op => op.Category == ExcelOperationCategory.BasicOperation);
        int dataListOperations = await _context.ExcelOperationPoints
            .CountAsync(op => op.Category == ExcelOperationCategory.DataListOperation);
        int chartOperations = await _context.ExcelOperationPoints
            .CountAsync(op => op.Category == ExcelOperationCategory.ChartOperation);
        int totalParameters = await _context.ExcelOperationParameters.CountAsync();
        int totalEnumTypes = await _context.ExcelEnumTypes.CountAsync();
        int totalEnumValues = await _context.ExcelEnumValues.CountAsync();

        return new ExcelOperationStatistics
        {
            TotalOperationPoints = totalOperationPoints,
            BasicOperations = basicOperations,
            DataListOperations = dataListOperations,
            ChartOperations = chartOperations,
            TotalParameters = totalParameters,
            TotalEnumTypes = totalEnumTypes,
            TotalEnumValues = totalEnumValues
        };
    }
}

/// <summary>
/// Excel操作统计信息
/// </summary>
public class ExcelOperationStatistics
{
    /// <summary>
    /// 总操作点数量
    /// </summary>
    public int TotalOperationPoints { get; set; }

    /// <summary>
    /// 基础操作数量
    /// </summary>
    public int BasicOperations { get; set; }

    /// <summary>
    /// 数据清单操作数量
    /// </summary>
    public int DataListOperations { get; set; }

    /// <summary>
    /// 图表操作数量
    /// </summary>
    public int ChartOperations { get; set; }

    /// <summary>
    /// 总参数数量
    /// </summary>
    public int TotalParameters { get; set; }

    /// <summary>
    /// 总枚举类型数量
    /// </summary>
    public int TotalEnumTypes { get; set; }

    /// <summary>
    /// 总枚举值数量
    /// </summary>
    public int TotalEnumValues { get; set; }
}
