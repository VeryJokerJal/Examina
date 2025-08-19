using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Examina.Services;
using Examina.ViewModels;
using Examina.Views;

namespace Examina.Views.Pages;

public partial class SchoolBindingView : UserControl
{
    public SchoolBindingView()
    {
        System.Diagnostics.Debug.WriteLine("SchoolBindingView: 构造函数被调用");
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("SchoolBindingView: InitializeComponent完成");

        // 监听DataContext变化
        this.DataContextChanged += (sender, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"SchoolBindingView: DataContext changed to {DataContext?.GetType().Name ?? "null"}");
        };

        // 监听Loaded事件
        this.Loaded += (sender, e) =>
        {
            System.Diagnostics.Debug.WriteLine("SchoolBindingView: Loaded事件触发");
            System.Diagnostics.Debug.WriteLine($"SchoolBindingView: DataContext = {DataContext?.GetType().Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"SchoolBindingView: IsVisible = {IsVisible}");
            System.Diagnostics.Debug.WriteLine($"SchoolBindingView: Bounds = {Bounds}");
        };
    }

    /// <summary>
    /// 返回主页按钮点击事件
    /// </summary>
    private void ReturnToMainPage_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("SchoolBindingView: 用户点击返回主页按钮");

            // 方法1：通过当前窗口查找MainView和MainViewModel
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.Content is MainView mainView)
                {
                    if (mainView.DataContext is MainViewModel mainViewModel)
                    {
                        System.Diagnostics.Debug.WriteLine("SchoolBindingView: 找到当前MainViewModel实例");
                        mainViewModel.NavigateToPage("overview");
                        System.Diagnostics.Debug.WriteLine("SchoolBindingView: 成功导航到概览页面");
                        return;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"SchoolBindingView: MainView的DataContext不是MainViewModel，实际类型: {mainView.DataContext?.GetType().Name ?? "null"}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"SchoolBindingView: MainWindow的Content不是MainView，实际类型: {desktop.MainWindow?.Content?.GetType().Name ?? "null"}");
                }
            }

            // 方法2：通过App服务获取MainViewModel（备用方案）
            System.Diagnostics.Debug.WriteLine("SchoolBindingView: 尝试备用方案 - 通过App服务获取MainViewModel");
            if (Avalonia.Application.Current is App app)
            {
                MainViewModel? mainViewModel = app.GetService<MainViewModel>();
                if (mainViewModel != null)
                {
                    System.Diagnostics.Debug.WriteLine("SchoolBindingView: 通过App服务获取到MainViewModel");
                    mainViewModel.NavigateToPage("overview");
                    System.Diagnostics.Debug.WriteLine("SchoolBindingView: 备用方案成功导航到概览页面");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SchoolBindingView: 无法通过App服务获取MainViewModel");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("SchoolBindingView: 无法获取App实例");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SchoolBindingView: 返回主页失败: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"SchoolBindingView: 异常堆栈: {ex.StackTrace}");
        }
    }
}
