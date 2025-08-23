using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ExaminaWebApplication.Models.ImportedSpecializedTraining;

/// <summary>
/// 导入的专项训练实体
/// </summary>
public class ImportedSpecializedTraining
{
    /// <summary>
    /// 基于 SpecializedTrainingExportDto 创建 ImportedSpecializedTraining 及其层级关联实体的完整映射方法
    /// </summary>
    /// <param name="export">来自 ExamLab 的专项训练导出数据</param>
    /// <param name="importedBy">导入者用户ID</param>
    /// <param name="importFileName">导入文件名</param>
    /// <param name="importFileSize">导入文件大小（字节）</param>
    /// <param name="importStatus">导入状态（默认 Success）</param>
    /// <param name="importErrorMessage">导入错误信息（如有）</param>
    /// <returns>构建完成的 ImportedSpecializedTraining 实例</returns>
    public static ImportedSpecializedTraining FromSpecializedTrainingExportDto(
        SpecializedTrainingExportDto export,
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

        // 根实体映射（SpecializedTrainingDto -> ImportedSpecializedTraining）
        ImportedSpecializedTraining importedSpecializedTraining = new()
        {
            OriginalSpecializedTrainingId = export.SpecializedTraining.Id,
            Name = export.SpecializedTraining.Name,
            Description = export.SpecializedTraining.Description,
            ModuleType = export.SpecializedTraining.ModuleType,
            TotalScore = export.SpecializedTraining.TotalScore,
            Duration = export.SpecializedTraining.Duration,
            DifficultyLevel = export.SpecializedTraining.DifficultyLevel,
            RandomizeQuestions = export.SpecializedTraining.RandomizeQuestions,
            Tags = export.SpecializedTraining.Tags,
            IsEnabled = true, // 导入时默认启用
            ImportedBy = importedBy,
            ImportedAt = now,
            OriginalCreatedTime = ParseDateTimeOrDefault(export.SpecializedTraining.CreatedTime),
            OriginalLastModifiedTime = ParseDateTimeOrDefault(export.SpecializedTraining.LastModifiedTime),
            ImportFileName = importFileName,
            ImportFileSize = importFileSize,
            ImportVersion = export.Metadata?.ExportVersion ?? "1.0",
            ImportStatus = importStatus,
            ImportErrorMessage = importErrorMessage
        };

        // 模块映射（SpecializedTrainingModuleDto -> ImportedSpecializedTrainingModule）
        foreach (SpecializedTrainingModuleDto moduleDto in export.SpecializedTraining.Modules)
        {
            ImportedSpecializedTrainingModule module = new()
            {
                OriginalModuleId = moduleDto.Id,
                Name = moduleDto.Name,
                Type = moduleDto.Type,
                Description = moduleDto.Description,
                Score = moduleDto.Score,
                Order = moduleDto.Order,
                IsEnabled = moduleDto.IsEnabled,
                ImportedAt = now,
                SpecializedTraining = importedSpecializedTraining
            };

            // 题目映射（在模块下）
            foreach (SpecializedTrainingQuestionDto questionDto in moduleDto.Questions)
            {
                ImportedSpecializedTrainingQuestion question = new()
                {
                    OriginalQuestionId = questionDto.Id,
                    Title = questionDto.Title,
                    Content = questionDto.Content,
                    QuestionType = questionDto.QuestionType,
                    Score = questionDto.Score,
                    DifficultyLevel = questionDto.DifficultyLevel,
                    EstimatedMinutes = questionDto.EstimatedMinutes,
                    Order = questionDto.Order,
                    IsRequired = questionDto.IsRequired,
                    IsEnabled = questionDto.IsEnabled,
                    StandardAnswer = questionDto.StandardAnswer,
                    Tags = questionDto.Tags,
                    ImportedAt = now,
                    SpecializedTraining = importedSpecializedTraining,
                    Module = module
                };

                // 操作点映射（在题目下）
                foreach (SpecializedTrainingOperationPointDto opDto in questionDto.OperationPoints)
                {
                    ImportedSpecializedTrainingOperationPoint op = new()
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
                    foreach (SpecializedTrainingParameterDto paramDto in opDto.Parameters)
                    {
                        ImportedSpecializedTrainingParameter parameter = new()
                        {
                            Name = paramDto.Name,
                            DisplayName = paramDto.DisplayName,
                            Description = paramDto.Description,
                            Type = paramDto.Type,
                            Value = paramDto.Value,
                            DefaultValue = paramDto.DefaultValue,
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

            importedSpecializedTraining.Modules.Add(module);
        }

        return importedSpecializedTraining;
    }

    private static DateTime ParseDateTimeOrDefault(string dateTimeString)
    {
        if (DateTime.TryParse(dateTimeString, out DateTime result))
        {
            return result;
        }
        return DateTime.UtcNow;
    }

    /// <summary>
    /// 专项训练ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 原始专项训练ID（来自ExamLab）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OriginalSpecializedTrainingId { get; set; } = string.Empty;

    /// <summary>
    /// 专项训练名称
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 专项训练描述
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 专项模块类型
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ModuleType { get; set; } = "Windows";

    /// <summary>
    /// 试卷总分
    /// </summary>
    public int TotalScore { get; set; } = 100;

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int Duration { get; set; } = 60;

    /// <summary>
    /// 难度等级（1-5）
    /// </summary>
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 是否启用随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 专项训练标签
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

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
    /// 原始创建时间（来自ExamLab）
    /// </summary>
    public DateTime OriginalCreatedTime { get; set; }

    /// <summary>
    /// 原始最后修改时间（来自ExamLab）
    /// </summary>
    public DateTime OriginalLastModifiedTime { get; set; }

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
    /// 模块列表
    /// </summary>
    public virtual ICollection<ImportedSpecializedTrainingModule> Modules { get; set; } = [];

    /// <summary>
    /// 题目列表（包含所有模块下的题目）
    /// </summary>
    public virtual ICollection<ImportedSpecializedTrainingQuestion> Questions { get; set; } = [];

    /// <summary>
    /// 文件关联列表
    /// </summary>
    public virtual ICollection<ExaminaWebApplication.Models.FileUpload.SpecializedTrainingFileAssociation> FileAssociations { get; set; } = [];
}
