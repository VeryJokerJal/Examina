using System;
using System.Collections.Concurrent;
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
    /// ContentDialog队列，确保同时只显示一个对话框
    /// </summary>
    private static readonly ConcurrentQueue<Func<Task>> _dialogQueue = new();

    /// <summary>
    /// 当前是否有对话框正在显示
    /// </summary>
    private static bool _isDialogShowing = false;

    /// <summary>
    /// 队列锁对象
    /// </summary>
    private static readonly object _queueLock = new();
    /// <summary>
    /// 将对话框操作加入队列并执行
    /// </summary>
    /// <param name="dialogAction">对话框操作</param>
    private static async Task EnqueueDialogAsync(Func<Task> dialogAction)
    {
        // 确保在UI线程上执行对话框操作
        Func<Task> wrappedAction = async () =>
        {
            if (App.MainWindow?.DispatcherQueue.HasThreadAccess == true)
            {
                await dialogAction();
            }
            else
            {
                TaskCompletionSource tcs = new();
                App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        await dialogAction();
                        tcs.SetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
                await tcs.Task;
            }
        };

        _dialogQueue.Enqueue(wrappedAction);
        await ProcessDialogQueueAsync();
    }

    /// <summary>
    /// 将对话框操作加入队列并执行（带返回值）
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="dialogFunc">对话框函数</param>
    /// <returns>对话框结果</returns>
    private static async Task<T> EnqueueDialogAsync<T>(Func<Task<T>> dialogFunc)
    {
        TaskCompletionSource<T> tcs = new();

        // 确保在UI线程上执行对话框操作
        Func<Task> wrappedAction = async () =>
        {
            try
            {
                T result;
                if (App.MainWindow?.DispatcherQueue.HasThreadAccess == true)
                {
                    result = await dialogFunc();
                }
                else
                {
                    TaskCompletionSource<T> uiTcs = new();
                    App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
                    {
                        try
                        {
                            T uiResult = await dialogFunc();
                            uiTcs.SetResult(uiResult);
                        }
                        catch (Exception ex)
                        {
                            uiTcs.SetException(ex);
                        }
                    });
                    result = await uiTcs.Task;
                }
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        };

        _dialogQueue.Enqueue(wrappedAction);
        _ = ProcessDialogQueueAsync();
        return await tcs.Task;
    }

    /// <summary>
    /// 处理对话框队列
    /// </summary>
    private static async Task ProcessDialogQueueAsync()
    {
        lock (_queueLock)
        {
            if (_isDialogShowing)
            {
                return; // 已经有对话框在显示，等待当前对话框完成
            }
            _isDialogShowing = true;
        }

        try
        {
            while (_dialogQueue.TryDequeue(out Func<Task>? dialogAction))
            {
                await dialogAction();
            }
        }
        finally
        {
            lock (_queueLock)
            {
                _isDialogShowing = false;
            }
        }
    }

    /// <summary>
    /// 为ContentDialog设置XamlRoot
    /// </summary>
    /// <param name="dialog">要设置XamlRoot的ContentDialog</param>
    private static void SetDialogXamlRoot(ContentDialog dialog)
    {
        try
        {
            // 尝试多种方式获取XamlRoot
            Microsoft.UI.Xaml.XamlRoot? xamlRoot = null;

            // 方式1：从App.MainWindow获取
            if (App.MainWindow?.Content?.XamlRoot != null)
            {
                xamlRoot = App.MainWindow.Content.XamlRoot;
            }
            // 方式2：从当前活动窗口获取
            else if (Microsoft.UI.Xaml.Window.Current?.Content?.XamlRoot != null)
            {
                xamlRoot = Microsoft.UI.Xaml.Window.Current.Content.XamlRoot;
            }
            // 方式3：尝试从应用程序主窗口获取
            else if (Microsoft.UI.Xaml.Application.Current is App app && app.MainWindow?.Content?.XamlRoot != null)
            {
                xamlRoot = app.MainWindow.Content.XamlRoot;
            }

            if (xamlRoot != null)
            {
                dialog.XamlRoot = xamlRoot;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Warning: Could not find XamlRoot for ContentDialog");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting XamlRoot for ContentDialog: {ex.Message}");
        }
    }
    /// <summary>
    /// 显示成功消息
    /// </summary>
    public static async Task ShowSuccessAsync(string title, string message)
    {
        await EnqueueDialogAsync(async () =>
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
        });
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    public static async Task ShowErrorAsync(string title, string message)
    {
        await EnqueueDialogAsync(async () =>
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
        });
    }

    /// <summary>
    /// 显示警告消息
    /// </summary>
    public static async Task ShowWarningAsync(string title, string message)
    {
        await EnqueueDialogAsync(async () =>
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
        });
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public static async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        return await EnqueueDialogAsync(async () =>
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
        });
    }

    /// <summary>
    /// 显示输入对话框
    /// </summary>
    public static async Task<string?> ShowInputDialogAsync(string title, string placeholder = "", string defaultValue = "")
    {
        return await EnqueueDialogAsync(async () =>
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
        });
    }

    /// <summary>
    /// 显示多行文本输入对话框
    /// </summary>
    public static async Task<string?> ShowMultilineInputDialogAsync(string title, string placeholder = "", string defaultValue = "")
    {
        return await EnqueueDialogAsync(async () =>
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
        });
    }

    /// <summary>
    /// 显示选择对话框
    /// </summary>
    public static async Task<string?> ShowSelectionDialogAsync(string title, IEnumerable<string> options)
    {
        return await EnqueueDialogAsync(async () =>
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
        });
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
        return await EnqueueDialogAsync(async () =>
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
        });
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
        return await EnqueueDialogAsync(async () =>
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
        });
    }
}
