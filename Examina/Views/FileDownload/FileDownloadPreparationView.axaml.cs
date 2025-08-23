using Avalonia.Controls;
using Examina.ViewModels.FileDownload;

namespace Examina.Views.FileDownload;

/// <summary>
/// 文件下载准备视图
/// </summary>
public partial class FileDownloadPreparationView : UserControl
{
    public FileDownloadPreparationView()
    {
        InitializeComponent();
    }

    public FileDownloadPreparationView(FileDownloadPreparationViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
