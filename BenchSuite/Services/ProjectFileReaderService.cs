using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;

namespace BenchSuite.Services;

/// <summary>
/// 项目文件读取服务 - 读取.csproj项目文件下的所有C#源代码文件
/// </summary>
public static class ProjectFileReaderService
{
    /// <summary>
    /// 项目代码读取结果
    /// </summary>
    public class ProjectCodeResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 项目根目录
        /// </summary>
        public string ProjectDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// 目标框架
        /// </summary>
        public string TargetFramework { get; set; } = string.Empty;

        /// <summary>
        /// 源代码文件列表
        /// </summary>
        public List<SourceCodeFile> SourceFiles { get; set; } = [];

        /// <summary>
        /// 合并后的所有源代码内容
        /// </summary>
        public string CombinedSourceCode { get; set; } = string.Empty;

        /// <summary>
        /// 代码统计信息
        /// </summary>
        public CodeStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// 源代码文件信息
    /// </summary>
    public class SourceCodeFile
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 相对路径
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long SizeBytes { get; set; }

        /// <summary>
        /// 行数
        /// </summary>
        public int LineCount { get; set; }

        /// <summary>
        /// 是否为生成的文件
        /// </summary>
        public bool IsGenerated { get; set; }
    }

    /// <summary>
    /// 代码统计信息
    /// </summary>
    public class CodeStatistics
    {
        /// <summary>
        /// 总文件数
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// 总行数
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// 总字符数
        /// </summary>
        public int TotalCharacters { get; set; }

        /// <summary>
        /// 类数量
        /// </summary>
        public int ClassCount { get; set; }

        /// <summary>
        /// 接口数量
        /// </summary>
        public int InterfaceCount { get; set; }

        /// <summary>
        /// 方法数量
        /// </summary>
        public int MethodCount { get; set; }

        /// <summary>
        /// 命名空间数量
        /// </summary>
        public int NamespaceCount { get; set; }
    }

    /// <summary>
    /// 读取项目文件下的所有C#源代码
    /// </summary>
    /// <param name="projectFilePath">项目文件路径</param>
    /// <returns>项目代码读取结果</returns>
    public static async Task<ProjectCodeResult> ReadProjectCodeAsync(string projectFilePath)
    {
        ProjectCodeResult result = new();

        try
        {
            // 验证项目文件
            if (!File.Exists(projectFilePath))
            {
                result.ErrorMessage = $"项目文件不存在: {projectFilePath}";
                return result;
            }

            if (!projectFilePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorMessage = "只支持.csproj项目文件";
                return result;
            }

            // 获取项目信息
            result.ProjectDirectory = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
            result.ProjectName = Path.GetFileNameWithoutExtension(projectFilePath);

            // 使用MSBuild加载项目
            ProjectCollection projectCollection = new();
            Project project = projectCollection.LoadProject(projectFilePath);
            
            result.TargetFramework = project.GetPropertyValue("TargetFramework");

            // 收集源代码文件
            List<SourceCodeFile> sourceFiles = await CollectSourceFilesAsync(result.ProjectDirectory);
            
            // 过滤和处理文件
            result.SourceFiles = FilterAndProcessFiles(sourceFiles, result.ProjectDirectory);

            // 生成合并的源代码
            result.CombinedSourceCode = GenerateCombinedSourceCode(result.SourceFiles);

            // 生成统计信息
            result.Statistics = GenerateStatistics(result.SourceFiles);

            result.IsSuccess = true;

            // 清理项目集合
            projectCollection.UnloadAllProjects();
            projectCollection.Dispose();
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"读取项目代码时发生异常: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 递归收集所有C#源文件
    /// </summary>
    /// <param name="projectDirectory">项目目录</param>
    /// <returns>源文件列表</returns>
    private static async Task<List<SourceCodeFile>> CollectSourceFilesAsync(string projectDirectory)
    {
        List<SourceCodeFile> sourceFiles = [];

        try
        {
            string[] csFiles = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories);

            foreach (string filePath in csFiles)
            {
                try
                {
                    // 跳过不需要的目录
                    if (ShouldSkipFile(filePath, projectDirectory))
                    {
                        continue;
                    }

                    string content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                    FileInfo fileInfo = new(filePath);

                    SourceCodeFile sourceFile = new()
                    {
                        FilePath = filePath,
                        RelativePath = Path.GetRelativePath(projectDirectory, filePath),
                        FileName = Path.GetFileName(filePath),
                        Content = content,
                        SizeBytes = fileInfo.Length,
                        LineCount = content.Split('\n').Length,
                        IsGenerated = IsGeneratedFile(content, filePath)
                    };

                    sourceFiles.Add(sourceFile);
                }
                catch (Exception ex)
                {
                    // 记录但不中断处理
                    Console.WriteLine($"读取文件失败 {filePath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"收集源文件时发生异常: {ex.Message}");
        }

        return sourceFiles;
    }

    /// <summary>
    /// 判断是否应该跳过文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="projectDirectory">项目目录</param>
    /// <returns>是否跳过</returns>
    private static bool ShouldSkipFile(string filePath, string projectDirectory)
    {
        string relativePath = Path.GetRelativePath(projectDirectory, filePath);
        
        // 跳过的目录
        string[] skipDirectories = 
        [
            "bin", "obj", ".vs", ".git", "packages", 
            "TestResults", "Debug", "Release", "x64", "x86"
        ];

        foreach (string skipDir in skipDirectories)
        {
            if (relativePath.StartsWith(skipDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith(skipDir + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // 跳过的文件模式
        string fileName = Path.GetFileName(filePath);
        string[] skipPatterns = 
        [
            "AssemblyInfo.cs", "GlobalAssemblyInfo.cs", "TemporaryGeneratedFile_",
            ".Designer.cs", ".g.cs", ".g.i.cs"
        ];

        foreach (string pattern in skipPatterns)
        {
            if (fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断是否为生成的文件
    /// </summary>
    /// <param name="content">文件内容</param>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否为生成的文件</returns>
    private static bool IsGeneratedFile(string content, string filePath)
    {
        // 检查文件内容中的生成标记
        string[] generatedMarkers = 
        [
            "<auto-generated", "This code was generated", "// <auto-generated>",
            "#pragma warning disable", "// <autogenerated />",
            "Microsoft Visual Studio", "// Generated by"
        ];

        foreach (string marker in generatedMarkers)
        {
            if (content.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // 检查文件名模式
        string fileName = Path.GetFileName(filePath);
        if (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 过滤和处理文件
    /// </summary>
    /// <param name="sourceFiles">源文件列表</param>
    /// <param name="projectDirectory">项目目录</param>
    /// <returns>过滤后的文件列表</returns>
    private static List<SourceCodeFile> FilterAndProcessFiles(List<SourceCodeFile> sourceFiles, string projectDirectory)
    {
        return sourceFiles
            .Where(f => !f.IsGenerated) // 过滤生成的文件
            .Where(f => !string.IsNullOrWhiteSpace(f.Content)) // 过滤空文件
            .Where(f => f.SizeBytes < 1024 * 1024) // 过滤过大的文件（1MB）
            .OrderBy(f => f.RelativePath) // 按路径排序
            .ToList();
    }

    /// <summary>
    /// 生成合并的源代码
    /// </summary>
    /// <param name="sourceFiles">源文件列表</param>
    /// <returns>合并的源代码</returns>
    private static string GenerateCombinedSourceCode(List<SourceCodeFile> sourceFiles)
    {
        StringBuilder combined = new();

        combined.AppendLine("// ===== 项目源代码合并文件 =====");
        combined.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        combined.AppendLine($"// 文件数量: {sourceFiles.Count}");
        combined.AppendLine();

        foreach (SourceCodeFile file in sourceFiles)
        {
            combined.AppendLine($"// ===== 文件: {file.RelativePath} =====");
            combined.AppendLine($"// 大小: {file.SizeBytes} 字节, 行数: {file.LineCount}");
            combined.AppendLine();
            combined.AppendLine(file.Content);
            combined.AppendLine();
            combined.AppendLine($"// ===== 文件结束: {file.RelativePath} =====");
            combined.AppendLine();
        }

        return combined.ToString();
    }

    /// <summary>
    /// 生成代码统计信息
    /// </summary>
    /// <param name="sourceFiles">源文件列表</param>
    /// <returns>统计信息</returns>
    private static CodeStatistics GenerateStatistics(List<SourceCodeFile> sourceFiles)
    {
        CodeStatistics stats = new()
        {
            TotalFiles = sourceFiles.Count,
            TotalLines = sourceFiles.Sum(f => f.LineCount),
            TotalCharacters = sourceFiles.Sum(f => f.Content.Length)
        };

        // 统计代码结构
        foreach (SourceCodeFile file in sourceFiles)
        {
            stats.ClassCount += CountPattern(file.Content, @"\bclass\s+\w+");
            stats.InterfaceCount += CountPattern(file.Content, @"\binterface\s+\w+");
            stats.MethodCount += CountPattern(file.Content, @"\b(public|private|protected|internal)?\s*(static)?\s*\w+\s+\w+\s*\([^)]*\)\s*\{");
            stats.NamespaceCount += CountPattern(file.Content, @"\bnamespace\s+[\w\.]+");
        }

        return stats;
    }

    /// <summary>
    /// 统计模式匹配数量
    /// </summary>
    /// <param name="content">内容</param>
    /// <param name="pattern">正则表达式模式</param>
    /// <returns>匹配数量</returns>
    private static int CountPattern(string content, string pattern)
    {
        try
        {
            return Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline).Count;
        }
        catch
        {
            return 0;
        }
    }
}
