using System.Net.Http.Headers;
using Examina.Models.Api;
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
            _ = await SetAuthenticationHeaderAsync();
            string apiUrl = BuildApiUrl("mock-exams/quick-start");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);
            string responseContent = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode ? JsonSerializer.Deserialize<StudentMockExamDto>(responseContent, JsonOptions) : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 快速开始模拟考试异常: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 异常堆栈: {ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// 快速开始模拟考试（返回综合训练格式，包含模块结构）
    /// </summary>
    public async Task<MockExamComprehensiveTrainingDto?> QuickStartMockExamComprehensiveTrainingAsync()
    {
        try
        {
            _ = await SetAuthenticationHeaderAsync();
            string apiUrl = BuildApiUrl("mock-exams/quick-start");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);
            string responseContent = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<MockExamComprehensiveTrainingDto>(responseContent, JsonOptions)
                : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 快速开始模拟考试（综合训练格式）异常: {ex.Message}");
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
            _ = await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl("mock-exams");
            string json = JsonSerializer.Serialize(request, JsonOptions);
            StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode ? JsonSerializer.Deserialize<StudentMockExamDto>(responseContent, JsonOptions) : null;
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
            bool authSuccess = await SetAuthenticationHeaderAsync();
            if (!authSuccess)
            {
                return [];
            }

            string apiUrl = BuildApiUrl($"mock-exams?pageNumber={pageNumber}&pageSize={pageSize}");
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                List<StudentMockExamDto>? mockExams = JsonSerializer.Deserialize<List<StudentMockExamDto>>(responseContent, JsonOptions);
                return mockExams ?? [];
            }

            return [];
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
            _ = await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}");
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode ? JsonSerializer.Deserialize<StudentMockExamDto>(responseContent, JsonOptions) : null;
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
            _ = await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}/start");
            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);

            return response.IsSuccessStatusCode;
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
            _ = await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}/complete");
            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);

            return response.IsSuccessStatusCode;
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
    public async Task<MockExamSubmissionResponseDto?> SubmitMockExamAsync(int mockExamId, int? actualDurationSeconds = null)
    {
        try
        {
            _ = await SetAuthenticationHeaderAsync();

            bool hasAccess = await HasAccessToMockExamAsync(mockExamId);
            if (!hasAccess)
            {
                await DiagnoseMockExamAccessAsync(mockExamId);

                return new MockExamSubmissionResponseDto
                {
                    Success = false,
                    Message = "无权限访问该模拟考试",
                    Status = "Unauthorized",
                    TimeStatusDescription = "权限验证失败"
                };
            }

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}/submit");
            if (actualDurationSeconds.HasValue)
            {
                apiUrl += $"?actualDurationSeconds={actualDurationSeconds.Value}";
            }

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MockExamSubmissionResponseDto>(responseContent, JsonOptions);
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                try
                {
                    return JsonSerializer.Deserialize<MockExamSubmissionResponseDto>(errorContent, JsonOptions);
                }
                catch
                {
                    return new MockExamSubmissionResponseDto
                    {
                        Success = false,
                        Message = "提交模拟考试失败",
                        Status = "Error",
                        TimeStatusDescription = "网络请求失败"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 提交模拟考试异常: {ex.Message}");
            return new MockExamSubmissionResponseDto
            {
                Success = false,
                Message = "提交模拟考试时发生异常",
                Status = "Error",
                TimeStatusDescription = $"客户端异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 提交模拟考试成绩
    /// </summary>
    public async Task<bool> SubmitMockExamScoreAsync(int mockExamId, SubmitMockExamScoreRequestDto scoreRequest)
    {
        try
        {
            _ = await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}/score");
            string jsonContent = JsonSerializer.Serialize(scoreRequest, JsonOptions);
            StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _ = await response.Content.ReadAsStringAsync();
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 提交模拟考试成绩异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取模拟考试成绩列表
    /// </summary>
    public async Task<List<MockExamCompletionDto>> GetMockExamCompletionsAsync(int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _ = await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/completions?pageNumber={pageNumber}&pageSize={pageSize}");
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                List<MockExamCompletionDto>? completions = JsonSerializer.Deserialize<List<MockExamCompletionDto>>(responseContent, JsonOptions);
                return completions ?? [];
            }
            else
            {
                _ = await response.Content.ReadAsStringAsync();
                return [];
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 获取模拟考试成绩列表异常: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// 删除模拟考试
    /// </summary>
    public async Task<bool> DeleteMockExamAsync(int mockExamId)
    {
        try
        {
            _ = await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}");
            HttpResponseMessage response = await _httpClient.DeleteAsync(apiUrl);

            return response.IsSuccessStatusCode;
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
            _ = await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl("mock-exams/count");
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode ? JsonSerializer.Deserialize<int>(responseContent, JsonOptions) : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 获取模拟考试总数异常: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 获取学生已完成的模拟考试数量
    /// </summary>
    public async Task<int> GetCompletedMockExamCountAsync()
    {
        try
        {
            _ = await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl("mock-exams/completed/count");
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode ? JsonSerializer.Deserialize<int>(responseContent, JsonOptions) : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 获取已完成模拟考试数量异常: {ex.Message}");
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
            _ = await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}/access");
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                JsonDocument doc = JsonDocument.Parse(responseContent);
                return doc.RootElement.GetProperty("hasAccess").GetBoolean();
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 检查模拟考试访问权限异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 诊断模拟考试访问权限问题
    /// </summary>
    public async Task DiagnoseMockExamAccessAsync(int mockExamId)
    {
        try
        {
            _ = await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}/diagnose");
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            _ = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 诊断模拟考试权限异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 设置认证头
    /// </summary>
    private async Task<bool> SetAuthenticationHeaderAsync()
    {
        try
        {
            string? accessToken = await _authenticationService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                return true;
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 设置认证头异常: {ex.Message}");
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return false;
        }
    }

    /// <summary>
    /// 构建API URL
    /// </summary>
    private string BuildApiUrl(string endpoint)
    {
        string baseUrl = _configurationService.ApiBaseUrl.TrimEnd('/');
        return $"{baseUrl}/api/student/{endpoint}";
    }
}
