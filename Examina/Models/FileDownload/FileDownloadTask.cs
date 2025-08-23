using System.ComponentModel;

namespace Examina.Models.FileDownload;

/// <summary>
/// 文件下载任务
/// </summary>
public class FileDownloadTask : INotifyPropertyChanged
{
    private string _taskId = Guid.NewGuid().ToString();
    private string _taskName = string.Empty;
    private FileDownloadTaskType _taskType;
    private int _relatedId;
    private List<FileDownloadInfo> _files = new();
    private FileDownloadTaskStatus _status = FileDownloadTaskStatus.Pending;
    private string _statusMessage = string.Empty;
    private double _overallProgress;
    private DateTime? _startTime;
    private DateTime? _endTime;
    private string? _errorMessage;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 任务ID
    /// </summary>
    public string TaskId
    {
        get => _taskId;
        set
        {
            _taskId = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 任务名称
    /// </summary>
    public string TaskName
    {
        get => _taskName;
        set
        {
            _taskName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 任务类型
    /// </summary>
    public FileDownloadTaskType TaskType
    {
        get => _taskType;
        set
        {
            _taskType = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 关联的ID（考试ID、训练ID等）
    /// </summary>
    public int RelatedId
    {
        get => _relatedId;
        set
        {
            _relatedId = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 文件列表
    /// </summary>
    public List<FileDownloadInfo> Files
    {
        get => _files;
        set
        {
            _files = value;
            OnPropertyChanged();
            UpdateOverallProgress();
        }
    }

    /// <summary>
    /// 任务状态
    /// </summary>
    public FileDownloadTaskStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 状态消息
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 整体进度（0-100）
    /// </summary>
    public double OverallProgress
    {
        get => _overallProgress;
        private set
        {
            _overallProgress = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime
    {
        get => _startTime;
        set
        {
            _startTime = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime
    {
        get => _endTime;
        set
        {
            _endTime = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 取消令牌源
    /// </summary>
    public CancellationTokenSource? CancellationTokenSource
    {
        get => _cancellationTokenSource;
        set
        {
            _cancellationTokenSource = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 总文件数
    /// </summary>
    public int TotalFileCount => Files.Count;

    /// <summary>
    /// 已完成文件数
    /// </summary>
    public int CompletedFileCount => Files.Count(f => f.Status == FileDownloadStatus.Completed);

    /// <summary>
    /// 失败文件数
    /// </summary>
    public int FailedFileCount => Files.Count(f => f.Status == FileDownloadStatus.Failed);

    /// <summary>
    /// 是否可以取消
    /// </summary>
    public bool CanCancel => Status == FileDownloadTaskStatus.Running && CancellationTokenSource != null;

    /// <summary>
    /// 是否可以重试
    /// </summary>
    public bool CanRetry => Status == FileDownloadTaskStatus.Failed || Status == FileDownloadTaskStatus.Cancelled;

    /// <summary>
    /// 任务类型显示名称
    /// </summary>
    public string TaskTypeDisplayName => TaskType switch
    {
        FileDownloadTaskType.MockExam => "模拟考试",
        FileDownloadTaskType.OnlineExam => "上机统考",
        FileDownloadTaskType.ComprehensiveTraining => "综合实训",
        FileDownloadTaskType.SpecializedTraining => "专项训练",
        _ => "未知"
    };

    /// <summary>
    /// 更新整体进度
    /// </summary>
    public void UpdateOverallProgress()
    {
        if (Files.Count == 0)
        {
            OverallProgress = 0;
            return;
        }

        var totalProgress = Files.Sum(f => f.Progress);
        OverallProgress = totalProgress / Files.Count;
    }

    /// <summary>
    /// 添加文件
    /// </summary>
    /// <param name="file">文件信息</param>
    public void AddFile(FileDownloadInfo file)
    {
        Files.Add(file);
        file.PropertyChanged += OnFilePropertyChanged;
        OnPropertyChanged(nameof(Files));
        OnPropertyChanged(nameof(TotalFileCount));
        UpdateOverallProgress();
    }

    /// <summary>
    /// 移除文件
    /// </summary>
    /// <param name="file">文件信息</param>
    public void RemoveFile(FileDownloadInfo file)
    {
        if (Files.Remove(file))
        {
            file.PropertyChanged -= OnFilePropertyChanged;
            OnPropertyChanged(nameof(Files));
            OnPropertyChanged(nameof(TotalFileCount));
            UpdateOverallProgress();
        }
    }

    /// <summary>
    /// 清空文件列表
    /// </summary>
    public void ClearFiles()
    {
        foreach (var file in Files)
        {
            file.PropertyChanged -= OnFilePropertyChanged;
        }
        Files.Clear();
        OnPropertyChanged(nameof(Files));
        OnPropertyChanged(nameof(TotalFileCount));
        UpdateOverallProgress();
    }

    private void OnFilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileDownloadInfo.Progress) || 
            e.PropertyName == nameof(FileDownloadInfo.Status))
        {
            UpdateOverallProgress();
            OnPropertyChanged(nameof(CompletedFileCount));
            OnPropertyChanged(nameof(FailedFileCount));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 文件下载任务类型
/// </summary>
public enum FileDownloadTaskType
{
    /// <summary>
    /// 模拟考试
    /// </summary>
    MockExam,
    
    /// <summary>
    /// 上机统考
    /// </summary>
    OnlineExam,
    
    /// <summary>
    /// 综合实训
    /// </summary>
    ComprehensiveTraining,
    
    /// <summary>
    /// 专项训练
    /// </summary>
    SpecializedTraining
}

/// <summary>
/// 文件下载任务状态
/// </summary>
public enum FileDownloadTaskStatus
{
    /// <summary>
    /// 等待中
    /// </summary>
    Pending,
    
    /// <summary>
    /// 运行中
    /// </summary>
    Running,
    
    /// <summary>
    /// 已完成
    /// </summary>
    Completed,
    
    /// <summary>
    /// 失败
    /// </summary>
    Failed,
    
    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled
}
