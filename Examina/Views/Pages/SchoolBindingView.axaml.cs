using Avalonia.Controls;

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
}
