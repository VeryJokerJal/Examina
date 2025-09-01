using System.Text.Json.Serialization;

namespace BenchSuite.Models;

/// <summary>
/// AI代码结构分析结果模型 - 符合OpenAI Structured Output要求
/// </summary>
public class CodeStructureAnalysisResponse
{
    /// <summary>
    /// 分析步骤数组
    /// </summary>
    [JsonPropertyName("analysis_steps")]
    public List<CodeAnalysisStep> AnalysisSteps { get; set; } = [];

    /// <summary>
    /// 代码结构概述
    /// </summary>
    [JsonPropertyName("structure_overview")]
    public string StructureOverview { get; set; } = string.Empty;

    /// <summary>
    /// 检测到的设计模式
    /// </summary>
    [JsonPropertyName("design_patterns")]
    public List<string> DesignPatterns { get; set; } = [];

    /// <summary>
    /// 架构评分（0-100）
    /// </summary>
    [JsonPropertyName("architecture_score")]
    public int ArchitectureScore { get; set; }

    /// <summary>
    /// 代码组织建议
    /// </summary>
    [JsonPropertyName("organization_suggestions")]
    public List<string> OrganizationSuggestions { get; set; } = [];
}

/// <summary>
/// 代码分析步骤
/// </summary>
public class CodeAnalysisStep
{
    /// <summary>
    /// 步骤说明
    /// </summary>
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// 分析结果
    /// </summary>
    [JsonPropertyName("finding")]
    public string Finding { get; set; } = string.Empty;

    /// <summary>
    /// 步骤类型
    /// </summary>
    [JsonPropertyName("step_type")]
    public string StepType { get; set; } = string.Empty;
}

/// <summary>
/// 代码质量评估模型
/// </summary>
public class CodeQualityAssessmentResponse
{
    /// <summary>
    /// 质量评估步骤
    /// </summary>
    [JsonPropertyName("assessment_steps")]
    public List<QualityAssessmentStep> AssessmentSteps { get; set; } = [];

    /// <summary>
    /// 总体质量评分（0-100）
    /// </summary>
    [JsonPropertyName("overall_quality_score")]
    public int OverallQualityScore { get; set; }

    /// <summary>
    /// 各维度评分
    /// </summary>
    [JsonPropertyName("dimension_scores")]
    public QualityDimensionScores DimensionScores { get; set; } = new();

    /// <summary>
    /// 检测到的代码问题
    /// </summary>
    [JsonPropertyName("code_issues")]
    public List<CodeIssue> CodeIssues { get; set; } = [];

    /// <summary>
    /// 质量改进建议
    /// </summary>
    [JsonPropertyName("improvement_recommendations")]
    public List<string> ImprovementRecommendations { get; set; } = [];

    /// <summary>
    /// 最佳实践遵循情况
    /// </summary>
    [JsonPropertyName("best_practices_compliance")]
    public List<BestPracticeCompliance> BestPracticesCompliance { get; set; } = [];
}

/// <summary>
/// 质量评估步骤
/// </summary>
public class QualityAssessmentStep
{
    /// <summary>
    /// 评估维度
    /// </summary>
    [JsonPropertyName("dimension")]
    public string Dimension { get; set; } = string.Empty;

    /// <summary>
    /// 评估说明
    /// </summary>
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// 评估结果
    /// </summary>
    [JsonPropertyName("assessment_result")]
    public string AssessmentResult { get; set; } = string.Empty;

    /// <summary>
    /// 维度评分（0-100）
    /// </summary>
    [JsonPropertyName("dimension_score")]
    public int DimensionScore { get; set; }
}

/// <summary>
/// 质量维度评分
/// </summary>
public class QualityDimensionScores
{
    /// <summary>
    /// 可读性评分
    /// </summary>
    [JsonPropertyName("readability")]
    public int Readability { get; set; }

    /// <summary>
    /// 可维护性评分
    /// </summary>
    [JsonPropertyName("maintainability")]
    public int Maintainability { get; set; }

    /// <summary>
    /// 性能评分
    /// </summary>
    [JsonPropertyName("performance")]
    public int Performance { get; set; }

    /// <summary>
    /// 安全性评分
    /// </summary>
    [JsonPropertyName("security")]
    public int Security { get; set; }

    /// <summary>
    /// 测试覆盖度评分
    /// </summary>
    [JsonPropertyName("testability")]
    public int Testability { get; set; }
}

/// <summary>
/// 代码问题
/// </summary>
public class CodeIssue
{
    /// <summary>
    /// 问题类型
    /// </summary>
    [JsonPropertyName("issue_type")]
    public string IssueType { get; set; } = string.Empty;

    /// <summary>
    /// 问题描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 严重程度
    /// </summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// 问题位置（行号）
    /// </summary>
    [JsonPropertyName("line_number")]
    public int? LineNumber { get; set; }

    /// <summary>
    /// 问题位置（文件名）
    /// </summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    /// <summary>
    /// 修复建议
    /// </summary>
    [JsonPropertyName("fix_suggestion")]
    public string? FixSuggestion { get; set; }

    /// <summary>
    /// 影响范围
    /// </summary>
    [JsonPropertyName("impact_scope")]
    public string ImpactScope { get; set; } = string.Empty;
}

/// <summary>
/// 最佳实践遵循情况
/// </summary>
public class BestPracticeCompliance
{
    /// <summary>
    /// 实践名称
    /// </summary>
    [JsonPropertyName("practice_name")]
    public string PracticeName { get; set; } = string.Empty;

    /// <summary>
    /// 遵循状态
    /// </summary>
    [JsonPropertyName("compliance_status")]
    public string ComplianceStatus { get; set; } = string.Empty;

    /// <summary>
    /// 详细说明
    /// </summary>
    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 改进建议
    /// </summary>
    [JsonPropertyName("improvement_suggestion")]
    public string? ImprovementSuggestion { get; set; }
}

/// <summary>
/// 代码解释和注释生成模型
/// </summary>
public class CodeExplanationResponse
{
    /// <summary>
    /// 代码整体解释
    /// </summary>
    [JsonPropertyName("overall_explanation")]
    public string OverallExplanation { get; set; } = string.Empty;

    /// <summary>
    /// 逐行解释
    /// </summary>
    [JsonPropertyName("line_by_line_explanations")]
    public List<LineExplanation> LineByLineExplanations { get; set; } = [];

    /// <summary>
    /// 关键概念解释
    /// </summary>
    [JsonPropertyName("key_concepts")]
    public List<ConceptExplanation> KeyConcepts { get; set; } = [];

    /// <summary>
    /// 生成的注释建议
    /// </summary>
    [JsonPropertyName("comment_suggestions")]
    public List<CommentSuggestion> CommentSuggestions { get; set; } = [];

    /// <summary>
    /// 算法复杂度分析
    /// </summary>
    [JsonPropertyName("complexity_analysis")]
    public ComplexityAnalysis ComplexityAnalysis { get; set; } = new();
}

/// <summary>
/// 逐行解释
/// </summary>
public class LineExplanation
{
    /// <summary>
    /// 行号
    /// </summary>
    [JsonPropertyName("line_number")]
    public int LineNumber { get; set; }

    /// <summary>
    /// 代码内容
    /// </summary>
    [JsonPropertyName("code_content")]
    public string CodeContent { get; set; } = string.Empty;

    /// <summary>
    /// 解释内容
    /// </summary>
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// 重要程度
    /// </summary>
    [JsonPropertyName("importance_level")]
    public string ImportanceLevel { get; set; } = string.Empty;
}

/// <summary>
/// 概念解释
/// </summary>
public class ConceptExplanation
{
    /// <summary>
    /// 概念名称
    /// </summary>
    [JsonPropertyName("concept_name")]
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// 概念解释
    /// </summary>
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// 使用示例
    /// </summary>
    [JsonPropertyName("usage_example")]
    public string? UsageExample { get; set; }

    /// <summary>
    /// 相关概念
    /// </summary>
    [JsonPropertyName("related_concepts")]
    public List<string> RelatedConcepts { get; set; } = [];
}

/// <summary>
/// 注释建议
/// </summary>
public class CommentSuggestion
{
    /// <summary>
    /// 位置（行号）
    /// </summary>
    [JsonPropertyName("line_number")]
    public int LineNumber { get; set; }

    /// <summary>
    /// 注释类型
    /// </summary>
    [JsonPropertyName("comment_type")]
    public string CommentType { get; set; } = string.Empty;

    /// <summary>
    /// 建议的注释内容
    /// </summary>
    [JsonPropertyName("suggested_comment")]
    public string SuggestedComment { get; set; } = string.Empty;

    /// <summary>
    /// 注释的重要性
    /// </summary>
    [JsonPropertyName("importance")]
    public string Importance { get; set; } = string.Empty;
}

/// <summary>
/// 复杂度分析
/// </summary>
public class ComplexityAnalysis
{
    /// <summary>
    /// 时间复杂度
    /// </summary>
    [JsonPropertyName("time_complexity")]
    public string TimeComplexity { get; set; } = string.Empty;

    /// <summary>
    /// 空间复杂度
    /// </summary>
    [JsonPropertyName("space_complexity")]
    public string SpaceComplexity { get; set; } = string.Empty;

    /// <summary>
    /// 复杂度解释
    /// </summary>
    [JsonPropertyName("complexity_explanation")]
    public string ComplexityExplanation { get; set; } = string.Empty;

    /// <summary>
    /// 优化建议
    /// </summary>
    [JsonPropertyName("optimization_suggestions")]
    public List<string> OptimizationSuggestions { get; set; } = [];
}

/// <summary>
/// 代码改进建议模型
/// </summary>
public class CodeImprovementResponse
{
    /// <summary>
    /// 改进建议列表
    /// </summary>
    [JsonPropertyName("improvement_suggestions")]
    public List<ImprovementSuggestion> ImprovementSuggestions { get; set; } = [];

    /// <summary>
    /// 重构建议
    /// </summary>
    [JsonPropertyName("refactoring_suggestions")]
    public List<RefactoringSuggestion> RefactoringSuggestions { get; set; } = [];

    /// <summary>
    /// 性能优化建议
    /// </summary>
    [JsonPropertyName("performance_improvements")]
    public List<PerformanceImprovement> PerformanceImprovements { get; set; } = [];

    /// <summary>
    /// 代码风格建议
    /// </summary>
    [JsonPropertyName("style_improvements")]
    public List<StyleImprovement> StyleImprovements { get; set; } = [];

    /// <summary>
    /// 优先级排序
    /// </summary>
    [JsonPropertyName("priority_ranking")]
    public List<string> PriorityRanking { get; set; } = [];
}

/// <summary>
/// 改进建议
/// </summary>
public class ImprovementSuggestion
{
    /// <summary>
    /// 建议类型
    /// </summary>
    [JsonPropertyName("suggestion_type")]
    public string SuggestionType { get; set; } = string.Empty;

    /// <summary>
    /// 建议描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 当前代码
    /// </summary>
    [JsonPropertyName("current_code")]
    public string? CurrentCode { get; set; }

    /// <summary>
    /// 改进后代码
    /// </summary>
    [JsonPropertyName("improved_code")]
    public string? ImprovedCode { get; set; }

    /// <summary>
    /// 改进理由
    /// </summary>
    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;

    /// <summary>
    /// 影响程度
    /// </summary>
    [JsonPropertyName("impact_level")]
    public string ImpactLevel { get; set; } = string.Empty;
}

/// <summary>
/// 重构建议
/// </summary>
public class RefactoringSuggestion
{
    /// <summary>
    /// 重构类型
    /// </summary>
    [JsonPropertyName("refactoring_type")]
    public string RefactoringType { get; set; } = string.Empty;

    /// <summary>
    /// 目标代码区域
    /// </summary>
    [JsonPropertyName("target_area")]
    public string TargetArea { get; set; } = string.Empty;

    /// <summary>
    /// 重构描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 预期收益
    /// </summary>
    [JsonPropertyName("expected_benefits")]
    public List<string> ExpectedBenefits { get; set; } = [];

    /// <summary>
    /// 实施难度
    /// </summary>
    [JsonPropertyName("implementation_difficulty")]
    public string ImplementationDifficulty { get; set; } = string.Empty;
}

/// <summary>
/// 性能改进建议
/// </summary>
public class PerformanceImprovement
{
    /// <summary>
    /// 性能问题描述
    /// </summary>
    [JsonPropertyName("performance_issue")]
    public string PerformanceIssue { get; set; } = string.Empty;

    /// <summary>
    /// 改进方案
    /// </summary>
    [JsonPropertyName("improvement_solution")]
    public string ImprovementSolution { get; set; } = string.Empty;

    /// <summary>
    /// 预期性能提升
    /// </summary>
    [JsonPropertyName("expected_improvement")]
    public string ExpectedImprovement { get; set; } = string.Empty;

    /// <summary>
    /// 实施复杂度
    /// </summary>
    [JsonPropertyName("implementation_complexity")]
    public string ImplementationComplexity { get; set; } = string.Empty;
}

/// <summary>
/// 代码风格改进
/// </summary>
public class StyleImprovement
{
    /// <summary>
    /// 风格问题类型
    /// </summary>
    [JsonPropertyName("style_issue_type")]
    public string StyleIssueType { get; set; } = string.Empty;

    /// <summary>
    /// 问题描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 建议的改进
    /// </summary>
    [JsonPropertyName("suggested_improvement")]
    public string SuggestedImprovement { get; set; } = string.Empty;

    /// <summary>
    /// 相关编码标准
    /// </summary>
    [JsonPropertyName("coding_standard")]
    public string? CodingStandard { get; set; }
}

/// <summary>
/// JSON Schema常量定义
/// </summary>
public static class AIJsonSchemas
{
    /// <summary>
    /// 代码结构分析JSON Schema
    /// </summary>
    public const string CodeStructureAnalysisSchema = """
        {
            "type": "object",
            "properties": {
                "analysis_steps": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "explanation": { "type": "string" },
                            "finding": { "type": "string" },
                            "step_type": { "type": "string" }
                        },
                        "required": ["explanation", "finding", "step_type"],
                        "additionalProperties": false
                    }
                },
                "structure_overview": { "type": "string" },
                "design_patterns": {
                    "type": "array",
                    "items": { "type": "string" }
                },
                "architecture_score": {
                    "type": "integer",
                    "minimum": 0,
                    "maximum": 100
                },
                "organization_suggestions": {
                    "type": "array",
                    "items": { "type": "string" }
                }
            },
            "required": ["analysis_steps", "structure_overview", "design_patterns", "architecture_score", "organization_suggestions"],
            "additionalProperties": false
        }
        """;

    /// <summary>
    /// 代码质量评估JSON Schema
    /// </summary>
    public const string CodeQualityAssessmentSchema = """
        {
            "type": "object",
            "properties": {
                "assessment_steps": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "dimension": { "type": "string" },
                            "explanation": { "type": "string" },
                            "assessment_result": { "type": "string" },
                            "dimension_score": {
                                "type": "integer",
                                "minimum": 0,
                                "maximum": 100
                            }
                        },
                        "required": ["dimension", "explanation", "assessment_result", "dimension_score"],
                        "additionalProperties": false
                    }
                },
                "overall_quality_score": {
                    "type": "integer",
                    "minimum": 0,
                    "maximum": 100
                },
                "dimension_scores": {
                    "type": "object",
                    "properties": {
                        "readability": { "type": "integer", "minimum": 0, "maximum": 100 },
                        "maintainability": { "type": "integer", "minimum": 0, "maximum": 100 },
                        "performance": { "type": "integer", "minimum": 0, "maximum": 100 },
                        "security": { "type": "integer", "minimum": 0, "maximum": 100 },
                        "testability": { "type": "integer", "minimum": 0, "maximum": 100 }
                    },
                    "required": ["readability", "maintainability", "performance", "security", "testability"],
                    "additionalProperties": false
                },
                "code_issues": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "issue_type": { "type": "string" },
                            "description": { "type": "string" },
                            "severity": {
                                "type": "string",
                                "enum": ["low", "medium", "high", "critical"]
                            },
                            "line_number": { "type": "integer" },
                            "file_name": { "type": "string" },
                            "fix_suggestion": { "type": "string" },
                            "impact_scope": { "type": "string" }
                        },
                        "required": ["issue_type", "description", "severity", "impact_scope"],
                        "additionalProperties": false
                    }
                },
                "improvement_recommendations": {
                    "type": "array",
                    "items": { "type": "string" }
                },
                "best_practices_compliance": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "practice_name": { "type": "string" },
                            "compliance_status": {
                                "type": "string",
                                "enum": ["compliant", "partially_compliant", "non_compliant", "not_applicable"]
                            },
                            "details": { "type": "string" },
                            "improvement_suggestion": { "type": "string" }
                        },
                        "required": ["practice_name", "compliance_status", "details"],
                        "additionalProperties": false
                    }
                }
            },
            "required": ["assessment_steps", "overall_quality_score", "dimension_scores", "code_issues", "improvement_recommendations", "best_practices_compliance"],
            "additionalProperties": false
        }
        """;
}

/// <summary>
/// AI提示词模板
/// </summary>
public static class AIPromptTemplates
{
    /// <summary>
    /// 代码结构分析提示词模板
    /// </summary>
    public const string CodeStructureAnalysisPrompt = """
        你是一个专业的软件架构师和代码评审专家。请分析以下C#项目的代码结构，并按照指定的JSON格式返回分析结果。

        项目信息：
        - 项目名称：{projectName}
        - 目标框架：{targetFramework}
        - 文件数量：{fileCount}

        项目源代码：
        ```csharp
        {sourceCode}
        ```

        请按照以下步骤进行结构分析：
        1. 整体架构分析 - 分析项目的整体架构模式和组织结构
        2. 模块划分分析 - 分析代码的模块化程度和职责分离
        3. 设计模式识别 - 识别使用的设计模式和架构模式
        4. 依赖关系分析 - 分析模块间的依赖关系和耦合度
        5. 可扩展性评估 - 评估代码的可扩展性和可维护性

        评分标准：
        - 90-100分：架构清晰，模块化良好，使用了合适的设计模式
        - 80-89分：架构基本合理，有轻微的结构问题
        - 70-79分：架构有一些问题，影响可维护性
        - 60-69分：架构混乱，模块职责不清
        - 0-59分：缺乏架构设计，代码组织混乱

        请严格按照JSON Schema格式返回结果。
        """;

    /// <summary>
    /// 代码质量评估提示词模板
    /// </summary>
    public const string CodeQualityAssessmentPrompt = """
        你是一个专业的代码质量评估专家。请对以下C#项目进行全面的质量评估，并按照指定的JSON格式返回评估结果。

        项目信息：
        - 项目名称：{projectName}
        - 代码行数：{lineCount}
        - 文件数量：{fileCount}

        项目源代码：
        ```csharp
        {sourceCode}
        ```

        请按照以下维度进行质量评估：
        1. 可读性 - 代码的可读性和清晰度
        2. 可维护性 - 代码的可维护性和可修改性
        3. 性能 - 代码的性能和效率
        4. 安全性 - 代码的安全性和漏洞风险
        5. 可测试性 - 代码的可测试性和测试覆盖度

        评估标准：
        - 每个维度评分范围：0-100分
        - 识别具体的代码问题和改进建议
        - 评估最佳实践的遵循情况
        - 提供优先级排序的改进建议

        请严格按照JSON Schema格式返回结果。
        """;

    /// <summary>
    /// 代码解释和注释生成提示词模板
    /// </summary>
    public const string CodeExplanationPrompt = """
        你是一个专业的代码教学专家。请对以下C#代码进行详细解释，并生成适当的注释建议，按照指定的JSON格式返回结果。

        代码内容：
        ```csharp
        {sourceCode}
        ```

        请提供以下内容：
        1. 代码整体解释 - 说明代码的主要功能和目的
        2. 逐行解释 - 对重要代码行进行详细解释
        3. 关键概念解释 - 解释代码中使用的重要概念和技术
        4. 注释建议 - 建议在哪些位置添加什么样的注释
        5. 复杂度分析 - 分析算法的时间和空间复杂度

        解释要求：
        - 使用通俗易懂的语言
        - 重点解释复杂的逻辑和算法
        - 提供实用的注释建议
        - 包含性能和优化相关的说明

        请严格按照JSON Schema格式返回结果。
        """;

    /// <summary>
    /// 代码改进建议提示词模板
    /// </summary>
    public const string CodeImprovementPrompt = """
        你是一个资深的软件开发专家。请对以下C#代码提供全面的改进建议，并按照指定的JSON格式返回结果。

        代码内容：
        ```csharp
        {sourceCode}
        ```

        请提供以下类型的改进建议：
        1. 功能改进 - 功能实现的改进建议
        2. 重构建议 - 代码结构和设计的重构建议
        3. 性能优化 - 性能相关的优化建议
        4. 代码风格 - 编码风格和规范的改进建议
        5. 最佳实践 - 符合最佳实践的改进建议

        建议要求：
        - 提供具体的改进方案和示例代码
        - 说明改进的理由和预期收益
        - 评估实施的难度和复杂度
        - 按优先级排序改进建议

        请严格按照JSON Schema格式返回结果。
        """;
}
