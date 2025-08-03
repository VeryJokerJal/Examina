using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Examina.Models;
using Examina.Services;
using Examina.Views.Pages;
using Prism.Commands;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 个人信息页面视图模型
/// </summary>
public class ProfileViewModel : ViewModelBase
{
    #region 字段

    private readonly IAuthenticationService _authenticationService;
    private UserInfo? _originalUserInfo;

    #endregion

    #region 属性

    /// <summary>
    /// 页面标题
    /// </summary>
    [Reactive]
    public string PageTitle { get; set; } = "个人信息";

    /// <summary>
    /// 用户名
    /// </summary>
    [Reactive]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    [Reactive]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 注册时间
    /// </summary>
    [Reactive]
    public DateTime RegistrationDate { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    [Reactive]
    public DateTime LastLoginTime { get; set; }

    /// <summary>
    /// 是否正在编辑
    /// </summary>
    [Reactive]
    public bool IsEditing { get; set; } = false;

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
    /// 是否正在加载
    /// </summary>
    [Reactive]
    public bool IsLoading { get; set; } = false;

    /// <summary>
    /// 用户名验证错误消息
    /// </summary>
    [Reactive]
    public string UsernameError { get; set; } = string.Empty;





    /// <summary>
    /// 是否有验证错误
    /// </summary>
    [Reactive]
    public bool HasValidationErrors { get; set; } = false;

    /// <summary>
    /// 用户名首字母（用于头像显示）
    /// </summary>
    public string FirstLetter => string.IsNullOrEmpty(Username) ? "U" : Username.Substring(0, 1).ToUpper();

    #endregion

    #region 命令

    /// <summary>
    /// 编辑信息命令
    /// </summary>
    public ICommand EditProfileCommand { get; }

    /// <summary>
    /// 保存信息命令
    /// </summary>
    public ICommand SaveProfileCommand { get; }

    /// <summary>
    /// 取消编辑命令
    /// </summary>
    public ICommand CancelEditCommand { get; }

    /// <summary>
    /// 修改密码命令
    /// </summary>
    public ICommand ChangePasswordCommand { get; }

    /// <summary>
    /// 刷新用户信息命令
    /// </summary>
    public ICommand RefreshProfileCommand { get; }

    /// <summary>
    /// 上传头像命令
    /// </summary>
    public ICommand UploadAvatarCommand { get; }

    #endregion

    #region 构造函数

    public ProfileViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;

        EditProfileCommand = new DelegateCommand(EditProfile, CanEditProfile);
        SaveProfileCommand = new DelegateCommand(SaveProfile, CanSaveProfile);
        CancelEditCommand = new DelegateCommand(CancelEdit, CanCancelEdit);
        ChangePasswordCommand = new DelegateCommand(ChangePassword);
        RefreshProfileCommand = new DelegateCommand(RefreshProfile);
        UploadAvatarCommand = new DelegateCommand(UploadAvatar);

        // 监听属性变化进行验证和命令状态更新
        this.WhenAnyValue(x => x.Username)
            .Subscribe(_ =>
            {
                ValidateUsername();
                this.RaisePropertyChanged(nameof(FirstLetter));
                RaiseCanExecuteChanged();
            });



        // 监听编辑状态变化
        this.WhenAnyValue(x => x.IsEditing)
            .Subscribe(_ => RaiseCanExecuteChanged());

        // 监听保存状态变化
        this.WhenAnyValue(x => x.IsSaving)
            .Subscribe(_ => RaiseCanExecuteChanged());

        LoadUserProfile();
    }

    #endregion

    #region 方法

    /// <summary>
    /// 加载用户资料
    /// </summary>
    private void LoadUserProfile()
    {
        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            UserInfo? currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                _originalUserInfo = currentUser;
                Username = currentUser.Username;
                PhoneNumber = currentUser.PhoneNumber;

                // 注册时间和最后登录时间从服务端获取
                RegistrationDate = DateTime.Now.AddDays(-30); // 临时数据
                LastLoginTime = DateTime.Now.AddHours(-2); // 临时数据
            }
            else
            {
                StatusMessage = "无法获取用户信息";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载用户信息失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 编辑资料
    /// </summary>
    private void EditProfile()
    {
        IsEditing = true;
        StatusMessage = string.Empty;
    }

    /// <summary>
    /// 是否可以编辑资料
    /// </summary>
    private bool CanEditProfile()
    {
        return !IsEditing && !IsSaving;
    }

    /// <summary>
    /// 保存资料
    /// </summary>
    private async void SaveProfile()
    {
        if (!ValidateForm())
        {
            StatusMessage = "请修正表单错误后再保存";
            return;
        }

        IsSaving = true;
        StatusMessage = "正在保存...";

        try
        {
            // 调用AuthenticationService更新用户信息
            bool success = await _authenticationService.UpdateUserProfileAsync(new UpdateUserProfileRequest
            {
                Username = Username
            });

            if (success)
            {
                IsEditing = false;
                StatusMessage = "保存成功";

                // 更新原始数据
                _originalUserInfo = _authenticationService.CurrentUser;
            }
            else
            {
                StatusMessage = "保存失败，请稍后重试";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// 是否可以保存资料
    /// </summary>
    private bool CanSaveProfile()
    {
        return IsEditing && !IsSaving && !string.IsNullOrWhiteSpace(Username);
    }

    /// <summary>
    /// 取消编辑
    /// </summary>
    private void CancelEdit()
    {
        IsEditing = false;
        StatusMessage = string.Empty;
        LoadUserProfile(); // 重新加载原始数据
    }

    /// <summary>
    /// 是否可以取消编辑
    /// </summary>
    private bool CanCancelEdit()
    {
        return IsEditing && !IsSaving;
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    private async void ChangePassword()
    {
        try
        {
            // 创建密码修改ViewModel
            ChangePasswordViewModel changePasswordViewModel = ((App)Application.Current!).GetService<ChangePasswordViewModel>()
                ?? new ChangePasswordViewModel(_authenticationService);

            // 创建密码修改View
            ChangePasswordView changePasswordView = new(changePasswordViewModel);

            // 创建对话框窗口
            Window dialog = new()
            {
                Title = "修改密码",
                Content = changePasswordView,
                Width = 480,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false
            };

            bool dialogClosed = false;

            // 监听密码修改成功事件
            changePasswordViewModel.WhenAnyValue(x => x.StatusMessage)
                .Where(msg => msg == "密码修改成功")
                .Subscribe(_ =>
                {
                    if (!dialogClosed)
                    {
                        dialogClosed = true;
                        StatusMessage = "密码修改成功";
                        dialog.Close();
                    }
                });

            // 监听取消事件
            changePasswordViewModel.WhenAnyValue(x => x.IsCancelled)
                .Where(cancelled => cancelled)
                .Subscribe(_ =>
                {
                    if (!dialogClosed)
                    {
                        dialogClosed = true;
                        dialog.Close();
                    }
                });

            // 显示对话框
            await dialog.ShowDialog(GetMainWindow());
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开密码修改对话框失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 获取主窗口
    /// </summary>
    private Window GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow ?? new Window()
            : new Window();
    }

    /// <summary>
    /// 刷新用户信息
    /// </summary>
    private void RefreshProfile()
    {
        LoadUserProfile();
    }

    /// <summary>
    /// 上传头像
    /// </summary>
    private void UploadAvatar()
    {
        // TODO: 实现头像上传功能
        StatusMessage = "头像上传功能待实现";
    }

    /// <summary>
    /// 验证用户名
    /// </summary>
    private void ValidateUsername()
    {
        UsernameError = string.Empty;

        if (string.IsNullOrWhiteSpace(Username))
        {
            UsernameError = "用户名不能为空";
        }
        else if (Username.Length < 2)
        {
            UsernameError = "用户名至少需要2个字符";
        }
        else if (Username.Length > 20)
        {
            UsernameError = "用户名不能超过20个字符";
        }

        UpdateValidationState();
    }



    /// <summary>
    /// 验证整个表单
    /// </summary>
    private bool ValidateForm()
    {
        ValidateUsername();
        return !HasValidationErrors;
    }

    /// <summary>
    /// 更新验证状态
    /// </summary>
    private void UpdateValidationState()
    {
        HasValidationErrors = !string.IsNullOrEmpty(UsernameError);
    }

    /// <summary>
    /// 触发所有命令的CanExecute重新评估
    /// </summary>
    private void RaiseCanExecuteChanged()
    {
        (EditProfileCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (SaveProfileCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (CancelEditCommand as DelegateCommand)?.RaiseCanExecuteChanged();
    }

    #endregion
}
