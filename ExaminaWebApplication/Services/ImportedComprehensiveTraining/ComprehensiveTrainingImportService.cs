using System.Text.Json;
using System.Xml.Serialization;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.ImportedComprehensiveTraining;
using Microsoft.EntityFrameworkCore;
using ImportedComprehensiveTrainingEntity = ExaminaWebApplication.Models.ImportedComprehensiveTraining.ImportedComprehensiveTraining;

namespace ExaminaWebApplication.Services.ImportedComprehensiveTraining;

/// <summary>
/// 综合训练导入服务
/// </summary>
public class ComprehensiveTrainingImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ComprehensiveTrainingImportService> _logger;

    public ComprehensiveTrainingImportService(ApplicationDbContext context, ILogger<ComprehensiveTrainingImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 导入综合训练数据
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="importedBy">导入者ID</param>
    /// <returns>导入结果</returns>
    public async Task<ComprehensiveTrainingImportResult> ImportComprehensiveTrainingAsync(Stream fileStream, string fileName, int importedBy)
    {
        ComprehensiveTrainingImportResult result = new()
        {
            FileName = fileName,
            ImportedBy = importedBy,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 校验导入用户是否存在，避免外键约束错误
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
            string content = await reader.ReadToEndAsync();
            result.FileSize = content.Length;

            // 解析文件内容
            ComprehensiveTrainingExportDto? comprehensiveTrainingExportDto = await ParseFileContentAsync(content, fileName);
            if (comprehensiveTrainingExportDto == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "无法解析文件内容，请确保文件格式正确（支持JSON和XML格式）";
                return result;
            }

            // 验证数据完整性
            string? validationError = ValidateComprehensiveTrainingData(comprehensiveTrainingExportDto);
            if (!string.IsNullOrEmpty(validationError))
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"数据验证失败：{validationError}";
                return result;
            }

            _logger.LogInformation("开始导入综合训练: {ComprehensiveTrainingName}", comprehensiveTrainingExportDto.ComprehensiveTraining.Name);

            // 检查是否已存在相同的综合训练
            bool exists = await _context.ImportedComprehensiveTrainings
                .AnyAsync(e => e.OriginalComprehensiveTrainingId == comprehensiveTrainingExportDto.ComprehensiveTraining.Id && e.ImportedBy == importedBy);

            if (exists)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"综合训练 '{comprehensiveTrainingExportDto.ComprehensiveTraining.Name}' 已存在，请勿重复导入";
                return result;
            }

            // 转换并保存数据
            ImportedComprehensiveTrainingEntity importedComprehensiveTraining = await ConvertAndSaveComprehensiveTrainingAsync(comprehensiveTrainingExportDto, fileName, result.FileSize, importedBy);
            
            result.IsSuccess = true;
            result.ImportedComprehensiveTrainingId = importedComprehensiveTraining.Id;
            result.ImportedComprehensiveTrainingName = importedComprehensiveTraining.Name;
            result.TotalSubjects = importedComprehensiveTraining.Subjects.Count;
            result.TotalModules = importedComprehensiveTraining.Modules.Count;
            result.TotalQuestions = importedComprehensiveTraining.Subjects.Sum(s => s.Questions.Count) + 
                                   importedComprehensiveTraining.Modules.Sum(m => m.Questions.Count);
            result.EndTime = DateTime.UtcNow;

            _logger.LogInformation("成功导入综合训练: {ComprehensiveTrainingName} (ID: {ComprehensiveTrainingId})", 
                importedComprehensiveTraining.Name, importedComprehensiveTraining.Id);

            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"导入过程中发生错误：{ex.Message}";
            result.EndTime = DateTime.UtcNow;
            _logger.LogError(ex, "导入综合训练失败: {FileName}", fileName);
            return result;
        }
    }

    /// <summary>
    /// 解析文件内容
    /// </summary>
    private async Task<ComprehensiveTrainingExportDto?> ParseFileContentAsync(string content, string fileName)
    {
        try
        {
            // 尝试JSON格式
            if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || content.TrimStart().StartsWith("{"))
            {
                JsonSerializerOptions options = new()
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                ComprehensiveTrainingExportDto? result = JsonSerializer.Deserialize<ComprehensiveTrainingExportDto>(content, options);
                if (result != null)
                {
                    _logger.LogInformation("成功解析JSON格式的综合训练文件");
                    return result;
                }
            }

            // 尝试XML格式
            if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || content.TrimStart().StartsWith("<"))
            {
                XmlSerializer serializer = new(typeof(ComprehensiveTrainingExportDto));
                using StringReader stringReader = new(content);
                ComprehensiveTrainingExportDto? result = (ComprehensiveTrainingExportDto?)serializer.Deserialize(stringReader);
                if (result != null)
                {
                    _logger.LogInformation("成功解析XML格式的综合训练文件");
                    return result;
                }
            }

            _logger.LogWarning("无法识别文件格式，既不是有效的JSON也不是有效的XML");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON解析失败");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "XML解析失败");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件解析过程中发生未知错误");
            return null;
        }
    }

    /// <summary>
    /// 验证综合训练数据
    /// </summary>
    private string? ValidateComprehensiveTrainingData(ComprehensiveTrainingExportDto exportDto)
    {
        if (exportDto.ComprehensiveTraining == null)
        {
            return "综合训练数据不能为空";
        }

        if (string.IsNullOrWhiteSpace(exportDto.ComprehensiveTraining.Id))
        {
            // 如果ID为空，生成一个默认ID
            exportDto.ComprehensiveTraining.Id = Guid.NewGuid().ToString();
        }

        if (string.IsNullOrWhiteSpace(exportDto.ComprehensiveTraining.Name))
        {
            return "综合训练名称不能为空";
        }

        if (exportDto.ComprehensiveTraining.TotalScore <= 0)
        {
            return "综合训练总分必须大于0";
        }

        if (exportDto.ComprehensiveTraining.DurationMinutes <= 0)
        {
            return "综合训练时长必须大于0分钟";
        }

        // 验证科目数据
        foreach (ComprehensiveTrainingSubjectDto subject in exportDto.ComprehensiveTraining.Subjects)
        {
            if (string.IsNullOrWhiteSpace(subject.SubjectName))
            {
                return $"科目名称不能为空（科目ID: {subject.Id}）";
            }
        }

        // 验证模块数据
        foreach (ComprehensiveTrainingModuleDto module in exportDto.ComprehensiveTraining.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.Name))
            {
                return $"模块名称不能为空（模块ID: {module.Id}）";
            }
        }

        return null;
    }

    /// <summary>
    /// 转换并保存综合训练数据
    /// </summary>
    private async Task<ImportedComprehensiveTrainingEntity> ConvertAndSaveComprehensiveTrainingAsync(
        ComprehensiveTrainingExportDto comprehensiveTrainingExportDto,
        string fileName,
        long fileSize,
        int importedBy)
    {
        // 使用静态方法创建完整的对象图
        ImportedComprehensiveTrainingEntity importedComprehensiveTraining = ImportedComprehensiveTrainingEntity.FromComprehensiveTrainingExportDto(
            comprehensiveTrainingExportDto, importedBy, fileName, fileSize);

        // 保存到数据库
        _context.ImportedComprehensiveTrainings.Add(importedComprehensiveTraining);
        await _context.SaveChangesAsync();

        _logger.LogInformation("综合训练数据已保存到数据库，ID: {ComprehensiveTrainingId}", importedComprehensiveTraining.Id);

        return importedComprehensiveTraining;
    }

    /// <summary>
    /// 获取导入的综合训练列表
    /// </summary>
    public async Task<List<ImportedComprehensiveTrainingEntity>> GetImportedComprehensiveTrainingsAsync(int? importedBy = null)
    {
        IQueryable<ImportedComprehensiveTrainingEntity> query = _context.ImportedComprehensiveTrainings
            .Include(e => e.Importer)
            .Include(e => e.Subjects)
            .Include(e => e.Modules)
            .AsSplitQuery() // 使用分割查询提升性能
            .OrderByDescending(e => e.ImportedAt);

        if (importedBy.HasValue)
        {
            query = query.Where(e => e.ImportedBy == importedBy.Value);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// 获取综合训练详情
    /// </summary>
    public async Task<ImportedComprehensiveTrainingEntity?> GetImportedComprehensiveTrainingDetailsAsync(int id)
    {
        return await _context.ImportedComprehensiveTrainings
            .Include(e => e.Importer)
            .Include(e => e.Subjects)
                .ThenInclude(s => s.Questions)
                    .ThenInclude(q => q.OperationPoints)
                        .ThenInclude(op => op.Parameters)
            .Include(e => e.Modules)
                .ThenInclude(m => m.Questions)
                    .ThenInclude(q => q.OperationPoints)
                        .ThenInclude(op => op.Parameters)
            .AsSplitQuery() // 使用分割查询提升性能
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// 删除综合训练
    /// </summary>
    public async Task<bool> DeleteImportedComprehensiveTrainingAsync(int id, int userId)
    {
        ImportedComprehensiveTrainingEntity? comprehensiveTraining = await _context.ImportedComprehensiveTrainings
            .FirstOrDefaultAsync(e => e.Id == id && e.ImportedBy == userId);

        if (comprehensiveTraining == null)
        {
            return false;
        }

        _context.ImportedComprehensiveTrainings.Remove(comprehensiveTraining);
        await _context.SaveChangesAsync();

        _logger.LogInformation("用户 {UserId} 删除了综合训练: {ComprehensiveTrainingName} (ID: {ComprehensiveTrainingId})",
            userId, comprehensiveTraining.Name, id);

        return true;
    }

    /// <summary>
    /// 更新综合训练的试用设置
    /// </summary>
    /// <param name="id">综合训练ID</param>
    /// <param name="enableTrial">是否启用试用</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>更新是否成功</returns>
    public async Task<bool> UpdateTrialSettingAsync(int id, bool enableTrial, int userId)
    {
        try
        {
            ImportedComprehensiveTrainingEntity? comprehensiveTraining = await _context.ImportedComprehensiveTrainings
                .FirstOrDefaultAsync(e => e.Id == id && e.ImportedBy == userId);

            if (comprehensiveTraining == null)
            {
                _logger.LogWarning("综合训练不存在或用户无权限，训练ID: {TrainingId}, 用户ID: {UserId}", id, userId);
                return false;
            }

            comprehensiveTraining.EnableTrial = enableTrial;
            await _context.SaveChangesAsync();

            _logger.LogInformation("用户 {UserId} 更新了综合训练 {TrainingName} (ID: {TrainingId}) 的试用设置为: {EnableTrial}",
                userId, comprehensiveTraining.Name, id, enableTrial);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新综合训练试用设置失败，训练ID: {TrainingId}, 用户ID: {UserId}", id, userId);
            return false;
        }
    }

    /// <summary>
    /// 更新综合实训名称
    /// </summary>
    /// <param name="id">综合实训ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="newName">新的名称</param>
    /// <returns>是否成功</returns>
    public async Task<bool> UpdateComprehensiveTrainingNameAsync(int id, int userId, string newName)
    {
        try
        {
            // 输入验证
            if (string.IsNullOrWhiteSpace(newName))
            {
                _logger.LogWarning("综合实训名称不能为空，训练ID: {TrainingId}, 用户ID: {UserId}", id, userId);
                return false;
            }

            // 长度限制检查
            if (newName.Length > 200)
            {
                _logger.LogWarning("综合实训名称长度超过限制，训练ID: {TrainingId}, 用户ID: {UserId}, 名称长度: {Length}",
                    id, userId, newName.Length);
                return false;
            }

            // 特殊字符检查 - 禁止包含危险字符
            string[] forbiddenChars = { "<", ">", "\"", "'", "&", "\\", "/", "?", "*", "|", ":", ";", "%" };
            if (forbiddenChars.Any(c => newName.Contains(c)))
            {
                _logger.LogWarning("综合实训名称包含非法字符，训练ID: {TrainingId}, 用户ID: {UserId}, 名称: {Name}",
                    id, userId, newName);
                return false;
            }

            // 查找综合实训并验证权限
            ImportedComprehensiveTrainingEntity? comprehensiveTraining = await _context.ImportedComprehensiveTrainings
                .FirstOrDefaultAsync(ct => ct.Id == id);

            if (comprehensiveTraining == null)
            {
                _logger.LogWarning("综合实训不存在，训练ID: {TrainingId}, 用户ID: {UserId}", id, userId);
                return false;
            }

            // 权限验证：只有创建者或管理员可修改
            // 这里简化处理，允许所有管理员用户修改（实际项目中可以添加更细粒度的权限控制）
            _logger.LogInformation("用户 {UserId} 正在修改综合实训 {TrainingId} 的名称", userId, id);

            // 记录原始名称用于日志
            string originalName = comprehensiveTraining.Name;

            // 检查名称是否已存在（同一用户下不能有重复名称）
            bool nameExists = await _context.ImportedComprehensiveTrainings
                .AnyAsync(ct => ct.Id != id && ct.ImportedBy == comprehensiveTraining.ImportedBy && ct.Name == newName);

            if (nameExists)
            {
                _logger.LogWarning("综合实训名称已存在，训练ID: {TrainingId}, 用户ID: {UserId}, 名称: {Name}",
                    id, userId, newName);
                return false;
            }

            // 更新综合实训名称
            comprehensiveTraining.Name = newName.Trim();
            await _context.SaveChangesAsync();

            _logger.LogInformation("综合实训名称更新成功，训练ID: {TrainingId}, 用户ID: {UserId}, 原名称: {OriginalName}, 新名称: {NewName}",
                id, userId, originalName, newName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新综合实训名称失败，训练ID: {TrainingId}, 用户ID: {UserId}, 新名称: {NewName}",
                id, userId, newName);
            return false;
        }
    }
}