using ExamLab.Services;
using ExamLab.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace ExamLab;

/// <summary>
/// 主窗口
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public MainWindowViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        ViewModel = new MainWindowViewModel();
        mainGrid.DataContext = ViewModel;

        // 在Activated事件中设置XamlRoot，确保UI完全加载
        Activated += OnMainWindowActivated;
        Closed += OnMainWindowClosed;
    }

    private void OnMainWindowActivated(object sender, WindowActivatedEventArgs e)
    {
        // 设置XamlRoot（只设置一次）
        if (!XamlRootService.IsXamlRootSet() && Content?.XamlRoot != null)
        {
            XamlRootService.SetXamlRoot(Content.XamlRoot);
        }
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs e)
    {
        // 清除XamlRoot
        XamlRootService.ClearXamlRoot();
    }
}
