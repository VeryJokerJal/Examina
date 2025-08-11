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

        XamlRootService.SetXamlRoot(Content.XamlRoot);
        Closed += OnMainWindowClosed;
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs e)
    {
        // 清除XamlRoot
        XamlRootService.ClearXamlRoot();
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
