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
    public async Task<ExamImportResult> ImportExamAsync(Stream fileStream, string fileName, string importedBy)
    {
        ExamImportResult result = new ExamImportResult
        {
            FileName = fileName,
            ImportedBy = importedBy,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 读取文件内容
            using StreamReader reader = new StreamReader(fileStream);
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
                JsonSerializerOptions options = new JsonSerializerOptions
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
                XmlSerializer serializer = new XmlSerializer(typeof(ExamExportDto));
                using StringReader stringReader = new StringReader(content);
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
        List<string> errors = new List<string>();

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
    private async Task<Models.ImportedExam.ImportedExam> ConvertAndSaveExamAsync(ExamExportDto examExportDto, string fileName, long fileSize, string importedBy)
    {
        // 使用执行策略来处理 MySQL 重试机制
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();

            try
            {
            // 创建导入的考试实体
            Models.ImportedExam.ImportedExam importedExam = new Models.ImportedExam.ImportedExam
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
                Models.ImportedExam.ImportedSubject importedSubject = new Models.ImportedExam.ImportedSubject
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
                Models.ImportedExam.ImportedModule importedModule = new Models.ImportedExam.ImportedModule
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
            Models.ImportedExam.ImportedQuestion importedQuestion = new Models.ImportedExam.ImportedQuestion
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
            Models.ImportedExam.ImportedOperationPoint importedOperationPoint = new Models.ImportedExam.ImportedOperationPoint
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
            Models.ImportedExam.ImportedParameter importedParameter = new Models.ImportedExam.ImportedParameter
            {
                OperationPointId = operationPointId,
                Name = parameterDto.Name,
                DisplayName = parameterDto.DisplayName,
                Description = parameterDto.Description,
                Type = parameterDto.Type,
                Value = parameterDto.Value,
                DefaultValue = parameterDto.DefaultValue,
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
    public async Task<List<Models.ImportedExam.ImportedExam>> GetImportedExamsAsync(string? importedBy = null)
    {
        IQueryable<Models.ImportedExam.ImportedExam> query = _context.ImportedExams
            .Include(e => e.Importer)
            .Include(e => e.Subjects)
            .Include(e => e.Modules)
            .OrderByDescending(e => e.ImportedAt);

        if (!string.IsNullOrEmpty(importedBy))
        {
            query = query.Where(e => e.ImportedBy == importedBy);
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
    public async Task<bool> DeleteImportedExamAsync(int examId, string userId)
    {
        Models.ImportedExam.ImportedExam? exam = await _context.ImportedExams
            .FirstOrDefaultAsync(e => e.Id == examId && e.ImportedBy == userId);

        if (exam == null)
            return false;

        _context.ImportedExams.Remove(exam);
        await _context.SaveChangesAsync();
        return true;
    }
}
