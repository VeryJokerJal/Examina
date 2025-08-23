using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using Examina.Models.FileDownload;
using Examina.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels.FileDownload;

/// <summary>
/// 文件下载准备视图模型
/// </summary>
public class FileDownloadPreparationViewModel : ViewModelBase
{
    private readonly IFileDownloadService _fileDownloadService;
    private readonly FileDownloadTask? _currentTask;
    private CancellationTokenSource? _cancellationTokenSource;

    public FileDownloadPreparationViewModel(IFileDownloadService fileDownloadService)
    {
        _fileDownloadService = fileDownloadService;

        // 初始化命令
        StartDownloadCommand = ReactiveCommand.CreateFromTask(StartDownloadAsync, this.WhenAnyValue(x => x.CanStartDownload));
        CancelDownloadCommand = ReactiveCommand.Create(CancelDownload, this.WhenAnyValue(x => x.CanCancelDownload));
        RetryDownloadCommand = ReactiveCommand.CreateFromTask(RetryDownloadAsync, this.WhenAnyValue(x => x.CanRetryDownload));
        CloseCommand = ReactiveCommand.Create(Close, this.WhenAnyValue(x => x.CanClose));

        // 初始化集合
        Files = [];

        // 监听属性变化
        _ = this.WhenAnyValue(x => x.CurrentTask)
            .Where(task => task != null)
            .Subscribe(task =>
            {
                Files.Clear();
                if (task?.Files != null)
                {
                    foreach (FileDownloadInfo file in task.Files)
                    {
                        Files.Add(file);
                    }
                }
                UpdateCanExecuteStates();
            });

        _ = this.WhenAnyValue(x => x.IsDownloading, x => x.IsCompleted, x => x.HasError)
            .Subscribe(_ => UpdateCanExecuteStates());
    }

    #region 属性

    /// <summary>
    /// 任务名称
    /// </summary>
    [Reactive]
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 任务类型
    /// </summary>
    [Reactive]
    public FileDownloadTaskType TaskType { get; set; }

    /// <summary>
    /// 关联ID
    /// </summary>
    [Reactive]
    public int RelatedId { get; set; }

    /// <summary>
    /// 当前下载任务
    /// </summary>
    [Reactive]
    public FileDownloadTask? CurrentTask { get; set; }

    /// <summary>
    /// 文件列表
    /// </summary>
    public ObservableCollection<FileDownloadInfo> Files { get; }

    /// <summary>
    /// 是否正在下载
    /// </summary>
    [Reactive]
    public bool IsDownloading { get; set; }

    /// <summary>
    /// 是否已完成
    /// </summary>
    [Reactive]
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 是否有错误
    /// </summary>
    [Reactive]
    public bool HasError { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [Reactive]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 整体进度
    /// </summary>
    [Reactive]
    public double OverallProgress { get; set; }

    /// <summary>
    /// 状态消息
    /// </summary>
    [Reactive]
    public string StatusMessage { get; set; } = "准备下载...";

    /// <summary>
    /// 总文件数
    /// </summary>
    [Reactive]
    public int TotalFileCount { get; set; }

    /// <summary>
    /// 已完成文件数
    /// </summary>
    [Reactive]
    public int CompletedFileCount { get; set; }

    /// <summary>
    /// 失败文件数
    /// </summary>
    [Reactive]
    public int FailedFileCount { get; set; }

    /// <summary>
    /// 是否显示详细信息
    /// </summary>
    [Reactive]
    public bool ShowDetails { get; set; }

    /// <summary>
    /// 是否可以开始下载
    /// </summary>
    [Reactive]
    public bool CanStartDownload { get; set; } = true;

    /// <summary>
    /// 是否可以取消下载
    /// </summary>
    [Reactive]
    public bool CanCancelDownload { get; set; }

    /// <summary>
    /// 是否可以重试下载
    /// </summary>
    [Reactive]
    public bool CanRetryDownload { get; set; }

    /// <summary>
    /// 是否可以关闭
    /// </summary>
    [Reactive]
    public bool CanClose { get; set; } = true;

    #endregion

    #region 命令

    /// <summary>
    /// 开始下载命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartDownloadCommand { get; }

    /// <summary>
    /// 取消下载命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelDownloadCommand { get; }

    /// <summary>
    /// 重试下载命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RetryDownloadCommand { get; }

    /// <summary>
    /// 关闭命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    #endregion

    #region 方法

    /// <summary>
    /// 初始化下载任务
    /// </summary>
    /// <param name="taskName">任务名称</param>
    /// <param name="taskType">任务类型</param>
    /// <param name="relatedId">关联ID</param>
    public async Task InitializeAsync(string taskName, FileDownloadTaskType taskType, int relatedId)
    {
        try
        {
            TaskName = taskName;
            TaskType = taskType;
            RelatedId = relatedId;
            StatusMessage = "正在获取文件列表...";

            List<FileDownloadInfo> files = taskType is FileDownloadTaskType.MockExam or FileDownloadTaskType.OnlineExam
                ? await _fileDownloadService.GetExamFilesAsync(relatedId, taskType)
                : await _fileDownloadService.GetTrainingFilesAsync(relatedId, taskType);
            if (files.Count == 0)
            {
                StatusMessage = "没有需要下载的文件";
                IsCompleted = true;
                return;
            }

            CurrentTask = _fileDownloadService.CreateDownloadTask(taskName, taskType, relatedId, files);
            TotalFileCount = files.Count;
            StatusMessage = $"找到 {files.Count} 个文件，准备下载";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusMessage = "获取文件列表失败";
        }
    }

    /// <summary>
    /// 开始下载
    /// </summary>
    private async Task StartDownloadAsync()
    {
        if (CurrentTask == null)
        {
            return;
        }

        try
        {
            IsDownloading = true;
            HasError = false;
            ErrorMessage = null;
            _cancellationTokenSource = new CancellationTokenSource();

            Progress<FileDownloadTask> progress = new(OnDownloadProgress);
            bool result = await _fileDownloadService.StartDownloadTaskAsync(CurrentTask, progress, _cancellationTokenSource.Token);

            if (result)
            {
                IsCompleted = true;
                StatusMessage = "下载完成";

                // 清理临时文件
                await _fileDownloadService.CleanupTempFilesAsync(CurrentTask);
            }
            else
            {
                HasError = true;
                ErrorMessage = CurrentTask.ErrorMessage;
                StatusMessage = "下载失败";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "下载已取消";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusMessage = "下载失败";
        }
        finally
        {
            IsDownloading = false;
        }
    }

    /// <summary>
    /// 取消下载
    /// </summary>
    private void CancelDownload()
    {
        if (CurrentTask != null)
        {
            _fileDownloadService.CancelDownloadTask(CurrentTask);
        }
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// 重试下载
    /// </summary>
    private async Task RetryDownloadAsync()
    {
        if (CurrentTask == null)
        {
            return;
        }

        HasError = false;
        ErrorMessage = null;
        IsCompleted = false;

        await StartDownloadAsync();
    }

    /// <summary>
    /// 关闭
    /// </summary>
    private void Close()
    {
        if (IsDownloading)
        {
            CancelDownload();
        }
    }

    /// <summary>
    /// 下载进度回调
    /// </summary>
    private void OnDownloadProgress(FileDownloadTask task)
    {
        OverallProgress = task.OverallProgress;
        StatusMessage = task.StatusMessage;
        CompletedFileCount = task.CompletedFileCount;
        FailedFileCount = task.FailedFileCount;

        if (task.Status == FileDownloadTaskStatus.Failed)
        {
            HasError = true;
            ErrorMessage = task.ErrorMessage;
        }
    }

    /// <summary>
    /// 更新命令可执行状态
    /// </summary>
    private void UpdateCanExecuteStates()
    {
        CanStartDownload = !IsDownloading && !IsCompleted && CurrentTask != null && CurrentTask.Files.Count > 0;
        CanCancelDownload = IsDownloading;
        CanRetryDownload = HasError && !IsDownloading;
        CanClose = !IsDownloading || IsCompleted || HasError;
    }

    #endregion
}
