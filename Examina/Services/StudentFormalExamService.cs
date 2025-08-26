using System.Net.Http;
using System.Text;
using System.Text.Json;
using Examina.Models.Api;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// 学生正式考试服务实现
/// </summary>
public class StudentFormalExamService : IStudentFormalExamService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<StudentFormalExamService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public StudentFormalExamService(
        HttpClient httpClient,
        IAuthenticationService authenticationService,
        ILogger<StudentFormalExamService> logger)
    {
        _httpClient = httpClient;
        _authenticationService = authenticationService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// 设置认证头
    /// </summary>
    private async Task<bool> SetAuthenticationHeaderAsync()
    {
        try
        {
            string? token = await _authenticationService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                System.Diagnostics.Debug.WriteLine("StudentFormalExamService: 已设置JWT认证头");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("StudentFormalExamService: 警告 - 无法获取访问令牌，可能认证尚未完成");
                _httpClient.DefaultRequestHeaders.Authorization = null;
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 设置认证头异常: {ex.Message}");
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return false;
        }
    }

    /// <summary>
    /// 构建API URL
    /// </summary>
    private string BuildApiUrl(string endpoint)
    {
        return $"/api/student/exams/{endpoint}";
    }

    /// <summary>
    /// 开始正式考试
    /// </summary>
    public async Task<bool> StartExamAsync(int examId)
    {
        try
        {
            bool authSuccess = await SetAuthenticationHeaderAsync();
            if (!authSuccess)
            {
                System.Diagnostics.Debug.WriteLine("StudentFormalExamService: 认证失败，无法开始考试");
                return false;
            }

            string apiUrl = BuildApiUrl($"{examId}/start");
            System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 发送开始正式考试请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);

            bool success = response.IsSuccessStatusCode;
            System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 开始正式考试结果: {success}, 考试ID: {examId}");

            if (!success)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 开始考试失败，响应内容: {responseContent}");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始正式考试异常，考试ID: {ExamId}", examId);
            System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 开始正式考试异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 提交正式考试成绩
    /// </summary>
    public async Task<bool> SubmitExamScoreAsync(int examId, SubmitExamScoreRequestDto scoreRequest)
    {
        try
        {
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"{examId}/score");
            System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 发送提交正式考试成绩请求到 {apiUrl}");

            string jsonContent = JsonSerializer.Serialize(scoreRequest, _jsonOptions);
            StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

            bool success = response.IsSuccessStatusCode;
            System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 提交正式考试成绩结果: {success}");

            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 成功提交正式考试成绩，得分: {scoreRequest.Score}/{scoreRequest.MaxScore}");
            }
            else
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 提交成绩失败，响应内容: {responseContent}");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交正式考试成绩异常，考试ID: {ExamId}", examId);
            System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 提交正式考试成绩异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 完成正式考试（不包含成绩）
    /// </summary>
    public async Task<bool> CompleteExamAsync(int examId)
    {
        try
        {
            await SetAuthenticationHeaderAsync();

            string apiUrl = BuildApiUrl($"{examId}/complete");
            System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 发送完成正式考试请求到 {apiUrl}");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, null);

            bool success = response.IsSuccessStatusCode;
            System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 完成正式考试结果: {success}, 考试ID: {examId}");

            if (!success)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 完成考试失败，响应内容: {responseContent}");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成正式考试异常，考试ID: {ExamId}", examId);
            System.Diagnostics.Debug.WriteLine($"StudentFormalExamService: 完成正式考试异常: {ex.Message}");
            return false;
        }
    }
}
