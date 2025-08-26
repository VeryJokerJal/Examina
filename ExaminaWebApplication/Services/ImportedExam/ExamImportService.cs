using System.Text.Json;
using System.Xml.Serialization;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.ImportedExam;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Services.ImportedExam;

/// <summary>
/// 考试导入服务
/// </summary>
public class ExamImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExamImportService> _logger;

    public ExamImportService(ApplicationDbContext context, ILogger<ExamImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 导入考试数据
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="importedBy">导入者ID</param>
    /// <returns>导入结果</returns>
    public async Task<ExamImportResult> ImportExamAsync(Stream fileStream, string fileName, int importedBy)
    {
        ExamImportResult result = new()
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
            ExamExportDto? examExportDto = await ParseFileContentAsync(content, fileName);
            if (examExportDto == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "无法解析文件内容，请确保文件格式正确（支持JSON和XML格式）";
                return result;
            }

            // 验证数据
            List<string> validationErrors = ValidateExamData(examExportDto);
            if (validationErrors.Count > 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = string.Join("; ", validationErrors);
                return result;
            }

            // 检查是否已存在相同的考试
            bool examExists = await _context.ImportedExams
                .AnyAsync(e => e.OriginalExamId == examExportDto.Exam.Id);
            
            if (examExists)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"考试 '{examExportDto.Exam.Name}' (ID: {examExportDto.Exam.Id}) 已存在，无法重复导入";
                return result;
            }

            // 转换并保存数据
            Models.ImportedExam.ImportedExam importedExam = await ConvertAndSaveExamAsync(examExportDto, fileName, result.FileSize, importedBy);
            
            result.IsSuccess = true;
            result.ImportedExamId = importedExam.Id;
            result.ImportedExamName = importedExam.Name;
            result.TotalSubjects = importedExam.Subjects.Count;
            result.TotalModules = importedExam.Modules.Count;
            result.TotalQuestions = importedExam.Subjects.Sum(s => s.Questions.Count) + 
                                   importedExam.Modules.Sum(m => m.Questions.Count);
            result.EndTime = DateTime.UtcNow;

            _logger.LogInformation("成功导入考试: {ExamName} (ID: {ExamId})", 
                importedExam.Name, importedExam.Id);

            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"导入过程中发生错误: {ex.Message}";
            result.EndTime = DateTime.UtcNow;

            _logger.LogError(ex, "导入考试失败: {FileName}", fileName);
            return result;
        }
    }

    /// <summary>
    /// 解析文件内容
    /// </summary>
    private async Task<ExamExportDto?> ParseFileContentAsync(string content, string fileName)
    {
        try
        {
            // 尝试解析为JSON
            if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || 
                content.TrimStart().StartsWith("{"))
            {
                JsonSerializerOptions options = new()
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };
                return JsonSerializer.Deserialize<ExamExportDto>(content, options);
            }

            // 尝试解析为XML
            if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || 
                content.TrimStart().StartsWith("<"))
            {
                XmlSerializer serializer = new(typeof(ExamExportDto));
                using StringReader stringReader = new(content);
                return (ExamExportDto?)serializer.Deserialize(stringReader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析文件内容失败: {FileName}", fileName);
            return null;
        }
    }

    /// <summary>
    /// 验证考试数据
    /// </summary>
    private List<string> ValidateExamData(ExamExportDto examExportDto)
    {
        List<string> errors = [];

        // 验证考试基本信息
        if (string.IsNullOrWhiteSpace(examExportDto.Exam.Id))
            errors.Add("考试ID不能为空");

        if (string.IsNullOrWhiteSpace(examExportDto.Exam.Name))
            errors.Add("考试名称不能为空");

        if (examExportDto.Exam.TotalScore <= 0)
            errors.Add("考试总分必须大于0");

        if (examExportDto.Exam.DurationMinutes <= 0)
            errors.Add("考试时长必须大于0分钟");

        // 验证科目和模块
        if (examExportDto.Exam.Subjects.Count == 0 && examExportDto.Exam.Modules.Count == 0)
            errors.Add("考试必须包含至少一个科目或模块");

        // 验证科目
        foreach (SubjectDto subject in examExportDto.Exam.Subjects)
        {
            if (string.IsNullOrWhiteSpace(subject.SubjectName))
                errors.Add($"科目名称不能为空 (科目ID: {subject.Id})");

            if (subject.Score <= 0)
                errors.Add($"科目分值必须大于0 (科目: {subject.SubjectName})");
        }

        // 验证模块
        foreach (ModuleDto module in examExportDto.Exam.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.Name))
                errors.Add($"模块名称不能为空 (模块ID: {module.Id})");

            if (module.Score <= 0)
                errors.Add($"模块分值必须大于0 (模块: {module.Name})");
        }

        return errors;
    }

    /// <summary>
    /// 转换并保存考试数据
    /// </summary>
    private async Task<Models.ImportedExam.ImportedExam> ConvertAndSaveExamAsync(ExamExportDto examExportDto, string fileName, long fileSize, int importedBy)
    {
        // 再次防御性校验导入者存在性
        if (!await _context.Users.AnyAsync(u => u.Id == importedBy))
        {
            throw new InvalidOperationException($"导入者用户不存在或无效（ID={importedBy}），无法保存导入考试。");
        }

        // 使用执行策略来处理 MySQL 重试机制
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();

            try
            {
            // 创建导入的考试实体
            Models.ImportedExam.ImportedExam importedExam = new()
            {
                OriginalExamId = examExportDto.Exam.Id,
                Name = examExportDto.Exam.Name,
                Description = examExportDto.Exam.Description,
                ExamType = examExportDto.Exam.ExamType,
                Status = examExportDto.Exam.Status,
                TotalScore = examExportDto.Exam.TotalScore,
                DurationMinutes = examExportDto.Exam.DurationMinutes,
                StartTime = examExportDto.Exam.StartTime,
                EndTime = examExportDto.Exam.EndTime,
                AllowRetake = examExportDto.Exam.AllowRetake,
                MaxRetakeCount = examExportDto.Exam.MaxRetakeCount,
                PassingScore = examExportDto.Exam.PassingScore,
                RandomizeQuestions = examExportDto.Exam.RandomizeQuestions,
                ShowScore = examExportDto.Exam.ShowScore,
                ShowAnswers = examExportDto.Exam.ShowAnswers,
                IsEnabled = examExportDto.Exam.IsEnabled,
                ExamCategory = ExamCategory.School, // 导入时默认为学校统考
                Tags = examExportDto.Exam.Tags,
                ExtendedConfig = examExportDto.Exam.ExtendedConfig != null ?
                    JsonSerializer.Serialize(examExportDto.Exam.ExtendedConfig) : null,
                ImportedBy = importedBy,
                ImportedAt = DateTime.UtcNow,
                OriginalCreatedBy = examExportDto.Exam.CreatedBy,
                OriginalCreatedAt = examExportDto.Exam.CreatedAt,
                OriginalUpdatedAt = examExportDto.Exam.UpdatedAt,
                OriginalPublishedAt = examExportDto.Exam.PublishedAt,
                OriginalPublishedBy = examExportDto.Exam.PublishedBy,
                ImportFileName = fileName,
                ImportFileSize = fileSize,
                ImportVersion = examExportDto.Metadata.ExportVersion,
                ImportStatus = "Success"
            };

            _context.ImportedExams.Add(importedExam);
            await _context.SaveChangesAsync();

            // 导入科目
            foreach (SubjectDto subjectDto in examExportDto.Exam.Subjects)
            {
                    ImportedSubject importedSubject = new()
                {
                    OriginalSubjectId = subjectDto.Id,
                    ExamId = importedExam.Id,
                    SubjectType = subjectDto.SubjectType,
                    SubjectName = subjectDto.SubjectName,
                    Description = subjectDto.Description,
                    Score = subjectDto.Score,
                    DurationMinutes = subjectDto.DurationMinutes,
                    SortOrder = subjectDto.SortOrder,
                    IsRequired = subjectDto.IsRequired,
                    IsEnabled = subjectDto.IsEnabled,
                    MinScore = subjectDto.MinScore,
                    Weight = subjectDto.Weight,
                    SubjectConfig = subjectDto.SubjectConfig != null ?
                        JsonSerializer.Serialize(subjectDto.SubjectConfig) : null,
                    QuestionCount = subjectDto.QuestionCount,
                    ImportedAt = DateTime.UtcNow
                };

                _context.ImportedSubjects.Add(importedSubject);
                await _context.SaveChangesAsync();

                // 导入科目下的题目
                await ImportQuestionsAsync(subjectDto.Questions, importedExam.Id, importedSubject.Id, null);
            }

            // 导入模块
            foreach (ModuleDto moduleDto in examExportDto.Exam.Modules)
            {
                    ImportedModule importedModule = new()
                {
                    OriginalModuleId = moduleDto.Id,
                    ExamId = importedExam.Id,
                    Name = moduleDto.Name,
                    Type = moduleDto.Type,
                    Description = moduleDto.Description,
                    Score = moduleDto.Score,
                    Order = moduleDto.Order,
                    IsEnabled = moduleDto.IsEnabled,
                    ImportedAt = DateTime.UtcNow
                };

                _context.ImportedModules.Add(importedModule);
                await _context.SaveChangesAsync();

                // 导入模块下的题目
                await ImportQuestionsAsync(moduleDto.Questions, importedExam.Id, null, importedModule.Id);
            }

                await transaction.CommitAsync();
                return importedExam;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    /// <summary>
    /// 导入题目列表
    /// </summary>
    private async Task ImportQuestionsAsync(List<QuestionDto> questions, int examId, int? subjectId, int? moduleId)
    {
        foreach (QuestionDto questionDto in questions)
        {
            ImportedQuestion importedQuestion = new()
            {
                OriginalQuestionId = questionDto.Id,
                ExamId = examId,
                SubjectId = subjectId,
                ModuleId = moduleId,
                Title = questionDto.Title,
                Content = questionDto.Content,
                QuestionType = questionDto.QuestionType,
                Score = questionDto.Score,
                DifficultyLevel = questionDto.DifficultyLevel,
                EstimatedMinutes = questionDto.EstimatedMinutes,
                SortOrder = questionDto.SortOrder,
                IsRequired = questionDto.IsRequired,
                IsEnabled = questionDto.IsEnabled,
                QuestionConfig = questionDto.QuestionConfig != null ?
                    JsonSerializer.Serialize(questionDto.QuestionConfig) : null,
                AnswerValidationRules = questionDto.AnswerValidationRules != null ?
                    JsonSerializer.Serialize(questionDto.AnswerValidationRules) : null,
                StandardAnswer = questionDto.StandardAnswer != null ?
                    JsonSerializer.Serialize(questionDto.StandardAnswer) : null,
                ScoringRules = questionDto.ScoringRules != null ?
                    JsonSerializer.Serialize(questionDto.ScoringRules) : null,
                Tags = questionDto.Tags,
                Remarks = questionDto.Remarks,
                ProgramInput = questionDto.ProgramInput,
                ExpectedOutput = questionDto.ExpectedOutput,
                CodeFilePath = questionDto.CodeFilePath,
                CSharpDirectScore = questionDto.CSharpDirectScore.HasValue ? (decimal)questionDto.CSharpDirectScore.Value : null,
                DocumentFilePath = questionDto.DocumentFilePath,
                OriginalCreatedAt = questionDto.CreatedAt,
                OriginalUpdatedAt = questionDto.UpdatedAt,
                ImportedAt = DateTime.UtcNow
            };

            _context.ImportedQuestions.Add(importedQuestion);
            await _context.SaveChangesAsync();

            // 导入操作点
            await ImportOperationPointsAsync(questionDto.OperationPoints, importedQuestion.Id);
        }
    }

    /// <summary>
    /// 导入操作点列表
    /// </summary>
    private async Task ImportOperationPointsAsync(List<OperationPointDto> operationPoints, int questionId)
    {
        foreach (OperationPointDto operationPointDto in operationPoints)
        {
            ImportedOperationPoint importedOperationPoint = new()
            {
                OriginalOperationPointId = operationPointDto.Id,
                QuestionId = questionId,
                Name = operationPointDto.Name,
                Description = operationPointDto.Description,
                ModuleType = operationPointDto.ModuleType,
                Score = operationPointDto.Score,
                Order = operationPointDto.Order,
                IsEnabled = operationPointDto.IsEnabled,
                CreatedTime = operationPointDto.CreatedTime,
                ImportedAt = DateTime.UtcNow
            };

            _context.ImportedOperationPoints.Add(importedOperationPoint);
            await _context.SaveChangesAsync();

            // 导入参数
            await ImportParametersAsync(operationPointDto.Parameters, importedOperationPoint.Id);
        }
    }

    /// <summary>
    /// 导入参数列表
    /// </summary>
    private async Task ImportParametersAsync(List<ParameterDto> parameters, int operationPointId)
    {
        foreach (ParameterDto parameterDto in parameters)
        {
            ImportedParameter importedParameter = new()
            {
                OperationPointId = operationPointId,
                Name = parameterDto.Name,
                DisplayName = parameterDto.DisplayName,
                Description = parameterDto.Description,
                Type = parameterDto.Type,
                Value = parameterDto.Value,
                DefaultValue = parameterDto.DefaultValue ?? string.Empty, // 确保不为null
                IsRequired = parameterDto.IsRequired,
                Order = parameterDto.Order,
                EnumOptions = parameterDto.EnumOptions,
                ValidationRule = parameterDto.ValidationRule,
                ValidationErrorMessage = parameterDto.ValidationErrorMessage,
                MinValue = parameterDto.MinValue,
                MaxValue = parameterDto.MaxValue,
                IsEnabled = parameterDto.IsEnabled,
                ImportedAt = DateTime.UtcNow
            };

            _context.ImportedParameters.Add(importedParameter);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 获取导入的考试列表
    /// </summary>
    public async Task<List<Models.ImportedExam.ImportedExam>> GetImportedExamsAsync(int? importedBy = null)
    {
        IQueryable<Models.ImportedExam.ImportedExam> query = _context.ImportedExams
            .Include(e => e.Importer)
            .Include(e => e.Subjects)
            .Include(e => e.Modules)
            .OrderByDescending(e => e.ImportedAt);

        if (importedBy.HasValue)
        {
            query = query.Where(e => e.ImportedBy == importedBy.Value);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// 获取导入的考试详情
    /// </summary>
    public async Task<Models.ImportedExam.ImportedExam?> GetImportedExamDetailsAsync(int examId)
    {
        return await _context.ImportedExams
            .Include(e => e.Importer)
            .Include(e => e.Subjects)
                .ThenInclude(s => s.Questions)
                    .ThenInclude(q => q.OperationPoints)
                        .ThenInclude(op => op.Parameters)
            .Include(e => e.Modules)
                .ThenInclude(m => m.Questions)
                    .ThenInclude(q => q.OperationPoints)
                        .ThenInclude(op => op.Parameters)
            .FirstOrDefaultAsync(e => e.Id == examId);
    }

    /// <summary>
    /// 删除导入的考试
    /// </summary>
    public async Task<bool> DeleteImportedExamAsync(int examId, int userId)
    {
        Models.ImportedExam.ImportedExam? exam = await _context.ImportedExams
            .FirstOrDefaultAsync(e => e.Id == examId && e.ImportedBy == userId);

        if (exam == null)
            return false;

        _context.ImportedExams.Remove(exam);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 更新考试类型
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="examCategory">考试类型</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>更新是否成功</returns>
    public async Task<bool> UpdateExamCategoryAsync(int examId, ExamCategory examCategory, int userId)
    {
        try
        {
            // 先检查考试是否存在（暂时不检查权限）
            Models.ImportedExam.ImportedExam? exam = await _context.ImportedExams
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
            {
                _logger.LogWarning("考试不存在，考试ID: {ExamId}", examId);
                return false;
            }

            // 记录操作信息（包含导入者信息以便调试）
            _logger.LogInformation("用户 {UserId} 尝试更新考试 {ExamName} (ID: {ExamId}) 的类型为: {ExamCategory}，考试导入者: {ImportedBy}",
                userId, exam.Name, examId, examCategory, exam.ImportedBy);

            // 更新考试类型
            exam.ExamCategory = examCategory;
            await _context.SaveChangesAsync();

            _logger.LogInformation("考试类型更新成功，考试 {ExamName} (ID: {ExamId}) 的类型已更新为: {ExamCategory}",
                exam.Name, examId, examCategory);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新考试类型失败，考试ID: {ExamId}, 用户ID: {UserId}", examId, userId);
            return false;
        }
    }

    /// <summary>
    /// 更新考试时间安排
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>更新是否成功</returns>
    public async Task<bool> UpdateExamScheduleAsync(int examId, int userId, DateTime startTime, DateTime endTime)
    {
        try
        {
            Models.ImportedExam.ImportedExam? exam = await _context.ImportedExams
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
            {
                _logger.LogWarning("考试不存在，考试ID: {ExamId}", examId);
                return false;
            }

            _logger.LogInformation("用户 {UserId} 尝试更新考试 {ExamName} (ID: {ExamId}) 的时间安排，开始时间: {StartTime}, 结束时间: {EndTime}",
                userId, exam.Name, examId, startTime, endTime);

            // 更新考试时间
            exam.StartTime = startTime;
            exam.EndTime = endTime;

            // 如果状态是草稿，自动更新为已安排
            if (exam.Status == "Draft")
            {
                exam.Status = "Scheduled";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("考试时间安排更新成功，考试 {ExamName} (ID: {ExamId})",
                exam.Name, examId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新考试时间安排失败，考试ID: {ExamId}, 用户ID: {UserId}", examId, userId);
            return false;
        }
    }

    /// <summary>
    /// 更新考试状态
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">操作用户ID</param>
    /// <param name="status">新状态</param>
    /// <returns>更新是否成功</returns>
    public async Task<bool> UpdateExamStatusAsync(int examId, int userId, string status)
    {
        try
        {
            Models.ImportedExam.ImportedExam? exam = await _context.ImportedExams
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
            {
                _logger.LogWarning("考试不存在，考试ID: {ExamId}", examId);
                return false;
            }

            string oldStatus = exam.Status;

            _logger.LogInformation("用户 {UserId} 尝试更新考试 {ExamName} (ID: {ExamId}) 的状态从 {OldStatus} 到 {NewStatus}",
                userId, exam.Name, examId, oldStatus, status);

            // 验证状态转换是否合法
            if (!IsValidStatusTransition(oldStatus, status))
            {
                _logger.LogWarning("无效的状态转换，考试ID: {ExamId}, 当前状态: {CurrentStatus}, 目标状态: {TargetStatus}",
                    examId, oldStatus, status);
                return false;
            }

            // 更新考试状态
            exam.Status = status;
            await _context.SaveChangesAsync();

            _logger.LogInformation("考试状态更新成功，考试 {ExamName} (ID: {ExamId}) 状态从 {OldStatus} 更新为 {NewStatus}",
                exam.Name, examId, oldStatus, status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新考试状态失败，考试ID: {ExamId}, 用户ID: {UserId}", examId, userId);
            return false;
        }
    }

    /// <summary>
    /// 验证状态转换是否合法
    /// </summary>
    /// <param name="currentStatus">当前状态</param>
    /// <param name="targetStatus">目标状态</param>
    /// <returns>是否合法</returns>
    private static bool IsValidStatusTransition(string currentStatus, string targetStatus)
    {
        // 如果状态相同，允许（无需转换）
        if (currentStatus == targetStatus)
        {
            return true;
        }

        return currentStatus switch
        {
            "Draft" => targetStatus is "Scheduled" or "Published" or "Cancelled",
            "Scheduled" => targetStatus is "Draft" or "Published" or "InProgress" or "Cancelled",
            "Published" => targetStatus is "Draft" or "Scheduled" or "InProgress" or "Cancelled",
            "InProgress" => targetStatus is "Published" or "Completed" or "Cancelled",
            "Completed" => targetStatus is "Draft" or "Scheduled" or "Published", // 允许管理员重新设置已完成的考试
            "Cancelled" => targetStatus is "Draft" or "Scheduled" or "Published", // 允许重新激活已取消的考试
            _ => false
        };
    }

    /// <summary>
    /// 根据ID获取导入的考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>考试实体，如果不存在或无权限则返回null</returns>
    public async Task<Models.ImportedExam.ImportedExam?> GetImportedExamByIdAsync(int examId, int userId)
    {
        try
        {
            Models.ImportedExam.ImportedExam? exam = await _context.ImportedExams
                .Include(e => e.Subjects)
                .Include(e => e.Modules)
                .FirstOrDefaultAsync(e => e.Id == examId && e.ImportedBy == userId);

            if (exam == null)
            {
                _logger.LogWarning("考试不存在或用户无权限访问，考试ID: {ExamId}, 用户ID: {UserId}", examId, userId);
                return null;
            }

            _logger.LogInformation("成功获取考试信息，考试ID: {ExamId}, 考试名称: {ExamName}, 用户ID: {UserId}",
                examId, exam.Name, userId);

            return exam;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试信息失败，考试ID: {ExamId}, 用户ID: {UserId}", examId, userId);
            return null;
        }
    }
}
