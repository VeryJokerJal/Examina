namespace BenchSuite.Models;

/// <summary>
/// 试卷模型 - 简化版本，基于ExamLab.Models.Exam
/// </summary>
public class ExamModel
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 试卷名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 试卷包含的模块
    /// </summary>
    public List<ExamModuleModel> Modules { get; set; } = [];
}

/// <summary>
/// 试卷模块模型
/// </summary>
public class ExamModuleModel
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
    public ModuleType Type { get; set; }

    /// <summary>
    /// 模块描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块分值
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 模块包含的题目
    /// </summary>
    public List<QuestionModel> Questions { get; set; } = [];

    /// <summary>
    /// 是否启用该模块
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 模块排序
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// 题目模型
/// </summary>
public class QuestionModel
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
    /// 题目内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 题目排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 关联的操作点
    /// </summary>
    public List<OperationPointModel> OperationPoints { get; set; } = [];

    /// <summary>
    /// 是否启用该题目
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 操作点模型
/// </summary>
public class OperationPointModel
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
    /// 操作点描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    public ModuleType ModuleType { get; set; } = ModuleType.Windows;

    /// <summary>
    /// PPT知识点类型（当ModuleType为PowerPoint时使用）
    /// </summary>
    public string? PowerPointKnowledgeType { get; set; }

    /// <summary>
    /// Word知识点类型（当ModuleType为Word时使用）
    /// </summary>
    public string? WordKnowledgeType { get; set; }

    /// <summary>
    /// Excel知识点类型（当ModuleType为Excel时使用）
    /// </summary>
    public string? ExcelKnowledgeType { get; set; }

    /// <summary>
    /// 操作点分值
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 配置参数
    /// </summary>
    public List<ConfigurationParameterModel> Parameters { get; set; } = [];

    /// <summary>
    /// 是否启用该操作点
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 操作点排序
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// 配置参数模型
/// </summary>
public class ConfigurationParameterModel
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 参数值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    public ParameterType Type { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }
}

/// <summary>
/// 模块类型枚举
/// </summary>
public enum ModuleType
{
    Excel = 1,
    Word = 2,
    PowerPoint = 3,
    CSharp = 4,
    Windows = 5
}

/// <summary>
/// 参数类型枚举
/// </summary>
public enum ParameterType
{
    Text = 1,
    Number = 2,
    Boolean = 3,
    Enum = 4,
    Date = 5
}
