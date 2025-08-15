using ExamLab.Models;
using Microsoft.UI.Xaml.Controls;

namespace ExamLab.Views;

public sealed partial class ModuleSelectionDialog : ContentDialog
{
    public ModuleType? SelectedModuleType { get; private set; }

    public ModuleSelectionDialog()
    {
        InitializeComponent();
        
        // 默认选择第一个项目
        if (ModuleTypeListView.Items.Count > 0)
        {
            ModuleTypeListView.SelectedIndex = 0;
        }

        PrimaryButtonClick += OnPrimaryButtonClick;
        SecondaryButtonClick += OnSecondaryButtonClick;
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
