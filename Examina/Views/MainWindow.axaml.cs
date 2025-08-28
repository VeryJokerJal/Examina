using Avalonia.Controls;

namespace Examina.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        TransparencyLevelHint = [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur];
    }
}
