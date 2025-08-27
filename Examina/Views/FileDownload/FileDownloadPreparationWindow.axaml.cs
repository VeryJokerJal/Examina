using Avalonia.Controls;
using Avalonia.Interactivity;
using Examina.Models.FileDownload;
using Examina.ViewModels.FileDownload;

namespace Examina.Views.FileDownload;

/// <summary>
/// 文件下载准备窗口
/// </summary>
public partial class FileDownloadPreparationWindow : Window
{
    private readonly FileDownloadPreparationViewModel? _viewModel;

    public FileDownloadPreparationWindow()
    {
        InitializeComponent();
    }

    public FileDownloadPreparationWindow(FileDownloadPreparationViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;

        // 监听ViewModel的关闭事件
        _ = _viewModel?.CloseCommand.Subscribe(_ => Close());
    }

    /// <summary>
    /// 初始化下载任务
    /// </summary>
    /// <param name="taskName">任务名称</param>
    /// <param name="taskType">任务类型</param>
    /// <param name="relatedId">关联ID</param>
    public async Task InitializeAsync(string taskName, FileDownloadTaskType taskType, int relatedId)
    {
        if (_viewModel != null)
        {
            await _viewModel.InitializeAsync(taskName, taskType, relatedId);
        }
    }

    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// 窗口关闭时的处理
    /// </summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // 如果正在下载，询问用户是否确认关闭
        if (_viewModel?.IsDownloading == true)
        {
            // 这里可以添加确认对话框
            // 暂时直接取消下载
            _ = _viewModel.CancelDownloadCommand.Execute().Subscribe();
        }

        base.OnClosing(e);
    }

    /// <summary>
    /// 显示文件下载准备窗口
    /// </summary>
    /// <param name="parent">父窗口</param>
    /// <param name="taskName">任务名称</param>
    /// <param name="taskType">任务类型</param>
    /// <param name="relatedId">关联ID</param>
    /// <returns>下载是否成功完成</returns>
    public static async Task<bool> ShowDownloadPreparationAsync(
        Window? parent,
        string taskName,
        FileDownloadTaskType taskType,
        int relatedId)
    {
        try
        {
            // 从应用程序服务容器获取ViewModel
            App? app = Avalonia.Application.Current as App;
            FileDownloadPreparationViewModel? viewModel = app?.GetService<FileDownloadPreparationViewModel>();

            if (viewModel == null)
            {
                return false;
            }

            FileDownloadPreparationWindow window = new(viewModel);

            // 初始化下载任务
            await window.InitializeAsync(taskName, taskType, relatedId);

            // 如果没有文件需要下载，直接返回成功
            if (viewModel.TotalFileCount == 0)
            {
                return true;
            }

            // 显示窗口并等待关闭
            if (parent != null)
            {
                await window.ShowDialog(parent);
            }
            else
            {
                window.Show();

                // 等待窗口关闭
                TaskCompletionSource<bool> tcs = new();
                window.Closed += (_, _) => tcs.SetResult(viewModel.IsCompleted && !viewModel.HasError);
                return await tcs.Task;
            }

            // 返回下载是否成功完成
            return viewModel.IsCompleted && !viewModel.HasError;
        }
        catch (Exception ex)
        {
            // 记录错误日志
            System.Diagnostics.Debug.WriteLine($"显示文件下载准备窗口时发生错误: {ex.Message}");
            return false;
        }
    }
}
