using Avalonia.Controls;
using Examina.ViewModels.Windows;

namespace Examina.Views.Windows;

/// <summary>
/// 解锁推广窗口
/// </summary>
public partial class UnlockPromotionWindow : Window
{
    public UnlockPromotionWindow()
    {
        InitializeComponent();
        DataContext = new UnlockPromotionWindowViewModel();

        System.Diagnostics.Debug.WriteLine("[UnlockPromotionWindow] 窗口初始化完成");
    }
}
