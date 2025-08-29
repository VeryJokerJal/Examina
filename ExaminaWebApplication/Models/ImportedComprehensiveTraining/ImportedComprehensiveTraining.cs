using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ExaminaWebApplication.Services.ImportedComprehensiveTraining;

namespace ExaminaWebApplication.Models.ImportedComprehensiveTraining;

/// <summary>
/// 导入的综合训练实体
/// </summary>
public class ImportedComprehensiveTraining
{
    /// <summary>
    /// 基于 ComprehensiveTrainingExportDto 创建 ImportedComprehensiveTraining 及其层级关联实体（科目/模块/题目/操作点/参数）的完整映射方法
    /// 注意：此方法只构建对象图，不负责持久化；保存到数据库时由 EF Core 根据导航属性自动设定外键。
    /// </summary>
    /// <param name="export">来自 ExamLab 的导出数据</param>
    /// <param name="importedBy">导入者用户ID</param>
    /// <param name="importFileName">导入文件名</param>
    /// <param name="importFileSize">导入文件大小（字节）</param>
    /// <param name="importStatus">导入状态（默认 Success）</param>
    /// <param name="importErrorMessage">导入错误信息（如有）</param>
    /// <returns>构建完成的 ImportedComprehensiveTraining 实例</returns>
    public static ImportedComprehensiveTraining FromComprehensiveTrainingExportDto(
        ComprehensiveTrainingExportDto export,
        int importedBy,
        string? importFileName = null,
        long importFileSize = 0,
        string importStatus = "Success",
        string? importErrorMessage = null)
    {
        if (export == null)
        {
            throw new ArgumentNullException(nameof(export));
        }
        if (importedBy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(importedBy), "导入者用户ID必须为正整数");
        }

        DateTime now = DateTime.UtcNow;

        // 根实体映射（ComprehensiveTrainingDto -> ImportedComprehensiveTraining）
        ImportedComprehensiveTraining importedComprehensiveTraining = new()
        {
            OriginalComprehensiveTrainingId = export.ComprehensiveTraining.Id,
            Name = export.ComprehensiveTraining.Name,
            Description = export.ComprehensiveTraining.Description,
            ComprehensiveTrainingType = export.ComprehensiveTraining.ComprehensiveTrainingType,
            Status = export.ComprehensiveTraining.Status,
            TotalScore = export.ComprehensiveTraining.TotalScore,
            DurationMinutes = export.ComprehensiveTraining.DurationMinutes,
            StartTime = export.ComprehensiveTraining.StartTime,
            EndTime = export.ComprehensiveTraining.EndTime,
            AllowRetake = export.ComprehensiveTraining.AllowRetake,
            MaxRetakeCount = export.ComprehensiveTraining.MaxRetakeCount,
            PassingScore = export.ComprehensiveTraining.PassingScore,
            RandomizeQuestions = export.ComprehensiveTraining.RandomizeQuestions,
            ShowScore = export.ComprehensiveTraining.ShowScore,
            ShowAnswers = export.ComprehensiveTraining.ShowAnswers,
            IsEnabled = export.ComprehensiveTraining.IsEnabled,
            EnableTrial = true, // 导入时默认启用试用功能
            Tags = export.ComprehensiveTraining.Tags,
            ExtendedConfig = SerializeToJsonOrNull(export.ComprehensiveTraining.ExtendedConfig),
            ImportedBy = importedBy,
            ImportedAt = now,
            OriginalCreatedBy = export.ComprehensiveTraining.CreatedBy,
            OriginalCreatedAt = export.ComprehensiveTraining.CreatedAt,
            OriginalUpdatedAt = export.ComprehensiveTraining.UpdatedAt,
            OriginalPublishedAt = export.ComprehensiveTraining.PublishedAt,
            OriginalPublishedBy = export.ComprehensiveTraining.PublishedBy,
            ImportFileName = importFileName,
            ImportFileSize = importFileSize,
            ImportVersion = export.Metadata != null ? export.Metadata.ExportVersion : "1.0",
            ImportStatus = importStatus,
            ImportErrorMessage = importErrorMessage
        };

        // 科目映射（ComprehensiveTrainingSubjectDto -> ImportedComprehensiveTrainingSubject）
        foreach (ComprehensiveTrainingSubjectDto subjectDto in export.ComprehensiveTraining.Subjects)
        {
            ImportedComprehensiveTrainingSubject subject = new()
            {
                OriginalSubjectId = subjectDto.Id,
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
                SubjectConfig = SerializeToJsonOrNull(subjectDto.SubjectConfig),
                QuestionCount = subjectDto.QuestionCount,
                ImportedAt = now,
                ComprehensiveTraining = importedComprehensiveTraining
            };

            // 题目映射（在科目下）
            foreach (ComprehensiveTrainingQuestionDto questionDto in subjectDto.Questions)
            {
                ImportedComprehensiveTrainingQuestion question = new()
                {
                    OriginalQuestionId = questionDto.Id,
                    Title = questionDto.Title,
                    Content = questionDto.Content,
                    QuestionType = questionDto.QuestionType,
                    Score = questionDto.Score,
                    DifficultyLevel = questionDto.DifficultyLevel,
                    EstimatedMinutes = questionDto.EstimatedMinutes,
                    SortOrder = questionDto.SortOrder,
                    IsRequired = questionDto.IsRequired,
                    IsEnabled = questionDto.IsEnabled,
                    QuestionConfig = SerializeToJsonOrNull(questionDto.QuestionConfig),
                    AnswerValidationRules = SerializeToJsonOrNull(questionDto.AnswerValidationRules),
                    StandardAnswer = SerializeToJsonOrNull(questionDto.StandardAnswer),
                    ScoringRules = SerializeToJsonOrNull(questionDto.ScoringRules),
                    Tags = questionDto.Tags,
                    Remarks = questionDto.Remarks,
                    ProgramInput = questionDto.ProgramInput,
                    ExpectedOutput = questionDto.ExpectedOutput,
                    CodeFilePath = questionDto.CodeFilePath,
                    DocumentFilePath = questionDto.DocumentFilePath,
                    OriginalCreatedAt = questionDto.CreatedAt,
                    OriginalUpdatedAt = questionDto.UpdatedAt,
                    ImportedAt = now,
                    ComprehensiveTraining = importedComprehensiveTraining,
                    Subject = subject
                };

                // 操作点映射（在题目下）
                foreach (ComprehensiveTrainingOperationPointDto opDto in questionDto.OperationPoints)
                {
                    ImportedComprehensiveTrainingOperationPoint op = new()
                    {
                        OriginalOperationPointId = opDto.Id,
                        Name = opDto.Name,
                        Description = opDto.Description,
                        ModuleType = opDto.ModuleType,
                        Score = opDto.Score,
                        Order = opDto.Order,
                        IsEnabled = opDto.IsEnabled,
                        CreatedTime = opDto.CreatedTime,
                        ImportedAt = now,
                        Question = question
                    };

                    // 参数映射（在操作点下）
                    foreach (ComprehensiveTrainingParameterDto paramDto in opDto.Parameters)
                    {
                        ImportedComprehensiveTrainingParameter parameter = new()
                        {
                            Name = paramDto.Name,
                            DisplayName = paramDto.DisplayName,
                            Description = paramDto.Description,
                            Type = paramDto.Type,
                            Value = paramDto.Value,
                            DefaultValue = paramDto.DefaultValue ?? string.Empty,
                            IsRequired = paramDto.IsRequired,
                            Order = paramDto.Order,
                            EnumOptions = paramDto.EnumOptions,
                            ValidationRule = paramDto.ValidationRule,
                            ValidationErrorMessage = paramDto.ValidationErrorMessage,
                            MinValue = paramDto.MinValue,
                            MaxValue = paramDto.MaxValue,
                            IsEnabled = paramDto.IsEnabled,
                            ImportedAt = now,
                            OperationPoint = op
                        };

                        op.Parameters.Add(parameter);
                    }

                    question.OperationPoints.Add(op);
                }

                subject.Questions.Add(question);
            }

            importedComprehensiveTraining.Subjects.Add(subject);
        }

        // 模块映射（ComprehensiveTrainingModuleDto -> ImportedComprehensiveTrainingModule）
        foreach (ComprehensiveTrainingModuleDto moduleDto in export.ComprehensiveTraining.Modules)
        {
            ImportedComprehensiveTrainingModule module = new()
            {
                OriginalModuleId = moduleDto.Id,
                Name = moduleDto.Name,
                Type = moduleDto.Type,
                Description = moduleDto.Description,
                Score = moduleDto.Score,
                Order = moduleDto.Order,
                IsEnabled = moduleDto.IsEnabled,
                ImportedAt = now,
                ComprehensiveTraining = importedComprehensiveTraining
            };

            // 题目映射（在模块下）
            foreach (ComprehensiveTrainingQuestionDto questionDto in moduleDto.Questions)
            {
                ImportedComprehensiveTrainingQuestion question = new()
                {
                    OriginalQuestionId = questionDto.Id,
                    Title = questionDto.Title,
                    Content = questionDto.Content,
                    QuestionType = questionDto.QuestionType,
                    Score = questionDto.Score,
                    DifficultyLevel = questionDto.DifficultyLevel,
                    EstimatedMinutes = questionDto.EstimatedMinutes,
                    SortOrder = questionDto.SortOrder,
                    IsRequired = questionDto.IsRequired,
                    IsEnabled = questionDto.IsEnabled,
                    QuestionConfig = SerializeToJsonOrNull(questionDto.QuestionConfig),
                    AnswerValidationRules = SerializeToJsonOrNull(questionDto.AnswerValidationRules),
                    StandardAnswer = SerializeToJsonOrNull(questionDto.StandardAnswer),
                    ScoringRules = SerializeToJsonOrNull(questionDto.ScoringRules),
                    Tags = questionDto.Tags,
                    Remarks = questionDto.Remarks,
                    ProgramInput = questionDto.ProgramInput,
                    ExpectedOutput = questionDto.ExpectedOutput,
                    CSharpQuestionType = questionDto.CSharpQuestionType,
                    CodeFilePath = questionDto.CodeFilePath,
                    CSharpDirectScore = questionDto.CSharpDirectScore,
                    CodeBlanks = SerializeToJsonOrNull(questionDto.CodeBlanks),
                    TemplateCode = questionDto.TemplateCode,
                    DocumentFilePath = questionDto.DocumentFilePath,
                    OriginalCreatedAt = questionDto.CreatedAt,
                    OriginalUpdatedAt = questionDto.UpdatedAt,
                    ImportedAt = now,
                    ComprehensiveTraining = importedComprehensiveTraining,
                    Module = module
                };

                // 操作点映射（在题目下）
                foreach (ComprehensiveTrainingOperationPointDto opDto in questionDto.OperationPoints)
                {
                    ImportedComprehensiveTrainingOperationPoint op = new()
                    {
                        OriginalOperationPointId = opDto.Id,
                        Name = opDto.Name,
                        Description = opDto.Description,
                        ModuleType = opDto.ModuleType,
                        Score = opDto.Score,
                        Order = opDto.Order,
                        IsEnabled = opDto.IsEnabled,
                        CreatedTime = opDto.CreatedTime,
                        ImportedAt = now,
                        Question = question
                    };

                    // 参数映射（在操作点下）
                    foreach (ComprehensiveTrainingParameterDto paramDto in opDto.Parameters)
                    {
                        ImportedComprehensiveTrainingParameter parameter = new()
                        {
                            Name = paramDto.Name,
                            DisplayName = paramDto.DisplayName,
                            Description = paramDto.Description,
                            Type = paramDto.Type,
                            Value = paramDto.Value,
                            DefaultValue = paramDto.DefaultValue ?? string.Empty,
                            IsRequired = paramDto.IsRequired,
                            Order = paramDto.Order,
                            EnumOptions = paramDto.EnumOptions,
                            ValidationRule = paramDto.ValidationRule,
                            ValidationErrorMessage = paramDto.ValidationErrorMessage,
                            MinValue = paramDto.MinValue,
                            MaxValue = paramDto.MaxValue,
                            IsEnabled = paramDto.IsEnabled,
                            ImportedAt = now,
                            OperationPoint = op
                        };

                        op.Parameters.Add(parameter);
                    }

                    question.OperationPoints.Add(op);
                }

                module.Questions.Add(question);
            }

            importedComprehensiveTraining.Modules.Add(module);
        }

        return importedComprehensiveTraining;
    }

    private static string? SerializeToJsonOrNull(object? value)
    {
        if (value == null)
        {
            return null;
        }

        JsonSerializerOptions options = new()
        {
            WriteIndented = false
        };

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// 综合训练ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 原始综合训练ID（来自ExamLab）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OriginalComprehensiveTrainingId { get; set; } = string.Empty;

    /// <summary>
    /// 综合训练名称
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 综合训练描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 综合训练类型
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ComprehensiveTrainingType { get; set; } = "UnifiedTraining";

    /// <summary>
    /// 综合训练状态
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// 总分
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public double TotalScore { get; set; } = 100.0;

    /// <summary>
    /// 训练时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 训练开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 训练结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重做
    /// </summary>
    public bool AllowRetake { get; set; } = false;

    /// <summary>
    /// 最大重做次数
    /// </summary>
    public int MaxRetakeCount { get; set; } = 0;

    /// <summary>
    /// 及格分数
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public double PassingScore { get; set; } = 60.0;

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 是否显示分数
    /// </summary>
    public bool ShowScore { get; set; } = true;

    /// <summary>
    /// 是否显示答案
    /// </summary>
    public bool ShowAnswers { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否启用试用功能
    /// </summary>
    public bool EnableTrial { get; set; } = true;

    /// <summary>
    /// 训练标签
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 扩展配置（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? ExtendedConfig { get; set; }

    /// <summary>
    /// 导入者ID
    /// </summary>
    [Required]
    public int ImportedBy { get; set; }

    /// <summary>
    /// 导入时间
    /// </summary>
    [Required]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 原始创建者ID（来自ExamLab）
    /// </summary>
    public int OriginalCreatedBy { get; set; }

    /// <summary>
    /// 原始创建时间（来自ExamLab）
    /// </summary>
    public DateTime OriginalCreatedAt { get; set; }

    /// <summary>
    /// 原始更新时间（来自ExamLab）
    /// </summary>
    public DateTime? OriginalUpdatedAt { get; set; }

    /// <summary>
    /// 原始发布时间（来自ExamLab）
    /// </summary>
    public DateTime? OriginalPublishedAt { get; set; }

    /// <summary>
    /// 原始发布者ID（来自ExamLab）
    /// </summary>
    public int? OriginalPublishedBy { get; set; }

    /// <summary>
    /// 导入文件名
    /// </summary>
    [StringLength(255)]
    public string? ImportFileName { get; set; }

    /// <summary>
    /// 导入文件大小（字节）
    /// </summary>
    public long ImportFileSize { get; set; }

    /// <summary>
    /// 导入版本
    /// </summary>
    [StringLength(20)]
    public string ImportVersion { get; set; } = "1.0";

    /// <summary>
    /// 导入状态
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ImportStatus { get; set; } = "Success";

    /// <summary>
    /// 导入错误信息
    /// </summary>
    [StringLength(2000)]
    public string? ImportErrorMessage { get; set; }

    /// <summary>
    /// 导入者用户
    /// </summary>
    [ForeignKey(nameof(ImportedBy))]
    public virtual User? Importer { get; set; }

    /// <summary>
    /// 科目列表
    /// </summary>
    public virtual ICollection<ImportedComprehensiveTrainingSubject> Subjects { get; set; } = [];

    /// <summary>
    /// 模块列表
    /// </summary>
    public virtual ICollection<ImportedComprehensiveTrainingModule> Modules { get; set; } = [];

    /// <summary>
    /// 题目列表（包含所有科目和模块下的题目）
    /// </summary>
    public virtual ICollection<ImportedComprehensiveTrainingQuestion> Questions { get; set; } = [];

    /// <summary>
    /// 文件关联列表
    /// </summary>
    public virtual ICollection<ExaminaWebApplication.Models.FileUpload.ComprehensiveTrainingFileAssociation> FileAssociations { get; set; } = [];
}
