using System;
using System.Reactive.Linq;
using ExamLab.Models;
using ExamLab.Services;
using ExamLab.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

        // 设置XamlRoot - 使用Activated事件，确保Content已经初始化
        Activated += OnMainWindowActivated;
        Closed += OnMainWindowClosed;

        // 设置默认选中的导航项
        MainNavigationView.SelectedItem = MainNavigationView.MenuItems[0];
    }

    private void OnMainWindowActivated(object sender, WindowActivatedEventArgs e)
    {
        // 在窗口激活时设置XamlRoot（只设置一次）
        if (Content?.XamlRoot != null && !Services.XamlRootService.IsXamlRootSet())
        {
            Services.XamlRootService.SetXamlRoot(Content.XamlRoot);
        }
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs e)
    {
        // 清除XamlRoot
        XamlRootService.ClearXamlRoot();
    }

    /// <summary>
    /// NavigationView选择变化事件处理
    /// </summary>
    private void MainNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem selectedItem && selectedItem.Tag is string tag)
        {
            switch (tag)
            {
                case "ExamPage":
                    ViewModel.SelectedTabIndex = 0;
                    break;
                case "SpecializedPage":
                    ViewModel.SelectedTabIndex = 1;
                    break;
            }
        }
    }

    private void CloneExam_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is Exam exam)
        {
            _ = ViewModel.CloneExamCommand.Execute(exam).Subscribe(_ => { });
        }
    }

    private void DeleteExam_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is Exam exam)
        {
            _ = ViewModel.DeleteExamCommand.Execute(exam).Subscribe(_ => { });
        }
    }
}
