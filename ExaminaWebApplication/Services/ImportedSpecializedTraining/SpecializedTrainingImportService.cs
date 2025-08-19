using System.Text.Json;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.ImportedSpecializedTraining;
using Microsoft.EntityFrameworkCore;
using ImportedSpecializedTrainingEntity = ExaminaWebApplication.Models.ImportedSpecializedTraining.ImportedSpecializedTraining;

namespace ExaminaWebApplication.Services.ImportedSpecializedTraining;

/// <summary>
/// 专项训练导入服务
/// </summary>
public class SpecializedTrainingImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SpecializedTrainingImportService> _logger;

    public SpecializedTrainingImportService(ApplicationDbContext context, ILogger<SpecializedTrainingImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 导入专项训练数据
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="importedBy">导入者ID</param>
    /// <returns>导入结果</returns>
    public async Task<SpecializedTrainingImportResult> ImportSpecializedTrainingAsync(Stream fileStream, string fileName, int importedBy)
    {
        SpecializedTrainingImportResult result = new()
        {
            FileName = fileName,
            ImportedBy = importedBy,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 校验导入用户是否存在
            bool userExists = await _context.Users.AnyAsync(u => u.Id == importedBy);
            if (!userExists)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"导入者用户不存在或无效（ID={importedBy}），请先创建有效用户后再导入。";
                result.EndTime = DateTime.UtcNow;
                _logger.LogWarning("导入者不存在，已阻止导入。ImportedBy={ImportedBy}", importedBy);
                return result;
            }

            // 读取文件内容
            using StreamReader reader = new(fileStream);
            string jsonContent = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "文件内容为空或无效";
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            // 解析JSON
            SpecializedTrainingExportDto? exportDto;
            try
            {
                JsonSerializerOptions options = new()
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };
                exportDto = JsonSerializer.Deserialize<SpecializedTrainingExportDto>(jsonContent, options);
            }
            catch (JsonException ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"JSON格式解析失败：{ex.Message}";
                result.EndTime = DateTime.UtcNow;
                _logger.LogError(ex, "JSON解析失败，文件：{FileName}", fileName);
                return result;
            }

            if (exportDto?.SpecializedTraining == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "导入数据格式无效：缺少专项训练数据";
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            // 验证数据
            var validationResult = ValidateSpecializedTrainingData(exportDto.SpecializedTraining);
            if (!validationResult.IsValid)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"数据验证失败：{string.Join("; ", validationResult.Errors)}";
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            // 检查是否已存在相同的专项训练
            bool exists = await _context.ImportedSpecializedTrainings
                .AnyAsync(st => st.OriginalSpecializedTrainingId == exportDto.SpecializedTraining.Id && st.ImportedBy == importedBy);

            if (exists)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"专项训练已存在：{exportDto.SpecializedTraining.Name}（ID: {exportDto.SpecializedTraining.Id}）";
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            // 创建导入实体
            long fileSize = fileStream.Length;
            ImportedSpecializedTrainingEntity importedSpecializedTraining = ImportedSpecializedTrainingEntity.FromSpecializedTrainingExportDto(
                exportDto, importedBy, fileName, fileSize);

            // 保存到数据库 - 使用执行策略处理 MySQL 重试机制
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.ImportedSpecializedTrainings.Add(importedSpecializedTraining);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 统计信息
                    result.Statistics.ModuleCount = importedSpecializedTraining.Modules.Count;
                    result.Statistics.QuestionCount = importedSpecializedTraining.Questions.Count;
                    result.Statistics.OperationPointCount = importedSpecializedTraining.Questions.Sum(q => q.OperationPoints.Count);
                    result.Statistics.ParameterCount = importedSpecializedTraining.Questions
                        .SelectMany(q => q.OperationPoints)
                        .Sum(op => op.Parameters.Count);
                    result.Statistics.FileSize = fileSize;

                    result.IsSuccess = true;
                    result.ImportedSpecializedTrainingId = importedSpecializedTraining.Id;
                    result.ImportedSpecializedTrainingName = importedSpecializedTraining.Name;
                    result.EndTime = DateTime.UtcNow;

                    _logger.LogInformation("专项训练导入成功：{Name}，ID：{Id}，导入者：{ImportedBy}",
                        importedSpecializedTraining.Name, importedSpecializedTraining.Id, importedBy);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    result.IsSuccess = false;
                    result.ErrorMessage = $"数据库保存失败：{ex.Message}";
                    result.EndTime = DateTime.UtcNow;
                    _logger.LogError(ex, "专项训练导入失败，数据库保存错误");
                    throw; // 重新抛出异常以便执行策略处理
                }
            });
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"导入过程发生未知错误：{ex.Message}";
            result.EndTime = DateTime.UtcNow;
            _logger.LogError(ex, "专项训练导入过程发生未知错误");
        }

        return result;
    }

    /// <summary>
    /// 获取导入的专项训练列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>专项训练列表</returns>
    public async Task<List<ImportedSpecializedTrainingEntity>> GetImportedSpecializedTrainingsAsync(int userId)
    {
        return await _context.ImportedSpecializedTrainings
            .Where(st => st.ImportedBy == userId)
            .Include(st => st.Modules)
            .Include(st => st.Questions)
            .AsSplitQuery() // 使用分割查询提升性能
            .OrderByDescending(st => st.ImportedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 根据ID获取专项训练详情
    /// </summary>
    /// <param name="id">专项训练ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>专项训练详情</returns>
    public async Task<ImportedSpecializedTrainingEntity?> GetSpecializedTrainingByIdAsync(int id, int userId)
    {
        return await _context.ImportedSpecializedTrainings
            .Where(st => st.Id == id && st.ImportedBy == userId)
            .Include(st => st.Modules)
                .ThenInclude(m => m.Questions)
                    .ThenInclude(q => q.OperationPoints)
                        .ThenInclude(op => op.Parameters)
            .AsSplitQuery() // 使用分割查询提升性能
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 删除专项训练
    /// </summary>
    /// <param name="id">专项训练ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>是否删除成功</returns>
    public async Task<bool> DeleteSpecializedTrainingAsync(int id, int userId)
    {
        try
        {
            ImportedSpecializedTrainingEntity? specializedTraining = await _context.ImportedSpecializedTrainings
                .Where(st => st.Id == id && st.ImportedBy == userId)
                .FirstOrDefaultAsync();

            if (specializedTraining == null)
            {
                return false;
            }

            _context.ImportedSpecializedTrainings.Remove(specializedTraining);
            await _context.SaveChangesAsync();

            _logger.LogInformation("专项训练删除成功：{Name}，ID：{Id}，用户：{UserId}",
                specializedTraining.Name, id, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除专项训练失败，ID：{Id}，用户：{UserId}", id, userId);
            return false;
        }
    }

    /// <summary>
    /// 验证专项训练数据
    /// </summary>
    /// <param name="specializedTraining">专项训练数据</param>
    /// <returns>验证结果</returns>
    private static ValidationResult ValidateSpecializedTrainingData(SpecializedTrainingDto specializedTraining)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(specializedTraining.Id))
        {
            result.Errors.Add("专项训练ID不能为空");
        }

        if (string.IsNullOrWhiteSpace(specializedTraining.Name))
        {
            result.Errors.Add("专项训练名称不能为空");
        }

        if (string.IsNullOrWhiteSpace(specializedTraining.ModuleType))
        {
            result.Errors.Add("模块类型不能为空");
        }

        if (specializedTraining.TotalScore <= 0)
        {
            result.Errors.Add("总分必须大于0");
        }

        if (specializedTraining.Duration <= 0)
        {
            result.Errors.Add("时长必须大于0");
        }

        if (specializedTraining.Modules == null || specializedTraining.Modules.Count == 0)
        {
            result.Errors.Add("至少需要包含一个模块");
        }
        else
        {
            foreach (var module in specializedTraining.Modules)
            {
                if (string.IsNullOrWhiteSpace(module.Name))
                {
                    result.Errors.Add($"模块名称不能为空（模块ID: {module.Id}）");
                }

                if (module.Questions == null || module.Questions.Count == 0)
                {
                    result.Errors.Add($"模块必须包含至少一个题目（模块: {module.Name}）");
                }
            }
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    private class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = [];
    }
}
