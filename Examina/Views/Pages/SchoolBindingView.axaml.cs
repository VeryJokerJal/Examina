using Avalonia.Controls;
using Avalonia.Interactivity;
using Examina.Services;

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

            // 获取导航服务
            var navigationService = App.ServiceProvider?.GetService(typeof(INavigationService)) as INavigationService;
            if (navigationService != null)
            {
                // 导航到主页面
                navigationService.NavigateToMainPage();
                System.Diagnostics.Debug.WriteLine("SchoolBindingView: 成功导航到主页面");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("SchoolBindingView: 无法获取导航服务");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SchoolBindingView: 返回主页失败: {ex.Message}");
        }
    }
}
