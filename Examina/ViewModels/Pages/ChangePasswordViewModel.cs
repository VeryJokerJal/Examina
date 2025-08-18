using System.Reactive.Linq;
using System.Windows.Input;
using Examina.Models;
using Examina.Services;
using Prism.Commands;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 修改密码视图模型
/// </summary>
public class ChangePasswordViewModel : ViewModelBase
{
    #region 字段

    private readonly IAuthenticationService _authenticationService;

    #endregion

    #region 属性

    /// <summary>
    /// 当前密码
    /// </summary>
    [Reactive]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    [Reactive]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 确认新密码
    /// </summary>
    [Reactive]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// 是否正在保存
    /// </summary>
    [Reactive]
    public bool IsSaving { get; set; } = false;

    /// <summary>
    /// 状态消息
    /// </summary>
    [Reactive]
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// 当前密码验证错误
    /// </summary>
    [Reactive]
    public string CurrentPasswordError { get; set; } = string.Empty;

    /// <summary>
    /// 新密码验证错误
    /// </summary>
    [Reactive]
    public string NewPasswordError { get; set; } = string.Empty;

    /// <summary>
    /// 确认密码验证错误
    /// </summary>
    [Reactive]
    public string ConfirmPasswordError { get; set; } = string.Empty;

    /// <summary>
    /// 是否有验证错误
    /// </summary>
    [Reactive]
    public bool HasValidationErrors { get; set; } = false;

    /// <summary>
    /// 是否已取消
    /// </summary>
    [Reactive]
    public bool IsCancelled { get; set; } = false;

    #endregion

    #region 命令

    /// <summary>
    /// 保存密码命令
    /// </summary>
    public ICommand SavePasswordCommand { get; }

    /// <summary>
    /// 取消命令
    /// </summary>
    public ICommand CancelCommand { get; }

    #endregion

    #region 构造函数

    public ChangePasswordViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;

        SavePasswordCommand = new DelegateCommand(SavePassword, CanSavePassword);
        CancelCommand = new DelegateCommand(Cancel);

        // 监听属性变化进行验证
        _ = this.WhenAnyValue(x => x.CurrentPassword)
            .Subscribe(_ =>
            {
                ValidateCurrentPassword();
                RaiseCanExecuteChanged();
            });

        _ = this.WhenAnyValue(x => x.NewPassword)
            .Subscribe(_ =>
            {
                ValidateNewPassword();
                ValidateConfirmPassword(); // 重新验证确认密码
                RaiseCanExecuteChanged();
            });

        _ = this.WhenAnyValue(x => x.ConfirmPassword)
            .Subscribe(_ =>
            {
                ValidateConfirmPassword();
                RaiseCanExecuteChanged();
            });

        _ = this.WhenAnyValue(x => x.IsSaving)
            .Subscribe(_ => RaiseCanExecuteChanged());
    }

    #endregion

    #region 方法

    /// <summary>
    /// 保存密码
    /// </summary>
    private async void SavePassword()
    {
        if (!ValidateForm())
        {
            StatusMessage = "请修正表单错误后再保存";
            return;
        }

        IsSaving = true;
        StatusMessage = "正在修改密码...";

        try
        {
            bool success = await _authenticationService.ChangePasswordAsync(new ChangePasswordRequest
            {
                CurrentPassword = CurrentPassword,
                NewPassword = NewPassword
            });

            if (success)
            {
                StatusMessage = "密码修改成功";
                // 清空表单
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
            }
            else
            {
                StatusMessage = "密码修改失败，请检查当前密码是否正确";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"密码修改失败: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// 是否可以保存密码
    /// </summary>
    private bool CanSavePassword()
    {
        return !IsSaving &&
               !HasValidationErrors &&
               !string.IsNullOrWhiteSpace(CurrentPassword) &&
               !string.IsNullOrWhiteSpace(NewPassword) &&
               !string.IsNullOrWhiteSpace(ConfirmPassword);
    }

    /// <summary>
    /// 取消
    /// </summary>
    private void Cancel()
    {
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        StatusMessage = string.Empty;
        ClearValidationErrors();
        IsCancelled = true;
    }

    /// <summary>
    /// 验证当前密码
    /// </summary>
    private void ValidateCurrentPassword()
    {
        CurrentPasswordError = string.Empty;

        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            CurrentPasswordError = "请输入当前密码";
        }

        UpdateValidationState();
    }

    /// <summary>
    /// 验证新密码
    /// </summary>
    private void ValidateNewPassword()
    {
        NewPasswordError = string.Empty;

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            NewPasswordError = "请输入新密码";
        }
        else if (NewPassword.Length < 6)
        {
            NewPasswordError = "密码长度至少6位";
        }
        else if (NewPassword.Length > 20)
        {
            NewPasswordError = "密码长度不能超过20位";
        }
        else if (!Regex.IsMatch(NewPassword, @"^(?=.*[a-zA-Z])(?=.*\d).+$"))
        {
            NewPasswordError = "密码必须包含字母和数字";
        }
        else if (NewPassword == CurrentPassword)
        {
            NewPasswordError = "新密码不能与当前密码相同";
        }

        UpdateValidationState();
    }

    /// <summary>
    /// 验证确认密码
    /// </summary>
    private void ValidateConfirmPassword()
    {
        ConfirmPasswordError = string.Empty;

        if (string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ConfirmPasswordError = "请确认新密码";
        }
        else if (ConfirmPassword != NewPassword)
        {
            ConfirmPasswordError = "两次输入的密码不一致";
        }

        UpdateValidationState();
    }

    /// <summary>
    /// 验证整个表单
    /// </summary>
    private bool ValidateForm()
    {
        ValidateCurrentPassword();
        ValidateNewPassword();
        ValidateConfirmPassword();
        return !HasValidationErrors;
    }

    /// <summary>
    /// 更新验证状态
    /// </summary>
    private void UpdateValidationState()
    {
        HasValidationErrors = !string.IsNullOrEmpty(CurrentPasswordError) ||
                             !string.IsNullOrEmpty(NewPasswordError) ||
                             !string.IsNullOrEmpty(ConfirmPasswordError);
    }

    /// <summary>
    /// 清除验证错误
    /// </summary>
    private void ClearValidationErrors()
    {
        CurrentPasswordError = string.Empty;
        NewPasswordError = string.Empty;
        ConfirmPasswordError = string.Empty;
        UpdateValidationState();
    }

    /// <summary>
    /// 触发命令的CanExecute重新评估
    /// </summary>
    private void RaiseCanExecuteChanged()
    {
        (SavePasswordCommand as DelegateCommand)?.RaiseCanExecuteChanged();
    }

    #endregion
}
