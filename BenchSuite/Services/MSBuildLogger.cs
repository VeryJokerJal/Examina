using Microsoft.Build.Framework;
using BenchSuite.Models;
using System.Text.RegularExpressions;

namespace BenchSuite.Services;

/// <summary>
/// MSBuild日志记录器 - 捕获编译过程中的错误、警告和信息
/// </summary>
public class MSBuildLogger : ILogger
{
    private readonly CompilationResult _compilationResult;
    private readonly List<string> _buildOutput;

    /// <summary>
    /// 初始化MSBuild日志记录器
    /// </summary>
    /// <param name="compilationResult">编译结果对象</param>
    public MSBuildLogger(CompilationResult compilationResult)
    {
        _compilationResult = compilationResult;
        _buildOutput = [];
    }

    /// <summary>
    /// 日志记录器参数
    /// </summary>
    public string? Parameters { get; set; }

    /// <summary>
    /// 日志记录器详细程度
    /// </summary>
    public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Normal;

    /// <summary>
    /// 初始化日志记录器
    /// </summary>
    /// <param name="eventSource">事件源</param>
    public void Initialize(IEventSource eventSource)
    {
        if (eventSource == null) return;

        // 订阅各种构建事件
        eventSource.ErrorRaised += OnErrorRaised;
        eventSource.WarningRaised += OnWarningRaised;
        eventSource.MessageRaised += OnMessageRaised;
        eventSource.BuildStarted += OnBuildStarted;
        eventSource.BuildFinished += OnBuildFinished;
        eventSource.ProjectStarted += OnProjectStarted;
        eventSource.ProjectFinished += OnProjectFinished;
    }

    /// <summary>
    /// 关闭日志记录器
    /// </summary>
    public void Shutdown()
    {
        // 构建完成后处理输出信息
        if (_buildOutput.Count > 0)
        {
            _compilationResult.Details += "\n构建输出:\n" + string.Join("\n", _buildOutput);
        }
    }

    /// <summary>
    /// 处理错误事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">错误事件参数</param>
    private void OnErrorRaised(object sender, BuildErrorEventArgs e)
    {
        if (e == null) return;

        CompilationError error = new()
        {
            Code = e.Code ?? "UNKNOWN",
            Message = e.Message ?? "未知错误",
            Line = e.LineNumber,
            Column = e.ColumnNumber,
            FileName = ExtractFileName(e.File),
            Severity = "Error"
        };

        _compilationResult.Errors.Add(error);
        
        // 记录到构建输出
        _buildOutput.Add($"错误 {error.Code}: {error.Message} ({error.FileName}:{error.Line},{error.Column})");
    }

    /// <summary>
    /// 处理警告事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">警告事件参数</param>
    private void OnWarningRaised(object sender, BuildWarningEventArgs e)
    {
        if (e == null) return;

        CompilationWarning warning = new()
        {
            Code = e.Code ?? "UNKNOWN",
            Message = e.Message ?? "未知警告",
            Line = e.LineNumber,
            Column = e.ColumnNumber,
            FileName = ExtractFileName(e.File)
        };

        _compilationResult.Warnings.Add(warning);
        
        // 记录到构建输出
        _buildOutput.Add($"警告 {warning.Code}: {warning.Message} ({warning.FileName}:{warning.Line},{warning.Column})");
    }

    /// <summary>
    /// 处理消息事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">消息事件参数</param>
    private void OnMessageRaised(object sender, BuildMessageEventArgs e)
    {
        if (e == null || e.Importance == MessageImportance.Low) return;

        // 只记录重要的消息
        if (e.Importance == MessageImportance.High)
        {
            _buildOutput.Add($"信息: {e.Message}");
        }
    }

    /// <summary>
    /// 处理构建开始事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">构建开始事件参数</param>
    private void OnBuildStarted(object sender, BuildStartedEventArgs e)
    {
        _buildOutput.Add("开始MSBuild编译...");
    }

    /// <summary>
    /// 处理构建完成事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">构建完成事件参数</param>
    private void OnBuildFinished(object sender, BuildFinishedEventArgs e)
    {
        string result = e.Succeeded ? "成功" : "失败";
        _buildOutput.Add($"MSBuild编译{result}");
    }

    /// <summary>
    /// 处理项目开始事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">项目开始事件参数</param>
    private void OnProjectStarted(object sender, ProjectStartedEventArgs e)
    {
        if (e?.ProjectFile != null)
        {
            string projectName = Path.GetFileNameWithoutExtension(e.ProjectFile);
            _buildOutput.Add($"编译项目: {projectName}");
        }
    }

    /// <summary>
    /// 处理项目完成事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">项目完成事件参数</param>
    private void OnProjectFinished(object sender, ProjectFinishedEventArgs e)
    {
        if (e?.ProjectFile != null)
        {
            string projectName = Path.GetFileNameWithoutExtension(e.ProjectFile);
            string result = e.Succeeded ? "成功" : "失败";
            _buildOutput.Add($"项目 {projectName} 编译{result}");
        }
    }

    /// <summary>
    /// 提取文件名（去除完整路径，只保留文件名）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件名</returns>
    private static string ExtractFileName(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return "源代码";
        }

        try
        {
            // 如果是临时文件路径，尝试提取有意义的文件名
            string fileName = Path.GetFileName(filePath);
            
            // 如果是我们创建的临时文件，返回更友好的名称
            if (fileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase))
            {
                return "源代码";
            }
            
            if (fileName.Equals("TempProject.csproj", StringComparison.OrdinalIgnoreCase))
            {
                return "项目文件";
            }

            return fileName;
        }
        catch
        {
            return "源代码";
        }
    }

    /// <summary>
    /// 解析编译器错误消息中的位置信息
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>解析后的位置信息</returns>
    private static (int Line, int Column) ParseLocationFromMessage(string message)
    {
        try
        {
            // 尝试从错误消息中提取行号和列号
            // 格式通常是: "Program.cs(10,5): error CS1002: ..."
            Regex locationRegex = new(@"\((\d+),(\d+)\):", RegexOptions.Compiled);
            Match match = locationRegex.Match(message);
            
            if (match.Success && match.Groups.Count >= 3)
            {
                if (int.TryParse(match.Groups[1].Value, out int line) &&
                    int.TryParse(match.Groups[2].Value, out int column))
                {
                    return (line, column);
                }
            }
        }
        catch
        {
            // 忽略解析错误
        }

        return (0, 0);
    }

    /// <summary>
    /// 获取构建输出摘要
    /// </summary>
    /// <returns>构建输出摘要</returns>
    public string GetBuildSummary()
    {
        List<string> summary = [];
        
        if (_compilationResult.Errors.Count > 0)
        {
            summary.Add($"错误: {_compilationResult.Errors.Count} 个");
        }
        
        if (_compilationResult.Warnings.Count > 0)
        {
            summary.Add($"警告: {_compilationResult.Warnings.Count} 个");
        }
        
        if (_compilationResult.IsSuccess)
        {
            summary.Add("编译成功");
        }
        else
        {
            summary.Add("编译失败");
        }

        return string.Join(", ", summary);
    }
}
