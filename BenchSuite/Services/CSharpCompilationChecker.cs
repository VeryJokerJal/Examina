using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using BenchSuite.Models;
using System.Reflection;
using System.Text;

namespace BenchSuite.Services;

/// <summary>
/// C#编译检查器 - 验证代码是否能正确编译和运行
/// 现在使用MSBuild API进行编译，提供更好的项目支持和诊断信息
/// </summary>
public static class CSharpCompilationChecker
{
    /// <summary>
    /// 编译C#代码
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="references">引用程序集路径列表</param>
    /// <returns>编译结果</returns>
    public static CompilationResult CompileCode(string sourceCode, List<string>? references = null)
    {
        // 使用新的MSBuild编译器
        return CompileCodeAsync(sourceCode, references).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 异步编译C#代码
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="references">引用程序集路径列表</param>
    /// <param name="targetFramework">目标框架</param>
    /// <returns>编译结果</returns>
    public static async Task<CompilationResult> CompileCodeAsync(
        string sourceCode,
        List<string>? references = null,
        string targetFramework = "net9.0")
    {
        try
        {
            // 使用MSBuild编译器进行编译
            return await CSharpMSBuildCompiler.CompileCodeAsync(
                sourceCode,
                targetFramework,
                references,
                "Library");
        }
        catch (Exception ex)
        {
            // 如果MSBuild编译失败，回退到Roslyn编译（兼容性保证）
            return CompileCodeWithRoslyn(sourceCode, references, ex);
        }
    }

    /// <summary>
    /// 使用Roslyn编译器作为回退方案（已弃用，保留用于兼容性）
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="references">引用程序集路径列表</param>
    /// <param name="msbuildException">MSBuild异常</param>
    /// <returns>编译结果</returns>
    [Obsolete("此方法已弃用，建议直接使用MSBuild编译。保留仅用于紧急回退。")]
    private static CompilationResult CompileCodeWithRoslyn(string sourceCode, List<string>? references, Exception msbuildException)
    {
        CompilationResult result = new()
        {
            IsSuccess = false,
            Details = $"⚠️ MSBuild编译失败，尝试Roslyn回退编译。MSBuild错误: {msbuildException.Message}\n" +
                     "注意：Roslyn回退编译功能已弃用，建议检查MSBuild配置。"
        };

        DateTime startTime = DateTime.Now;

        try
        {
            // 解析语法树
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            // 获取默认引用
            List<MetadataReference> metadataReferences = GetDefaultReferences();

            // 添加自定义引用
            if (references != null)
            {
                foreach (string reference in references)
                {
                    if (File.Exists(reference))
                    {
                        metadataReferences.Add(MetadataReference.CreateFromFile(reference));
                    }
                }
            }

            // 创建编译选项
            CSharpCompilationOptions compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithPlatform(Platform.AnyCpu);

            // 创建编译
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "StudentCode_Fallback",
                syntaxTrees: [syntaxTree],
                references: metadataReferences,
                options: compilationOptions);

            // 编译到内存流
            using MemoryStream ms = new();
            Microsoft.CodeAnalysis.Emit.EmitResult emitResult = compilation.Emit(ms);

            if (emitResult.Success)
            {
                result.IsSuccess = true;
                result.AssemblyBytes = ms.ToArray();
                result.Details += "\n✅ Roslyn回退编译成功（建议修复MSBuild配置）";

                // 处理警告
                foreach (Diagnostic diagnostic in emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning))
                {
                    result.Warnings.Add(new CompilationWarning
                    {
                        Code = diagnostic.Id,
                        Message = diagnostic.GetMessage(),
                        Line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                        Column = diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1,
                        FileName = diagnostic.Location.SourceTree?.FilePath ?? "源代码"
                    });
                }
            }
            else
            {
                result.Details += "\n❌ Roslyn回退编译也失败";

                // 处理错误
                foreach (Diagnostic diagnostic in emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                {
                    result.Errors.Add(new CompilationError
                    {
                        Code = diagnostic.Id,
                        Message = diagnostic.GetMessage(),
                        Line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                        Column = diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1,
                        FileName = diagnostic.Location.SourceTree?.FilePath ?? "源代码",
                        Severity = diagnostic.Severity.ToString()
                    });
                }

                // 处理警告
                foreach (Diagnostic diagnostic in emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning))
                {
                    result.Warnings.Add(new CompilationWarning
                    {
                        Code = diagnostic.Id,
                        Message = diagnostic.GetMessage(),
                        Line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                        Column = diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1,
                        FileName = diagnostic.Location.SourceTree?.FilePath ?? "源代码"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            result.Details += $"\n❌ Roslyn回退编译异常: {ex.Message}";
            result.Errors.Add(new CompilationError
            {
                Code = "ROSLYN_FALLBACK_EXCEPTION",
                Message = ex.Message,
                Line = 0,
                Column = 0,
                FileName = "Roslyn回退编译器",
                Severity = "Error"
            });
        }
        finally
        {
            DateTime endTime = DateTime.Now;
            result.CompilationTimeMs = (long)(endTime - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 尝试执行编译后的代码（简单验证）
    /// </summary>
    /// <param name="compilationResult">编译结果</param>
    /// <param name="entryPointClass">入口点类名</param>
    /// <param name="entryPointMethod">入口点方法名</param>
    /// <param name="parameters">方法参数</param>
    /// <returns>执行结果</returns>
    public static (bool Success, string Output, string Error) TryExecuteCode(
        CompilationResult compilationResult,
        string entryPointClass = "Program",
        string entryPointMethod = "Main",
        object[]? parameters = null)
    {
        // 使用MSBuild编译器的执行方法
        return CSharpMSBuildCompiler.TryExecuteCode(compilationResult, entryPointClass, entryPointMethod, parameters);
    }

    /// <summary>
    /// 获取默认的程序集引用（用于Roslyn回退编译）
    /// </summary>
    /// <returns>元数据引用列表</returns>
    private static List<MetadataReference> GetDefaultReferences()
    {
        List<MetadataReference> references = [];

        // 添加基本的.NET引用
        string[] basicAssemblies =
        [
            "System.Runtime",
            "System.Console",
            "System.Collections",
            "System.Linq",
            "System.Text.RegularExpressions",
            "System.Threading.Tasks",
            "netstandard"
        ];

        foreach (string assemblyName in basicAssemblies)
        {
            try
            {
                Assembly? assembly = Assembly.Load(assemblyName);
                if (assembly != null && !string.IsNullOrEmpty(assembly.Location))
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }
            catch
            {
                // 忽略加载失败的程序集
            }
        }

        // 添加当前运行时的核心库
        try
        {
            string runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? "";
            if (!string.IsNullOrEmpty(runtimePath))
            {
                string[] coreLibs =
                [
                    "System.Private.CoreLib.dll",
                    "System.Runtime.dll",
                    "System.Console.dll"
                ];

                foreach (string lib in coreLibs)
                {
                    string libPath = Path.Combine(runtimePath, lib);
                    if (File.Exists(libPath))
                    {
                        references.Add(MetadataReference.CreateFromFile(libPath));
                    }
                }
            }
        }
        catch
        {
            // 忽略错误
        }

        return references;
    }

    /// <summary>
    /// 获取支持的目标框架列表
    /// </summary>
    /// <returns>目标框架列表</returns>
    public static List<string> GetSupportedTargetFrameworks()
    {
        return
        [
            "net9.0",
            "net8.0",
            "net7.0",
            "net6.0",
            "netstandard2.1",
            "netstandard2.0"
        ];
    }

    /// <summary>
    /// 检查是否支持指定的目标框架
    /// </summary>
    /// <param name="targetFramework">目标框架</param>
    /// <returns>是否支持</returns>
    public static bool IsSupportedTargetFramework(string targetFramework)
    {
        return GetSupportedTargetFrameworks().Contains(targetFramework, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 验证代码语法是否正确
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <returns>语法验证结果</returns>
    public static (bool IsValid, List<string> Errors) ValidateSyntax(string sourceCode)
    {
        try
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            IEnumerable<Diagnostic> diagnostics = syntaxTree.GetDiagnostics();

            List<string> errors = [.. diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => $"行 {d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage()}")];

            return (errors.Count == 0, errors);
        }
        catch (Exception ex)
        {
            return (false, [$"语法分析异常: {ex.Message}"]);
        }
    }
}
