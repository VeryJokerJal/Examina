using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ExamLab.Models;

namespace ExamLab.Services;

/// <summary>
/// 导出服务
/// </summary>
public static class ExportService
{
    /// <summary>
    /// 导出试卷为JSON格式
    /// </summary>
    public static Task<string> ExportExamToJsonAsync(Exam exam)
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // 添加自定义转换器
        options.Converters.Add(new Converters.ModuleTypeJsonConverter());
        options.Converters.Add(new Converters.CSharpQuestionTypeJsonConverter());

        return Task.FromResult(JsonSerializer.Serialize(exam, options));
    }

    /// <summary>
    /// 从JSON导入试卷
    /// </summary>
    public static Task<Exam?> ImportExamFromJsonAsync(string json)
    {
        try
        {
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };

            // 添加自定义转换器
            options.Converters.Add(new Converters.ModuleTypeJsonConverter());
            options.Converters.Add(new Converters.CSharpQuestionTypeJsonConverter());

            Exam? exam = JsonSerializer.Deserialize<Exam>(json, options);

            // 重新初始化事件监听（修复反序列化后分值更新问题）
            exam?.ReinitializeEventListeners();

            return Task.FromResult(exam);
        }
        catch (Exception)
        {
            return Task.FromResult<Exam?>(null);
        }
    }

    /// <summary>
    /// 导出试卷为CSV格式（题目列表）
    /// </summary>
    public static Task<string> ExportExamToCsvAsync(Exam exam)
    {
        StringBuilder csv = new();

        // CSV头部
        _ = csv.AppendLine("模块名称,题目标题,题目内容,题目分值,操作点数量");

        foreach (ExamModule module in exam.Modules)
        {
            foreach (Question question in module.Questions)
            {
                string line = $"\"{module.Name}\",\"{question.Title}\",\"{question.Content.Replace("\"", "\"\"")}\",{question.TotalScore},{question.OperationPoints.Count}";
                _ = csv.AppendLine(line);
            }
        }

        return Task.FromResult(csv.ToString());
    }

    /// <summary>
    /// 导出操作点配置为文本格式
    /// </summary>
    public static Task<string> ExportOperationPointsToTextAsync(Exam exam)
    {
        StringBuilder text = new();

        _ = text.AppendLine($"试卷：{exam.Name}");
        _ = text.AppendLine($"描述：{exam.Description}");
        _ = text.AppendLine($"总分：{exam.TotalScore}");
        _ = text.AppendLine($"时长：{exam.Duration}分钟");
        _ = text.AppendLine();

        foreach (ExamModule module in exam.Modules)
        {
            _ = text.AppendLine($"模块：{module.Name} (分值：{module.Score})");
            _ = text.AppendLine(new string('=', 50));

            foreach (Question question in module.Questions)
            {
                _ = text.AppendLine($"题目：{question.Title}");
                _ = text.AppendLine($"内容：{question.Content}");
                _ = text.AppendLine($"分值：{question.TotalScore}");
                _ = text.AppendLine();

                if (question.OperationPoints.Count > 0)
                {
                    _ = text.AppendLine("操作点配置");
                    foreach (OperationPoint op in question.OperationPoints)
                    {
                        _ = text.AppendLine($"  - {op.Name}");
                        _ = text.AppendLine($"    描述：{op.Description}");
                        _ = text.AppendLine($"    分值：{op.Score}");

                        if (op.Parameters.Count > 0)
                        {
                            _ = text.AppendLine("    参数");
                            foreach (ConfigurationParameter param in op.Parameters)
                            {
                                _ = text.AppendLine($"      {param.DisplayName}: {param.Value ?? param.DefaultValue ?? "未设置"}");
                            }
                        }
                        _ = text.AppendLine();
                    }
                }

                _ = text.AppendLine(new string('-', 30));
            }
            _ = text.AppendLine();
        }

        return Task.FromResult(text.ToString());
    }

    /// <summary>
    /// 导出PPT知识点配置为文档格式
    /// </summary>
    public static Task<string> ExportPowerPointKnowledgeToDocumentAsync(Exam exam)
    {
        StringBuilder doc = new();

        _ = doc.AppendLine("PowerPoint知识点配置文档");
        _ = doc.AppendLine(new string('=', 40));
        _ = doc.AppendLine();

        ExamModule? pptModule = exam.Modules.FirstOrDefault(m => m.Type == ModuleType.PowerPoint);
        if (pptModule == null)
        {
            _ = doc.AppendLine("该试卷不包含PowerPoint模块。");
            return Task.FromResult(doc.ToString());
        }

        _ = doc.AppendLine($"模块：{pptModule.Name}");
        _ = doc.AppendLine($"分值：{pptModule.Score}");
        _ = doc.AppendLine();

        // 按知识点分类统计
        Dictionary<string, List<OperationPoint>> knowledgeByCategory = [];

        foreach (Question question in pptModule.Questions)
        {
            foreach (OperationPoint op in question.OperationPoints)
            {
                if (op.PowerPointKnowledgeType.HasValue)
                {
                    PowerPointKnowledgeConfig? config = PowerPointKnowledgeService.Instance.GetKnowledgeConfig(op.PowerPointKnowledgeType.Value);
                    if (config != null)
                    {
                        if (!knowledgeByCategory.ContainsKey(config.Category))
                        {
                            knowledgeByCategory[config.Category] = [];
                        }
                        knowledgeByCategory[config.Category].Add(op);
                    }
                }
            }
        }

        foreach (KeyValuePair<string, List<OperationPoint>> category in knowledgeByCategory)
        {
            _ = doc.AppendLine($"分类：{category.Key}");
            _ = doc.AppendLine(new string('-', 20));

            foreach (OperationPoint op in category.Value)
            {
                _ = doc.AppendLine($"知识点：{op.Name}");
                _ = doc.AppendLine($"描述：{op.Description}");

                if (op.Parameters.Count > 0)
                {
                    _ = doc.AppendLine("配置参数");
                    foreach (ConfigurationParameter param in op.Parameters)
                    {
                        _ = doc.AppendLine($"  - {param.DisplayName}: {param.Value ?? param.DefaultValue ?? "未设置"}");
                        if (!string.IsNullOrEmpty(param.Description))
                        {
                            _ = doc.AppendLine($"    说明：{param.Description}");
                        }
                    }
                }
                _ = doc.AppendLine();
            }
            _ = doc.AppendLine();
        }

        return Task.FromResult(doc.ToString());
    }

    /// <summary>
    /// 生成试卷统计报告
    /// </summary>
    public static Task<string> GenerateExamStatisticsAsync(Exam exam)
    {
        StringBuilder stats = new();

        _ = stats.AppendLine("试卷统计报告");
        _ = stats.AppendLine(new string('=', 30));
        _ = stats.AppendLine();

        _ = stats.AppendLine($"试卷名称：{exam.Name}");
        _ = stats.AppendLine($"创建时间：{exam.CreatedTime}");
        _ = stats.AppendLine($"最后修改：{exam.LastModifiedTime}");
        _ = stats.AppendLine($"考试时长：{exam.Duration}分钟");
        _ = stats.AppendLine();

        _ = stats.AppendLine("模块统计");
        _ = stats.AppendLine($"模块总数：{exam.Modules.Count}");

        int totalQuestions = exam.Modules.Sum(m => m.Questions.Count);
        int totalOperationPoints = exam.Modules.SelectMany(m => m.Questions).Sum(q => q.OperationPoints.Count);
        int totalScore = exam.Modules.Sum(m => m.Score);

        _ = stats.AppendLine($"题目总数：{totalQuestions}");
        _ = stats.AppendLine($"操作点总数：{totalOperationPoints}");
        _ = stats.AppendLine($"总分：{totalScore}");
        _ = stats.AppendLine();

        _ = stats.AppendLine("各模块详情");
        foreach (ExamModule module in exam.Modules)
        {
            _ = stats.AppendLine($"  {module.Name}:");
            _ = stats.AppendLine($"    题目数：{module.Questions.Count}");
            _ = stats.AppendLine($"    操作点数：{module.Questions.Sum(q => q.OperationPoints.Count)}");
            _ = stats.AppendLine($"    分值：{module.Score}");

            // 题目统计
            _ = stats.AppendLine($"    题目总数：{module.Questions.Count}");
            _ = stats.AppendLine();
        }

        return Task.FromResult(stats.ToString());
    }
}
