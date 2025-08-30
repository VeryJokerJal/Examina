using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace ExamLab.Services;

/// <summary>
/// 通知服务
/// </summary>
public static class NotificationService
{
    /// <summary>
    /// 为ContentDialog设置XamlRoot
    /// </summary>
    /// <param name="dialog">要设置XamlRoot的ContentDialog</param>
    private static void SetDialogXamlRoot(ContentDialog dialog)
    {
        dialog.XamlRoot = App.MainWindow?.Content.XamlRoot;
    }
    /// <summary>
    /// 显示成功消息
    /// </summary>
    public static async Task ShowSuccessAsync(string title, string message)
    {
        ContentDialog dialog = new()
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
            DefaultButton = ContentDialogButton.Close
        };

        SetDialogXamlRoot(dialog);

        _ = await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    public static async Task ShowErrorAsync(string title, string message)
    {
        ContentDialog dialog = new()
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
            DefaultButton = ContentDialogButton.Close
        };

        SetDialogXamlRoot(dialog);

        _ = await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示警告消息
    /// </summary>
    public static async Task ShowWarningAsync(string title, string message)
    {
        ContentDialog dialog = new()
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
            DefaultButton = ContentDialogButton.Close
        };

        SetDialogXamlRoot(dialog);

        _ = await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public static async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        ContentDialog dialog = new()
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        SetDialogXamlRoot(dialog);

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// 显示输入对话框
    /// </summary>
    public static async Task<string?> ShowInputDialogAsync(string title, string placeholder = "", string defaultValue = "")
    {
        TextBox textBox = new()
        {
            PlaceholderText = placeholder,
            Text = defaultValue,
            AcceptsReturn = false
        };

        ContentDialog dialog = new()
        {
            Title = title,
            Content = textBox,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        SetDialogXamlRoot(dialog);

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? textBox.Text : null;
    }

    /// <summary>
    /// 显示多行文本输入对话框
    /// </summary>
    public static async Task<string?> ShowMultilineInputDialogAsync(string title, string placeholder = "", string defaultValue = "")
    {
        TextBox textBox = new()
        {
            PlaceholderText = placeholder,
            Text = defaultValue,
            AcceptsReturn = true,
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            Height = 150
        };

        ContentDialog dialog = new()
        {
            Title = title,
            Content = textBox,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        SetDialogXamlRoot(dialog);

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? textBox.Text : null;
    }

    /// <summary>
    /// 显示选择对话框
    /// </summary>
    public static async Task<string?> ShowSelectionDialogAsync(string title, IEnumerable<string> options)
    {
        ComboBox comboBox = new()
        {
            ItemsSource = options.ToList(),
            SelectedIndex = 0
        };

        ContentDialog dialog = new()
        {
            Title = title,
            Content = comboBox,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        SetDialogXamlRoot(dialog);

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? comboBox.SelectedItem?.ToString() : null;
    }

    /// <summary>
    /// 显示验证错误消息
    /// </summary>
    public static async Task ShowValidationErrorsAsync(ValidationResult validationResult)
    {
        if (validationResult.IsValid)
        {
            return;
        }

        string errorMessage = string.Join("\n• ", validationResult.Errors);
        await ShowErrorAsync("验证失败", $"请修正以下错误：\n• {errorMessage}");
    }

    /// <summary>
    /// 显示操作成功消息
    /// </summary>
    public static async Task ShowOperationSuccessAsync(string operation)
    {
        await ShowSuccessAsync("操作成功", $"{operation}已成功完成。");
    }

    /// <summary>
    /// 显示删除确认对话框
    /// </summary>
    public static async Task<bool> ShowDeleteConfirmationAsync(string itemName)
    {
        return await ShowConfirmationAsync(
            "确认删除",
            $"您确定要删除 '{itemName}' 吗？此操作无法撤销。");
    }

    /// <summary>
    /// 显示保存确认对话框
    /// </summary>
    public static async Task<bool> ShowSaveConfirmationAsync()
    {
        return await ShowConfirmationAsync(
            "保存更改",
            "您有未保存的更改，是否要保存？");
    }

    /// <summary>
    /// 显示带有操作按钮的成功消息
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    /// <param name="actionButtonText">操作按钮文本</param>
    /// <param name="closeButtonText">关闭按钮文本</param>
    /// <returns>用户选择的结果</returns>
    public static async Task<ContentDialogResult> ShowSuccessWithActionAsync(string title, string message, string actionButtonText, string closeButtonText = "确定")
    {
        ContentDialog dialog = new()
        {
            Title = title,
            Content = message,
            PrimaryButtonText = actionButtonText,
            CloseButtonText = closeButtonText,
            DefaultButton = ContentDialogButton.Close
        };

        SetDialogXamlRoot(dialog);

        return await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示带有操作按钮的信息消息
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    /// <param name="actionButtonText">操作按钮文本</param>
    /// <param name="closeButtonText">关闭按钮文本</param>
    /// <returns>用户选择的结果</returns>
    public static async Task<ContentDialogResult> ShowInfoWithActionAsync(string title, string message, string actionButtonText, string closeButtonText = "确定")
    {
        ContentDialog dialog = new()
        {
            Title = title,
            Content = message,
            PrimaryButtonText = actionButtonText,
            CloseButtonText = closeButtonText,
            DefaultButton = ContentDialogButton.Close
        };

        SetDialogXamlRoot(dialog);

        return await dialog.ShowAsync();
    }
}
