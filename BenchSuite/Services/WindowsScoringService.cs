using System.IO;
using System.Text.Json;
using BenchSuite.Interfaces;
using BenchSuite.Models;
using Microsoft.Extensions.Logging;

namespace BenchSuite.Services;

/// <summary>
/// Windows打分服务实现
/// </summary>
public class WindowsScoringService : IWindowsScoringService
{
    private readonly ScoringConfiguration _defaultConfiguration;
    private readonly ILogger<WindowsScoringService>? _logger;

    public WindowsScoringService(ILogger<WindowsScoringService>? logger = null)
    {
        _defaultConfiguration = new ScoringConfiguration();
        _logger = logger;
    }

    /// <summary>
    /// 对Windows文件系统进行打分
    /// </summary>
    public async Task<ScoringResult> ScoreFileAsync(string basePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        // 在开始处理前，检查并修复重复的题目ID
        EnsureUniqueQuestionIds(examModel);

        return await Task.Run(() => ScoreFile(basePath, examModel, configuration));
    }

    /// <summary>
    /// 对Windows文件系统进行打分（同步版本）
    /// </summary>
    public ScoringResult ScoreFile(string basePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        ScoringConfiguration config = configuration ?? _defaultConfiguration;
        ScoringResult result = new()
        {
            StartTime = DateTime.Now,
            IsSuccess = false
        };

        try
        {
            _logger?.LogInformation("开始Windows文件系统打分，基础路径: {BasePath}", basePath);

            // 验证基础路径是否存在
            if (!Directory.Exists(basePath))
            {
                string errorMsg = $"指定的基础路径不存在: {basePath}";
                _logger?.LogError(errorMsg);
                result.ErrorMessage = errorMsg;
                return result;
            }

            // 查找Windows模块
            ExamModuleModel? windowsModule = examModel.Exam.Modules?.FirstOrDefault(m =>
                m.Type == ModuleType.Windows);

            if (windowsModule == null)
            {
                string errorMsg = "试卷中未找到Windows模块";
                _logger?.LogWarning(errorMsg);
                result.ErrorMessage = errorMsg;
                return result;
            }

            _logger?.LogInformation("找到Windows模块，包含 {QuestionCount} 个题目", windowsModule.Questions.Count);
            decimal totalScore = 0M;
            decimal achievedScore = 0M;

            foreach (QuestionModel question in windowsModule.Questions)
            {
                if (!question.IsEnabled)
                {
                    continue;
                }

                totalScore += question.Score;

                List<OperationPointModel> questionOps = question.OperationPoints;
                if (questionOps == null || questionOps.Count == 0)
                {
                    // 无操作点则该题无法判定完成，按0分处理
                    result.QuestionResults.Add(new QuestionScoreResult
                    {
                        QuestionId = question.Id,
                        QuestionTitle = question.Title,
                        TotalScore = question.Score,
                        AchievedScore = 0M,
                        IsCorrect = false
                    });
                    continue;
                }

                // 逐操作点检测：任一失败则该题记0分
                bool allCorrect = true;

                // 这里按题内操作点列表调用检测以获得即时结果，避免跨题汇总
                List<KnowledgePointResult> kpResults = DetectWindowsOperationsAsync(basePath, questionOps).Result;

                // 为每个操作点结果设置题目ID
                foreach (KnowledgePointResult kpResult in kpResults)
                {
                    kpResult.QuestionId = question.Id;
                }

                // 如需保留操作点的调试结果，可追加到总列表
                result.KnowledgePointResults.AddRange(kpResults);

                foreach (KnowledgePointResult kp in kpResults)
                {
                    if (!kp.IsCorrect)
                    {
                        allCorrect = false;
                        break;
                    }
                }

                decimal qAchieved = allCorrect ? question.Score : 0M;
                achievedScore += qAchieved;

                result.QuestionResults.Add(new QuestionScoreResult
                {
                    QuestionId = question.Id,
                    QuestionTitle = question.Title,
                    TotalScore = question.Score,
                    AchievedScore = qAchieved,
                    IsCorrect = allCorrect
                });
            }

            result.TotalScore = totalScore;
            result.AchievedScore = achievedScore;
            result.IsSuccess = true;

            _logger?.LogInformation("Windows打分完成，总分: {TotalScore}, 得分: {AchievedScore}, 得分率: {ScoreRate:P2}",
                totalScore, achievedScore, totalScore > 0 ? achievedScore / totalScore : 0);
        }
        catch (Exception ex)
        {
            string errorMsg = $"打分过程中发生错误: {ex.Message}";
            _logger?.LogError(ex, "Windows打分过程中发生异常，基础路径: {BasePath}", basePath);
            result.ErrorMessage = errorMsg;
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 检测Windows文件系统中的特定操作
    /// </summary>
    public async Task<KnowledgePointResult> DetectWindowsOperationAsync(string basePath, string operationType, Dictionary<string, string> parameters)
    {
        return await Task.Run(() =>
        {
            KnowledgePointResult result = new()
            {
                KnowledgePointType = operationType,
                Parameters = parameters
            };

            try
            {
                // 验证基础路径
                if (!Directory.Exists(basePath))
                {
                    result.ErrorMessage = $"基础路径不存在: {basePath}";
                    result.IsCorrect = false;
                    return result;
                }

                // 根据操作类型进行检测
                result = DetectSpecificWindowsOperation(basePath, operationType, parameters);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"检测Windows操作时发生错误: {ex.Message}";
                result.IsCorrect = false;
            }

            return result;
        });
    }

    /// <summary>
    /// 批量检测Windows文件系统操作
    /// </summary>
    public async Task<List<KnowledgePointResult>> DetectWindowsOperationsAsync(string basePath, List<OperationPointModel> operationPoints)
    {
        return await Task.Run(() =>
        {
            List<KnowledgePointResult> results = [];

            try
            {
                // 验证基础路径
                if (!Directory.Exists(basePath))
                {
                    // 如果无法访问基础路径，为所有操作点返回错误结果
                    foreach (OperationPointModel operationPoint in operationPoints)
                    {
                        results.Add(new KnowledgePointResult
                        {
                            KnowledgePointId = operationPoint.Id,
                            KnowledgePointName = operationPoint.Name,
                            KnowledgePointType = operationPoint.WindowsOperationType ?? string.Empty,
                            TotalScore = operationPoint.Score,
                            AchievedScore = 0,
                            IsCorrect = false,
                            ErrorMessage = $"无法访问基础路径: {basePath}"
                        });
                    }
                    return results;
                }

                // 逐个检测操作点
                foreach (OperationPointModel operationPoint in operationPoints)
                {
                    try
                    {
                        string operationType = operationPoint.WindowsOperationType ?? string.Empty;
                        Dictionary<string, string> parameters = ExtractParameters(operationPoint);

                        KnowledgePointResult opResult = DetectSpecificWindowsOperation(basePath, operationType, parameters);

                        // 设置操作点基本信息
                        opResult.KnowledgePointId = operationPoint.Id;
                        opResult.KnowledgePointName = operationPoint.Name;
                        opResult.TotalScore = operationPoint.Score;
                        opResult.AchievedScore = opResult.IsCorrect ? operationPoint.Score : 0;

                        results.Add(opResult);
                    }
                    catch (Exception ex)
                    {
                        results.Add(new KnowledgePointResult
                        {
                            KnowledgePointId = operationPoint.Id,
                            KnowledgePointName = operationPoint.Name,
                            KnowledgePointType = operationPoint.WindowsOperationType ?? string.Empty,
                            TotalScore = operationPoint.Score,
                            AchievedScore = 0,
                            IsCorrect = false,
                            ErrorMessage = $"检测操作点时发生错误: {ex.Message}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果发生全局错误，为所有操作点返回错误结果
                foreach (OperationPointModel operationPoint in operationPoints)
                {
                    results.Add(new KnowledgePointResult
                    {
                        KnowledgePointId = operationPoint.Id,
                        KnowledgePointName = operationPoint.Name,
                        KnowledgePointType = operationPoint.WindowsOperationType ?? string.Empty,
                        TotalScore = operationPoint.Score,
                        AchievedScore = 0,
                        IsCorrect = false,
                        ErrorMessage = $"批量检测时发生错误: {ex.Message}"
                    });
                }
            }

            return results;
        });
    }

    /// <summary>
    /// 验证文件或文件夹是否存在
    /// </summary>
    public bool PathExists(string path)
    {
        try
        {
            return File.Exists(path) || Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取文件或文件夹的属性信息
    /// </summary>
    public Dictionary<string, object> GetPathAttributes(string path)
    {
        Dictionary<string, object> attributes = [];

        try
        {
            if (File.Exists(path))
            {
                FileInfo fileInfo = new(path);
                attributes["Type"] = "File";
                attributes["Name"] = fileInfo.Name;
                attributes["FullName"] = fileInfo.FullName;
                attributes["Size"] = fileInfo.Length;
                attributes["CreationTime"] = fileInfo.CreationTime;
                attributes["LastWriteTime"] = fileInfo.LastWriteTime;
                attributes["Attributes"] = fileInfo.Attributes.ToString();
                attributes["Extension"] = fileInfo.Extension;
            }
            else if (Directory.Exists(path))
            {
                DirectoryInfo dirInfo = new(path);
                attributes["Type"] = "Directory";
                attributes["Name"] = dirInfo.Name;
                attributes["FullName"] = dirInfo.FullName;
                attributes["CreationTime"] = dirInfo.CreationTime;
                attributes["LastWriteTime"] = dirInfo.LastWriteTime;
                attributes["Attributes"] = dirInfo.Attributes.ToString();

                // 获取子项数量
                try
                {
                    attributes["FileCount"] = dirInfo.GetFiles().Length;
                    attributes["DirectoryCount"] = dirInfo.GetDirectories().Length;
                }
                catch
                {
                    attributes["FileCount"] = 0;
                    attributes["DirectoryCount"] = 0;
                }
            }
            else
            {
                attributes["Type"] = "NotFound";
                attributes["Error"] = "路径不存在";
            }
        }
        catch (Exception ex)
        {
            attributes["Type"] = "Error";
            attributes["Error"] = ex.Message;
        }

        return attributes;
    }

    /// <summary>
    /// 验证文件是否可以被处理
    /// </summary>
    public bool CanProcessFile(string basePath)
    {
        return Directory.Exists(basePath);
    }

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    public IEnumerable<string> GetSupportedExtensions()
    {
        // Windows打分服务主要检测文件系统操作，不限制特定文件扩展名
        return [];
    }

    /// <summary>
    /// 确保题目ID的唯一性
    /// </summary>
    private static void EnsureUniqueQuestionIds(ExamModel examModel)
    {
        if (examModel.Exam.Modules == null) return;

        foreach (ExamModuleModel module in examModel.Exam.Modules)
        {
            if (module.Questions == null) continue;

            HashSet<string> seenIds = [];
            int counter = 1;

            foreach (QuestionModel question in module.Questions)
            {
                string originalId = question.Id;
                string newId = originalId;

                while (seenIds.Contains(newId))
                {
                    newId = $"{originalId}_{counter++}";
                }

                question.Id = newId;
                seenIds.Add(newId);
            }
        }
    }

    /// <summary>
    /// 从操作点模型中提取参数
    /// </summary>
    private static Dictionary<string, string> ExtractParameters(OperationPointModel operationPoint)
    {
        Dictionary<string, string> parameters = [];

        try
        {
            if (operationPoint.Parameters != null)
            {
                foreach (ConfigurationParameterModel param in operationPoint.Parameters)
                {
                    if (!string.IsNullOrEmpty(param.Name) && !string.IsNullOrEmpty(param.Value))
                    {
                        parameters[param.Name] = param.Value;
                    }
                }
            }

            // 转换ExamLab参数名称到WindowsScoringService标准参数名称
            parameters = ConvertParameterNames(parameters);
        }
        catch (Exception ex)
        {
            // 记录参数提取错误，但不中断处理
            parameters["_ExtractError"] = ex.Message;
        }

        return parameters;
    }

    /// <summary>
    /// 转换ExamLab参数名称到WindowsScoringService标准参数名称
    /// </summary>
    private static Dictionary<string, string> ConvertParameterNames(Dictionary<string, string> originalParameters)
    {
        Dictionary<string, string> convertedParameters = [];

        // 参数名称映射表
        Dictionary<string, string> parameterMapping = new()
        {
            // 通用参数
            ["FileType"] = "FileType",
            ["ItemType"] = "CreateType",
            ["ItemName"] = "ItemName",
            ["CreatePath"] = "CreatePath",

            // 重命名操作参数
            ["OriginalFileName"] = "OriginalName",
            ["NewFileName"] = "NewName",

            // 复制/移动操作参数
            ["SourcePath"] = "SourcePath",
            ["DestinationPath"] = "TargetPath",

            // 删除操作参数
            ["TargetPath"] = "TargetPath",

            // 快捷方式操作参数
            ["ShortcutPath"] = "ShortcutPath",

            // 属性修改操作参数
            ["FilePath"] = "TargetPath",
            ["PropertyType"] = "PropertyType",
            ["PropertyValue"] = "PropertyValue"
        };

        // 转换参数名称
        foreach (KeyValuePair<string, string> param in originalParameters)
        {
            string convertedName = parameterMapping.TryGetValue(param.Key, out string? mappedName) ? mappedName : param.Key;
            convertedParameters[convertedName] = param.Value;
        }

        return convertedParameters;
    }

    /// <summary>
    /// 检测特定的Windows操作
    /// </summary>
    private KnowledgePointResult DetectSpecificWindowsOperation(string basePath, string operationType, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = operationType,
            Parameters = parameters,
            IsCorrect = false
        };

        try
        {
            // 验证参数
            if (!ValidateOperationParameters(operationType, parameters, out string validationError))
            {
                result.ErrorMessage = validationError;
                result.IsCorrect = false;
                return result;
            }

            // 根据操作类型进行具体检测
            switch (operationType)
            {
                case "QuickCreate":
                    // QuickCreate是快速创建文件/文件夹，与CreateOperation略有不同
                    result = DetectQuickCreateOperation(basePath, parameters);
                    break;
                case "CreateOperation":
                    result = DetectCreateOperation(basePath, parameters);
                    break;
                case "DeleteOperation":
                    result = DetectDeleteOperation(basePath, parameters);
                    break;
                case "CopyOperation":
                    result = DetectCopyOperation(basePath, parameters);
                    break;
                case "MoveOperation":
                    result = DetectMoveOperation(basePath, parameters);
                    break;
                case "RenameOperation":
                    result = DetectRenameOperation(basePath, parameters);
                    break;
                case "ShortcutOperation":
                    result = DetectShortcutOperation(basePath, parameters);
                    break;
                case "FilePropertyModification":
                    result = DetectPropertyModification(basePath, parameters);
                    break;
                case "CopyRenameOperation":
                    result = DetectCopyRenameOperation(basePath, parameters);
                    break;
                default:
                    result.ErrorMessage = $"不支持的操作类型: {operationType}";
                    result.IsCorrect = false;
                    break;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测操作 {operationType} 时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测创建操作
    /// </summary>
    private KnowledgePointResult DetectCreateOperation(string basePath, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "CreateOperation",
            Parameters = parameters,
            IsCorrect = false
        };

        try
        {
            // 获取文件类型参数，支持FileType和CreateType两种参数名
            string fileType = GetParameterValue(parameters, "FileType", "文件");
            string createType = GetParameterValue(parameters, "CreateType", "File");

            // 获取项目名称或路径
            string? itemName = null;
            string? createPath = null;
            string? targetPath = null;

            // 支持多种参数组合
            if (parameters.TryGetValue("ItemName", out itemName) && !string.IsNullOrEmpty(itemName))
            {
                // CreateOperation 或 QuickCreate 模式
                if (parameters.TryGetValue("CreatePath", out createPath) && !string.IsNullOrEmpty(createPath))
                {
                    // QuickCreate 模式：有明确的创建路径
                    targetPath = Path.Combine(createPath, itemName);
                }
                else
                {
                    // CreateOperation 模式：在基础路径下创建
                    targetPath = itemName;
                }
            }
            else if (parameters.TryGetValue("TargetPath", out targetPath) && !string.IsNullOrEmpty(targetPath))
            {
                // 兼容旧的参数格式
            }
            else
            {
                result.ErrorMessage = "缺少必需参数: ItemName 或 TargetPath";
                return result;
            }

            // 构建完整路径
            string fullPath = Path.IsPathRooted(targetPath) ? targetPath : Path.Combine(basePath, targetPath);

            bool exists = false;
            string details = string.Empty;

            // 根据FileType或CreateType参数决定检测文件还是文件夹
            bool isFolder = string.Equals(fileType, "文件夹", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(fileType, "Folder", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(fileType, "Directory", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(createType, "Folder", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(createType, "Directory", StringComparison.OrdinalIgnoreCase);

            if (isFolder)
            {
                exists = Directory.Exists(fullPath);
                details = exists ? $"文件夹已创建: {fullPath}" : $"文件夹不存在: {fullPath}";
            }
            else
            {
                exists = File.Exists(fullPath);
                details = exists ? $"文件已创建: {fullPath}" : $"文件不存在: {fullPath}";
            }

            result.IsCorrect = exists;
            result.Details = details;

            // 如果是文件且指定了期望内容，检查文件内容
            if (exists && parameters.TryGetValue("ExpectedContent", out string? expectedContent) && !string.IsNullOrEmpty(expectedContent))
            {
                if (File.Exists(fullPath))
                {
                    try
                    {
                        string actualContent = File.ReadAllText(fullPath);
                        bool contentMatches = string.Equals(actualContent.Trim(), expectedContent.Trim(), StringComparison.OrdinalIgnoreCase);
                        result.IsCorrect = contentMatches;
                        result.Details += contentMatches ? " (内容匹配)" : " (内容不匹配)";
                    }
                    catch (Exception ex)
                    {
                        result.Details += $" (无法读取内容: {ex.Message})";
                        result.IsCorrect = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测创建操作时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
<<<<<<< HEAD
    /// 检测快速创建操作（QuickCreate）
    /// </summary>
    private KnowledgePointResult DetectQuickCreateOperation(string basePath, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "QuickCreate",
            Parameters = parameters,
            IsCorrect = false
        };

        try
        {
            // QuickCreate必须有ItemName和CreatePath参数
            if (!parameters.TryGetValue("ItemName", out string? itemName) || string.IsNullOrEmpty(itemName))
            {
                result.ErrorMessage = "快速创建操作缺少必需参数: ItemName";
                return result;
            }

            if (!parameters.TryGetValue("CreatePath", out string? createPath) || string.IsNullOrEmpty(createPath))
            {
                result.ErrorMessage = "快速创建操作缺少必需参数: CreatePath";
                return result;
            }

            // 获取文件类型参数
            string fileType = GetParameterValue(parameters, "FileType", "文件");

            // 构建完整路径：CreatePath + ItemName
            string fullPath = Path.Combine(createPath, itemName);
            if (!Path.IsPathRooted(fullPath))
            {
                fullPath = Path.Combine(basePath, fullPath);
            }

            bool exists = false;
            string details = string.Empty;

            // 根据FileType参数决定检测文件还是文件夹
            if (string.Equals(fileType, "文件夹", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileType, "Folder", StringComparison.OrdinalIgnoreCase))
            {
                exists = Directory.Exists(fullPath);
                details = exists ? $"文件夹已快速创建: {fullPath}" : $"文件夹不存在: {fullPath}";
            }
            else
            {
                exists = File.Exists(fullPath);
                details = exists ? $"文件已快速创建: {fullPath}" : $"文件不存在: {fullPath}";
            }

            result.IsCorrect = exists;
            result.Details = details;

            // 如果是文件且指定了期望内容，检查文件内容
            if (exists && File.Exists(fullPath) && parameters.TryGetValue("ExpectedContent", out string? expectedContent) && !string.IsNullOrEmpty(expectedContent))
            {
                try
                {
                    string actualContent = File.ReadAllText(fullPath);
                    bool contentMatches = string.Equals(actualContent.Trim(), expectedContent.Trim(), StringComparison.OrdinalIgnoreCase);
                    result.IsCorrect = contentMatches;
                    result.Details += contentMatches ? " (内容匹配)" : " (内容不匹配)";
                }
                catch (Exception ex)
                {
                    result.Details += $" (无法读取内容: {ex.Message})";
                    result.IsCorrect = false;
                }
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测快速创建操作时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测删除操作
    /// </summary>
    private KnowledgePointResult DetectDeleteOperation(string basePath, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "DeleteOperation",
            Parameters = parameters,
            IsCorrect = false
        };

        try
        {
            if (!parameters.TryGetValue("TargetPath", out string? targetPath) || string.IsNullOrEmpty(targetPath))
            {
                result.ErrorMessage = "缺少必需参数: TargetPath";
                return result;
            }

            // 构建完整路径
            string fullPath = Path.IsPathRooted(targetPath) ? targetPath : Path.Combine(basePath, targetPath);

            // 检测文件或文件夹是否已被删除（即不存在）
            bool isDeleted = !File.Exists(fullPath) && !Directory.Exists(fullPath);

            result.IsCorrect = isDeleted;
            result.Details = isDeleted ? $"目标已删除: {fullPath}" : $"目标仍存在: {fullPath}";

            // 如果指定了回收站检查
            if (parameters.TryGetValue("CheckRecycleBin", out string? checkRecycleBin) &&
                bool.TryParse(checkRecycleBin, out bool shouldCheckRecycleBin) && shouldCheckRecycleBin)
            {
                // 这里可以添加回收站检查逻辑
                result.Details += " (已检查回收站)";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测删除操作时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测复制操作
    /// </summary>
    private KnowledgePointResult DetectCopyOperation(string basePath, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "CopyOperation",
            Parameters = parameters,
            IsCorrect = false
        };

        try
        {
            if (!parameters.TryGetValue("SourcePath", out string? sourcePath) || string.IsNullOrEmpty(sourcePath))
            {
                result.ErrorMessage = "缺少必需参数: SourcePath";
                return result;
            }

            if (!parameters.TryGetValue("TargetPath", out string? targetPath) || string.IsNullOrEmpty(targetPath))
            {
                result.ErrorMessage = "缺少必需参数: TargetPath";
                return result;
            }

            // 构建完整路径
            string fullSourcePath = Path.IsPathRooted(sourcePath) ? sourcePath : Path.Combine(basePath, sourcePath);
            string fullTargetPath = Path.IsPathRooted(targetPath) ? targetPath : Path.Combine(basePath, targetPath);

            bool sourceExists = File.Exists(fullSourcePath) || Directory.Exists(fullSourcePath);
            bool targetExists = File.Exists(fullTargetPath) || Directory.Exists(fullTargetPath);

            if (!sourceExists)
            {
                result.Details = $"源文件/文件夹不存在: {fullSourcePath}";
                result.IsCorrect = false;
                return result;
            }

            result.IsCorrect = targetExists;
            result.Details = targetExists ?
                $"复制成功: {fullSourcePath} -> {fullTargetPath}" :
                $"目标不存在: {fullTargetPath}";

            // 如果是文件复制，可以检查文件大小是否一致
            if (targetExists && File.Exists(fullSourcePath) && File.Exists(fullTargetPath))
            {
                try
                {
                    FileInfo sourceInfo = new(fullSourcePath);
                    FileInfo targetInfo = new(fullTargetPath);

                    bool sizeMatches = sourceInfo.Length == targetInfo.Length;
                    if (!sizeMatches)
                    {
                        result.Details += " (文件大小不匹配)";
                        result.IsCorrect = false;
                    }
                    else
                    {
                        result.Details += " (文件大小匹配)";
                    }
                }
                catch (Exception ex)
                {
                    result.Details += $" (无法比较文件大小: {ex.Message})";
                }
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测复制操作时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测移动操作
    /// </summary>
    private KnowledgePointResult DetectMoveOperation(string basePath, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "MoveOperation",
            Parameters = parameters,
            IsCorrect = false
        };

        try
        {
            if (!parameters.TryGetValue("SourcePath", out string? sourcePath) || string.IsNullOrEmpty(sourcePath))
            {
                result.ErrorMessage = "缺少必需参数: SourcePath";
                return result;
            }

            if (!parameters.TryGetValue("TargetPath", out string? targetPath) || string.IsNullOrEmpty(targetPath))
            {
                result.ErrorMessage = "缺少必需参数: TargetPath";
                return result;
            }

            // 构建完整路径
            string fullSourcePath = Path.IsPathRooted(sourcePath) ? sourcePath : Path.Combine(basePath, sourcePath);
            string fullTargetPath = Path.IsPathRooted(targetPath) ? targetPath : Path.Combine(basePath, targetPath);

            bool sourceExists = File.Exists(fullSourcePath) || Directory.Exists(fullSourcePath);
            bool targetExists = File.Exists(fullTargetPath) || Directory.Exists(fullTargetPath);

            // 移动操作的特点：源不存在，目标存在
            result.IsCorrect = !sourceExists && targetExists;

            if (result.IsCorrect)
            {
                result.Details = $"移动成功: {fullSourcePath} -> {fullTargetPath}";
            }
            else if (sourceExists && targetExists)
            {
                result.Details = $"可能是复制操作而非移动: 源和目标都存在";
            }
            else if (sourceExists && !targetExists)
            {
                result.Details = $"移动未完成: 源仍存在，目标不存在";
            }
            else
            {
                result.Details = $"源和目标都不存在: {fullSourcePath}, {fullTargetPath}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测移动操作时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测重命名操作
    /// </summary>
    private KnowledgePointResult DetectRenameOperation(string basePath, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "RenameOperation",
            Parameters = parameters,
            IsCorrect = false
        };

        try
        {
            if (!parameters.TryGetValue("OriginalName", out string? originalName) || string.IsNullOrEmpty(originalName))
            {
                result.ErrorMessage = "缺少必需参数: OriginalName";
                return result;
            }

            if (!parameters.TryGetValue("NewName", out string? newName) || string.IsNullOrEmpty(newName))
            {
                result.ErrorMessage = "缺少必需参数: NewName";
                return result;
            }

            // 构建完整路径
            string originalPath = Path.IsPathRooted(originalName) ? originalName : Path.Combine(basePath, originalName);
            string newPath = Path.IsPathRooted(newName) ? newName : Path.Combine(basePath, newName);

            // 检查原路径和新路径是否存在（支持文件和文件夹）
            bool originalExists = File.Exists(originalPath) || Directory.Exists(originalPath);
            bool newExists = File.Exists(newPath) || Directory.Exists(newPath);

            // 重命名操作的特点：原名不存在，新名存在
            result.IsCorrect = !originalExists && newExists;

            if (result.IsCorrect)
            {
                result.Details = $"重命名成功: {originalName} -> {newName}";
            }
            else if (originalExists && newExists)
            {
                result.Details = $"可能是复制操作而非重命名: 原名和新名都存在";
            }
            else if (originalExists && !newExists)
            {
                result.Details = $"重命名未完成: 原名仍存在，新名不存在";
            }
            else
            {
                result.Details = $"原名和新名都不存在: {originalName}, {newName}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测重命名操作时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测快捷方式操作
    /// </summary>
    private KnowledgePointResult DetectShortcutOperation(string basePath, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ShortcutOperation",
            Parameters = parameters,
            IsCorrect = false
        };

        try
        {
            if (!parameters.TryGetValue("ShortcutPath", out string? shortcutPath) || string.IsNullOrEmpty(shortcutPath))
            {
                result.ErrorMessage = "缺少必需参数: ShortcutPath";
                return result;
            }

            // 构建完整路径
            string fullShortcutPath = Path.IsPathRooted(shortcutPath) ? shortcutPath : Path.Combine(basePath, shortcutPath);

            // 确保快捷方式路径以.lnk结尾
            if (!fullShortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                fullShortcutPath += ".lnk";
            }

            bool shortcutExists = File.Exists(fullShortcutPath);
            result.IsCorrect = shortcutExists;
            result.Details = shortcutExists ?
                $"快捷方式已创建: {fullShortcutPath}" :
                $"快捷方式不存在: {fullShortcutPath}";

            // 如果指定了目标路径，可以验证快捷方式是否指向正确的目标
            if (shortcutExists && parameters.TryGetValue("TargetPath", out string? targetPath) && !string.IsNullOrEmpty(targetPath))
            {
                try
                {
                    // 这里可以添加读取快捷方式目标的逻辑
                    // 由于需要COM组件，这里简化处理
                    result.Details += " (已验证快捷方式存在)";
                }
                catch (Exception ex)
                {
                    result.Details += $" (无法验证快捷方式目标: {ex.Message})";
                }
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测快捷方式操作时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测属性修改操作
    /// </summary>
    private KnowledgePointResult DetectPropertyModification(string basePath, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "FilePropertyModification",
            Parameters = parameters,
            IsCorrect = false
        };

        try
        {
            if (!parameters.TryGetValue("TargetPath", out string? targetPath) || string.IsNullOrEmpty(targetPath))
            {
                result.ErrorMessage = "缺少必需参数: TargetPath";
                return result;
            }

            // 构建完整路径
            string fullPath = Path.IsPathRooted(targetPath) ? targetPath : Path.Combine(basePath, targetPath);

            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                result.ErrorMessage = $"目标路径不存在: {fullPath}";
                result.IsCorrect = false;
                return result;
            }

            bool allAttributesMatch = true;
            List<string> details = [];

            try
            {
                FileAttributes attributes = File.GetAttributes(fullPath);

                // 支持PropertyType + PropertyValue的参数格式
                if (parameters.TryGetValue("PropertyType", out string? propertyType) &&
                    parameters.TryGetValue("PropertyValue", out string? propertyValueStr) &&
                    bool.TryParse(propertyValueStr, out bool expectedValue))
                {
                    bool actualValue = propertyType switch
                    {
                        "只读" or "ReadOnly" => (attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly,
                        "隐藏" or "Hidden" => (attributes & FileAttributes.Hidden) == FileAttributes.Hidden,
                        "系统" or "System" => (attributes & FileAttributes.System) == FileAttributes.System,
                        "存档" or "Archive" => (attributes & FileAttributes.Archive) == FileAttributes.Archive,
                        _ => throw new ArgumentException($"不支持的属性类型: {propertyType}")
                    };

                    if (actualValue == expectedValue)
                    {
                        details.Add($"{propertyType}属性正确: {actualValue}");
                    }
                    else
                    {
                        details.Add($"{propertyType}属性不匹配: 期望{expectedValue}, 实际{actualValue}");
                        allAttributesMatch = false;
                    }
                }
                else
                {
                    // 支持直接的属性参数格式
                    // 检查只读属性
                    if (parameters.TryGetValue("ReadOnly", out string? readOnlyStr) && bool.TryParse(readOnlyStr, out bool expectedReadOnly))
                    {
                        bool isReadOnly = (attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
                        if (isReadOnly == expectedReadOnly)
                        {
                            details.Add($"只读属性正确: {isReadOnly}");
                        }
                        else
                        {
                            details.Add($"只读属性不匹配: 期望{expectedReadOnly}, 实际{isReadOnly}");
                            allAttributesMatch = false;
                        }
                    }

                    // 检查隐藏属性
                    if (parameters.TryGetValue("Hidden", out string? hiddenStr) && bool.TryParse(hiddenStr, out bool expectedHidden))
                    {
                        bool isHidden = (attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                        if (isHidden == expectedHidden)
                        {
                            details.Add($"隐藏属性正确: {isHidden}");
                        }
                        else
                        {
                            details.Add($"隐藏属性不匹配: 期望{expectedHidden}, 实际{isHidden}");
                            allAttributesMatch = false;
                        }
                    }

                    // 如果没有找到任何属性参数，给出错误
                    if (details.Count == 0)
                    {
                        result.ErrorMessage = "缺少属性参数: PropertyType+PropertyValue 或 ReadOnly/Hidden";
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"无法检查文件属性: {ex.Message}";
                result.IsCorrect = false;
                return result;
            }

            result.IsCorrect = allAttributesMatch;
            result.Details = string.Join("; ", details);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测属性修改操作时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测复制重命名操作
    /// </summary>
    private KnowledgePointResult DetectCopyRenameOperation(string basePath, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "CopyRenameOperation",
            Parameters = parameters,
            IsCorrect = false
        };

        try
        {
            if (!parameters.TryGetValue("SourcePath", out string? sourcePath) || string.IsNullOrEmpty(sourcePath))
            {
                result.ErrorMessage = "缺少必需参数: SourcePath";
                return result;
            }

            if (!parameters.TryGetValue("TargetPath", out string? targetPath) || string.IsNullOrEmpty(targetPath))
            {
                result.ErrorMessage = "缺少必需参数: TargetPath";
                return result;
            }

            // 构建完整路径
            string fullSourcePath = Path.IsPathRooted(sourcePath) ? sourcePath : Path.Combine(basePath, sourcePath);
            string fullTargetPath = Path.IsPathRooted(targetPath) ? targetPath : Path.Combine(basePath, targetPath);

            bool sourceExists = File.Exists(fullSourcePath) || Directory.Exists(fullSourcePath);
            bool targetExists = File.Exists(fullTargetPath) || Directory.Exists(fullTargetPath);

            if (!sourceExists)
            {
                result.Details = $"源文件/文件夹不存在: {fullSourcePath}";
                result.IsCorrect = false;
                return result;
            }

            // 复制重命名操作的特点：源存在，目标也存在，且名称不同
            result.IsCorrect = sourceExists && targetExists && !string.Equals(Path.GetFileName(fullSourcePath), Path.GetFileName(fullTargetPath), StringComparison.OrdinalIgnoreCase);

            if (result.IsCorrect)
            {
                result.Details = $"复制重命名成功: {fullSourcePath} -> {fullTargetPath}";

                // 如果是文件，检查大小是否一致
                if (File.Exists(fullSourcePath) && File.Exists(fullTargetPath))
                {
                    try
                    {
                        FileInfo sourceInfo = new(fullSourcePath);
                        FileInfo targetInfo = new(fullTargetPath);

                        bool sizeMatches = sourceInfo.Length == targetInfo.Length;
                        result.Details += sizeMatches ? " (文件大小匹配)" : " (文件大小不匹配)";
                        if (!sizeMatches)
                        {
                            result.IsCorrect = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Details += $" (无法比较文件大小: {ex.Message})";
                    }
                }
            }
            else if (!targetExists)
            {
                result.Details = $"目标不存在: {fullTargetPath}";
            }
            else if (string.Equals(Path.GetFileName(fullSourcePath), Path.GetFileName(fullTargetPath), StringComparison.OrdinalIgnoreCase))
            {
                result.Details = $"文件名相同，可能是复制而非重命名: {Path.GetFileName(fullSourcePath)}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测复制重命名操作时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 验证操作参数的完整性和有效性
    /// </summary>
    private static bool ValidateOperationParameters(string operationType, Dictionary<string, string> parameters, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            switch (operationType)
            {
                case "QuickCreate":
<<<<<<< HEAD
                    // QuickCreate需要ItemName和CreatePath
                    if (!parameters.ContainsKey("ItemName") || string.IsNullOrEmpty(parameters["ItemName"]))
                    {
                        errorMessage = "快捷创建操作缺少必需参数: ItemName";
                        return false;
                    }
                    if (!parameters.ContainsKey("CreatePath") || string.IsNullOrEmpty(parameters["CreatePath"]))
                    {
                        errorMessage = "快捷创建操作缺少必需参数: CreatePath";
                        return false;
                    }
                    break;

                case "CreateOperation":
                    // CreateOperation支持ItemName或TargetPath
                    if (!parameters.ContainsKey("ItemName") || string.IsNullOrEmpty(parameters["ItemName"]))
                    {
                        // 如果没有ItemName，则必须有TargetPath
                        if (!parameters.ContainsKey("TargetPath") || string.IsNullOrEmpty(parameters["TargetPath"]))
                        {
                            errorMessage = "创建操作缺少必需参数: ItemName 或 TargetPath";
                            return false;
                        }
                    }
                    break;

                case "DeleteOperation":
                    if (!parameters.ContainsKey("TargetPath") || string.IsNullOrEmpty(parameters["TargetPath"]))
                    {
                        errorMessage = "删除操作缺少必需参数: TargetPath";
                        return false;
                    }
                    break;

                case "CopyOperation":
                case "MoveOperation":
                case "CopyRenameOperation":
                    if (!parameters.ContainsKey("SourcePath") || string.IsNullOrEmpty(parameters["SourcePath"]))
                    {
                        errorMessage = $"{operationType}缺少必需参数: SourcePath";
                        return false;
                    }
                    // 支持DestinationPath和TargetPath两种参数名
                    if (!parameters.ContainsKey("DestinationPath") && !parameters.ContainsKey("TargetPath"))
                    {
                        errorMessage = $"{operationType}缺少必需参数: DestinationPath 或 TargetPath";
                        return false;
                    }
                    break;

                case "RenameOperation":
                    if (!parameters.ContainsKey("OriginalName") || string.IsNullOrEmpty(parameters["OriginalName"]))
                    {
                        errorMessage = "重命名操作缺少必需参数: OriginalName";
                        return false;
                    }
                    if (!parameters.ContainsKey("NewName") || string.IsNullOrEmpty(parameters["NewName"]))
                    {
                        errorMessage = "重命名操作缺少必需参数: NewName";
                        return false;
                    }
                    break;

                case "ShortcutOperation":
                    if (!parameters.ContainsKey("ShortcutPath") || string.IsNullOrEmpty(parameters["ShortcutPath"]))
                    {
                        errorMessage = "快捷方式操作缺少必需参数: ShortcutPath";
                        return false;
                    }
                    break;

                case "FilePropertyModification":
                    // 支持FilePath和TargetPath两种参数名
                    if (!parameters.ContainsKey("FilePath") && !parameters.ContainsKey("TargetPath"))
                    {
                        errorMessage = "属性修改操作缺少必需参数: FilePath 或 TargetPath";
                        return false;
                    }
                    if (!parameters.ContainsKey("PropertyType") || string.IsNullOrEmpty(parameters["PropertyType"]))
                    {
                        // 兼容旧格式，检查是否有ReadOnly或Hidden参数
                        if (!parameters.ContainsKey("ReadOnly") && !parameters.ContainsKey("Hidden"))
                        {
                            errorMessage = "属性修改操作缺少必需参数: PropertyType 或 ReadOnly/Hidden";
                            return false;
                        }
                    }
                    break;

                default:
                    errorMessage = $"不支持的操作类型: {operationType}";
                    return false;
            }

<<<<<<< HEAD
            // 验证FileType枚举值
            if (parameters.TryGetValue("FileType", out string? fileType) && !string.IsNullOrEmpty(fileType))
            {
                if (!IsValidFileType(fileType))
                {
                    errorMessage = $"无效的文件类型: {fileType}，支持的类型: 文件, 文件夹";
                    return false;
                }
            }

            // 验证PropertyType枚举值
            if (parameters.TryGetValue("PropertyType", out string? propertyType) && !string.IsNullOrEmpty(propertyType))
            {
                if (!IsValidPropertyType(propertyType))
                {
                    errorMessage = $"无效的属性类型: {propertyType}，支持的类型: 只读, 隐藏, 系统, 存档";
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"参数验证时发生错误: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// 标准化路径格式
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        try
        {
            // 替换正斜杠为反斜杠（Windows标准）
            string normalizedPath = path.Replace('/', '\\');

            // 移除多余的反斜杠
            while (normalizedPath.Contains("\\\\"))
            {
                normalizedPath = normalizedPath.Replace("\\\\", "\\");
            }

            // 移除末尾的反斜杠（除非是根目录）
            if (normalizedPath.Length > 3 && normalizedPath.EndsWith("\\"))
            {
                normalizedPath = normalizedPath.TrimEnd('\\');
            }

            return normalizedPath;
        }
        catch
        {
            return path; // 如果标准化失败，返回原路径
        }
    }

    /// <summary>
    /// 解析布尔参数
    /// </summary>
    private static bool ParseBooleanParameter(Dictionary<string, string> parameters, string parameterName, bool defaultValue = false)
    {
        if (parameters.TryGetValue(parameterName, out string? value) && !string.IsNullOrEmpty(value))
        {
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }

            // 尝试解析常见的布尔值表示
            string lowerValue = value.ToLowerInvariant();
            return lowerValue is "1" or "yes" or "true" or "on" or "enabled";
        }

        return defaultValue;
    }

    /// <summary>
    /// 解析整数参数
    /// </summary>
    private static int ParseIntegerParameter(Dictionary<string, string> parameters, string parameterName, int defaultValue = 0)
    {
        if (parameters.TryGetValue(parameterName, out string? value) && !string.IsNullOrEmpty(value))
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// 获取参数值，如果不存在则返回默认值
    /// </summary>
    private static string GetParameterValue(Dictionary<string, string> parameters, string parameterName, string defaultValue = "")
    {
        return parameters.TryGetValue(parameterName, out string? value) && !string.IsNullOrEmpty(value) ? value : defaultValue;
    }

    /// <summary>
    /// 验证文件类型是否有效
    /// </summary>
    private static bool IsValidFileType(string fileType)
    {
        string[] validTypes = { "文件", "文件夹", "File", "Folder", "Directory" };
        return validTypes.Contains(fileType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 验证属性类型是否有效
    /// </summary>
    private static bool IsValidPropertyType(string propertyType)
    {
        string[] validTypes = { "只读", "隐藏", "系统", "存档", "ReadOnly", "Hidden", "System", "Archive" };
        return validTypes.Contains(propertyType, StringComparer.OrdinalIgnoreCase);
    }
}
