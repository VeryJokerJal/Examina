using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 快速开始模拟考试失败");
                System.Diagnostics.Debug.WriteLine($"  状态码: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"  请求URL: {apiUrl}");
                System.Diagnostics.Debug.WriteLine($"  响应内容: {responseContent}");
                return null;
            }
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
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl("mock-exams/quick-start");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送快速开始模拟考试请求（综合训练格式）到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应内容长度: {responseContent.Length}");

            if (response.IsSuccessStatusCode)
            {
                MockExamComprehensiveTrainingDto? mockExam = JsonSerializer.Deserialize<MockExamComprehensiveTrainingDto>(responseContent, JsonOptions);
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 成功快速开始模拟考试（综合训练格式），ID: {mockExam?.Id}");
                return mockExam;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 快速开始模拟考试（综合训练格式）失败");
                System.Diagnostics.Debug.WriteLine($"  状态码: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"  请求URL: {apiUrl}");
                System.Diagnostics.Debug.WriteLine($"  响应内容: {responseContent}");
                return null;
            }
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
    public async Task<MockExamSubmissionResponseDto?> SubmitMockExamAsync(int mockExamId, int? actualDurationSeconds = null)
    {
        try
        {
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}/submit");

            // 如果提供了实际用时，添加到查询参数中
            if (actualDurationSeconds.HasValue)
            {
                apiUrl += $"?actualDurationSeconds={actualDurationSeconds.Value}";
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 传递客户端实际用时: {actualDurationSeconds.Value}秒");
            }

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送提交模拟考试请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应内容: {responseContent}");

                MockExamSubmissionResponseDto? result = JsonSerializer.Deserialize<MockExamSubmissionResponseDto>(responseContent, JsonOptions);
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 提交模拟考试成功，时间状态: {result?.TimeStatusDescription}");
                return result;
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 提交模拟考试失败，错误内容: {errorContent}");

                // 尝试解析错误响应
                try
                {
                    MockExamSubmissionResponseDto? errorResult = JsonSerializer.Deserialize<MockExamSubmissionResponseDto>(errorContent, JsonOptions);
                    return errorResult;
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
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/{mockExamId}/score");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送提交模拟考试成绩请求到 {apiUrl}");

            string jsonContent = JsonSerializer.Serialize(scoreRequest, JsonOptions);
            StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");

            bool success = response.IsSuccessStatusCode;
            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 提交模拟考试成绩结果: {success}");

            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 成功提交模拟考试成绩，得分: {scoreRequest.Score}/{scoreRequest.MaxScore}");
            }
            else
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 提交成绩失败，响应内容: {responseContent}");
            }

            return success;
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
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"mock-exams/completions?pageNumber={pageNumber}&pageSize={pageSize}");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送获取模拟考试成绩列表请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                List<MockExamCompletionDto>? completions = JsonSerializer.Deserialize<List<MockExamCompletionDto>>(responseContent, JsonOptions);

                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 成功获取模拟考试成绩列表，数量: {completions?.Count ?? 0}");
                return completions ?? [];
            }
            else
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 获取成绩列表失败，响应内容: {responseContent}");
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
    /// 获取学生已完成的模拟考试数量
    /// </summary>
    public async Task<int> GetCompletedMockExamCountAsync()
    {
        try
        {
            // 设置认证头
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl("mock-exams/completed/count");

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 发送获取已完成模拟考试数量请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 响应状态码: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                int count = JsonSerializer.Deserialize<int>(responseContent, JsonOptions);
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 成功获取已完成模拟考试数量: {count}");
                return count;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"StudentMockExamService: 获取已完成模拟考试数量失败，状态码: {response.StatusCode}");
                return 0;
            }
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
        // 使用学生API端点，而不是认证端点
        // 模拟考试功能在 /api/student/mock-exams/ 路径下
        return $"{baseUrl}/student/{endpoint}";
    }
}