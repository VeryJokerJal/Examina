using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Windows;
using ExaminaWebApplication.Data.Windows;

namespace ExaminaWebApplication.Services.Windows;

/// <summary>
/// Windows操作服务 - 提供Windows文件系统操作点的业务逻辑
/// </summary>
public class WindowsOperationService
{
    private readonly ApplicationDbContext _context;

    public WindowsOperationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取所有Windows操作点
    /// </summary>
    /// <returns></returns>
    public async Task<List<WindowsOperationPoint>> GetAllOperationPointsAsync()
    {
        return await _context.WindowsOperationPoints
            .Include(op => op.Parameters.OrderBy(p => p.ParameterOrder))
            .ThenInclude(p => p.EnumType)
            .ThenInclude(et => et!.EnumValues.OrderBy(ev => ev.SortOrder))
            .Include(op => op.QuestionTemplates)
            .OrderBy(op => op.OperationNumber)
            .ToListAsync();
    }

    /// <summary>
    /// 根据ID获取Windows操作点
    /// </summary>
    /// <param name="id">操作点ID</param>
    /// <returns></returns>
    public async Task<WindowsOperationPoint?> GetOperationPointByIdAsync(int id)
    {
        return await _context.WindowsOperationPoints
            .Include(op => op.Parameters.OrderBy(p => p.ParameterOrder))
            .ThenInclude(p => p.EnumType)
            .ThenInclude(et => et!.EnumValues.OrderBy(ev => ev.SortOrder))
            .Include(op => op.QuestionTemplates)
            .FirstOrDefaultAsync(op => op.Id == id);
    }

    /// <summary>
    /// 根据操作类型获取Windows操作点
    /// </summary>
    /// <param name="operationType">操作类型</param>
    /// <returns></returns>
    public async Task<List<WindowsOperationPoint>> GetOperationPointsByTypeAsync(WindowsOperationType operationType)
    {
        return await _context.WindowsOperationPoints
            .Include(op => op.Parameters.OrderBy(p => p.ParameterOrder))
            .Where(op => op.OperationType == operationType && op.IsEnabled)
            .OrderBy(op => op.OperationNumber)
            .ToListAsync();
    }

    /// <summary>
    /// 根据操作模式获取Windows操作点
    /// </summary>
    /// <param name="operationMode">操作模式</param>
    /// <returns></returns>
    public async Task<List<WindowsOperationPoint>> GetOperationPointsByModeAsync(WindowsOperationMode operationMode)
    {
        return await _context.WindowsOperationPoints
            .Include(op => op.Parameters.OrderBy(p => p.ParameterOrder))
            .Where(op => op.OperationMode == operationMode || op.OperationMode == WindowsOperationMode.Universal)
            .Where(op => op.IsEnabled)
            .OrderBy(op => op.OperationNumber)
            .ToListAsync();
    }

    /// <summary>
    /// 获取操作点的参数配置
    /// </summary>
    /// <param name="operationPointId">操作点ID</param>
    /// <returns></returns>
    public async Task<List<WindowsOperationParameter>> GetOperationParametersAsync(int operationPointId)
    {
        return await _context.WindowsOperationParameters
            .Include(p => p.EnumType)
            .ThenInclude(et => et!.EnumValues.OrderBy(ev => ev.SortOrder))
            .Where(p => p.OperationPointId == operationPointId && p.IsEnabled)
            .OrderBy(p => p.ParameterOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有枚举类型
    /// </summary>
    /// <returns></returns>
    public async Task<List<WindowsEnumType>> GetAllEnumTypesAsync()
    {
        return await _context.WindowsEnumTypes
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
    public async Task<List<WindowsEnumValue>> GetEnumValuesByTypeNameAsync(string typeName)
    {
        return await _context.WindowsEnumValues
            .Include(ev => ev.EnumType)
            .Where(ev => ev.EnumType.TypeName == typeName)
            .OrderBy(ev => ev.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 创建Windows操作点
    /// </summary>
    /// <param name="operationPoint">操作点信息</param>
    /// <returns></returns>
    public async Task<WindowsOperationPoint> CreateOperationPointAsync(WindowsOperationPoint operationPoint)
    {
        operationPoint.CreatedAt = DateTime.UtcNow;
        _context.WindowsOperationPoints.Add(operationPoint);
        await _context.SaveChangesAsync();
        return operationPoint;
    }

    /// <summary>
    /// 更新Windows操作点
    /// </summary>
    /// <param name="operationPoint">操作点信息</param>
    /// <returns></returns>
    public async Task<WindowsOperationPoint> UpdateOperationPointAsync(WindowsOperationPoint operationPoint)
    {
        operationPoint.UpdatedAt = DateTime.UtcNow;
        _context.WindowsOperationPoints.Update(operationPoint);
        await _context.SaveChangesAsync();
        return operationPoint;
    }

    /// <summary>
    /// 删除Windows操作点
    /// </summary>
    /// <param name="id">操作点ID</param>
    /// <returns></returns>
    public async Task<bool> DeleteOperationPointAsync(int id)
    {
        WindowsOperationPoint? operationPoint = await _context.WindowsOperationPoints.FindAsync(id);
        if (operationPoint == null)
        {
            return false;
        }

        _context.WindowsOperationPoints.Remove(operationPoint);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 初始化Windows操作点数据
    /// </summary>
    /// <returns></returns>
    public async Task InitializeWindowsOperationDataAsync()
    {
        // 检查是否已经初始化
        bool hasData = await _context.WindowsOperationPoints.AnyAsync();
        if (hasData)
        {
            return;
        }

        // 添加枚举类型
        List<WindowsEnumType> enumTypes = WindowsEnumData.GetEnumTypes();
        _context.WindowsEnumTypes.AddRange(enumTypes);
        await _context.SaveChangesAsync();

        // 添加枚举值
        List<WindowsEnumValue> enumValues = WindowsEnumData.GetEnumValues();
        _context.WindowsEnumValues.AddRange(enumValues);
        await _context.SaveChangesAsync();

        // 添加操作点
        List<WindowsOperationPoint> operationPoints = WindowsOperationData.GetWindowsOperationPoints();
        _context.WindowsOperationPoints.AddRange(operationPoints);
        await _context.SaveChangesAsync();

        // 添加参数配置
        List<WindowsOperationParameter> parameters = WindowsOperationData.GetWindowsOperationParameters();
        _context.WindowsOperationParameters.AddRange(parameters);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 验证路径格式
    /// </summary>
    /// <param name="path">路径字符串</param>
    /// <param name="pathType">路径类型</param>
    /// <returns></returns>
    public bool ValidatePath(string path, WindowsParameterDataType pathType)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // 基本路径格式验证
        try
        {
            string fullPath = Path.GetFullPath(path);
            
            return pathType switch
            {
                WindowsParameterDataType.FilePath => !string.IsNullOrEmpty(Path.GetFileName(fullPath)),
                WindowsParameterDataType.FolderPath => string.IsNullOrEmpty(Path.GetExtension(fullPath)),
                WindowsParameterDataType.Path => true,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取操作点统计信息
    /// </summary>
    /// <returns></returns>
    public async Task<Dictionary<WindowsOperationType, int>> GetOperationPointStatisticsAsync()
    {
        return await _context.WindowsOperationPoints
            .Where(op => op.IsEnabled)
            .GroupBy(op => op.OperationType)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }
}
