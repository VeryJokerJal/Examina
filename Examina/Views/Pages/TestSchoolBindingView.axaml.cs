using Avalonia.Controls;

namespace Examina.Views.Pages;

public partial class TestSchoolBindingView : UserControl
{
    public TestSchoolBindingView()
    {
        System.Diagnostics.Debug.WriteLine("TestSchoolBindingView: 构造函数被调用");
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("TestSchoolBindingView: InitializeComponent完成");
        
        this.DataContextChanged += (sender, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"TestSchoolBindingView: DataContext changed to {DataContext?.GetType().Name ?? "null"}");
        };
        
        this.Loaded += (sender, e) =>
        {
            System.Diagnostics.Debug.WriteLine("TestSchoolBindingView: Loaded事件触发");
            System.Diagnostics.Debug.WriteLine($"TestSchoolBindingView: DataContext = {DataContext?.GetType().Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"TestSchoolBindingView: IsVisible = {IsVisible}");
            System.Diagnostics.Debug.WriteLine($"TestSchoolBindingView: Bounds = {Bounds}");
        };
    }
}
