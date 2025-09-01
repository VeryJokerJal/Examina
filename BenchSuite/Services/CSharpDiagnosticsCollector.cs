using BenchSuite.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace BenchSuite.Services;

/// <summary>
/// C#诊断信息收集器 - 提供详细的编译和运行时诊断信息
/// </summary>
public static class CSharpDiagnosticsCollector
{
    /// <summary>
    /// 增强编译结果的诊断信息
    /// </summary>
    /// <param name="result">编译结果</param>
    /// <param name="sourceCode">源代码</param>
    /// <returns>增强后的编译结果</returns>
    public static CompilationResult EnhanceCompilationResult(CompilationResult result, string sourceCode)
    {
        if (result == null) return new CompilationResult();

        try
        {
            // 添加代码统计信息
            result.Details += GenerateCodeStatistics(sourceCode);

            // 增强错误信息
            EnhanceErrorMessages(result.Errors, sourceCode);

            // 增强警告信息
            EnhanceWarningMessages(result.Warnings, sourceCode);

            // 添加编译性能信息
            result.Details += GeneratePerformanceInfo(result);

            // 添加建议和修复提示
            result.Details += GenerateFixSuggestions(result.Errors);
        }
        catch (Exception ex)
        {
            result.Details += $"\n诊断信息收集异常: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 生成代码统计信息
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <returns>统计信息</returns>
    private static string GenerateCodeStatistics(string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode)) return "";

        StringBuilder stats = new();
        stats.AppendLine("\n📊 代码统计:");

        try
        {
            string[] lines = sourceCode.Split('\n');
            int totalLines = lines.Length;
            int codeLines = lines.Count(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("//"));
            int commentLines = lines.Count(line => line.Trim().StartsWith("//"));
            int emptyLines = totalLines - codeLines - commentLines;

            stats.AppendLine($"  • 总行数: {totalLines}");
            stats.AppendLine($"  • 代码行数: {codeLines}");
            stats.AppendLine($"  • 注释行数: {commentLines}");
            stats.AppendLine($"  • 空行数: {emptyLines}");

            // 分析代码复杂度
            int methodCount = CountMethods(sourceCode);
            int classCount = CountClasses(sourceCode);
            int ifStatements = CountPattern(sourceCode, @"\bif\s*\(");
            int loops = CountPattern(sourceCode, @"\b(for|while|foreach)\s*\(");

            stats.AppendLine($"  • 类数量: {classCount}");
            stats.AppendLine($"  • 方法数量: {methodCount}");
            stats.AppendLine($"  • 条件语句: {ifStatements}");
            stats.AppendLine($"  • 循环语句: {loops}");
        }
        catch (Exception ex)
        {
            stats.AppendLine($"  统计信息生成失败: {ex.Message}");
        }

        return stats.ToString();
    }

    /// <summary>
    /// 增强错误消息
    /// </summary>
    /// <param name="errors">错误列表</param>
    /// <param name="sourceCode">源代码</param>
    private static void EnhanceErrorMessages(List<CompilationError> errors, string sourceCode)
    {
        foreach (CompilationError error in errors)
        {
            try
            {
                // 添加上下文代码
                error.Message += GetCodeContext(sourceCode, error.Line);

                // 添加常见错误的修复建议
                error.Message += GetErrorFixSuggestion(error.Code, error.Message);
            }
            catch
            {
                // 忽略增强失败
            }
        }
    }

    /// <summary>
    /// 增强警告消息
    /// </summary>
    /// <param name="warnings">警告列表</param>
    /// <param name="sourceCode">源代码</param>
    private static void EnhanceWarningMessages(List<CompilationWarning> warnings, string sourceCode)
    {
        foreach (CompilationWarning warning in warnings)
        {
            try
            {
                // 添加上下文代码
                warning.Message += GetCodeContext(sourceCode, warning.Line);

                // 添加警告的解释和建议
                warning.Message += GetWarningExplanation(warning.Code);
            }
            catch
            {
                // 忽略增强失败
            }
        }
    }

    /// <summary>
    /// 获取代码上下文
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="lineNumber">行号</param>
    /// <returns>上下文信息</returns>
    private static string GetCodeContext(string sourceCode, int lineNumber)
    {
        if (string.IsNullOrEmpty(sourceCode) || lineNumber <= 0) return "";

        try
        {
            string[] lines = sourceCode.Split('\n');
            if (lineNumber > lines.Length) return "";

            StringBuilder context = new();
            context.AppendLine("\n📍 代码上下文:");

            // 显示错误行前后的代码
            int start = Math.Max(0, lineNumber - 3);
            int end = Math.Min(lines.Length - 1, lineNumber + 1);

            for (int i = start; i <= end; i++)
            {
                string prefix = i == lineNumber - 1 ? ">>> " : "    ";
                context.AppendLine($"{prefix}{i + 1:D3}: {lines[i]}");
            }

            return context.ToString();
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// 获取错误修复建议
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="errorMessage">错误消息</param>
    /// <returns>修复建议</returns>
    private static string GetErrorFixSuggestion(string errorCode, string errorMessage)
    {
        Dictionary<string, string> suggestions = new()
        {
            ["CS1002"] = "💡 缺少分号。在语句末尾添加分号(;)。",
            ["CS1513"] = "💡 缺少右大括号。检查代码块的大括号是否匹配。",
            ["CS1514"] = "💡 缺少左大括号。在类或方法定义后添加左大括号({)。",
            ["CS0103"] = "💡 名称不存在。检查变量名拼写或确保已声明该变量。",
            ["CS0246"] = "💡 找不到类型或命名空间。检查using语句或类型名拼写。",
            ["CS0161"] = "💡 方法必须返回值。在方法中添加return语句。",
            ["CS1061"] = "💡 类型不包含该成员。检查方法名拼写或确保类型正确。",
            ["CS0029"] = "💡 类型转换错误。使用显式类型转换或检查类型兼容性。",
            ["CS0019"] = "💡 运算符不能应用于操作数。检查操作数类型是否支持该运算符。"
        };

        if (suggestions.TryGetValue(errorCode, out string? suggestion))
        {
            return $"\n{suggestion}";
        }

        return "";
    }

    /// <summary>
    /// 获取警告解释
    /// </summary>
    /// <param name="warningCode">警告代码</param>
    /// <returns>警告解释</returns>
    private static string GetWarningExplanation(string warningCode)
    {
        Dictionary<string, string> explanations = new()
        {
            ["CS0168"] = "ℹ️ 声明了变量但从未使用。考虑删除未使用的变量。",
            ["CS0219"] = "ℹ️ 变量已赋值但从未使用。考虑删除或使用该变量。",
            ["CS0414"] = "ℹ️ 字段已赋值但从未使用。考虑删除未使用的字段。",
            ["CS8600"] = "ℹ️ 可能的空引用赋值。考虑添加空值检查。",
            ["CS8602"] = "ℹ️ 可能的空引用解引用。使用空条件运算符(?.)或添加空值检查。",
            ["CS8618"] = "ℹ️ 不可为空的属性未初始化。在构造函数中初始化或使用默认值。"
        };

        if (explanations.TryGetValue(warningCode, out string? explanation))
        {
            return $"\n{explanation}";
        }

        return "";
    }

    /// <summary>
    /// 生成性能信息
    /// </summary>
    /// <param name="result">编译结果</param>
    /// <returns>性能信息</returns>
    private static string GeneratePerformanceInfo(CompilationResult result)
    {
        StringBuilder perf = new();
        perf.AppendLine("\n⚡ 编译性能:");
        perf.AppendLine($"  • 编译耗时: {result.CompilationTimeMs}ms");

        if (result.CompilationTimeMs > 5000)
        {
            perf.AppendLine("  ⚠️ 编译时间较长，可能是代码复杂度过高");
        }
        else if (result.CompilationTimeMs < 100)
        {
            perf.AppendLine("  ✅ 编译速度很快");
        }

        if (result.AssemblyBytes != null)
        {
            double sizeKB = result.AssemblyBytes.Length / 1024.0;
            perf.AppendLine($"  • 程序集大小: {sizeKB:F2} KB");
        }

        return perf.ToString();
    }

    /// <summary>
    /// 生成修复建议
    /// </summary>
    /// <param name="errors">错误列表</param>
    /// <returns>修复建议</returns>
    private static string GenerateFixSuggestions(List<CompilationError> errors)
    {
        if (errors.Count == 0) return "\n✅ 没有编译错误！";

        StringBuilder suggestions = new();
        suggestions.AppendLine($"\n🔧 修复建议 ({errors.Count} 个错误):");

        var errorGroups = errors.GroupBy(e => e.Code).OrderByDescending(g => g.Count());
        
        foreach (var group in errorGroups.Take(5))
        {
            suggestions.AppendLine($"  • {group.Key}: {group.Count()} 个错误");
        }

        if (errors.Count > 10)
        {
            suggestions.AppendLine("  💡 建议先修复前几个错误，其他错误可能会自动解决");
        }

        return suggestions.ToString();
    }

    /// <summary>
    /// 统计方法数量
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <returns>方法数量</returns>
    private static int CountMethods(string sourceCode)
    {
        return CountPattern(sourceCode, @"\b(public|private|protected|internal|static)?\s*(static)?\s*\w+\s+\w+\s*\([^)]*\)\s*\{");
    }

    /// <summary>
    /// 统计类数量
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <returns>类数量</returns>
    private static int CountClasses(string sourceCode)
    {
        return CountPattern(sourceCode, @"\b(public|private|protected|internal)?\s*class\s+\w+");
    }

    /// <summary>
    /// 统计模式匹配数量
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <returns>匹配数量</returns>
    private static int CountPattern(string sourceCode, string pattern)
    {
        try
        {
            return Regex.Matches(sourceCode, pattern, RegexOptions.IgnoreCase).Count;
        }
        catch
        {
            return 0;
        }
    }
}
