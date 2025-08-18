using System.Net.Http;
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
    private const string BaseUrl = "https://qiuzhenbd.com/api";
    private const string OrganizationUrl = "organization";

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
            // 确保用户已登录
            UserInfo? currentUser = _authService.CurrentUser;
            if (currentUser == null)
            {
                return JoinOrganizationResult.CreateFailure("用户未登录");
            }

            // 构建请求
            JoinOrganizationRequest request = new()
            {
                InvitationCode = invitationCode
            };

            // 构建API URL
            string apiUrl = BuildApiUrl("join");

            // 设置认证头
            await SetAuthorizationHeaderAsync();

            // 发送请求
            string jsonContent = JsonSerializer.Serialize(request);
            StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                JoinOrganizationResult? result = JsonSerializer.Deserialize<JoinOrganizationResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? JoinOrganizationResult.CreateFailure("响应解析失败");
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();

                // 尝试解析错误响应
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(errorContent);
                    if (doc.RootElement.TryGetProperty("message", out JsonElement messageElement))
                    {
                        return JoinOrganizationResult.CreateFailure(messageElement.GetString() ?? "加入组织失败");
                    }
                }
                catch
                {
                    // 解析失败，使用默认错误消息
                }

                return JoinOrganizationResult.CreateFailure($"加入组织失败: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return JoinOrganizationResult.CreateFailure($"网络错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查用户是否已加入组织
    /// </summary>
    public async Task<bool> IsUserInOrganizationAsync()
    {
        try
        {
            StudentOrganizationDto? organization = await GetUserOrganizationAsync();
            return organization != null;
        }
        catch
        {
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
            // 确保用户已登录
            UserInfo? currentUser = _authService.CurrentUser;
            if (currentUser == null)
            {
                return null;
            }

            // 构建API URL
            string apiUrl = BuildApiUrl("my-organization");

            // 设置认证头
            await SetAuthorizationHeaderAsync();

            // 发送请求
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                StudentOrganizationDto? result = JsonSerializer.Deserialize<StudentOrganizationDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // 用户未加入任何组织
                return null;
            }
            else
            {
                // 其他错误
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 构建API端点URL
    /// </summary>
    private string BuildApiUrl(string endpoint)
    {
        string apiPath = "api";
        string organizationPath = OrganizationUrl.Trim('/');
        string endpointPath = endpoint.TrimStart('/');

        return $"/{apiPath}/{organizationPath}/{endpointPath}";
    }

    /// <summary>
    /// 设置认证头
    /// </summary>
    private async Task SetAuthorizationHeaderAsync()
    {
        // 获取当前访问令牌
        string? accessToken = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(accessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}
