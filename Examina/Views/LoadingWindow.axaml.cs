using Avalonia.Controls;
using Examina.ViewModels;

namespace Examina.Views;

public partial class LoadingWindow : Window
{
    public LoadingWindow()
    {
        InitializeComponent();
    }

    public LoadingWindow(LoadingViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    /// <summary>
    /// 窗口加载完成后开始自动认证
    /// </summary>
    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // 等待窗口完全显示后开始认证流程
        await Task.Delay(500);

        if (DataContext is LoadingViewModel viewModel)
        {
            await viewModel.StartAutoAuthenticationAsync();
        }
    }
}
