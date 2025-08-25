using System.Text.Json.Serialization;

namespace BenchSuite.Models;

/// <summary>
/// AI数学推理结构化输出模型 - 符合OpenAI Structured Output要求
/// </summary>
public class MathReasoningResponse
{
    /// <summary>
    /// 推理步骤数组
    /// </summary>
    [JsonPropertyName("steps")]
    public List<MathReasoningStep> Steps { get; set; } = [];

    /// <summary>
    /// 最终答案
    /// </summary>
    [JsonPropertyName("final_answer")]
    public string FinalAnswer { get; set; } = string.Empty;
}

/// <summary>
/// 数学推理步骤
/// </summary>
public class MathReasoningStep
{
    /// <summary>
    /// 步骤说明
    /// </summary>
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// 步骤输出
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;
}

/// <summary>
/// C#代码逻辑分析结构化输出模型
/// </summary>
public class CSharpLogicalAnalysisResponse
{
    /// <summary>
    /// 分析步骤数组
    /// </summary>
    [JsonPropertyName("steps")]
    public List<LogicalAnalysisStep> Steps { get; set; } = [];

    /// <summary>
    /// 最终评估结果
    /// </summary>
    [JsonPropertyName("final_answer")]
    public string FinalAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 逻辑性评分（0-100）
    /// </summary>
    [JsonPropertyName("logical_score")]
    public int LogicalScore { get; set; }

    /// <summary>
    /// 检测到的逻辑错误
    /// </summary>
    [JsonPropertyName("logical_errors")]
    public List<StructuredLogicalError> LogicalErrors { get; set; } = [];

    /// <summary>
    /// 改进建议
    /// </summary>
    [JsonPropertyName("improvement_suggestions")]
    public List<string> ImprovementSuggestions { get; set; } = [];
}

/// <summary>
/// 逻辑分析步骤
/// </summary>
public class LogicalAnalysisStep
{
    /// <summary>
    /// 分析说明
    /// </summary>
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// 分析结果
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// 步骤类型
    /// </summary>
    [JsonPropertyName("step_type")]
    public string StepType { get; set; } = string.Empty;
}

/// <summary>
/// 结构化逻辑错误
/// </summary>
public class StructuredLogicalError
{
    /// <summary>
    /// 错误类型
    /// </summary>
    [JsonPropertyName("error_type")]
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// 错误描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 严重程度
    /// </summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// 错误位置（行号）
    /// </summary>
    [JsonPropertyName("line_number")]
    public int? LineNumber { get; set; }

    /// <summary>
    /// 修复建议
    /// </summary>
    [JsonPropertyName("fix_suggestion")]
    public string? FixSuggestion { get; set; }
}

/// <summary>
/// JSON Schema常量定义
/// </summary>
public static class AIJsonSchemas
{
    /// <summary>
    /// 数学推理JSON Schema
    /// </summary>
    public const string MathReasoningSchema = """
        {
            "type": "object",
            "properties": {
                "steps": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "explanation": { "type": "string" },
                            "output": { "type": "string" }
                        },
                        "required": ["explanation", "output"],
                        "additionalProperties": false
                    }
                },
                "final_answer": { "type": "string" }
            },
            "required": ["steps", "final_answer"],
            "additionalProperties": false
        }
        """;

    /// <summary>
    /// C#逻辑分析JSON Schema
    /// </summary>
    public const string CSharpLogicalAnalysisSchema = """
        {
            "type": "object",
            "properties": {
                "steps": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "explanation": { "type": "string" },
                            "output": { "type": "string" },
                            "step_type": { "type": "string" }
                        },
                        "required": ["explanation", "output", "step_type"],
                        "additionalProperties": false
                    }
                },
                "final_answer": { "type": "string" },
                "logical_score": { 
                    "type": "integer", 
                    "minimum": 0, 
                    "maximum": 100 
                },
                "logical_errors": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "error_type": { "type": "string" },
                            "description": { "type": "string" },
                            "severity": { 
                                "type": "string",
                                "enum": ["minor", "moderate", "severe", "critical"]
                            },
                            "line_number": { "type": "integer" },
                            "fix_suggestion": { "type": "string" }
                        },
                        "required": ["error_type", "description", "severity"],
                        "additionalProperties": false
                    }
                },
                "improvement_suggestions": {
                    "type": "array",
                    "items": { "type": "string" }
                }
            },
            "required": ["steps", "final_answer", "logical_score", "logical_errors", "improvement_suggestions"],
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
    /// C#代码逻辑分析提示词模板
    /// </summary>
    public const string CSharpLogicalAnalysisPrompt = """
        你是一个专业的C#代码评审专家。请分析以下C#代码的逻辑性，并按照指定的JSON格式返回分析结果。

        题目描述：
        {problemDescription}

        学生代码：
        ```csharp
        {sourceCode}
        ```

        {expectedOutputSection}

        请按照以下步骤进行分析：
        1. 代码结构分析 - 分析代码的整体结构和组织
        2. 逻辑流程分析 - 分析代码的执行逻辑和控制流
        3. 算法正确性分析 - 分析算法是否能正确解决问题
        4. 边界情况处理 - 分析是否考虑了边界情况和异常处理
        5. 代码效率分析 - 分析代码的时间和空间复杂度

        评分标准：
        - 90-100分：逻辑完全正确，代码结构清晰，考虑了所有边界情况
        - 80-89分：逻辑基本正确，有轻微问题但不影响主要功能
        - 70-79分：逻辑有一些问题，可能影响部分功能
        - 60-69分：逻辑有明显错误，影响主要功能
        - 0-59分：逻辑严重错误或无法运行

        请严格按照JSON Schema格式返回结果，包含分析步骤、最终评估、逻辑错误和改进建议。
        """;

    /// <summary>
    /// 数学推理提示词模板
    /// </summary>
    public const string MathReasoningPrompt = """
        请解决以下数学问题，并按照指定的JSON格式返回推理步骤和最终答案。

        问题：{problem}

        请按步骤详细说明你的推理过程，每个步骤都要包含说明和输出结果。
        """;
}
