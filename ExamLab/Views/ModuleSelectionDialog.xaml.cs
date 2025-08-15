using ExamLab.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ExamLab.Views;

public sealed partial class ModuleSelectionDialog : ContentDialog
{
    public ModuleType? SelectedModuleType { get; private set; }

    public ModuleSelectionDialog()
    {
        InitializeComponent();

        // 设置XamlRoot
        SetXamlRoot();

        // 默认选择第一个项目
        if (ModuleTypeListView.Items.Count > 0)
        {
            ModuleTypeListView.SelectedIndex = 0;
        }

        PrimaryButtonClick += OnPrimaryButtonClick;
        SecondaryButtonClick += OnSecondaryButtonClick;
    }

    /// <summary>
    /// 设置XamlRoot
    /// </summary>
    private void SetXamlRoot()
    {
        try
        {
            // 尝试从XamlRootService获取
            Microsoft.UI.Xaml.XamlRoot? xamlRoot = Services.XamlRootService.GetXamlRoot();
            if (xamlRoot is not null)
            {
                XamlRoot = xamlRoot;
                return;
            }

            // 备用方案：从App.MainWindow获取
            if (App.MainWindow?.Content?.XamlRoot is not null)
            {
                XamlRoot = App.MainWindow.Content.XamlRoot;
            }
        }
        catch
        {
            // 如果设置失败，对话框仍然可以显示，只是可能位置不正确
        }
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (ModuleTypeListView.SelectedItem is ListViewItem selectedItem && selectedItem.Tag is string tag)
        {
            SelectedModuleType = tag switch
            {
                "Windows" => ModuleType.Windows,
                "CSharp" => ModuleType.CSharp,
                "PowerPoint" => ModuleType.PowerPoint,
                "Excel" => ModuleType.Excel,
                "Word" => ModuleType.Word,
                _ => null
            };
        }
        else
        {
            args.Cancel = true; // 阻止对话框关闭
        }
    }

    private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        SelectedModuleType = null;
    }
}
