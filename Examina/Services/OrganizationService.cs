using System.Net.Http;
using System.Text;
using System.Text.Json;
using Examina.Models;
using Examina.Models.Organization;

namespace Examina.Services;

/// <summary>
/// 组织服务实现
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authService;
    private const string BaseUrl = "https://qiuzhenbd.com";
    private const string StudentOrganizationApiPath = "api/student/organization";

    public OrganizationService(HttpClient httpClient, IAuthenticationService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    /// <summary>
    /// 用户加入组织
    /// </summary>
    public async Task<JoinOrganizationResult> JoinOrganizationAsync(string invitationCode)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"OrganizationService: 开始加入组织，邀请码: {invitationCode}");

            // 确保用户已登录
            UserInfo? currentUser = _authService.CurrentUser;
            if (currentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("OrganizationService: 用户未登录");
                return JoinOrganizationResult.CreateFailure("用户未登录");
            }

            // 构建请求
            JoinOrganizationRequest request = new()
            {
                InvitationCode = invitationCode
            };

            // 构建API URL
            string apiUrl = $"{BaseUrl}/{StudentOrganizationApiPath}/join";
            System.Diagnostics.Debug.WriteLine($"OrganizationService: API URL: {apiUrl}");

            // 设置认证头
            await SetAuthorizationHeaderAsync();

            // 发送请求
            string jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

            System.Diagnostics.Debug.WriteLine($"OrganizationService: 发送请求内容: {jsonContent}");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"OrganizationService: 响应状态码: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"OrganizationService: 响应内容: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                // 解析API响应
                JoinOrganizationResponse? apiResponse = JsonSerializer.Deserialize<JoinOrganizationResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.Success == true && apiResponse.Organization != null && apiResponse.StudentOrganization != null)
                {
                    // 转换为客户端模型
                    StudentOrganizationDto studentOrgDto = new()
                    {
                        Id = apiResponse.StudentOrganization.Id,
                        OrganizationId = apiResponse.Organization.Id,
                        OrganizationName = apiResponse.Organization.Name,
                        OrganizationType = apiResponse.Organization.Type,
                        OrganizationDescription = apiResponse.Organization.Description,
                        JoinedAt = apiResponse.StudentOrganization.JoinedAt,
                        IsActive = apiResponse.StudentOrganization.IsActive,
                        Role = apiResponse.StudentOrganization.Role
                    };

                    System.Diagnostics.Debug.WriteLine("OrganizationService: 加入组织成功");
                    return JoinOrganizationResult.CreateSuccess(studentOrgDto);
                }
                else
                {
                    string errorMsg = apiResponse?.ErrorMessage ?? "加入组织失败";
                    System.Diagnostics.Debug.WriteLine($"OrganizationService: API返回失败: {errorMsg}");
                    return JoinOrganizationResult.CreateFailure(errorMsg);
                }
            }
            else
            {
                // 尝试解析错误响应
                try
                {
                    JoinOrganizationResponse? errorResponse = JsonSerializer.Deserialize<JoinOrganizationResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (!string.IsNullOrEmpty(errorResponse?.ErrorMessage))
                    {
                        System.Diagnostics.Debug.WriteLine($"OrganizationService: 服务器错误: {errorResponse.ErrorMessage}");
                        return JoinOrganizationResult.CreateFailure(errorResponse.ErrorMessage);
                    }
                }
                catch
                {
                    // 解析失败，使用默认错误消息
                }

                string defaultError = $"加入组织失败: {response.StatusCode}";
                System.Diagnostics.Debug.WriteLine($"OrganizationService: {defaultError}");
                return JoinOrganizationResult.CreateFailure(defaultError);
            }
        }
        catch (Exception ex)
        {
            string errorMsg = $"网络错误: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"OrganizationService: 异常: {ex}");
            return JoinOrganizationResult.CreateFailure(errorMsg);
        }
    }

    /// <summary>
    /// 检查用户是否已加入组织
    /// </summary>
    public async Task<bool> IsUserInOrganizationAsync()
    {
        try
        {
            SchoolBindingStatus status = await GetSchoolBindingStatusAsync();
            return status.IsSchoolBound;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OrganizationService: 检查用户组织状态异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取用户的组织信息
    /// </summary>
    public async Task<StudentOrganizationDto?> GetUserOrganizationAsync()
    {
        try
        {
            List<StudentOrganizationDto> organizations = await GetUserOrganizationsAsync();
            // 返回第一个学校类型的组织
            return organizations.FirstOrDefault(o => o.OrganizationType.Equals("School", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OrganizationService: 获取用户组织信息异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取用户已加入的组织列表
    /// </summary>
    public async Task<List<StudentOrganizationDto>> GetUserOrganizationsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("OrganizationService: 开始获取用户组织列表");

            // 确保用户已登录
            UserInfo? currentUser = _authService.CurrentUser;
            if (currentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("OrganizationService: 用户未登录");
                return new List<StudentOrganizationDto>();
            }

            // 构建API URL
            string apiUrl = $"{BaseUrl}/{StudentOrganizationApiPath}/my-organizations";
            System.Diagnostics.Debug.WriteLine($"OrganizationService: API URL: {apiUrl}");

            // 设置认证头
            await SetAuthorizationHeaderAsync();

            // 发送请求
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"OrganizationService: 响应状态码: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"OrganizationService: 响应内容: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                List<StudentOrganizationDto>? organizations = JsonSerializer.Deserialize<List<StudentOrganizationDto>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                List<StudentOrganizationDto> result = organizations ?? new List<StudentOrganizationDto>();
                System.Diagnostics.Debug.WriteLine($"OrganizationService: 获取到 {result.Count} 个组织");
                return result;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // 用户未加入任何组织
                System.Diagnostics.Debug.WriteLine("OrganizationService: 用户未加入任何组织");
                return new List<StudentOrganizationDto>();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"OrganizationService: 获取组织列表失败: {response.StatusCode}");
                return new List<StudentOrganizationDto>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OrganizationService: 获取用户组织列表异常: {ex}");
            return new List<StudentOrganizationDto>();
        }
    }

    /// <summary>
    /// 获取学校绑定状态
    /// </summary>
    public async Task<SchoolBindingStatus> GetSchoolBindingStatusAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("OrganizationService: 开始获取学校绑定状态");

            // 确保用户已登录
            UserInfo? currentUser = _authService.CurrentUser;
            if (currentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("OrganizationService: 用户未登录");
                return new SchoolBindingStatus { IsSchoolBound = false };
            }

            // 构建API URL
            string apiUrl = $"{BaseUrl}/{StudentOrganizationApiPath}/school-status";
            System.Diagnostics.Debug.WriteLine($"OrganizationService: API URL: {apiUrl}");

            // 设置认证头
            await SetAuthorizationHeaderAsync();

            // 发送请求
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"OrganizationService: 响应状态码: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"OrganizationService: 响应内容: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                SchoolBindingStatus? status = JsonSerializer.Deserialize<SchoolBindingStatus>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                SchoolBindingStatus result = status ?? new SchoolBindingStatus { IsSchoolBound = false };
                System.Diagnostics.Debug.WriteLine($"OrganizationService: 学校绑定状态: {result.IsSchoolBound}, 当前学校: {result.CurrentSchool}");
                return result;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"OrganizationService: 获取学校绑定状态失败: {response.StatusCode}");
                return new SchoolBindingStatus { IsSchoolBound = false };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OrganizationService: 获取学校绑定状态异常: {ex}");
            return new SchoolBindingStatus { IsSchoolBound = false };
        }
    }

    /// <summary>
    /// 设置认证头
    /// </summary>
    private async Task SetAuthorizationHeaderAsync()
    {
        try
        {
            // 获取当前访问令牌
            string? accessToken = await _authService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                System.Diagnostics.Debug.WriteLine("OrganizationService: 已设置Bearer认证头");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("OrganizationService: 访问令牌为空，无法设置认证头");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OrganizationService: 设置认证头异常: {ex.Message}");
        }
    }
}
