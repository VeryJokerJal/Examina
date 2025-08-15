using ExamLab.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ExamLab.Views;

/// <summary>
/// 题目配置对话框
/// </summary>
public sealed partial class QuestionConfigurationDialog : ContentDialog
{
    /// <summary>
    /// 视图模型
    /// </summary>
    public QuestionConfigurationViewModel ViewModel { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="viewModel">视图模型</param>
    public QuestionConfigurationDialog(QuestionConfigurationViewModel viewModel)
    {
        this.InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        
        // 设置对话框事件
        PrimaryButtonClick += OnPrimaryButtonClick;
        SecondaryButtonClick += OnSecondaryButtonClick;
    }

    /// <summary>
    /// 主按钮点击事件（保存）
    /// </summary>
    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 验证题目信息
        if (string.IsNullOrWhiteSpace(ViewModel.Question.Title))
        {
            args.Cancel = true;
            await ViewModel.ShowErrorAsync("验证失败", "题目标题不能为空");
            return;
        }

        if (string.IsNullOrWhiteSpace(ViewModel.Question.Content))
        {
            args.Cancel = true;
            await ViewModel.ShowErrorAsync("验证失败", "题目内容不能为空");
            return;
        }

        // 保存题目
        try
        {
            await ViewModel.SaveQuestionAsync();
        }
        catch (System.Exception ex)
        {
            args.Cancel = true;
            await ViewModel.ShowErrorAsync("保存失败", $"保存题目时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 次按钮点击事件（取消）
    /// </summary>
    private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 取消操作，不需要特殊处理
    }
}
