using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using BenchSuite.Models;
using System.IO;
using System.Text;

namespace BenchSuite.Services;

/// <summary>
/// C# MSBuild编译器 - 使用Microsoft.Build API处理.csproj项目文件
/// </summary>
public static class CSharpMSBuildCompiler
{
    /// <summary>
    /// 使用MSBuild编译C#代码
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="targetFramework">目标框架（如net9.0）</param>
    /// <param name="additionalReferences">额外的程序集引用</param>
    /// <param name="outputType">输出类型（Library或Exe）</param>
    /// <returns>编译结果</returns>
    public static async Task<CompilationResult> CompileCodeAsync(
        string sourceCode,
        string targetFramework = "",
        List<string>? additionalReferences = null,
        string outputType = "Library")
    {
        CompilationResult result = new()
        {
            IsSuccess = false
        };

        DateTime startTime = DateTime.Now;
        string tempDirectory = string.Empty;

        try
        {
            // 验证和选择目标框架
            targetFramework = ValidateAndSelectTargetFramework(targetFramework);
            result.Details = $"使用目标框架: {targetFramework}";

            // 创建临时目录
            tempDirectory = CreateTempDirectory();

            // 创建项目文件
            string projectFilePath = await CreateProjectFileAsync(tempDirectory, targetFramework, additionalReferences, outputType);

            // 创建源代码文件
            string sourceFilePath = await CreateSourceFileAsync(tempDirectory, sourceCode);

            // 执行MSBuild编译
            result = await ExecuteMSBuildAsync(projectFilePath, tempDirectory, sourceCode);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.Details = $"MSBuild编译过程中发生异常: {ex.Message}";
            result.Errors.Add(new CompilationError
            {
                Code = "MSBUILD_EXCEPTION",
                Message = ex.Message,
                Line = 0,
                Column = 0,
                FileName = "MSBuild编译器",
                Severity = "Error"
            });
        }
        finally
        {
            DateTime endTime = DateTime.Now;
            result.CompilationTimeMs = (long)(endTime - startTime).TotalMilliseconds;

            // 清理临时文件
            CleanupTempDirectory(tempDirectory);
        }

        return result;
    }

    /// <summary>
    /// 验证和选择目标框架
    /// </summary>
    /// <param name="targetFramework">指定的目标框架</param>
    /// <returns>有效的目标框架</returns>
    private static string ValidateAndSelectTargetFramework(string targetFramework)
    {
        // 如果未指定框架，使用默认框架
        if (string.IsNullOrEmpty(targetFramework))
        {
            return TargetFrameworkManager.GetDefaultTargetFramework();
        }

        // 验证框架是否受支持
        if (!TargetFrameworkManager.IsFrameworkSupported(targetFramework))
        {
            string recommended = TargetFrameworkManager.GetRecommendedFramework();
            throw new ArgumentException($"不支持的目标框架: {targetFramework}。推荐使用: {recommended}");
        }

        return targetFramework;
    }

    /// <summary>
    /// 创建临时目录
    /// </summary>
    /// <returns>临时目录路径</returns>
    private static string CreateTempDirectory()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "BenchSuite_MSBuild_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempPath);
        return tempPath;
    }

    /// <summary>
    /// 创建项目文件
    /// </summary>
    /// <param name="projectDirectory">项目目录</param>
    /// <param name="targetFramework">目标框架</param>
    /// <param name="additionalReferences">额外引用</param>
    /// <param name="outputType">输出类型</param>
    /// <returns>项目文件路径</returns>
    private static async Task<string> CreateProjectFileAsync(
        string projectDirectory,
        string targetFramework,
        List<string>? additionalReferences,
        string outputType)
    {
        string projectFilePath = Path.Combine(projectDirectory, "TempProject.csproj");

        StringBuilder projectContent = new();
        projectContent.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        projectContent.AppendLine("  <PropertyGroup>");
        projectContent.AppendLine($"    <TargetFramework>{targetFramework}</TargetFramework>");
        projectContent.AppendLine($"    <OutputType>{outputType}</OutputType>");

        // 获取框架特定的编译选项
        var frameworkOptions = TargetFrameworkManager.GetFrameworkSpecificOptions(targetFramework);
        foreach (var option in frameworkOptions)
        {
            projectContent.AppendLine($"    <{option.Key}>{option.Value}</{option.Key}>");
        }

        projectContent.AppendLine("    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>");
        projectContent.AppendLine("    <UseAppHost>false</UseAppHost>");
        projectContent.AppendLine("    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>");
        projectContent.AppendLine("  </PropertyGroup>");

        // 添加额外的程序集引用
        if (additionalReferences?.Count > 0)
        {
            projectContent.AppendLine("  <ItemGroup>");
            foreach (string reference in additionalReferences)
            {
                if (File.Exists(reference))
                {
                    projectContent.AppendLine($"    <Reference Include=\"{Path.GetFileNameWithoutExtension(reference)}\">");
                    projectContent.AppendLine($"      <HintPath>{reference}</HintPath>");
                    projectContent.AppendLine("    </Reference>");
                }
            }
            projectContent.AppendLine("  </ItemGroup>");
        }

        projectContent.AppendLine("</Project>");

        await File.WriteAllTextAsync(projectFilePath, projectContent.ToString());
        return projectFilePath;
    }

    /// <summary>
    /// 创建源代码文件
    /// </summary>
    /// <param name="projectDirectory">项目目录</param>
    /// <param name="sourceCode">源代码</param>
    /// <returns>源文件路径</returns>
    private static async Task<string> CreateSourceFileAsync(string projectDirectory, string sourceCode)
    {
        string sourceFilePath = Path.Combine(projectDirectory, "Program.cs");
        await File.WriteAllTextAsync(sourceFilePath, sourceCode);
        return sourceFilePath;
    }

    /// <summary>
    /// 执行MSBuild编译
    /// </summary>
    /// <param name="projectFilePath">项目文件路径</param>
    /// <param name="projectDirectory">项目目录</param>
    /// <param name="sourceCode">源代码（用于诊断增强）</param>
    /// <returns>编译结果</returns>
    private static async Task<CompilationResult> ExecuteMSBuildAsync(string projectFilePath, string projectDirectory, string sourceCode = "")
    {
        CompilationResult result = new();
        
        return await Task.Run(() =>
        {
            try
            {
                // 创建项目集合
                ProjectCollection projectCollection = new();
                
                // 加载项目
                Project project = projectCollection.LoadProject(projectFilePath);
                
                // 创建构建请求
                BuildRequestData buildRequest = new BuildRequestData(
                    project.CreateProjectInstance(), 
                    ["Build"], 
                    null);
                
                // 创建构建参数
                BuildParameters buildParameters = new BuildParameters(projectCollection)
                {
                    Loggers = [new MSBuildLogger(result)]
                };
                
                // 创建构建管理器
                BuildManager buildManager = BuildManager.DefaultBuildManager;
                
                // 执行构建
                BuildResult buildResult = buildManager.Build(buildParameters, buildRequest);
                
                // 处理构建结果
                result.IsSuccess = buildResult.OverallResult == BuildResultCode.Success;

                if (result.IsSuccess)
                {
                    result.Details = "MSBuild编译成功";

                    // 尝试读取编译后的程序集
                    string outputPath = GetOutputAssemblyPath(project, projectDirectory);
                    if (File.Exists(outputPath))
                    {
                        result.AssemblyBytes = File.ReadAllBytes(outputPath);
                    }
                }
                else
                {
                    result.Details = "MSBuild编译失败";
                }

                // 增强诊断信息
                result = CSharpDiagnosticsCollector.EnhanceCompilationResult(result, sourceCode);
                
                // 清理项目集合
                projectCollection.UnloadAllProjects();
                projectCollection.Dispose();
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Details = $"MSBuild执行异常: {ex.Message}";
                result.Errors.Add(new CompilationError
                {
                    Code = "MSBUILD_EXECUTION_ERROR",
                    Message = ex.Message,
                    Line = 0,
                    Column = 0,
                    FileName = "MSBuild",
                    Severity = "Error"
                });
            }
            
            return result;
        });
    }

    /// <summary>
    /// 获取输出程序集路径
    /// </summary>
    /// <param name="project">项目实例</param>
    /// <param name="projectDirectory">项目目录</param>
    /// <returns>程序集路径</returns>
    private static string GetOutputAssemblyPath(Project project, string projectDirectory)
    {
        try
        {
            string outputPath = project.GetPropertyValue("OutputPath");
            string assemblyName = project.GetPropertyValue("AssemblyName");
            string targetFramework = project.GetPropertyValue("TargetFramework");
            
            if (string.IsNullOrEmpty(assemblyName))
            {
                assemblyName = Path.GetFileNameWithoutExtension(project.FullPath);
            }
            
            string binPath = Path.Combine(projectDirectory, "bin", "Debug", targetFramework);
            return Path.Combine(binPath, $"{assemblyName}.dll");
        }
        catch
        {
            // 如果无法确定路径，返回默认路径
            return Path.Combine(projectDirectory, "bin", "Debug", "net9.0", "TempProject.dll");
        }
    }

    /// <summary>
    /// 清理临时目录
    /// </summary>
    /// <param name="tempDirectory">临时目录路径</param>
    private static void CleanupTempDirectory(string tempDirectory)
    {
        try
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
        catch
        {
            // 忽略清理错误
        }
    }

    /// <summary>
    /// 尝试执行编译后的代码
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
            System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(compilationResult.AssemblyBytes);

            // 查找入口点类型
            Type? entryType = assembly.GetTypes().FirstOrDefault(t => t.Name == entryPointClass);
            if (entryType == null)
            {
                return (false, "", $"未找到入口点类: {entryPointClass}");
            }

            // 查找入口点方法
            System.Reflection.MethodInfo? entryMethod = entryType.GetMethod(entryPointMethod, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
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
}
