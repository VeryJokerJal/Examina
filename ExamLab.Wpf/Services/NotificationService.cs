using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ExamLab.Services;

/// <summary>
/// 通知服务 - WPF版本，使用MessageBox
/// </summary>
public static class NotificationService
{
    /// <summary>
    /// 显示成功消息
    /// </summary>
    public static async Task ShowSuccessAsync(string title, string message)
    {
        await Task.Run(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    public static async Task ShowErrorAsync(string title, string message)
    {
        await Task.Run(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }

    /// <summary>
    /// 显示警告消息
    /// </summary>
    public static async Task ShowWarningAsync(string title, string message)
    {
        await Task.Run(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        });
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public static async Task<bool> ShowConfirmAsync(string title, string message)
    {
        return await Task.Run(() =>
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        });
    }

    /// <summary>
    /// 显示输入对话框（简化版本，使用InputBox或自定义窗口）
    /// </summary>
    public static async Task<string?> ShowInputDialogAsync(string title, string placeholder = "", string defaultValue = "")
    {
        // WPF 版本的简化实现，可以后续扩展为自定义窗口
        return await Task.FromResult<string?>(null);
    }

    /// <summary>
    /// 显示验证错误消息
    /// </summary>
    public static async Task ShowValidationErrorsAsync(object validationResult)
    {
        // 简化版本，可以根据需要扩展
        await ShowErrorAsync("验证失败", "请检查输入的数据。");
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
        return await ShowConfirmAsync(
            "确认删除",
            $"您确定要删除 '{itemName}' 吗？此操作无法撤销。");
    }

    /// <summary>
    /// 显示保存确认对话框
    /// </summary>
    public static async Task<bool> ShowSaveConfirmationAsync()
    {
        return await ShowConfirmAsync(
            "保存更改",
            "您有未保存的更改，是否要保存？");
    }
}
