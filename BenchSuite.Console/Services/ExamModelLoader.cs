using System.Text;
using System.Text.Json;
using BenchSuite.Converters;
using BenchSuite.Models;
using BenchSuite.Services;

namespace BenchSuite.Console.Services;

/// <summary>
/// 试卷模型加载器 - 支持多种格式的试卷模型文件
/// </summary>
public static class ExamModelLoader
{
    /// <summary>
    /// 支持的文件格式
    /// </summary>
    public enum FileFormat
    {
        Unknown,
        Json,           // BenchSuite JSON格式
        Xml,            // ExamLab XML格式
        ExamLabProject  // ExamLab项目文件
    }

    /// <summary>
    /// 加载结果
    /// </summary>
    public class LoadResult
    {
        public bool IsSuccess { get; set; }
        public ExamModel? ExamModel { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public FileFormat DetectedFormat { get; set; }
        public int IdConflictsFixed { get; set; }
        public string ValidationSummary { get; set; } = string.Empty;
    }

    /// <summary>
    /// 从文件加载试卷模型
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="verbose">是否启用详细日志</param>
    /// <returns>加载结果</returns>
    public static async Task<LoadResult> LoadAsync(string filePath, bool verbose = false)
    {
        LoadResult result = new();

        try
        {
            if (verbose)
            {
                System.Console.WriteLine($"正在检测文件格式: {filePath}");
            }

            // 检测文件格式
            result.DetectedFormat = DetectFileFormat(filePath);

            if (verbose)
            {
                System.Console.WriteLine($"检测到文件格式: {result.DetectedFormat}");
            }

            // 根据格式加载文件
            switch (result.DetectedFormat)
            {
                case FileFormat.Json:
                    result = await LoadJsonFormatAsync(filePath, verbose);
                    break;

                case FileFormat.Xml:
                case FileFormat.ExamLabProject:
                    result = await LoadExamLabFormatAsync(filePath, verbose);
                    break;

                case FileFormat.Unknown:
                default:
                    result.ErrorMessage = $"不支持的文件格式或无法识别文件类型: {Path.GetExtension(filePath)}";
                    return result;
            }

            if (!result.IsSuccess)
            {
                return result;
            }

            // 执行ID冲突检测和修复
            if (verbose)
            {
                System.Console.WriteLine("正在检测ID冲突...");
            }

            result.IdConflictsFixed = IdConflictResolver.ResolveConflicts(result.ExamModel!);

            if (result.IdConflictsFixed > 0)
            {
                System.Console.WriteLine($"✅ 检测到 {result.IdConflictsFixed} 个ID冲突，已自动修复");
            }
            else if (verbose)
            {
                System.Console.WriteLine("✅ 未发现ID冲突");
            }

            // 验证ID唯一性
            IdValidationResult validationResult = IdConflictResolver.ValidateIds(result.ExamModel!);
            result.ValidationSummary = validationResult.GetSummary();

            if (!validationResult.IsValid)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"ID验证失败: {result.ValidationSummary}";
                return result;
            }

            if (verbose)
            {
                System.Console.WriteLine($"✅ ID验证通过: {result.ValidationSummary}");
            }

            result.IsSuccess = true;
            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"加载文件时发生错误: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 检测文件格式
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件格式</returns>
    private static FileFormat DetectFileFormat(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return FileFormat.Unknown;
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        switch (extension)
        {
            case ".json":
                return FileFormat.Json;

            case ".xml":
                // 进一步检测是否为ExamLab格式
                try
                {
                    string content = File.ReadAllText(filePath, Encoding.UTF8);
                    return content.Contains("<ExamExportDto") || content.Contains("ExamLab") ? FileFormat.ExamLabProject : FileFormat.Xml;
                }
                catch
                {
                    return FileFormat.Xml;
                }

            default:
                return FileFormat.Unknown;
        }
    }

    /// <summary>
    /// 加载JSON格式文件
    /// </summary>
    private static async Task<LoadResult> LoadJsonFormatAsync(string filePath, bool verbose)
    {
        LoadResult result = new() { DetectedFormat = FileFormat.Json };

        try
        {
            if (verbose)
            {
                System.Console.WriteLine("正在读取JSON文件...");
            }

            // 尝试多种编码方式读取文件
            string jsonContent = await ReadFileWithCorrectEncodingAsync(filePath);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                result.ErrorMessage = "JSON文件为空";
                return result;
            }

            // 配置JSON序列化选项
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            // 添加自定义转换器
            options.Converters.Add(new ModuleTypeJsonConverter());
            options.Converters.Add(new ParameterTypeJsonConverter());
            options.Converters.Add(new CSharpQuestionTypeJsonConverter());

            if (verbose)
            {
                System.Console.WriteLine("正在反序列化JSON...");
            }

            // 检查是否为ExamLab导出格式
            JsonDocument jsonDocument = JsonDocument.Parse(jsonContent);
            JsonElement rootElement = jsonDocument.RootElement;

            if (rootElement.TryGetProperty("exam", out _) && rootElement.TryGetProperty("metadata", out _))
            {
                // 使用转换器转换为BenchSuite格式
                result.ExamModel = ExamModelConverter.FromExamLabExport(rootElement);
                result.IsSuccess = true;

                if (verbose)
                {
                    System.Console.WriteLine($"✅ ExamLab JSON格式解析成功: {result.ExamModel.Name}");
                }
            }
            else
            {
                // 尝试直接反序列化为ExamModel
                ExamExportModel? examExportModel = JsonSerializer.Deserialize<ExamExportModel>(jsonContent, options);

                if (examExportModel == null)
                {
                    result.ErrorMessage = "无法解析JSON文件为ExamModel";
                    return result;
                }

                result.ExamModel = examExportModel.Exam;
                result.IsSuccess = true;

                if (verbose)
                {
                    System.Console.WriteLine($"✅ BenchSuite JSON文件加载成功: {examExportModel.Exam.Name}");
                }
            }

            return result;
        }
        catch (JsonException ex)
        {
            result.ErrorMessage = $"JSON格式错误: {ex.Message}";
            if (ex.Path != null)
            {
                result.ErrorMessage += $" (路径: {ex.Path})";
            }

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"读取JSON文件失败: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 加载ExamLab格式文件
    /// </summary>
    private static async Task<LoadResult> LoadExamLabFormatAsync(string filePath, bool verbose)
    {
        LoadResult result = new() { DetectedFormat = FileFormat.ExamLabProject };

        try
        {
            if (verbose)
            {
                System.Console.WriteLine("正在读取ExamLab文件...");
            }

            string content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

            if (string.IsNullOrWhiteSpace(content))
            {
                result.ErrorMessage = "ExamLab文件为空";
                return result;
            }

            // 尝试解析为JSON格式 (ExamLab也支持JSON导出)
            if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || content.TrimStart().StartsWith('{'))
            {
                return await LoadExamLabJsonAsync(content, verbose);
            }

            // 尝试解析为XML格式
            if (filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || content.TrimStart().StartsWith('<'))
            {
                return await LoadExamLabXmlAsync(content, verbose);
            }

            result.ErrorMessage = "无法识别ExamLab文件格式，支持的格式：JSON (.json) 或 XML (.xml)";
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"读取ExamLab文件失败: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 加载ExamLab JSON格式
    /// </summary>
    private static Task<LoadResult> LoadExamLabJsonAsync(string jsonContent, bool verbose)
    {
        LoadResult result = new() { DetectedFormat = FileFormat.Json };

        try
        {
            if (verbose)
            {
                System.Console.WriteLine("正在解析ExamLab JSON格式...");
            }

            // 解析JSON为动态对象
            JsonDocument jsonDocument = JsonDocument.Parse(jsonContent);
            JsonElement rootElement = jsonDocument.RootElement;

            // 检查是否为ExamLab导出格式
            if (rootElement.TryGetProperty("exam", out _) && rootElement.TryGetProperty("metadata", out _))
            {
                // 使用转换器转换为BenchSuite格式
                result.ExamModel = ExamModelConverter.FromExamLabExport(rootElement);
                result.IsSuccess = true;

                if (verbose)
                {
                    System.Console.WriteLine($"✅ ExamLab JSON格式解析成功: {result.ExamModel.Name}");
                }
            }
            else
            {
                result.ErrorMessage = "JSON文件不是有效的ExamLab导出格式";
            }

            return Task.FromResult(result);
        }
        catch (JsonException ex)
        {
            result.ErrorMessage = $"ExamLab JSON格式错误: {ex.Message}";
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"解析ExamLab JSON失败: {ex.Message}";
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// 加载ExamLab XML格式
    /// </summary>
    private static Task<LoadResult> LoadExamLabXmlAsync(string xmlContent, bool verbose)
    {
        LoadResult result = new() { DetectedFormat = FileFormat.Xml };

        try
        {
            if (verbose)
            {
                System.Console.WriteLine("正在解析ExamLab XML格式...");
            }

            // 简单的XML到JSON转换 (用于演示，实际项目中可能需要更复杂的XML解析)
            // 这里可以使用System.Xml.Linq或其他XML解析库

            result.ErrorMessage = "ExamLab XML格式解析功能正在开发中。\n" +
                                 "建议使用ExamLab的JSON导出功能，或联系开发团队获取支持。\n" +
                                 "当前支持的格式：\n" +
                                 "- BenchSuite JSON格式\n" +
                                 "- ExamLab JSON导出格式";
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"解析ExamLab XML失败: {ex.Message}";
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// 验证试卷模型的有效性
    /// </summary>
    /// <param name="examModel">试卷模型</param>
    /// <param name="verbose">是否启用详细日志</param>
    /// <returns>验证结果</returns>
    public static (bool IsValid, string ErrorMessage) ValidateExamModel(ExamModel examModel, bool verbose = false)
    {
        if (verbose)
        {
            System.Console.WriteLine("正在验证试卷模型...");
        }

        if (string.IsNullOrWhiteSpace(examModel.Id))
        {
            return (false, "试卷模型缺少ID");
        }

        if (string.IsNullOrWhiteSpace(examModel.Name))
        {
            return (false, "试卷模型缺少名称");
        }

        if (examModel.Modules == null || examModel.Modules.Count == 0)
        {
            return (false, "试卷模型没有包含任何模块");
        }

        // 查找PowerPoint模块（可选验证）
        ExamModuleModel? pptModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.PowerPoint);
        if (pptModule != null)
        {
            // 如果PowerPoint模块存在，验证其内容
            if (pptModule.Questions == null || pptModule.Questions.Count == 0)
            {
                if (verbose)
                {
                    System.Console.WriteLine("⚠️ 警告: PowerPoint模块没有包含任何题目");
                }
            }
            else
            {
                // 验证是否有操作点
                int totalOperationPoints = pptModule.Questions.Sum(q => q.OperationPoints?.Count ?? 0);
                if (totalOperationPoints == 0)
                {
                    if (verbose)
                    {
                        System.Console.WriteLine("⚠️ 警告: PowerPoint模块没有包含任何操作点");
                    }
                }
            }
        }
        else if (verbose)
        {
            System.Console.WriteLine("⚠️ 警告: 试卷模型中未找到PowerPoint模块");
        }

        if (verbose)
        {
            System.Console.WriteLine($"✅ 试卷模型验证通过:");
            System.Console.WriteLine($"   试卷: {examModel.Name}");
            System.Console.WriteLine($"   模块总数: {examModel.Modules.Count}");

            if (pptModule != null)
            {
                int totalOperationPoints = pptModule.Questions?.Sum(q => q.OperationPoints?.Count ?? 0) ?? 0;
                System.Console.WriteLine($"   PowerPoint模块: {pptModule.Name}");
                System.Console.WriteLine($"   题目数量: {pptModule.Questions?.Count ?? 0}");
                System.Console.WriteLine($"   操作点数量: {totalOperationPoints}");
            }
            else
            {
                System.Console.WriteLine($"   PowerPoint模块: 未找到");
            }
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// 使用正确的编码读取文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件内容</returns>
    private static async Task<string> ReadFileWithCorrectEncodingAsync(string filePath)
    {
        // 注册编码提供程序以支持 GB2312 和 GBK
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // 尝试的编码列表
        List<Encoding> encodings =
        [
            Encoding.UTF8
        ];

        // 安全地添加中文编码
        try
        {
            encodings.Add(Encoding.GetEncoding("GBK"));
        }
        catch
        {
            System.Console.WriteLine("[警告] GBK 编码不可用");
        }

        try
        {
            encodings.Add(Encoding.GetEncoding("GB2312"));
        }
        catch
        {
            System.Console.WriteLine("[警告] GB2312 编码不可用");
        }

        encodings.Add(Encoding.Default);

        foreach (Encoding encoding in encodings)
        {
            try
            {
                string content = await File.ReadAllTextAsync(filePath, encoding);

                // 检查是否包含乱码（简单检测）
                if (!content.Contains("??") && !content.Contains("???") && !content.Contains("�"))
                {
                    System.Console.WriteLine($"[调试] 使用编码 {encoding.EncodingName} 成功读取文件");
                    return content;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[调试] 编码 {encoding.EncodingName} 读取失败: {ex.Message}");
            }
        }

        // 如果所有编码都失败，使用UTF8作为默认
        System.Console.WriteLine("[警告] 无法确定正确的文件编码，使用UTF-8");
        return await File.ReadAllTextAsync(filePath, Encoding.UTF8);
    }
}
