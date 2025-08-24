using System.ComponentModel;

namespace Examina.Models.FileDownload;

/// <summary>
/// 文件下载信息
/// </summary>
public class FileDownloadInfo : INotifyPropertyChanged
{
    private string _fileName = string.Empty;
    private string _downloadUrl = string.Empty;
    private long _totalSize;
    private long _downloadedSize;
    private double _progress;
    private FileDownloadStatus _status = FileDownloadStatus.Pending;
    private string _statusMessage = string.Empty;
    private string _localFilePath = string.Empty;
    private bool _isCompressed;
    private string _extractPath = string.Empty;
    private DateTime? _startTime;
    private DateTime? _endTime;
    private string? _errorMessage;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 下载URL
    /// </summary>
    public string DownloadUrl
    {
        get => _downloadUrl;
        set
        {
            _downloadUrl = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 文件总大小（字节）
    /// </summary>
    public long TotalSize
    {
        get => _totalSize;
        set
        {
            _totalSize = value;
            OnPropertyChanged();
            UpdateProgress();
        }
    }

    /// <summary>
    /// 已下载大小（字节）
    /// </summary>
    public long DownloadedSize
    {
        get => _downloadedSize;
        set
        {
            _downloadedSize = value;
            OnPropertyChanged();
            UpdateProgress();
        }
    }

    /// <summary>
    /// 下载进度（0-100）
    /// </summary>
    public double Progress
    {
        get => _progress;
        private set
        {
            _progress = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 下载状态
    /// </summary>
    public FileDownloadStatus Status
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
    /// 本地文件路径
    /// </summary>
    public string LocalFilePath
    {
        get => _localFilePath;
        set
        {
            _localFilePath = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否为压缩文件
    /// </summary>
    public bool IsCompressed
    {
        get => _isCompressed;
        set
        {
            _isCompressed = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 解压路径
    /// </summary>
    public string ExtractPath
    {
        get => _extractPath;
        set
        {
            _extractPath = value;
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
    /// 格式化的文件大小
    /// </summary>
    public string FormattedTotalSize => FormatFileSize(TotalSize);

    /// <summary>
    /// 格式化的已下载大小
    /// </summary>
    public string FormattedDownloadedSize => FormatFileSize(DownloadedSize);

    /// <summary>
    /// 下载速度（字节/秒）
    /// </summary>
    public double DownloadSpeed
    {
        get
        {
            if (StartTime == null || DownloadedSize == 0) return 0;
            var elapsed = DateTime.Now - StartTime.Value;
            return elapsed.TotalSeconds > 0 ? DownloadedSize / elapsed.TotalSeconds : 0;
        }
    }

    /// <summary>
    /// 格式化的下载速度
    /// </summary>
    public string FormattedDownloadSpeed => FormatFileSize((long)DownloadSpeed) + "/s";

    /// <summary>
    /// 预计剩余时间
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining
    {
        get
        {
            if (DownloadSpeed <= 0 || Progress >= 100) return null;
            var remainingBytes = TotalSize - DownloadedSize;
            return TimeSpan.FromSeconds(remainingBytes / DownloadSpeed);
        }
    }

    /// <summary>
    /// 格式化的预计剩余时间
    /// </summary>
    public string FormattedEstimatedTimeRemaining
    {
        get
        {
            var eta = EstimatedTimeRemaining;
            if (eta == null) return "未知";
            
            if (eta.Value.TotalHours >= 1)
                return $"{eta.Value.Hours:D2}:{eta.Value.Minutes:D2}:{eta.Value.Seconds:D2}";
            else
                return $"{eta.Value.Minutes:D2}:{eta.Value.Seconds:D2}";
        }
    }

    private void UpdateProgress()
    {
        if (TotalSize > 0)
        {
            Progress = (double)DownloadedSize / TotalSize * 100;
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 文件下载状态
/// </summary>
public enum FileDownloadStatus
{
    /// <summary>
    /// 等待中
    /// </summary>
    Pending,
    
    /// <summary>
    /// 下载中
    /// </summary>
    Downloading,
    
    /// <summary>
    /// 下载完成
    /// </summary>
    Downloaded,
    
    /// <summary>
    /// 解压中
    /// </summary>
    Extracting,
    
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
