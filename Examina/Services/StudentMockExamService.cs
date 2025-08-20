using System.Net.Http.Headers;
using Examina.Models.MockExam;

namespace Examina.Services;

/// <summary>
/// 学生端模拟考试服务实现
/// </summary>
public class StudentMockExamService : IStudentMockExamService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authenticationService;
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// 统一的JSON序列化选项配置
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public StudentMockExamService(
        HttpClient httpClient,
        IAuthenticationService authenticationService,
        IConfigurationService configurationService)
    {
        _httpClient = httpClient;
        _authenticationService = authenticationService;
        _configurationService = configurationService;
    }

    /// <summary>
    /// 快速开始模拟考试（使用预设规则自动生成并开始）
    /// </summary>
    public async Task<StudentMockExamDto?> QuickStartMockExamAsync()
    {
        try
        {
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl("mock-exams/quick-start");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送快速开始模拟考试请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应内容: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                StudentMockExamDto? mockExam = JsonSerializer.Deserialize<StudentMockExamDto>(responseContent, JsonOptions);
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 成功快速开始模拟考试，ID: {mockExam?.Id}");
                return mockExam;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 快速开始模拟考试失败，状态码: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 快速开始模拟考试异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 创建模拟考试
    /// </summary>
    public async Task<StudentMockExamDto?> CreateMockExamAsync(CreateMockExamRequestDto request)
    {
        try
        {
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl("mock-exams");
            string json = JsonSerializer.Serialize(request, JsonOptions);
            StringContent content = new(json, Encoding.UTF8, "application/json");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送创建模拟考试请求到 {apiUrl}");
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 请求内容: {json}");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应内容: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                StudentMockExamDto? mockExam = JsonSerializer.Deserialize<StudentMockExamDto>(responseContent, JsonOptions);
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 成功创建模拟考试，ID: {mockExam?.Id}");
                return mockExam;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 创建模拟考试失败，状态码: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 创建模拟考试异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取学生的模拟考试列表
    /// </summary>
    public async Task<List<StudentMockExamDto>> GetStudentMockExamsAsync(int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams?pageNumber={pageNumber}&pageSize={pageSize}");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送获取模拟考试列表请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                List<StudentMockExamDto>? mockExams = JsonSerializer.Deserialize<List<StudentMockExamDto>>(responseContent, JsonOptions);
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 成功获取模拟考试列表，数量: {mockExams?.Count ?? 0}");
                return mockExams ?? [];
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 获取模拟考试列表失败，状态码: {response.StatusCode}");
                return [];
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 获取模拟考试列表异常: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// 获取模拟考试详情
    /// </summary>
    public async Task<StudentMockExamDto?> GetMockExamDetailsAsync(int mockExamId)
    {
        try
        {
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送获取模拟考试详情请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                StudentMockExamDto? mockExam = JsonSerializer.Deserialize<StudentMockExamDto>(responseContent, JsonOptions);
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 成功获取模拟考试详情，ID: {mockExam?.Id}");
                return mockExam;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 获取模拟考试详情失败，状态码: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 获取模拟考试详情异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 开始模拟考试
    /// </summary>
    public async Task<bool> StartMockExamAsync(int mockExamId)
    {
        try
        {
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}/start");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送开始模拟考试请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");

            bool success = response.IsSuccessStatusCode;
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 开始模拟考试结果: {success}");
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 开始模拟考试异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 完成模拟考试
    /// </summary>
    public async Task<bool> CompleteMockExamAsync(int mockExamId)
    {
        try
        {
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}/complete");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送完成模拟考试请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");

            bool success = response.IsSuccessStatusCode;
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 完成模拟考试结果: {success}");
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 完成模拟考试异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 提交模拟考试
    /// </summary>
    public async Task<bool> SubmitMockExamAsync(int mockExamId)
    {
        try
        {
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}/submit");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送提交模拟考试请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");

            bool success = response.IsSuccessStatusCode;
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 提交模拟考试结果: {success}");
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 提交模拟考试异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 删除模拟考试
    /// </summary>
    public async Task<bool> DeleteMockExamAsync(int mockExamId)
    {
        try
        {
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送删除模拟考试请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.DeleteAsync(apiUrl);

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");

            bool success = response.IsSuccessStatusCode;
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 删除模拟考试结果: {success}");
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 删除模拟考试异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取学生可访问的模拟考试总数
    /// </summary>
    public async Task<int> GetStudentMockExamCountAsync()
    {
        try
        {
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl("mock-exams/count");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送获取模拟考试总数请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                int count = JsonSerializer.Deserialize<int>(responseContent, JsonOptions);
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 成功获取模拟考试总数: {count}");
                return count;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 获取模拟考试总数失败，状态码: {response.StatusCode}");
                return 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 获取模拟考试总数异常: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 检查是否有权限访问指定模拟考试
    /// </summary>
    public async Task<bool> HasAccessToMockExamAsync(int mockExamId)
    {
        try
        {
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}/access");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送检查模拟考试访问权限请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                JsonDocument doc = JsonDocument.Parse(responseContent);
                bool hasAccess = doc.RootElement.GetProperty("hasAccess").GetBoolean();
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 模拟考试访问权限检查结果: {hasAccess}");
                return hasAccess;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 检查模拟考试访问权限失败，状态码: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 检查模拟考试访问权限异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 设置认证头
    /// </summary>
    private async Task SetAuthenticationHeaderAsync()
    {
        try
        {
            string? accessToken = await _authenticationService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                System.Diagnostics.Debug.WriteLine("StudentMockExamService: 已设置JWT认证头");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("StudentMockExamService: 警告 - 无法获取访问令牌");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 设置认证头异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 构建API URL
    /// </summary>
    private string BuildApiUrl(string endpoint)
    {
        string baseUrl = _configurationService.ApiBaseUrl.TrimEnd('/');
        string studentEndpoint = _configurationService.StudentAuthEndpoint.TrimEnd('/');
        return $"{baseUrl}/{studentEndpoint}/{endpoint}";
    }
}