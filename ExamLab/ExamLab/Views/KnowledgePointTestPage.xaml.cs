using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ExamLab.Models;
using ExamLab.Tests;

namespace ExamLab.Views;

/// <summary>
/// 知识点配置测试页面
/// </summary>
public sealed partial class KnowledgePointTestPage : Page
{
    public KnowledgePointTestPage()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// 测试所有模块
    /// </summary>
    private void TestAllButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ResultTextBlock.Text = "正在测试所有模块，请稍候...";
            
            string result = TestRunner.RunAllKnowledgePointTests();
            ResultTextBlock.Text = result;
        }
        catch (Exception ex)
        {
            ResultTextBlock.Text = $"测试失败: {ex.Message}\n\n{ex.StackTrace}";
        }
    }

    /// <summary>
    /// 快速检查所有模块
    /// </summary>
    private void QuickCheckButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ResultTextBlock.Text = "正在快速检查，请稍候...";
            
            string result = TestRunner.QuickCheckAllModules();
            ResultTextBlock.Text = result;
        }
        catch (Exception ex)
        {
            ResultTextBlock.Text = $"快速检查失败: {ex.Message}\n\n{ex.StackTrace}";
        }
    }

    /// <summary>
    /// 测试特定模块
    /// </summary>
    private void TestSpecificButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ModuleComboBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                ResultTextBlock.Text = "请先选择要测试的模块";
                return;
            }

            string moduleTag = selectedItem.Tag?.ToString() ?? "";
            if (!Enum.TryParse<ModuleType>(moduleTag, out ModuleType moduleType))
            {
                ResultTextBlock.Text = $"无效的模块类型: {moduleTag}";
                return;
            }

            ResultTextBlock.Text = $"正在测试 {moduleType} 模块，请稍候...";
            
            string result = TestRunner.RunSpecificModuleTest(moduleType);
            ResultTextBlock.Text = result;
        }
        catch (Exception ex)
        {
            ResultTextBlock.Text = $"测试特定模块失败: {ex.Message}\n\n{ex.StackTrace}";
        }
    }

    /// <summary>
    /// 获取缺失的知识点列表
    /// </summary>
    private void GetMissingButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ModuleComboBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                ResultTextBlock.Text = "请先选择要检查的模块";
                return;
            }

            string moduleTag = selectedItem.Tag?.ToString() ?? "";
            if (!Enum.TryParse<ModuleType>(moduleTag, out ModuleType moduleType))
            {
                ResultTextBlock.Text = $"无效的模块类型: {moduleTag}";
                return;
            }

            ResultTextBlock.Text = $"正在获取 {moduleType} 模块缺失的知识点，请稍候...";
            
            string result = TestRunner.GetMissingKnowledgePoints(moduleType);
            ResultTextBlock.Text = result;
        }
        catch (Exception ex)
        {
            ResultTextBlock.Text = $"获取缺失知识点失败: {ex.Message}\n\n{ex.StackTrace}";
        }
    }

    /// <summary>
    /// 生成配置模板
    /// </summary>
    private void GenerateTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ModuleComboBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                ResultTextBlock.Text = "请先选择要生成模板的模块";
                return;
            }

            string moduleTag = selectedItem.Tag?.ToString() ?? "";
            if (!Enum.TryParse<ModuleType>(moduleTag, out ModuleType moduleType))
            {
                ResultTextBlock.Text = $"无效的模块类型: {moduleTag}";
                return;
            }

            ResultTextBlock.Text = $"正在生成 {moduleType} 模块的配置模板，请稍候...";
            
            string result = TestRunner.GenerateMissingConfigTemplates(moduleType);
            ResultTextBlock.Text = result;
        }
        catch (Exception ex)
        {
            ResultTextBlock.Text = $"生成配置模板失败: {ex.Message}\n\n{ex.StackTrace}";
        }
    }

    /// <summary>
    /// 清空结果
    /// </summary>
    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ResultTextBlock.Text = "点击上方按钮开始测试...";
    }
}
