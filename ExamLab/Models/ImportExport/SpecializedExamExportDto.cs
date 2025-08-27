using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExamLab.Models.ImportExport;

/// <summary>
/// 专项试卷导出数据传输对象 - 专门用于专项试卷的导入导出
/// </summary>
public class SpecializedExamExportDto
{
    /// <summary>
    /// 专项试卷数据
    /// </summary>
    [JsonPropertyName("specializedExam")]
    public SpecializedExamDto SpecializedExam { get; set; } = new();

    /// <summary>
    /// 导出元数据
    /// </summary>
    [JsonPropertyName("metadata")]
    public ExportMetadataDto Metadata { get; set; } = new();

    /// <summary>
    /// 格式版本号
    /// </summary>
    [JsonPropertyName("formatVersion")]
    public string FormatVersion { get; set; } = "1.0";

    /// <summary>
    /// 数据类型标识
    /// </summary>
    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = "SpecializedExam";
}

/// <summary>
/// 专项试卷数据传输对象
/// </summary>
public class SpecializedExamDto
{
    /// <summary>
    /// 专项试卷ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 专项试卷名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 专项试卷描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 专项模块类型
    /// </summary>
    [JsonPropertyName("moduleType")]
    public string ModuleType { get; set; } = "Windows";

    /// <summary>
    /// 难度等级（1-5）
    /// </summary>
    [JsonPropertyName("difficultyLevel")]
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 是否启用随机题目顺序
    /// </summary>
    [JsonPropertyName("randomizeQuestions")]
    public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 专项试卷标签
    /// </summary>
    [JsonPropertyName("tags")]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// 总分
    /// </summary>
    [JsonPropertyName("totalScore")]
    public double TotalScore { get; set; } = 100;

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [JsonPropertyName("duration")]
    public int Duration { get; set; } = 60;

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdTime")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [JsonPropertyName("lastModifiedTime")]
    public DateTime LastModifiedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 专项试卷包含的模块（通常只有一个）
    /// </summary>
    [JsonPropertyName("modules")]
    public List<ModuleDto> Modules { get; set; } = [];

    /// <summary>
    /// 扩展配置（JSON格式）
    /// </summary>
    [JsonPropertyName("extendedConfig")]
    public string? ExtendedConfig { get; set; }
}

/// <summary>
/// 专项试卷导入导出结果
/// </summary>
public class SpecializedExamImportResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 导入的专项试卷
    /// </summary>
    public SpecializedExam? SpecializedExam { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 警告消息列表
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// 是否从通用格式转换而来
    /// </summary>
    public bool IsConvertedFromGeneric { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static SpecializedExamImportResult Success(SpecializedExam specializedExam, bool isConverted = false)
    {
        return new SpecializedExamImportResult
        {
            IsSuccess = true,
            SpecializedExam = specializedExam,
            IsConvertedFromGeneric = isConverted
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static SpecializedExamImportResult Failure(string errorMessage)
    {
        return new SpecializedExamImportResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// 添加警告
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}

/// <summary>
/// 数据格式检测结果
/// </summary>
public enum DataFormatType
{
    /// <summary>
    /// 专项试卷格式
    /// </summary>
    SpecializedExam,

    /// <summary>
    /// 通用试卷格式
    /// </summary>
    GenericExam,

    /// <summary>
    /// 未知格式
    /// </summary>
    Unknown
}

/// <summary>
/// 数据格式检测结果
/// </summary>
public class DataFormatDetectionResult
{
    /// <summary>
    /// 检测到的格式类型
    /// </summary>
    public DataFormatType FormatType { get; set; }

    /// <summary>
    /// 置信度（0-1）
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 检测依据
    /// </summary>
    public string DetectionReason { get; set; } = string.Empty;

    /// <summary>
    /// 是否可以转换为专项试卷
    /// </summary>
    public bool CanConvertToSpecialized { get; set; }

    /// <summary>
    /// 转换建议
    /// </summary>
    public string? ConversionSuggestion { get; set; }
}
