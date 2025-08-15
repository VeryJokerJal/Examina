using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using BenchSuite.Models;

namespace BenchSuite.Services;

/// <summary>
/// C#调试纠错评分器 - 检测和验证代码错误修复
/// </summary>
public static class CSharpDebuggingGrader
{
    /// <summary>
    /// 调试纠错评分
    /// </summary>
    /// <param name="buggyCode">包含错误的原始代码</param>
    /// <param name="studentFixedCode">学生修复后的代码</param>
    /// <param name="expectedErrors">期望发现的错误列表</param>
    /// <param name="testCode">验证修复的测试代码</param>
    /// <returns>调试结果</returns>
    public static async Task<DebuggingResult> DebugCodeAsync(string buggyCode, string studentFixedCode, List<string> expectedErrors, string? testCode = null)
    {
        DebuggingResult result = new()
        {
            TotalErrors = expectedErrors.Count
        };

        DateTime startTime = DateTime.Now;

        try
        {
            // 1. 分析原始代码中的错误
            List<ErrorDetectionResult> originalErrors = AnalyzeCodeErrors(buggyCode);
            
            // 2. 分析修复后代码中的错误
            List<ErrorDetectionResult> remainingErrors = AnalyzeCodeErrors(studentFixedCode);
            
            // 3. 比较错误修复情况
            List<FixVerificationResult> fixVerifications = VerifyFixes(originalErrors, remainingErrors, expectedErrors);
            
            // 4. 如果提供了测试代码，运行测试验证
            UnitTestResult? testResult = null;
            if (!string.IsNullOrEmpty(testCode))
            {
                testResult = await CSharpUnitTestRunner.RunUnitTestsAsync(studentFixedCode, testCode);
            }
            
            // 5. 计算结果
            result.ErrorDetections = originalErrors;
            result.FixVerifications = fixVerifications;
            result.FixedErrors = fixVerifications.Count(f => f.IsCorrectFix);
            result.RemainingErrors = remainingErrors.Count;
            result.IsSuccess = result.RemainingErrors == 0 && (testResult?.IsSuccess ?? true);
            
            // 6. 生成详细信息
            result.Details = GenerateDebuggingDetails(result, testResult);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"调试评分过程中发生异常: {ex.Message}";
            result.IsSuccess = false;
        }
        finally
        {
            DateTime endTime = DateTime.Now;
            result.DebuggingTimeMs = (long)(endTime - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 分析代码中的错误
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <returns>错误检测结果列表</returns>
    private static List<ErrorDetectionResult> AnalyzeCodeErrors(string sourceCode)
    {
        List<ErrorDetectionResult> errors = [];

        try
        {
            // 语法分析
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            IEnumerable<Diagnostic> syntaxDiagnostics = syntaxTree.GetDiagnostics();

            foreach (Diagnostic diagnostic in syntaxDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                errors.Add(new ErrorDetectionResult
                {
                    ErrorType = "语法错误",
                    Description = diagnostic.GetMessage(),
                    LineNumber = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                    ColumnNumber = diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1,
                    Severity = diagnostic.Severity.ToString(),
                    IsFixed = false
                });
            }

            // 编译分析
            CompilationResult compilationResult = CSharpCompilationChecker.CompileCode(sourceCode);
            foreach (CompilationError compileError in compilationResult.Errors)
            {
                // 避免重复添加语法错误
                if (!errors.Any(e => e.LineNumber == compileError.Line && e.Description.Contains(compileError.Message)))
                {
                    errors.Add(new ErrorDetectionResult
                    {
                        ErrorType = "编译错误",
                        Description = compileError.Message,
                        LineNumber = compileError.Line,
                        ColumnNumber = compileError.Column,
                        Severity = compileError.Severity,
                        IsFixed = false
                    });
                }
            }

            // 逻辑错误检测（简化实现）
            errors.AddRange(DetectLogicalErrors(syntaxTree));
        }
        catch (Exception ex)
        {
            errors.Add(new ErrorDetectionResult
            {
                ErrorType = "分析异常",
                Description = $"代码分析失败: {ex.Message}",
                LineNumber = 0,
                ColumnNumber = 0,
                Severity = "Error",
                IsFixed = false
            });
        }

        return errors;
    }

    /// <summary>
    /// 检测逻辑错误
    /// </summary>
    /// <param name="syntaxTree">语法树</param>
    /// <returns>逻辑错误列表</returns>
    private static List<ErrorDetectionResult> DetectLogicalErrors(SyntaxTree syntaxTree)
    {
        List<ErrorDetectionResult> logicalErrors = [];
        SyntaxNode root = syntaxTree.GetRoot();

        // 检测常见的逻辑错误模式
        
        // 1. 检测无限循环
        IEnumerable<WhileStatementSyntax> whileLoops = root.DescendantNodes().OfType<WhileStatementSyntax>();
        foreach (WhileStatementSyntax whileLoop in whileLoops)
        {
            if (IsInfiniteLoop(whileLoop))
            {
                logicalErrors.Add(new ErrorDetectionResult
                {
                    ErrorType = "逻辑错误",
                    Description = "可能存在无限循环",
                    LineNumber = whileLoop.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    ColumnNumber = whileLoop.GetLocation().GetLineSpan().StartLinePosition.Character + 1,
                    Severity = "Warning",
                    IsFixed = false,
                    FixSuggestion = "检查循环条件和循环变量的更新"
                });
            }
        }

        // 2. 检测未使用的变量
        IEnumerable<VariableDeclarationSyntax> variables = root.DescendantNodes().OfType<VariableDeclarationSyntax>();
        foreach (VariableDeclarationSyntax variable in variables)
        {
            if (IsUnusedVariable(variable, root))
            {
                logicalErrors.Add(new ErrorDetectionResult
                {
                    ErrorType = "逻辑错误",
                    Description = "声明了未使用的变量",
                    LineNumber = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    ColumnNumber = variable.GetLocation().GetLineSpan().StartLinePosition.Character + 1,
                    Severity = "Warning",
                    IsFixed = false,
                    FixSuggestion = "移除未使用的变量或使用该变量"
                });
            }
        }

        // 3. 检测空的catch块
        IEnumerable<CatchClauseSyntax> catchClauses = root.DescendantNodes().OfType<CatchClauseSyntax>();
        foreach (CatchClauseSyntax catchClause in catchClauses)
        {
            if (IsEmptyCatchBlock(catchClause))
            {
                logicalErrors.Add(new ErrorDetectionResult
                {
                    ErrorType = "逻辑错误",
                    Description = "空的异常处理块",
                    LineNumber = catchClause.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    ColumnNumber = catchClause.GetLocation().GetLineSpan().StartLinePosition.Character + 1,
                    Severity = "Warning",
                    IsFixed = false,
                    FixSuggestion = "添加适当的异常处理逻辑"
                });
            }
        }

        return logicalErrors;
    }

    /// <summary>
    /// 验证错误修复情况
    /// </summary>
    /// <param name="originalErrors">原始错误列表</param>
    /// <param name="remainingErrors">剩余错误列表</param>
    /// <param name="expectedErrors">期望的错误列表</param>
    /// <returns>修复验证结果列表</returns>
    private static List<FixVerificationResult> VerifyFixes(List<ErrorDetectionResult> originalErrors, List<ErrorDetectionResult> remainingErrors, List<string> expectedErrors)
    {
        List<FixVerificationResult> verifications = [];

        foreach (string expectedError in expectedErrors)
        {
            // 查找原始错误中是否存在期望的错误
            ErrorDetectionResult? originalError = originalErrors.FirstOrDefault(e => 
                e.Description.Contains(expectedError, StringComparison.OrdinalIgnoreCase) ||
                e.ErrorType.Contains(expectedError, StringComparison.OrdinalIgnoreCase));

            // 查找剩余错误中是否还存在该错误
            ErrorDetectionResult? remainingError = remainingErrors.FirstOrDefault(e => 
                e.Description.Contains(expectedError, StringComparison.OrdinalIgnoreCase) ||
                e.ErrorType.Contains(expectedError, StringComparison.OrdinalIgnoreCase));

            bool isCorrectFix = originalError != null && remainingError == null;

            verifications.Add(new FixVerificationResult
            {
                ErrorType = expectedError,
                IsCorrectFix = isCorrectFix,
                BeforeCode = originalError?.Description ?? "未找到对应错误",
                AfterCode = remainingError?.Description ?? "错误已修复",
                Message = isCorrectFix ? "错误已正确修复" : "错误未正确修复",
                Score = isCorrectFix ? 1 : 0
            });
        }

        return verifications;
    }

    /// <summary>
    /// 生成调试详细信息
    /// </summary>
    /// <param name="result">调试结果</param>
    /// <param name="testResult">测试结果</param>
    /// <returns>详细信息字符串</returns>
    private static string GenerateDebuggingDetails(DebuggingResult result, UnitTestResult? testResult)
    {
        List<string> details = [];

        details.Add($"调试纠错评分完成。总错误数: {result.TotalErrors}, 已修复: {result.FixedErrors}, 剩余: {result.RemainingErrors}");

        if (result.FixedErrors == result.TotalErrors && result.RemainingErrors == 0)
        {
            details.Add("✅ 所有错误都已正确修复");
        }
        else if (result.FixedErrors > 0)
        {
            details.Add($"⚠️ 部分错误已修复 ({result.FixedErrors}/{result.TotalErrors})");
        }
        else
        {
            details.Add("❌ 没有正确修复任何错误");
        }

        if (testResult != null)
        {
            details.Add($"功能测试: {testResult.PassedTests}/{testResult.TotalTests} 通过");
        }

        return string.Join("\n", details);
    }

    /// <summary>
    /// 检测是否为无限循环
    /// </summary>
    private static bool IsInfiniteLoop(WhileStatementSyntax whileLoop)
    {
        // 简化检测：如果条件是常量true且循环体内没有break语句
        if (whileLoop.Condition is LiteralExpressionSyntax literal && 
            literal.Token.ValueText == "true")
        {
            // 检查循环体内是否有break语句
            return !whileLoop.Statement.DescendantNodes().OfType<BreakStatementSyntax>().Any();
        }
        return false;
    }

    /// <summary>
    /// 检测是否为未使用的变量
    /// </summary>
    private static bool IsUnusedVariable(VariableDeclarationSyntax variable, SyntaxNode root)
    {
        // 简化检测：检查变量名是否在后续代码中被引用
        foreach (VariableDeclaratorSyntax declarator in variable.Variables)
        {
            string variableName = declarator.Identifier.ValueText;
            
            // 在根节点中查找该变量名的使用
            IEnumerable<IdentifierNameSyntax> usages = root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(id => id.Identifier.ValueText == variableName);

            // 如果只有声明处的使用，则认为未使用
            if (usages.Count() <= 1)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 检测是否为空的catch块
    /// </summary>
    private static bool IsEmptyCatchBlock(CatchClauseSyntax catchClause)
    {
        return catchClause.Block.Statements.Count == 0;
    }
}
