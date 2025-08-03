using Avalonia.Controls;
using Examina.ViewModels;

namespace Examina.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        // 监听NavigationView的IsPaneOpen变化
        MainNavigationView.PropertyChanged += (sender, e) =>
        {
            if (e.Property.Name == nameof(MainNavigationView.IsPaneOpen) && DataContext is MainViewModel viewModel)
            {
                viewModel.IsNavigationPaneOpen = MainNavigationView.IsPaneOpen;
            }
        };
    }
}
