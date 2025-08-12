using System.Text;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using ExamLab.Models;
using ExamLab.Services;
using ExamLab.ViewModels;

namespace ExamLab;

/// <summary>
/// 主窗口
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public MainWindowViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        ViewModel = new MainWindowViewModel();
        mainGrid.DataContext = ViewModel;

        Closed += OnMainWindowClosed;
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        // WPF 版本不需要清除 XamlRoot
    }

    private void CloneExam_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is Exam exam)
        {
            _ = ViewModel.CloneExamCommand.Execute(exam).Subscribe(_ => { });
        }
    }

    private void DeleteExam_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is Exam exam)
        {
            _ = ViewModel.DeleteExamCommand.Execute(exam).Subscribe(_ => { });
        }
    }
}