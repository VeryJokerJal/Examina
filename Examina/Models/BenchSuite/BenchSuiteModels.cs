using System.ComponentModel;
using BenchSuite.Models;

namespace Examina.Models.BenchSuite;

/// <summary>
/// BenchSuite评分请求模型
/// </summary>
public class BenchSuiteScoringRequest
{
    /// <summary>
    /// 考试ID
    /// </summary>
    public int ExamId { get; set; }

    /// <summary>
    /// 考试类型
    /// </summary>
    public ExamType ExamType { get; set; }

    /// <summary>
    /// 学生用户ID
    /// </summary>
    public int StudentUserId { get; set; }

    /// <summary>
    /// 文件路径列表（按题目类型分组）
    /// </summary>
    public Dictionary<BenchSuiteFileType, List<string>> FilePaths { get; set; } = new();

    /// <summary>
    /// 考试模块信息（用于生成ExamModel）
    /// </summary>
    public List<ExamModuleInfo> ExamModules { get; set; } = new();

    /// <summary>
    /// 基础目录路径
    /// </summary>
    public string BasePath { get; set; } = @"C:\河北对口计算机\";
}

/// <summary>
/// BenchSuite评分结果模型
/// </summary>
public class BenchSuiteScoringResult
{
    /// <summary>
    /// 评分是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 获得分数
    /// </summary>
    public decimal AchievedScore { get; set; }

    /// <summary>
    /// 得分率
    /// </summary>
    public decimal ScoreRate => TotalScore > 0 ? AchievedScore / TotalScore : 0;

    /// <summary>
    /// 各文件类型的评分结果
    /// </summary>
    public Dictionary<BenchSuiteFileType, FileTypeScoringResult> FileTypeResults { get; set; } = new();

    /// <summary>
    /// 评分开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 评分结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 评分耗时（毫秒）
    /// </summary>
    public long ElapsedMilliseconds => (long)(EndTime - StartTime).TotalMilliseconds;
}

/// <summary>
/// 文件类型评分结果
/// </summary>
public class FileTypeScoringResult
{
    /// <summary>
    /// 文件类型
    /// </summary>
    public BenchSuiteFileType FileType { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 获得分数
    /// </summary>
    public decimal AchievedScore { get; set; }

    /// <summary>
    /// 详细结果信息
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 是否评分成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 原始评分结果列表（包含详细的题目和知识点信息）
    /// </summary>
    public List<ScoringResult> OriginalResults { get; set; } = new();
}

/// <summary>
/// 考试模块信息
/// </summary>
public class ExamModuleInfo
{
    /// <summary>
    /// 模块ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 题目列表
    /// </summary>
    public List<QuestionInfo> Questions { get; set; } = new();
}

/// <summary>
/// 题目信息
/// </summary>
public class QuestionInfo
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 题目标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>
    /// 分值
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 操作点列表
    /// </summary>
    public List<OperationPointInfo> OperationPoints { get; set; } = new();
}

/// <summary>
/// 操作点信息
/// </summary>
public class OperationPointInfo
{
    /// <summary>
    /// 操作点ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 操作点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分值
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 参数
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// BenchSuite支持的文件类型
/// </summary>
public enum BenchSuiteFileType
{
    /// <summary>
    /// C#编程文件
    /// </summary>
    [Description("C#编程")]
    CSharp,

    /// <summary>
    /// PowerPoint文件
    /// </summary>
    [Description("PowerPoint")]
    PowerPoint,

    /// <summary>
    /// Word文件
    /// </summary>
    [Description("Word")]
    Word,

    /// <summary>
    /// Excel文件
    /// </summary>
    [Description("Excel")]
    Excel,

    /// <summary>
    /// Windows操作
    /// </summary>
    [Description("Windows操作")]
    Windows
}

/// <summary>
/// 目录结构验证结果
/// </summary>
public class BenchSuiteDirectoryValidationResult
{
    /// <summary>
    /// 验证是否成功
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 缺失的目录列表
    /// </summary>
    public List<string> MissingDirectories { get; set; } = new();

    /// <summary>
    /// 验证详情
    /// </summary>
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// 目录使用情况信息
/// </summary>
public class BenchSuiteDirectoryUsageInfo
{
    /// <summary>
    /// 基础目录路径
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// 总文件数量
    /// </summary>
    public int TotalFileCount { get; set; }

    /// <summary>
    /// 总大小（字节）
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// 各文件类型的使用情况
    /// </summary>
    public Dictionary<BenchSuiteFileType, DirectoryTypeUsage> TypeUsages { get; set; } = new();

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// 目录类型使用情况
/// </summary>
public class DirectoryTypeUsage
{
    /// <summary>
    /// 文件类型
    /// </summary>
    public BenchSuiteFileType FileType { get; set; }

    /// <summary>
    /// 目录路径
    /// </summary>
    public string DirectoryPath { get; set; } = string.Empty;

    /// <summary>
    /// 文件数量
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// 目录大小（字节）
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// 最新文件时间
    /// </summary>
    public DateTime? LatestFileTime { get; set; }

    /// <summary>
    /// 最旧文件时间
    /// </summary>
    public DateTime? OldestFileTime { get; set; }
}
