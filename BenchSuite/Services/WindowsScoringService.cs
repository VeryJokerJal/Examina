using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Win32;
using BenchSuite.Interfaces;
using BenchSuite.Models;

namespace BenchSuite.Services;

/// <summary>
/// Windows打分服务实现
/// </summary>
public class WindowsScoringService : IWindowsScoringService
{
    private readonly ScoringConfiguration _defaultConfiguration;

    /// <summary>
    /// 基础路径，用于解析相对路径
    /// </summary>
    private string? _basePath;

    public WindowsScoringService()
    {
        _defaultConfiguration = new ScoringConfiguration();
    }

    /// <summary>
    /// 设置基础路径，用于解析相对路径
    /// </summary>
    /// <param name="basePath">基础路径</param>
    public void SetBasePath(string? basePath)
    {
        _basePath = basePath;
    }

    /// <summary>
    /// 对Windows系统进行打分
    /// </summary>
    public async Task<ScoringResult> ScoreFileAsync(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        return await Task.Run(() => ScoreFile(filePath, examModel, configuration));
    }

    /// <summary>
    /// 对单个题目进行评分
    /// </summary>
    public async Task<ScoringResult> ScoreQuestionAsync(string filePath, QuestionModel question, ScoringConfiguration? configuration = null)
    {
        return await Task.Run(() => ScoreQuestion(filePath, question, configuration));
    }

    /// <summary>
    /// 对Windows系统进行打分（同步版本）
    /// </summary>
    public ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        ScoringConfiguration config = configuration ?? _defaultConfiguration;
        ScoringResult result = new()
        {
            StartTime = DateTime.Now,
            IsSuccess = false
        };

        try
        {
            // 查找Windows模块
            ExamModuleModel? windowsModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.Windows);
            if (windowsModule == null)
            {
                result.ErrorMessage = "试卷中没有找到Windows模块";
                return result;
            }

            // 收集所有操作点
            List<OperationPointModel> allOperationPoints = [];
            Dictionary<string, string> operationPointToQuestionMap = [];

            foreach (QuestionModel question in windowsModule.Questions)
            {
                List<OperationPointModel> windowsOperationPoints = [.. question.OperationPoints.Where(op => op.ModuleType == ModuleType.Windows && op.IsEnabled)];

                allOperationPoints.AddRange(windowsOperationPoints);

                // 建立操作点到题目的映射
                foreach (OperationPointModel operationPoint in windowsOperationPoints)
                {
                    operationPointToQuestionMap[operationPoint.Id] = question.Id;
                }
            }

            if (allOperationPoints.Count == 0)
            {
                result.ErrorMessage = "Windows模块没有包含任何操作点";
                result.IsSuccess = false;
                return result;
            }

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(allOperationPoints).Result;

            // 为每个知识点结果设置题目关联信息
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                if (operationPointToQuestionMap.TryGetValue(kpResult.KnowledgePointId, out string? questionId))
                {
                    kpResult.QuestionId = questionId;
                }
            }

            // 计算总分和获得分数
            result.TotalScore = allOperationPoints.Sum(op => op.Score);
            result.AchievedScore = result.KnowledgePointResults.Sum(kpr => kpr.AchievedScore);

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"打分过程中发生错误: {ex.Message}";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 对单个题目进行评分（同步版本）
    /// </summary>
    public ScoringResult ScoreQuestion(string filePath, QuestionModel question, ScoringConfiguration? configuration = null)
    {
        ScoringConfiguration config = configuration ?? _defaultConfiguration;
        ScoringResult result = new()
        {
            StartTime = DateTime.Now,
            IsSuccess = false
        };

        try
        {
            // 获取题目的操作点（只处理Windows相关的操作点）
            List<OperationPointModel> windowsOperationPoints = [.. question.OperationPoints.Where(op => op.ModuleType == ModuleType.Windows && op.IsEnabled)];

            if (windowsOperationPoints.Count == 0)
            {
                result.ErrorMessage = "题目没有包含任何Windows操作点";
                return result;
            }

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(windowsOperationPoints).Result;

            // 为每个知识点结果设置题目ID
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                kpResult.QuestionId = question.Id;
            }

            // 计算总分和获得分数
            result.TotalScore = windowsOperationPoints.Sum(op => op.Score);
            result.AchievedScore = result.KnowledgePointResults.Sum(kpr => kpr.AchievedScore);

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"打分过程中发生错误: {ex.Message}";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 检测Windows系统中的特定知识点
    /// </summary>
    public async Task<KnowledgePointResult> DetectKnowledgePointAsync(string knowledgePointType, Dictionary<string, string> parameters)
    {
        return await Task.Run(() =>
        {
            KnowledgePointResult result = new()
            {
                KnowledgePointType = knowledgePointType,
                Parameters = parameters
            };

            try
            {
                // 根据知识点类型进行检测
                result = DetectSpecificKnowledgePoint(knowledgePointType, parameters);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"检测知识点时发生错误: {ex.Message}";
                result.IsCorrect = false;
            }

            return result;
        });
    }

    /// <summary>
    /// 批量检测Windows系统中的知识点
    /// </summary>
    public async Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(List<OperationPointModel> knowledgePoints)
    {
        return await Task.Run(() =>
        {
            List<KnowledgePointResult> results = [];

            // 创建参数解析上下文
            ParameterResolutionContext context = new("Windows_System");

            foreach (OperationPointModel operationPoint in knowledgePoints)
            {
                try
                {
                    Dictionary<string, string> parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value);

                    // 预处理参数（处理特殊值和路径格式）
                    ResolveParametersForWindows(parameters, context);

                    // 使用解析后的参数
                    Dictionary<string, string> resolvedParameters = GetResolvedParameters(parameters, context);

                    // 根据操作点名称映射到知识点类型
                    string knowledgePointType = MapOperationPointNameToKnowledgeType(operationPoint.Name);

                    KnowledgePointResult result = DetectSpecificKnowledgePoint(knowledgePointType, resolvedParameters);

                    // 设置操作点相关信息
                    result.KnowledgePointId = operationPoint.Id;
                    result.AchievedScore = result.IsCorrect ? operationPoint.Score : 0;

                    results.Add(result);
                }
                catch (Exception ex)
                {
                    KnowledgePointResult errorResult = new()
                    {
                        KnowledgePointId = operationPoint.Id,
                        KnowledgePointType = operationPoint.Name,
                        ErrorMessage = $"检测操作点时发生错误: {ex.Message}",
                        IsCorrect = false,
                        AchievedScore = 0
                    };
                    results.Add(errorResult);
                }
            }

            return results;
        });
    }

    /// <summary>
    /// 验证文件是否存在
    /// </summary>
    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    /// <summary>
    /// 验证文件夹是否存在
    /// </summary>
    public bool DirectoryExists(string folderPath)
    {
        return Directory.Exists(folderPath);
    }

    /// <summary>
    /// 获取文件属性
    /// </summary>
    public FileAttributes? GetFileAttributes(string filePath)
    {
        try
        {
            return File.GetAttributes(filePath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 验证注册表项是否存在
    /// </summary>
    public bool RegistryKeyExists(string rootKey, string keyPath)
    {
        try
        {
            RegistryKey? root = GetRegistryRoot(rootKey);
            if (root == null) return false;

            using RegistryKey? key = root.OpenSubKey(keyPath);
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取注册表值
    /// </summary>
    public object? GetRegistryValue(string rootKey, string keyPath, string valueName)
    {
        try
        {
            RegistryKey? root = GetRegistryRoot(rootKey);
            if (root == null) return null;

            using RegistryKey? key = root.OpenSubKey(keyPath);
            return key?.GetValue(valueName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 验证服务是否存在
    /// </summary>
    public bool ServiceExists(string serviceName)
    {
        try
        {
            ServiceController[] services = ServiceController.GetServices();
            return services.Any(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取服务状态
    /// </summary>
    public string? GetServiceStatus(string serviceName)
    {
        try
        {
            using ServiceController service = new(serviceName);
            return service.Status.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 验证进程是否正在运行
    /// </summary>
    public bool ProcessIsRunning(string processName)
    {
        try
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证网络连通性
    /// </summary>
    public async Task<bool> PingHostAsync(string hostName, int timeout = 5000)
    {
        try
        {
            using Ping ping = new();
            PingReply reply = await ping.SendPingAsync(hostName, timeout);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检测特定的知识点
    /// </summary>
    private KnowledgePointResult DetectSpecificKnowledgePoint(string knowledgePointType, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = knowledgePointType,
            Parameters = parameters,
            IsCorrect = false
        };

        try
        {
            switch (knowledgePointType)
            {
                case "CreateFile":
                    result = DetectCreateFile(parameters);
                    break;
                case "DeleteFile":
                    result = DetectDeleteFile(parameters);
                    break;
                case "CopyFile":
                    result = DetectCopyFile(parameters);
                    break;
                case "MoveFile":
                    result = DetectMoveFile(parameters);
                    break;
                case "RenameFile":
                    result = DetectRenameFile(parameters);
                    break;
                case "CreateFolder":
                    result = DetectCreateFolder(parameters);
                    break;
                case "DeleteFolder":
                    result = DetectDeleteFolder(parameters);
                    break;
                case "CopyFolder":
                    result = DetectCopyFolder(parameters);
                    break;
                case "MoveFolder":
                    result = DetectMoveFolder(parameters);
                    break;
                case "RenameFolder":
                    result = DetectRenameFolder(parameters);
                    break;
                case "SetFileAttributes":
                    result = DetectSetFileAttributes(parameters);
                    break;
                case "WriteTextToFile":
                    result = DetectWriteTextToFile(parameters);
                    break;
                case "AppendTextToFile":
                    result = DetectAppendTextToFile(parameters);
                    break;
                case "CreateShortcut":
                    result = DetectCreateShortcut(parameters);
                    break;
                case "SetEnvironmentVariable":
                    result = DetectSetEnvironmentVariable(parameters);
                    break;
                case "CreateRegistryKey":
                    result = DetectCreateRegistryKey(parameters);
                    break;
                case "SetRegistryValue":
                    result = DetectSetRegistryValue(parameters);
                    break;
                case "DeleteRegistryKey":
                    result = DetectDeleteRegistryKey(parameters);
                    break;
                case "StartService":
                    result = DetectStartService(parameters);
                    break;
                case "StopService":
                    result = DetectStopService(parameters);
                    break;
                case "StartProcess":
                    result = DetectStartProcess(parameters);
                    break;
                case "KillProcess":
                    result = DetectKillProcess(parameters);
                    break;
                case "PingHost":
                    result = DetectPingHost(parameters);
                    break;
                case "DownloadFile":
                    result = DetectDownloadFile(parameters);
                    break;
                case "CreateZipArchive":
                    result = DetectCreateZipArchive(parameters);
                    break;
                case "ExtractZipArchive":
                    result = DetectExtractZipArchive(parameters);
                    break;
                case "CopyAndRename":
                    result = DetectCopyAndRename(parameters);
                    break;
                case "QuickCreate":
                    result = DetectQuickCreate(parameters);
                    break;
                default:
                    result.ErrorMessage = $"未知的知识点类型: {knowledgePointType}";
                    break;
            }

            result.KnowledgePointType = knowledgePointType;
            result.Parameters = parameters;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测知识点 {knowledgePointType} 时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测文件创建
    /// </summary>
    private KnowledgePointResult DetectCreateFile(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("FilePath", out string? filePath) || string.IsNullOrEmpty(filePath))
        {
            result.ErrorMessage = "缺少文件路径参数";
            return result;
        }

        // 路径已在参数预处理阶段标准化，直接使用
        result.IsCorrect = FileExists(filePath);
        result.Details = result.IsCorrect ? $"文件 {filePath} 已创建" : $"文件 {filePath} 不存在";

        return result;
    }

    /// <summary>
    /// 检测文件删除
    /// </summary>
    private KnowledgePointResult DetectDeleteFile(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        // 支持多种参数名称：FilePath（BS标准）和TargetPath（EL导出）
        string? filePath = null;
        if (parameters.TryGetValue("FilePath", out filePath) && !string.IsNullOrEmpty(filePath))
        {
            // 使用BS标准参数名
        }
        else if (parameters.TryGetValue("TargetPath", out filePath) && !string.IsNullOrEmpty(filePath))
        {
            // 使用EL导出参数名
        }
        else
        {
            result.ErrorMessage = "缺少文件路径参数（FilePath或TargetPath）";
            return result;
        }

        // 路径已在参数预处理阶段标准化，直接使用
        result.IsCorrect = !FileExists(filePath);
        result.Details = result.IsCorrect ? $"文件 {filePath} 已删除" : $"文件 {filePath} 仍然存在";

        return result;
    }

    /// <summary>
    /// 检测文件复制
    /// </summary>
    private KnowledgePointResult DetectCopyFile(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("DestinationPath", out string? destinationPath) || string.IsNullOrEmpty(destinationPath))
        {
            result.ErrorMessage = "缺少目标文件路径参数";
            return result;
        }

        // 路径已在参数预处理阶段标准化，直接使用
        result.IsCorrect = FileExists(destinationPath);
        result.Details = result.IsCorrect ? $"文件已复制到 {destinationPath}" : $"目标文件 {destinationPath} 不存在";

        return result;
    }

    /// <summary>
    /// 检测文件移动
    /// </summary>
    private KnowledgePointResult DetectMoveFile(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("SourcePath", out string? sourcePath) || string.IsNullOrEmpty(sourcePath))
        {
            result.ErrorMessage = "缺少源文件路径参数";
            return result;
        }

        if (!parameters.TryGetValue("DestinationPath", out string? destinationPath) || string.IsNullOrEmpty(destinationPath))
        {
            result.ErrorMessage = "缺少目标文件路径参数";
            return result;
        }

        bool sourceExists = FileExists(sourcePath);
        bool destinationExists = FileExists(destinationPath);

        result.IsCorrect = !sourceExists && destinationExists;
        result.Details = result.IsCorrect ? $"文件已从 {sourcePath} 移动到 {destinationPath}" :
            $"移动操作未完成 - 源文件存在: {sourceExists}, 目标文件存在: {destinationExists}";

        return result;
    }

    /// <summary>
    /// 检测文件重命名
    /// </summary>
    private KnowledgePointResult DetectRenameFile(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("FilePath", out string? filePath) || string.IsNullOrEmpty(filePath))
        {
            result.ErrorMessage = "缺少文件路径参数";
            return result;
        }

        if (!parameters.TryGetValue("NewName", out string? newName) || string.IsNullOrEmpty(newName))
        {
            result.ErrorMessage = "缺少新文件名参数";
            return result;
        }

        string directory = Path.GetDirectoryName(filePath) ?? "";
        string newFilePath = Path.Combine(directory, newName);

        bool originalExists = FileExists(filePath);
        bool newExists = FileExists(newFilePath);

        result.IsCorrect = !originalExists && newExists;
        result.Details = result.IsCorrect ? $"文件已重命名为 {newName}" :
            $"重命名操作未完成 - 原文件存在: {originalExists}, 新文件存在: {newExists}";

        return result;
    }

    /// <summary>
    /// 检测复制重命名操作
    /// </summary>
    private KnowledgePointResult DetectCopyAndRename(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("SourcePath", out string? sourcePath) || string.IsNullOrEmpty(sourcePath))
        {
            result.ErrorMessage = "缺少源路径参数";
            return result;
        }

        if (!parameters.TryGetValue("DestinationPath", out string? destinationPath) || string.IsNullOrEmpty(destinationPath))
        {
            result.ErrorMessage = "缺少目标路径参数";
            return result;
        }

        if (!parameters.TryGetValue("NewName", out string? newName) || string.IsNullOrEmpty(newName))
        {
            result.ErrorMessage = "缺少新名称参数";
            return result;
        }

        // 路径已在参数预处理阶段标准化，直接使用
        // 构建最终的目标文件路径（目标路径 + 新名称）
        string finalDestinationPath = Path.Combine(destinationPath, newName);

        bool sourceExists = FileExists(sourcePath) || DirectoryExists(sourcePath);
        bool finalDestinationExists = FileExists(finalDestinationPath) || DirectoryExists(finalDestinationPath);

        result.IsCorrect = sourceExists && finalDestinationExists;
        result.Details = result.IsCorrect ?
            $"文件/文件夹已从 {sourcePath} 复制到 {finalDestinationPath}" :
            $"复制重命名操作未完成 - 源存在: {sourceExists}, 目标存在: {finalDestinationExists}";

        return result;
    }

    /// <summary>
    /// 检测快捷创建操作
    /// </summary>
    private KnowledgePointResult DetectQuickCreate(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("TargetPath", out string? targetPath) || string.IsNullOrEmpty(targetPath))
        {
            result.ErrorMessage = "缺少目标路径参数";
            return result;
        }

        if (!parameters.TryGetValue("ItemType", out string? itemType) || string.IsNullOrEmpty(itemType))
        {
            result.ErrorMessage = "缺少项目类型参数";
            return result;
        }

        // 路径已在参数预处理阶段标准化，直接使用
        // 根据项目类型检测相应的创建操作
        switch (itemType)
        {
            case "文件":
                result.IsCorrect = FileExists(targetPath);
                result.Details = result.IsCorrect ? $"文件 {targetPath} 已快捷创建" : $"文件 {targetPath} 不存在";
                break;
            case "文件夹":
                result.IsCorrect = DirectoryExists(targetPath);
                result.Details = result.IsCorrect ? $"文件夹 {targetPath} 已快捷创建" : $"文件夹 {targetPath} 不存在";
                break;
            default:
                result.ErrorMessage = $"不支持的项目类型: {itemType}，支持的类型为：文件、文件夹";
                break;
        }

        return result;
    }

    /// <summary>
    /// 检测文件夹创建
    /// </summary>
    private KnowledgePointResult DetectCreateFolder(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("FolderPath", out string? folderPath) || string.IsNullOrEmpty(folderPath))
        {
            result.ErrorMessage = "缺少文件夹路径参数";
            return result;
        }

        result.IsCorrect = DirectoryExists(folderPath);
        result.Details = result.IsCorrect ? $"文件夹 {folderPath} 已创建" : $"文件夹 {folderPath} 不存在";

        return result;
    }

    /// <summary>
    /// 检测文件夹删除
    /// </summary>
    private KnowledgePointResult DetectDeleteFolder(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("FolderPath", out string? folderPath) || string.IsNullOrEmpty(folderPath))
        {
            result.ErrorMessage = "缺少文件夹路径参数";
            return result;
        }

        result.IsCorrect = !DirectoryExists(folderPath);
        result.Details = result.IsCorrect ? $"文件夹 {folderPath} 已删除" : $"文件夹 {folderPath} 仍然存在";

        return result;
    }

    /// <summary>
    /// 检测文件夹复制
    /// </summary>
    private KnowledgePointResult DetectCopyFolder(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("DestinationPath", out string? destinationPath) || string.IsNullOrEmpty(destinationPath))
        {
            result.ErrorMessage = "缺少目标文件夹路径参数";
            return result;
        }

        result.IsCorrect = DirectoryExists(destinationPath);
        result.Details = result.IsCorrect ? $"文件夹已复制到 {destinationPath}" : $"目标文件夹 {destinationPath} 不存在";

        return result;
    }

    /// <summary>
    /// 检测文件夹移动
    /// </summary>
    private KnowledgePointResult DetectMoveFolder(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("SourcePath", out string? sourcePath) || string.IsNullOrEmpty(sourcePath))
        {
            result.ErrorMessage = "缺少源文件夹路径参数";
            return result;
        }

        if (!parameters.TryGetValue("DestinationPath", out string? destinationPath) || string.IsNullOrEmpty(destinationPath))
        {
            result.ErrorMessage = "缺少目标文件夹路径参数";
            return result;
        }

        bool sourceExists = DirectoryExists(sourcePath);
        bool destinationExists = DirectoryExists(destinationPath);

        result.IsCorrect = !sourceExists && destinationExists;
        result.Details = result.IsCorrect ? $"文件夹已从 {sourcePath} 移动到 {destinationPath}" :
            $"移动操作未完成 - 源文件夹存在: {sourceExists}, 目标文件夹存在: {destinationExists}";

        return result;
    }

    /// <summary>
    /// 检测文件夹重命名
    /// </summary>
    private KnowledgePointResult DetectRenameFolder(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("FolderPath", out string? folderPath) || string.IsNullOrEmpty(folderPath))
        {
            result.ErrorMessage = "缺少文件夹路径参数";
            return result;
        }

        if (!parameters.TryGetValue("NewName", out string? newName) || string.IsNullOrEmpty(newName))
        {
            result.ErrorMessage = "缺少新文件夹名参数";
            return result;
        }

        string? parentDirectory = Path.GetDirectoryName(folderPath);
        if (parentDirectory == null)
        {
            result.ErrorMessage = "无法获取父目录";
            return result;
        }

        string newFolderPath = Path.Combine(parentDirectory, newName);

        bool originalExists = DirectoryExists(folderPath);
        bool newExists = DirectoryExists(newFolderPath);

        result.IsCorrect = !originalExists && newExists;
        result.Details = result.IsCorrect ? $"文件夹已重命名为 {newName}" :
            $"重命名操作未完成 - 原文件夹存在: {originalExists}, 新文件夹存在: {newExists}";

        return result;
    }

    /// <summary>
    /// 检测文件属性设置
    /// </summary>
    private KnowledgePointResult DetectSetFileAttributes(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("FilePath", out string? filePath) || string.IsNullOrEmpty(filePath))
        {
            result.ErrorMessage = "缺少文件路径参数";
            return result;
        }

        if (!parameters.TryGetValue("Attributes", out string? attributesStr) || string.IsNullOrEmpty(attributesStr))
        {
            result.ErrorMessage = "缺少文件属性参数";
            return result;
        }

        if (!FileExists(filePath))
        {
            result.ErrorMessage = $"文件 {filePath} 不存在";
            return result;
        }

        FileAttributes? currentAttributes = GetFileAttributes(filePath);
        if (currentAttributes == null)
        {
            result.ErrorMessage = "无法获取文件属性";
            return result;
        }

        bool hasExpectedAttribute = attributesStr switch
        {
            "只读" => currentAttributes.Value.HasFlag(FileAttributes.ReadOnly),
            "隐藏" => currentAttributes.Value.HasFlag(FileAttributes.Hidden),
            "系统" => currentAttributes.Value.HasFlag(FileAttributes.System),
            "存档" => currentAttributes.Value.HasFlag(FileAttributes.Archive),
            "正常" => currentAttributes.Value == FileAttributes.Normal,
            _ => false
        };

        result.IsCorrect = hasExpectedAttribute;
        result.Details = result.IsCorrect ? $"文件 {filePath} 具有 {attributesStr} 属性" :
            $"文件 {filePath} 不具有 {attributesStr} 属性，当前属性: {currentAttributes}";

        return result;
    }

    /// <summary>
    /// 检测文本写入文件
    /// </summary>
    private KnowledgePointResult DetectWriteTextToFile(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("FilePath", out string? filePath) || string.IsNullOrEmpty(filePath))
        {
            result.ErrorMessage = "缺少文件路径参数";
            return result;
        }

        if (!parameters.TryGetValue("Content", out string? expectedContent) || string.IsNullOrEmpty(expectedContent))
        {
            result.ErrorMessage = "缺少文件内容参数";
            return result;
        }

        if (!FileExists(filePath))
        {
            result.ErrorMessage = $"文件 {filePath} 不存在";
            return result;
        }

        try
        {
            string actualContent = File.ReadAllText(filePath);
            result.IsCorrect = actualContent.Contains(expectedContent);
            result.Details = result.IsCorrect ? $"文件 {filePath} 包含预期内容" :
                $"文件 {filePath} 不包含预期内容";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"读取文件内容时发生错误: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 检测文本追加到文件
    /// </summary>
    private KnowledgePointResult DetectAppendTextToFile(Dictionary<string, string> parameters)
    {
        // 与写入文本检测逻辑相同，都是检查文件是否包含指定内容
        return DetectWriteTextToFile(parameters);
    }

    /// <summary>
    /// 检测快捷方式创建
    /// </summary>
    private KnowledgePointResult DetectCreateShortcut(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("ShortcutPath", out string? shortcutPath) || string.IsNullOrEmpty(shortcutPath))
        {
            result.ErrorMessage = "缺少快捷方式路径参数";
            return result;
        }

        result.IsCorrect = FileExists(shortcutPath) && Path.GetExtension(shortcutPath).Equals(".lnk", StringComparison.OrdinalIgnoreCase);
        result.Details = result.IsCorrect ? $"快捷方式 {shortcutPath} 已创建" : $"快捷方式 {shortcutPath} 不存在";

        return result;
    }

    /// <summary>
    /// 检测环境变量设置
    /// </summary>
    private KnowledgePointResult DetectSetEnvironmentVariable(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("VariableName", out string? variableName) || string.IsNullOrEmpty(variableName))
        {
            result.ErrorMessage = "缺少环境变量名参数";
            return result;
        }

        if (!parameters.TryGetValue("VariableValue", out string? expectedValue) || string.IsNullOrEmpty(expectedValue))
        {
            result.ErrorMessage = "缺少环境变量值参数";
            return result;
        }

        try
        {
            string? actualValue = Environment.GetEnvironmentVariable(variableName);
            result.IsCorrect = actualValue != null && actualValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase);
            result.Details = result.IsCorrect ? $"环境变量 {variableName} 已设置为 {expectedValue}" :
                $"环境变量 {variableName} 值不匹配，期望: {expectedValue}, 实际: {actualValue ?? "未设置"}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检查环境变量时发生错误: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 检测注册表项创建
    /// </summary>
    private KnowledgePointResult DetectCreateRegistryKey(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("RootKey", out string? rootKey) || string.IsNullOrEmpty(rootKey))
        {
            result.ErrorMessage = "缺少注册表根键参数";
            return result;
        }

        if (!parameters.TryGetValue("KeyPath", out string? keyPath) || string.IsNullOrEmpty(keyPath))
        {
            result.ErrorMessage = "缺少注册表项路径参数";
            return result;
        }

        result.IsCorrect = RegistryKeyExists(rootKey, keyPath);
        result.Details = result.IsCorrect ? $"注册表项 {rootKey}\\{keyPath} 已创建" :
            $"注册表项 {rootKey}\\{keyPath} 不存在";

        return result;
    }

    /// <summary>
    /// 检测注册表值设置
    /// </summary>
    private KnowledgePointResult DetectSetRegistryValue(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("RootKey", out string? rootKey) || string.IsNullOrEmpty(rootKey))
        {
            result.ErrorMessage = "缺少注册表根键参数";
            return result;
        }

        if (!parameters.TryGetValue("KeyPath", out string? keyPath) || string.IsNullOrEmpty(keyPath))
        {
            result.ErrorMessage = "缺少注册表项路径参数";
            return result;
        }

        if (!parameters.TryGetValue("ValueName", out string? valueName) || string.IsNullOrEmpty(valueName))
        {
            result.ErrorMessage = "缺少注册表值名称参数";
            return result;
        }

        if (!parameters.TryGetValue("ValueData", out string? expectedData) || string.IsNullOrEmpty(expectedData))
        {
            result.ErrorMessage = "缺少注册表值数据参数";
            return result;
        }

        object? actualValue = GetRegistryValue(rootKey, keyPath, valueName);
        result.IsCorrect = actualValue != null && actualValue.ToString() == expectedData;
        result.Details = result.IsCorrect ? $"注册表值 {rootKey}\\{keyPath}\\{valueName} 已设置为 {expectedData}" :
            $"注册表值 {rootKey}\\{keyPath}\\{valueName} 值不匹配，期望: {expectedData}, 实际: {actualValue?.ToString() ?? "未设置"}";

        return result;
    }

    /// <summary>
    /// 检测注册表项删除
    /// </summary>
    private KnowledgePointResult DetectDeleteRegistryKey(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("RootKey", out string? rootKey) || string.IsNullOrEmpty(rootKey))
        {
            result.ErrorMessage = "缺少注册表根键参数";
            return result;
        }

        if (!parameters.TryGetValue("KeyPath", out string? keyPath) || string.IsNullOrEmpty(keyPath))
        {
            result.ErrorMessage = "缺少注册表项路径参数";
            return result;
        }

        result.IsCorrect = !RegistryKeyExists(rootKey, keyPath);
        result.Details = result.IsCorrect ? $"注册表项 {rootKey}\\{keyPath} 已删除" :
            $"注册表项 {rootKey}\\{keyPath} 仍然存在";

        return result;
    }

    /// <summary>
    /// 检测服务启动
    /// </summary>
    private KnowledgePointResult DetectStartService(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("ServiceName", out string? serviceName) || string.IsNullOrEmpty(serviceName))
        {
            result.ErrorMessage = "缺少服务名称参数";
            return result;
        }

        if (!ServiceExists(serviceName))
        {
            result.ErrorMessage = $"服务 {serviceName} 不存在";
            return result;
        }

        string? status = GetServiceStatus(serviceName);
        result.IsCorrect = status == "Running";
        result.Details = result.IsCorrect ? $"服务 {serviceName} 正在运行" :
            $"服务 {serviceName} 状态: {status ?? "未知"}";

        return result;
    }

    /// <summary>
    /// 检测服务停止
    /// </summary>
    private KnowledgePointResult DetectStopService(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("ServiceName", out string? serviceName) || string.IsNullOrEmpty(serviceName))
        {
            result.ErrorMessage = "缺少服务名称参数";
            return result;
        }

        if (!ServiceExists(serviceName))
        {
            result.ErrorMessage = $"服务 {serviceName} 不存在";
            return result;
        }

        string? status = GetServiceStatus(serviceName);
        result.IsCorrect = status == "Stopped";
        result.Details = result.IsCorrect ? $"服务 {serviceName} 已停止" :
            $"服务 {serviceName} 状态: {status ?? "未知"}";

        return result;
    }

    /// <summary>
    /// 检测进程启动
    /// </summary>
    private KnowledgePointResult DetectStartProcess(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("ProcessPath", out string? processPath) || string.IsNullOrEmpty(processPath))
        {
            result.ErrorMessage = "缺少程序路径参数";
            return result;
        }

        string processName = Path.GetFileNameWithoutExtension(processPath);
        result.IsCorrect = ProcessIsRunning(processName);
        result.Details = result.IsCorrect ? $"进程 {processName} 正在运行" :
            $"进程 {processName} 未运行";

        return result;
    }

    /// <summary>
    /// 检测进程终止
    /// </summary>
    private KnowledgePointResult DetectKillProcess(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("ProcessName", out string? processName) || string.IsNullOrEmpty(processName))
        {
            result.ErrorMessage = "缺少进程名称参数";
            return result;
        }

        result.IsCorrect = !ProcessIsRunning(processName);
        result.Details = result.IsCorrect ? $"进程 {processName} 已终止" :
            $"进程 {processName} 仍在运行";

        return result;
    }

    /// <summary>
    /// 检测Ping主机
    /// </summary>
    private KnowledgePointResult DetectPingHost(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("HostName", out string? hostName) || string.IsNullOrEmpty(hostName))
        {
            result.ErrorMessage = "缺少主机名参数";
            return result;
        }

        int timeout = 5000;
        if (parameters.TryGetValue("Timeout", out string? timeoutStr) && int.TryParse(timeoutStr, out int parsedTimeout))
        {
            timeout = parsedTimeout;
        }

        try
        {
            result.IsCorrect = PingHostAsync(hostName, timeout).Result;
            result.Details = result.IsCorrect ? $"主机 {hostName} 连通" :
                $"主机 {hostName} 不可达";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Ping主机时发生错误: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 检测文件下载
    /// </summary>
    private KnowledgePointResult DetectDownloadFile(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("LocalPath", out string? localPath) || string.IsNullOrEmpty(localPath))
        {
            result.ErrorMessage = "缺少本地保存路径参数";
            return result;
        }

        result.IsCorrect = FileExists(localPath);
        result.Details = result.IsCorrect ? $"文件已下载到 {localPath}" :
            $"下载文件 {localPath} 不存在";

        return result;
    }

    /// <summary>
    /// 检测ZIP压缩包创建
    /// </summary>
    private KnowledgePointResult DetectCreateZipArchive(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("ZipPath", out string? zipPath) || string.IsNullOrEmpty(zipPath))
        {
            result.ErrorMessage = "缺少压缩包路径参数";
            return result;
        }

        result.IsCorrect = FileExists(zipPath) && Path.GetExtension(zipPath).Equals(".zip", StringComparison.OrdinalIgnoreCase);
        result.Details = result.IsCorrect ? $"ZIP压缩包 {zipPath} 已创建" :
            $"ZIP压缩包 {zipPath} 不存在";

        return result;
    }

    /// <summary>
    /// 检测ZIP压缩包解压
    /// </summary>
    private KnowledgePointResult DetectExtractZipArchive(Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new() { IsCorrect = false };

        if (!parameters.TryGetValue("ExtractPath", out string? extractPath) || string.IsNullOrEmpty(extractPath))
        {
            result.ErrorMessage = "缺少解压目录参数";
            return result;
        }

        result.IsCorrect = DirectoryExists(extractPath);
        result.Details = result.IsCorrect ? $"文件已解压到 {extractPath}" :
            $"解压目录 {extractPath} 不存在";

        return result;
    }

    /// <summary>
    /// 获取注册表根键
    /// </summary>
    private static RegistryKey? GetRegistryRoot(string rootKey)
    {
        return rootKey.ToUpper() switch
        {
            "HKEY_CURRENT_USER" => Registry.CurrentUser,
            "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
            "HKEY_USERS" => Registry.Users,
            "HKEY_CURRENT_CONFIG" => Registry.CurrentConfig,
            _ => null
        };
    }

    /// <summary>
    /// 根据操作点名称映射到知识点类型
    /// </summary>
    private static string MapOperationPointNameToKnowledgeType(string operationPointName)
    {
        Dictionary<string, string> nameToTypeMapping = new()
        {
            { "创建文件", "CreateFile" },
            { "删除文件", "DeleteFile" },
            { "复制文件", "CopyFile" },
            { "移动文件", "MoveFile" },
            { "重命名文件", "RenameFile" },
            { "创建文件夹", "CreateFolder" },
            { "删除文件夹", "DeleteFolder" },
            { "复制文件夹", "CopyFolder" },
            { "移动文件夹", "MoveFolder" },
            { "重命名文件夹", "RenameFolder" },
            { "设置文件属性", "SetFileAttributes" },
            { "设置文件权限", "SetFilePermissions" },
            { "写入文本到文件", "WriteTextToFile" },
            { "追加文本到文件", "AppendTextToFile" },
            { "创建快捷方式", "CreateShortcut" },
            { "设置环境变量", "SetEnvironmentVariable" },
            { "创建注册表项", "CreateRegistryKey" },
            { "设置注册表值", "SetRegistryValue" },
            { "删除注册表项", "DeleteRegistryKey" },
            { "启动服务", "StartService" },
            { "停止服务", "StopService" },
            { "启动进程", "StartProcess" },
            { "终止进程", "KillProcess" },
            { "Ping主机", "PingHost" },
            { "下载文件", "DownloadFile" },
            { "创建ZIP压缩包", "CreateZipArchive" },
            { "解压ZIP压缩包", "ExtractZipArchive" },
            { "快捷创建", "QuickCreate" },
            // 添加EL导出格式的映射支持
            { "删除操作", "DeleteFile" },
            { "复制重命名操作", "CopyAndRename" }
        };

        // 尝试精确匹配
        if (nameToTypeMapping.TryGetValue(operationPointName, out string? exactMatch))
        {
            return exactMatch;
        }

        // 如果没有精确匹配，尝试部分匹配
        foreach (KeyValuePair<string, string> mapping in nameToTypeMapping)
        {
            if (operationPointName.Contains(mapping.Key))
            {
                return mapping.Value;
            }
        }

        // 如果都没有匹配，返回原始名称
        return operationPointName;
    }

    /// <summary>
    /// 为Windows操作解析参数
    /// </summary>
    private void ResolveParametersForWindows(Dictionary<string, string> parameters, ParameterResolutionContext context)
    {
        foreach (KeyValuePair<string, string> parameter in parameters)
        {
            // 处理路径参数的格式转换
            if (IsPathParameter(parameter.Key))
            {
                string normalizedPath = NormalizePath(parameter.Value);
                if (normalizedPath != parameter.Value)
                {
                    context.SetResolvedParameter(parameter.Key, normalizedPath);
                }
            }

            // 处理特殊参数值（如-1等）
            if (parameter.Value == "-1")
            {
                // 对于Windows操作，-1通常表示默认值或最新值
                string resolvedValue = ResolveMinusOneParameter(parameter.Key, parameter.Value);
                if (resolvedValue != parameter.Value)
                {
                    context.SetResolvedParameter(parameter.Key, resolvedValue);
                }
            }
        }
    }

    /// <summary>
    /// 判断参数是否为路径参数
    /// </summary>
    private static bool IsPathParameter(string parameterName)
    {
        string[] pathParameterNames = [
            "FilePath", "FolderPath", "TargetPath", "SourcePath", "DestinationPath",
            "ShortcutPath", "ProcessPath", "LocalPath", "ZipPath", "ExtractPath"
        ];

        return pathParameterNames.Contains(parameterName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 解析-1参数值
    /// </summary>
    private static string ResolveMinusOneParameter(string parameterName, string parameterValue)
    {
        // 对于Windows操作，-1参数的处理相对简单
        // 大多数情况下保持原值，让具体的检测方法处理
        return parameterName switch
        {
            // 对于某些特定参数，可以提供默认值
            "Timeout" => "5000", // 默认超时5秒
            _ => parameterValue // 其他情况保持原值
        };
    }

    /// <summary>
    /// 获取解析后的参数
    /// </summary>
    private static Dictionary<string, string> GetResolvedParameters(Dictionary<string, string> parameters, ParameterResolutionContext context)
    {
        Dictionary<string, string> resolvedParameters = [];

        foreach (KeyValuePair<string, string> parameter in parameters)
        {
            // 首先检查context中是否有已解析的参数值
            if (context.IsParameterResolved(parameter.Key))
            {
                resolvedParameters[parameter.Key] = context.GetResolvedParameter(parameter.Key);
            }
            else
            {
                // 如果没有解析过，使用原始值
                resolvedParameters[parameter.Key] = parameter.Value;
            }
        }

        return resolvedParameters;
    }

    /// <summary>
    /// 验证是否可以处理指定的文件类型
    /// </summary>
    public bool CanProcessFile(string filePath)
    {
        // Windows打分服务不依赖特定文件，可以处理任何路径
        return true;
    }

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    public IEnumerable<string> GetSupportedExtensions()
    {
        // Windows打分服务不依赖特定文件扩展名，返回空列表
        return [];
    }

    /// <summary>
    /// 标准化路径格式，处理EL导出的路径格式兼容性
    /// </summary>
    /// <param name="path">原始路径</param>
    /// <returns>标准化后的路径</returns>
    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        // 标准化路径分隔符
        path = path.Replace('/', '\\');

        // 处理EL导出的路径格式：\WINDOWS\calc.exe
        if (path.StartsWith("\\") && !path.StartsWith("\\\\"))
        {
            // 如果设置了基础路径，使用基础路径组合
            if (!string.IsNullOrEmpty(_basePath))
            {
                // 移除开头的反斜杠，然后与基础路径组合
                string relativePath = path.TrimStart('\\');
                path = Path.Combine(_basePath, relativePath);
            }
            else
            {
                // 如果没有设置基础路径，使用默认的C:前缀
                path = "C:" + path;
            }
        }

        // 处理其他相对路径
        if (!Path.IsPathRooted(path))
        {
            if (!string.IsNullOrEmpty(_basePath))
            {
                // 使用基础路径组合相对路径
                path = Path.Combine(_basePath, path);
            }
            else
            {
                // 如果没有设置基础路径，转换为绝对路径
                path = Path.GetFullPath(path);
            }
        }

        return path;
    }
}
