using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ExamLab.Models;
using Windows.Storage;

namespace ExamLab.Services;

/// <summary>
/// 数据存储服务
/// </summary>
public class DataStorageService
{
    private static readonly Lazy<DataStorageService> _instance = new(() => new DataStorageService());
    public static DataStorageService Instance => _instance.Value;

    private const string ExamsFileName = "exams.json";
    private const string SettingsFileName = "settings.json";
    private const string BackupFolderName = "Backups";

    private readonly JsonSerializerOptions _jsonOptions;

    private DataStorageService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // 添加自定义转换器
        _jsonOptions.Converters.Add(new Converters.ModuleTypeJsonConverter());
        _jsonOptions.Converters.Add(new Converters.CSharpQuestionTypeJsonConverter());
    }

    /// <summary>
    /// 获取存储文件夹，兼容打包和非打包应用
    /// </summary>
    private async Task<StorageFolder> GetStorageFolderAsync()
    {
        try
        {
            // 优先尝试使用ApplicationData.Current（适用于打包应用）
            if (ApplicationData.Current?.RoamingFolder != null)
            {
                return ApplicationData.Current.RoamingFolder;
            }
        }
        catch
        {
            // 如果ApplicationData.Current不可用，继续使用备选方案
        }

        // 备选方案：使用传统文件系统路径（适用于非打包应用）
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolderPath = Path.Combine(appDataPath, "ExamLab");

        // 确保目录存在
        _ = Directory.CreateDirectory(appFolderPath);

        // 返回StorageFolder
        return await StorageFolder.GetFolderFromPathAsync(appFolderPath);
    }

    /// <summary>
    /// 保存所有试卷
    /// </summary>
    public async Task SaveExamsAsync(IEnumerable<Exam> exams)
    {
        try
        {
            StorageFolder localFolder = await GetStorageFolderAsync();
            StorageFile file = await localFolder.CreateFileAsync(ExamsFileName, CreationCollisionOption.ReplaceExisting);

            List<Exam> examList = exams.ToList();
            string json = JsonSerializer.Serialize(examList, _jsonOptions);

            await FileIO.WriteTextAsync(file, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"保存试卷数据失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 加载所有试卷
    /// </summary>
    public async Task<List<Exam>> LoadExamsAsync()
    {
        try
        {
            StorageFolder localFolder = await GetStorageFolderAsync();

            if (await localFolder.TryGetItemAsync(ExamsFileName) is not StorageFile file)
            {
                return [];
            }

            string json = await FileIO.ReadTextAsync(file);

            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            List<Exam>? exams = JsonSerializer.Deserialize<List<Exam>>(json, _jsonOptions);
            return exams ?? [];
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"加载试卷数据失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 保存单个试卷
    /// </summary>
    public async Task SaveExamAsync(Exam exam)
    {
        try
        {
            List<Exam> exams = await LoadExamsAsync();

            // 查找并更新现有试卷，或添加新试卷
            int existingIndex = exams.FindIndex(e => e.Id == exam.Id);
            if (existingIndex >= 0)
            {
                exams[existingIndex] = exam;
            }
            else
            {
                exams.Add(exam);
            }

            await SaveExamsAsync(exams);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"保存试卷失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 删除试卷
    /// </summary>
    public async Task DeleteExamAsync(string examId)
    {
        try
        {
            List<Exam> exams = await LoadExamsAsync();
            _ = exams.RemoveAll(e => e.Id == examId);
            await SaveExamsAsync(exams);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"删除试卷失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 创建备份
    /// </summary>
    public async Task CreateBackupAsync()
    {
        try
        {
            StorageFolder localFolder = await GetStorageFolderAsync();
            StorageFolder backupFolder = await localFolder.CreateFolderAsync(BackupFolderName, CreationCollisionOption.OpenIfExists);

            // 创建带时间戳的备份文件名
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFileName = $"exams_backup_{timestamp}.json";

            if (await localFolder.TryGetItemAsync(ExamsFileName) is StorageFile sourceFile)
            {
                _ = await sourceFile.CopyAsync(backupFolder, backupFileName, NameCollisionOption.ReplaceExisting);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"创建备份失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取所有备份文件
    /// </summary>
    public async Task<List<StorageFile>> GetBackupFilesAsync()
    {
        try
        {
            StorageFolder localFolder = await GetStorageFolderAsync();

            if (await localFolder.TryGetItemAsync(BackupFolderName) is not StorageFolder backupFolder)
            {
                return [];
            }

            IReadOnlyList<StorageFile> files = await backupFolder.GetFilesAsync();
            return files.Where(f => f.Name.StartsWith("exams_backup_") && f.Name.EndsWith(".json"))
                       .OrderByDescending(f => f.Name)
                       .ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"获取备份文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从备份恢复
    /// </summary>
    public async Task RestoreFromBackupAsync(StorageFile backupFile)
    {
        try
        {
            // 先创建当前数据的备份
            await CreateBackupAsync();

            // 读取备份文件内容
            string json = await FileIO.ReadTextAsync(backupFile);

            // 验证备份文件格式
            List<Exam>? exams = JsonSerializer.Deserialize<List<Exam>>(json, _jsonOptions);
            if (exams == null)
            {
                throw new InvalidOperationException("备份文件格式无效");
            }

            // 恢复数据
            await SaveExamsAsync(exams);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"从备份恢复失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 导出试卷到指定文件
    /// </summary>
    public async Task ExportExamToFileAsync(Exam exam, StorageFile file)
    {
        try
        {
            string json = JsonSerializer.Serialize(exam, _jsonOptions);
            await FileIO.WriteTextAsync(file, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"导出试卷失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从文件导入试卷
    /// </summary>
    public async Task<Exam> ImportExamFromFileAsync(StorageFile file)
    {
        try
        {
            string json = await FileIO.ReadTextAsync(file);

            Exam? exam = JsonSerializer.Deserialize<Exam>(json, _jsonOptions);
            if (exam == null)
            {
                throw new InvalidOperationException("文件格式无效");
            }

            // 生成新的ID避免冲突
            exam.Id = "exam-" + DateTime.Now.Ticks;
            exam.Name += " (导入)";

            return exam;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"导入试卷失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 保存应用设置
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            StorageFolder localFolder = await GetStorageFolderAsync();
            StorageFile file = await localFolder.CreateFileAsync(SettingsFileName, CreationCollisionOption.ReplaceExisting);

            string json = JsonSerializer.Serialize(settings, _jsonOptions);
            await FileIO.WriteTextAsync(file, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"保存设置失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 加载应用设置
    /// </summary>
    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            StorageFolder localFolder = await GetStorageFolderAsync();

            if (await localFolder.TryGetItemAsync(SettingsFileName) is not StorageFile file)
            {
                return new AppSettings();
            }

            string json = await FileIO.ReadTextAsync(file);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new AppSettings();
            }

            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
            return settings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"加载设置失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 清理旧备份文件
    /// </summary>
    public async Task CleanupOldBackupsAsync(int keepCount = 10)
    {
        try
        {
            List<StorageFile> backupFiles = await GetBackupFilesAsync();

            if (backupFiles.Count <= keepCount)
            {
                return;
            }

            // 删除多余的旧备份文件
            List<StorageFile> filesToDelete = backupFiles.Skip(keepCount).ToList();
            foreach (StorageFile file in filesToDelete)
            {
                await file.DeleteAsync();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"清理备份文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取数据存储统计信息
    /// </summary>
    public async Task<StorageStatistics> GetStorageStatisticsAsync()
    {
        try
        {
            StorageFolder localFolder = await GetStorageFolderAsync();

            long totalSize = 0;
            int examCount = 0;
            int backupCount = 0;

            // 计算主数据文件大小
            if (await localFolder.TryGetItemAsync(ExamsFileName) is StorageFile examsFile)
            {
                Windows.Storage.FileProperties.BasicProperties props = await examsFile.GetBasicPropertiesAsync();
                totalSize += (long)props.Size;

                List<Exam> exams = await LoadExamsAsync();
                examCount = exams.Count;
            }

            // 计算备份文件大小
            List<StorageFile> backupFiles = await GetBackupFilesAsync();
            backupCount = backupFiles.Count;

            foreach (StorageFile backupFile in backupFiles)
            {
                Windows.Storage.FileProperties.BasicProperties props = await backupFile.GetBasicPropertiesAsync();
                totalSize += (long)props.Size;
            }

            return new StorageStatistics
            {
                TotalSize = totalSize,
                ExamCount = examCount,
                BackupCount = backupCount,
                LastBackupTime = backupFiles.FirstOrDefault()?.DateCreated
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"获取存储统计失败: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// 应用设置
/// </summary>
public class AppSettings
{
    public bool AutoSave { get; set; } = true;
    public int AutoSaveInterval { get; set; } = 300; // 秒
    public bool CreateBackupOnSave { get; set; } = true;
    public int MaxBackupCount { get; set; } = 10;
    public string DefaultExportPath { get; set; } = "";
    public bool ShowWelcomeScreen { get; set; } = true;
    public string LastOpenedExamId { get; set; } = "";
}

/// <summary>
/// 存储统计信息
/// </summary>
public class StorageStatistics
{
    public long TotalSize { get; set; }
    public int ExamCount { get; set; }
    public int BackupCount { get; set; }
    public DateTimeOffset? LastBackupTime { get; set; }
}
