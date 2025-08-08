using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Word;
using ExaminaWebApplication.Data.Word;

namespace ExaminaWebApplication.Services.Word;

/// <summary>
/// Word操作服务 - 提供Word段落操作点的业务逻辑
/// </summary>
public class WordOperationService
{
    private readonly ApplicationDbContext _context;

    public WordOperationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取所有Word操作点
    /// </summary>
    /// <returns></returns>
    public async Task<List<WordOperationPoint>> GetAllOperationPointsAsync()
    {
        List<WordOperationPoint> points = await _context.WordOperationPoints
            .Include(op => op.Parameters)
                .ThenInclude(p => p.EnumType)
                .ThenInclude(et => et!.EnumValues)
            .Include(op => op.QuestionTemplates)
            .AsNoTracking()
            .OrderBy(op => op.OperationNumber)
            .ToListAsync();

        foreach (WordOperationPoint op in points)
        {
            op.Parameters = op.Parameters
                .OrderBy(p => p.ParameterOrder)
                .ToList();

            foreach (WordOperationParameter p in op.Parameters)
            {
                if (p.EnumType != null)
                {
                    p.EnumType.EnumValues = p.EnumType.EnumValues
                        .OrderBy(ev => ev.SortOrder)
                        .ToList();
                }
            }
        }

        return points;
    }

    /// <summary>
    /// 根据ID获取Word操作点
    /// </summary>
    /// <param name="id">操作点ID</param>
    /// <returns></returns>
    public async Task<WordOperationPoint?> GetOperationPointByIdAsync(int id)
    {
        WordOperationPoint? op = await _context.WordOperationPoints
            .Include(o => o.Parameters)
                .ThenInclude(p => p.EnumType)
                .ThenInclude(et => et!.EnumValues)
            .Include(o => o.QuestionTemplates)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (op != null)
        {
            op.Parameters = op.Parameters.OrderBy(p => p.ParameterOrder).ToList();
            foreach (WordOperationParameter p in op.Parameters)
            {
                if (p.EnumType != null)
                {
                    p.EnumType.EnumValues = p.EnumType.EnumValues.OrderBy(ev => ev.SortOrder).ToList();
                }
            }
        }

        return op;
    }

    /// <summary>
    /// 根据操作编号获取Word操作点
    /// </summary>
    /// <param name="operationNumber">操作编号</param>
    /// <returns></returns>
    public async Task<WordOperationPoint?> GetOperationPointByNumberAsync(int operationNumber)
    {
        WordOperationPoint? op = await _context.WordOperationPoints
            .Include(o => o.Parameters)
                .ThenInclude(p => p.EnumType)
                .ThenInclude(et => et!.EnumValues)
            .Include(o => o.QuestionTemplates)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OperationNumber == operationNumber);

        if (op != null)
        {
            op.Parameters = op.Parameters.OrderBy(p => p.ParameterOrder).ToList();
            foreach (WordOperationParameter p in op.Parameters)
            {
                if (p.EnumType != null)
                {
                    p.EnumType.EnumValues = p.EnumType.EnumValues.OrderBy(ev => ev.SortOrder).ToList();
                }
            }
        }

        return op;
    }

    /// <summary>
    /// 根据类别获取Word操作点
    /// </summary>
    /// <param name="category">操作类别</param>
    /// <returns></returns>
    public async Task<List<WordOperationPoint>> GetOperationPointsByCategoryAsync(WordOperationCategory category)
    {
        List<WordOperationPoint> points = await _context.WordOperationPoints
            .Include(o => o.Parameters)
                .ThenInclude(p => p.EnumType)
                .ThenInclude(et => et!.EnumValues)
            .Where(o => o.Category == category)
            .Where(o => o.IsEnabled)
            .AsNoTracking()
            .OrderBy(o => o.OperationNumber)
            .ToListAsync();

        foreach (WordOperationPoint op in points)
        {
            op.Parameters = op.Parameters.OrderBy(p => p.ParameterOrder).ToList();
            foreach (WordOperationParameter p in op.Parameters)
            {
                if (p.EnumType != null)
                {
                    p.EnumType.EnumValues = p.EnumType.EnumValues.OrderBy(ev => ev.SortOrder).ToList();
                }
            }
        }

        return points;
    }

    /// <summary>
    /// 获取所有枚举类型
    /// </summary>
    /// <returns></returns>
    public async Task<List<WordEnumType>> GetAllEnumTypesAsync()
    {
        return await _context.WordEnumTypes
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
    public async Task<List<WordEnumValue>> GetEnumValuesByTypeNameAsync(string typeName)
    {
        return await _context.WordEnumValues
            .Include(ev => ev.EnumType)
            .Where(ev => ev.EnumType.TypeName == typeName)
            .OrderBy(ev => ev.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 根据类型ID获取枚举值
    /// </summary>
    /// <param name="enumTypeId">枚举类型ID</param>
    /// <returns></returns>
    public async Task<List<WordEnumValue>> GetEnumValuesByTypeIdAsync(int enumTypeId)
    {
        return await _context.WordEnumValues
            .Where(ev => ev.EnumTypeId == enumTypeId)
            .Where(ev => ev.IsEnabled)
            .OrderBy(ev => ev.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 创建Word操作点
    /// </summary>
    /// <param name="operationPoint">操作点信息</param>
    /// <returns></returns>
    public async Task<WordOperationPoint> CreateOperationPointAsync(WordOperationPoint operationPoint)
    {
        operationPoint.CreatedAt = DateTime.UtcNow;
        _context.WordOperationPoints.Add(operationPoint);
        await _context.SaveChangesAsync();
        return operationPoint;
    }

    /// <summary>
    /// 更新Word操作点
    /// </summary>
    /// <param name="operationPoint">操作点信息</param>
    /// <returns></returns>
    public async Task<bool> UpdateOperationPointAsync(WordOperationPoint operationPoint)
    {
        WordOperationPoint? existingOperationPoint = await _context.WordOperationPoints.FindAsync(operationPoint.Id);
        if (existingOperationPoint == null)
        {
            return false;
        }

        existingOperationPoint.Name = operationPoint.Name;
        existingOperationPoint.Description = operationPoint.Description;
        existingOperationPoint.Category = operationPoint.Category;
        existingOperationPoint.IsEnabled = operationPoint.IsEnabled;
        existingOperationPoint.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 删除Word操作点
    /// </summary>
    /// <param name="id">操作点ID</param>
    /// <returns></returns>
    public async Task<bool> DeleteOperationPointAsync(int id)
    {
        WordOperationPoint? operationPoint = await _context.WordOperationPoints.FindAsync(id);
        if (operationPoint == null)
        {
            return false;
        }

        _context.WordOperationPoints.Remove(operationPoint);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 初始化Word操作点数据
    /// </summary>
    /// <returns></returns>
    public async Task InitializeWordOperationDataAsync()
    {
        // 检查是否已经初始化
        bool hasData = await _context.WordOperationPoints.AnyAsync();
        if (hasData)
        {
            return;
        }

        // 添加枚举类型
        List<WordEnumType> enumTypes = WordEnumData.GetEnumTypes();
        _context.WordEnumTypes.AddRange(enumTypes);
        await _context.SaveChangesAsync();

        // 添加枚举值
        List<WordEnumValue> enumValues = WordEnumData.GetEnumValues();
        _context.WordEnumValues.AddRange(enumValues);
        await _context.SaveChangesAsync();

        // 添加操作点
        List<WordOperationPoint> operationPoints = WordOperationData.GetWordOperationPoints();
        _context.WordOperationPoints.AddRange(operationPoints);
        await _context.SaveChangesAsync();

        // 添加参数配置
        List<WordOperationParameter> parameters = WordOperationData.GetWordOperationParameters();
        _context.WordOperationParameters.AddRange(parameters);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 强制重新初始化Word操作点数据（包含所有67个操作点）
    /// </summary>
    /// <returns></returns>
    public async Task ReinitializeWordOperationDataAsync()
    {
        // 清空现有数据
        _context.WordOperationParameters.RemoveRange(_context.WordOperationParameters);
        _context.WordOperationPoints.RemoveRange(_context.WordOperationPoints);
        _context.WordEnumValues.RemoveRange(_context.WordEnumValues);
        _context.WordEnumTypes.RemoveRange(_context.WordEnumTypes);
        await _context.SaveChangesAsync();

        // 添加完整的枚举类型
        List<WordEnumType> enumTypes = WordEnumDataComplete.GetEnumTypes();
        _context.WordEnumTypes.AddRange(enumTypes);
        await _context.SaveChangesAsync();

        // 添加完整的枚举值
        List<WordEnumValue> enumValues = WordEnumDataComplete.GetEnumValues();
        _context.WordEnumValues.AddRange(enumValues);
        await _context.SaveChangesAsync();

        // 添加完整的操作点（67个）
        List<WordOperationPoint> operationPoints = WordOperationDataComplete.GetWordOperationPoints();
        _context.WordOperationPoints.AddRange(operationPoints);
        await _context.SaveChangesAsync();

        // 添加完整的参数配置
        List<WordOperationParameter> parameters = WordOperationParametersComplete.GetWordOperationParameters();
        _context.WordOperationParameters.AddRange(parameters);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 获取操作点统计信息
    /// </summary>
    /// <returns></returns>
    public async Task<object> GetOperationPointStatisticsAsync()
    {
        int totalCount = await _context.WordOperationPoints.CountAsync();
        int enabledCount = await _context.WordOperationPoints.CountAsync(op => op.IsEnabled);
        
        Dictionary<WordOperationCategory, int> categoryStats = await _context.WordOperationPoints
            .Where(op => op.IsEnabled)
            .GroupBy(op => op.Category)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        return new
        {
            TotalCount = totalCount,
            EnabledCount = enabledCount,
            CategoryStatistics = categoryStats
        };
    }
}
