using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using BenchSuite.Models;
using System.Reflection;
using System.Text;

namespace BenchSuite.Services;

/// <summary>
/// C#编译检查器 - 验证代码是否能正确编译和运行
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
        CompilationResult result = new()
        {
            IsSuccess = false
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
                assemblyName: "StudentCode",
                syntaxTrees: [syntaxTree],
                references: metadataReferences,
                options: compilationOptions);

            // 编译到内存流
            using MemoryStream ms = new();
            EmitResult emitResult = compilation.Emit(ms);

            DateTime endTime = DateTime.Now;
            result.CompilationTimeMs = (long)(endTime - startTime).TotalMilliseconds;

            if (emitResult.Success)
            {
                result.IsSuccess = true;
                result.AssemblyBytes = ms.ToArray();
                result.Details = "编译成功";

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
                result.IsSuccess = false;
                result.Details = "编译失败";

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
            DateTime endTime = DateTime.Now;
            result.CompilationTimeMs = (long)(endTime - startTime).TotalMilliseconds;
            result.IsSuccess = false;
            result.Details = $"编译过程中发生异常: {ex.Message}";
            
            result.Errors.Add(new CompilationError
            {
                Code = "EXCEPTION",
                Message = ex.Message,
                Line = 0,
                Column = 0,
                FileName = "编译器",
                Severity = "Error"
            });
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
        if (!compilationResult.IsSuccess || compilationResult.AssemblyBytes == null)
        {
            return (false, "", "编译失败，无法执行");
        }

        try
        {
            // 加载程序集
            Assembly assembly = Assembly.Load(compilationResult.AssemblyBytes);

            // 查找入口点类型
            Type? entryType = assembly.GetTypes().FirstOrDefault(t => t.Name == entryPointClass);
            if (entryType == null)
            {
                return (false, "", $"未找到入口点类: {entryPointClass}");
            }

            // 查找入口点方法
            MethodInfo? entryMethod = entryType.GetMethod(entryPointMethod, BindingFlags.Public | BindingFlags.Static);
            if (entryMethod == null)
            {
                return (false, "", $"未找到入口点方法: {entryPointMethod}");
            }

            // 捕获控制台输出
            StringBuilder output = new();
            StringBuilder error = new();

            using (StringWriter outputWriter = new(output))
            using (StringWriter errorWriter = new(error))
            {
                TextWriter originalOut = Console.Out;
                TextWriter originalError = Console.Error;

                try
                {
                    Console.SetOut(outputWriter);
                    Console.SetError(errorWriter);

                    // 执行方法
                    object? result = entryMethod.Invoke(null, parameters);

                    return (true, output.ToString(), error.ToString());
                }
                finally
                {
                    Console.SetOut(originalOut);
                    Console.SetError(originalError);
                }
            }
        }
        catch (Exception ex)
        {
            return (false, "", $"执行异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取默认的程序集引用
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
