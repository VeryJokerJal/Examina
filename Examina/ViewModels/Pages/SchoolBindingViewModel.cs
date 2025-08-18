using System.Net.Http;
using System.Windows.Input;
using Examina.Models.Organization;
using Examina.Services;
using Prism.Commands;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 加入学校页面视图模型
/// </summary>
public class SchoolBindingViewModel : ViewModelBase
{
    private readonly IOrganizationService? _organizationService;
    private readonly IAuthenticationService? _authenticationService;

    #region 属性

    /// <summary>
    /// 页面标题
    /// </summary>
    [Reactive]
    public string PageTitle { get; set; } = "加入学校";

    /// <summary>
    /// 邀请码
    /// </summary>
    [Reactive]
    public string InvitationCode { get; set; } = string.Empty;

    /// <summary>
    /// 组织名称
    /// </summary>
    [Reactive]
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// 是否已绑定学校
    /// </summary>
    [Reactive]
    public bool IsSchoolBound { get; set; } = false;

    /// <summary>
    /// 当前绑定的学校
    /// </summary>
    [Reactive]
    public string CurrentSchool { get; set; } = string.Empty;

    /// <summary>
    /// 是否正在处理
    /// </summary>
    [Reactive]
    public bool IsProcessing { get; set; } = false;

    /// <summary>
    /// 状态消息
    /// </summary>
    [Reactive]
    public string StatusMessage { get; set; } = string.Empty;

    #endregion

    #region 命令

    /// <summary>
    /// 加入组织命令
    /// </summary>
    public ICommand JoinOrganizationCommand { get; }

    /// <summary>
    /// 解绑学校命令
    /// </summary>
    public ICommand UnbindSchoolCommand { get; }

    #endregion

    #region 构造函数

    public SchoolBindingViewModel(IOrganizationService organizationService, IAuthenticationService authenticationService)
    {
        System.Diagnostics.Debug.WriteLine("SchoolBindingViewModel: 构造函数开始");
        System.Diagnostics.Debug.WriteLine($"SchoolBindingViewModel: OrganizationService = {(organizationService != null ? "已提供" : "null")}");
        System.Diagnostics.Debug.WriteLine($"SchoolBindingViewModel: AuthenticationService = {(authenticationService != null ? "已提供" : "null")}");

        _organizationService = organizationService;
        _authenticationService = authenticationService;

        JoinOrganizationCommand = new DelegateCommand(async () => await JoinOrganizationAsync(), CanJoinOrganization);
        UnbindSchoolCommand = new DelegateCommand(async () => await UnbindSchoolAsync(), CanUnbindSchool);

        System.Diagnostics.Debug.WriteLine("SchoolBindingViewModel: 开始加载当前学校绑定状态");
        _ = LoadCurrentSchoolBindingAsync();
        System.Diagnostics.Debug.WriteLine("SchoolBindingViewModel: 构造函数完成");
    }

    #endregion

    #region 方法

    /// <summary>
    /// 加载当前学校绑定状态
    /// </summary>
    private async Task LoadCurrentSchoolBindingAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("SchoolBindingViewModel: LoadCurrentSchoolBindingAsync 开始");

            if (_organizationService == null)
            {
                System.Diagnostics.Debug.WriteLine("SchoolBindingViewModel: OrganizationService为null，无法加载绑定状态");
                StatusMessage = "服务未初始化，无法检查学校绑定状态";
                return;
            }

            IsProcessing = true;
            StatusMessage = "正在检查学校绑定状态...";
            System.Diagnostics.Debug.WriteLine("SchoolBindingViewModel: 开始检查用户是否已加入组织");

            // 检查用户是否已加入组织
            bool isInOrganization = await _organizationService.IsUserInOrganizationAsync();
            System.Diagnostics.Debug.WriteLine($"SchoolBindingViewModel: 用户是否已加入组织: {isInOrganization}");

            if (isInOrganization)
            {
                // 获取组织信息
                StudentOrganizationDto? organization = await _organizationService.GetUserOrganizationAsync();
                if (organization != null)
                {
                    IsSchoolBound = true;
                    CurrentSchool = organization.OrganizationName;
                    StatusMessage = "已成功加入学校";
                }
                else
                {
                    IsSchoolBound = false;
                    StatusMessage = "获取学校信息失败";
                }
            }
            else
            {
                IsSchoolBound = false;
                StatusMessage = "尚未加入任何学校";
            }
        }
        catch (Exception ex)
        {
            IsSchoolBound = false;
            StatusMessage = $"检查绑定状态失败: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// 加入组织
    /// </summary>
    private async Task JoinOrganizationAsync()
    {
        // 验证邀请码格式
        if (!ValidateInvitationCode(InvitationCode, out string validationError))
        {
            ShowUserGuidance(validationError);
            return;
        }

        IsProcessing = true;
        StatusMessage = "正在加入学校...";

        try
        {
            JoinOrganizationResult? result = await _organizationService?.JoinOrganizationAsync(InvitationCode);

            if (result.Success && result.StudentOrganization != null)
            {
                // 加入成功
                IsSchoolBound = true;
                CurrentSchool = result.StudentOrganization.OrganizationName;
                OrganizationName = result.StudentOrganization.OrganizationName;
                StatusMessage = "成功加入学校！";

                // 清空邀请码
                InvitationCode = string.Empty;

                // 刷新用户权限状态
                bool refreshSuccess = await RefreshUserPermissionsAsync();
                if (!refreshSuccess)
                {
                    StatusMessage += " 但权限状态更新失败，请重新登录以获取最新权限。";
                }

                System.Diagnostics.Debug.WriteLine($"用户成功加入学校: {CurrentSchool}");
            }
            else
            {
                // 加入失败
                string errorMessage = result.ErrorMessage ?? "加入学校失败";
                ShowUserGuidance(errorMessage);

                System.Diagnostics.Debug.WriteLine($"加入学校失败: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            string errorMessage = HandleNetworkError(ex);
            StatusMessage = $"加入失败: {errorMessage}";

            System.Diagnostics.Debug.WriteLine($"加入学校异常: {ex}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// 是否可以加入组织
    /// </summary>
    private bool CanJoinOrganization()
    {
        return !string.IsNullOrWhiteSpace(InvitationCode) && !IsSchoolBound && !IsProcessing;
    }

    /// <summary>
    /// 解绑学校
    /// </summary>
    private async Task UnbindSchoolAsync()
    {
        IsProcessing = true;
        StatusMessage = "正在退出学校...";

        try
        {
            // TODO: 实现学校退出逻辑（需要后端API支持）
            // 目前只是模拟退出操作
            await Task.Delay(1000); // 模拟网络请求

            IsSchoolBound = false;
            CurrentSchool = string.Empty;
            OrganizationName = string.Empty;
            StatusMessage = "已退出学校";

            // 刷新用户权限状态
            bool refreshSuccess = await RefreshUserPermissionsAsync();
            if (!refreshSuccess)
            {
                StatusMessage += " 但权限状态更新失败，请重新登录以获取最新权限。";
            }

            System.Diagnostics.Debug.WriteLine("用户已退出学校");
        }
        catch (Exception ex)
        {
            string errorMessage = HandleNetworkError(ex);
            StatusMessage = $"退出失败: {errorMessage}";

            System.Diagnostics.Debug.WriteLine($"退出学校异常: {ex}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// 刷新用户权限状态
    /// </summary>
    private async Task<bool> RefreshUserPermissionsAsync()
    {
        try
        {
            // 刷新用户信息以更新权限状态
            bool? success = await _authenticationService?.RefreshUserInfoAsync();
            if (success == true)
            {
                System.Diagnostics.Debug.WriteLine("用户权限状态刷新成功");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("用户权限状态刷新失败");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"刷新用户权限状态异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 是否可以解绑学校
    /// </summary>
    private bool CanUnbindSchool()
    {
        return IsSchoolBound && !IsProcessing;
    }

    /// <summary>
    /// 处理网络错误
    /// </summary>
    private string HandleNetworkError(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => "网络连接失败，请检查网络设置",
            TaskCanceledException => "请求超时，请稍后重试",
            System.Net.Sockets.SocketException => "网络连接错误，请检查网络连接",
            _ => $"网络错误: {ex.Message}"
        };
    }

    /// <summary>
    /// 验证邀请码格式
    /// </summary>
    private bool ValidateInvitationCode(string invitationCode, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(invitationCode))
        {
            errorMessage = "请输入邀请码";
            return false;
        }

        if (invitationCode.Length != 7)
        {
            errorMessage = "邀请码必须为7位字符";
            return false;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(invitationCode, @"^[A-Za-z0-9]{7}$"))
        {
            errorMessage = "邀请码只能包含字母和数字";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 显示用户引导信息
    /// </summary>
    private void ShowUserGuidance(string errorMessage)
    {
        StatusMessage = errorMessage.Contains("完善个人信息")
            ? errorMessage + "\n\n请点击底部的\"个人信息\"完善您的真实姓名和手机号码后再尝试加入学校。"
            : errorMessage.Contains("邀请码") ? errorMessage + "\n\n请向您的老师获取正确的7位班级邀请码。" : errorMessage;
    }

    #endregion
}
