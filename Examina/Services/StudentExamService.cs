using System.Net.Http.Headers;
using System.Text.Json;
using Examina.Models.Exam;

namespace Examina.Services;

/// <summary>
/// 学生端考试服务实现
/// </summary>
public class StudentExamService : IStudentExamService
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

    public StudentExamService(
        HttpClient httpClient,
        IAuthenticationService authenticationService,
        IConfigurationService configurationService)
    {
        _httpClient = httpClient;
        _authenticationService = authenticationService;
        _configurationService = configurationService;
    }

    /// <summary>
    /// 获取学生可访问的考试列表
    /// </summary>
    public async Task<List<StudentExamDto>> GetAvailableExamsAsync(int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/exams?pageNumber={pageNumber}&pageSize={pageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                List<StudentExamDto>? exams = JsonSerializer.Deserialize<List<StudentExamDto>>(content, JsonOptions);
                return exams ?? [];
            }

            System.Diagnostics.Debug.WriteLine($"获取考试列表失败: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取考试列表异常: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// 获取考试详情
    /// </summary>
    public async Task<StudentExamDto?> GetExamDetailsAsync(int examId)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/exams/{examId}";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<StudentExamDto>(content, JsonOptions);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                System.Diagnostics.Debug.WriteLine($"考试不存在或无权限访问: {examId}");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"获取考试详情失败: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取考试详情异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 检查是否有权限访问指定考试
    /// </summary>
    public async Task<bool> HasAccessToExamAsync(int examId)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/exams/{examId}/access";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<bool>(content, JsonOptions);
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"检查考试访问权限异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取学生可访问的考试总数
    /// </summary>
    public async Task<int> GetAvailableExamCountAsync()
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = "/api/student/exams/count";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<int>(content, JsonOptions);
            }

            return 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取考试总数异常: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 确保用户已认证并设置Authorization头
    /// </summary>
    private async Task EnsureAuthenticatedAsync()
    {
        if (!_authenticationService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("用户未认证");
        }

        // 获取当前访问令牌
        string? accessToken = await _authenticationService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new UnauthorizedAccessException("无法获取访问令牌");
        }

        // 设置Authorization头
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}

/// <summary>
/// 学生端综合训练服务实现
/// </summary>
public class StudentComprehensiveTrainingService : IStudentComprehensiveTrainingService
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

    public StudentComprehensiveTrainingService(
        HttpClient httpClient,
        IAuthenticationService authenticationService,
        IConfigurationService configurationService)
    {
        _httpClient = httpClient;
        _authenticationService = authenticationService;
        _configurationService = configurationService;
    }

    /// <summary>
    /// 获取学生可访问的综合训练列表
    /// </summary>
    public async Task<List<StudentComprehensiveTrainingDto>> GetAvailableTrainingsAsync(int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/comprehensive-trainings?pageNumber={pageNumber}&pageSize={pageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                List<StudentComprehensiveTrainingDto>? trainings = JsonSerializer.Deserialize<List<StudentComprehensiveTrainingDto>>(content, JsonOptions);
                return trainings ?? [];
            }

            System.Diagnostics.Debug.WriteLine($"获取综合训练列表失败: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取综合训练列表异常: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// 获取综合训练详情
    /// </summary>
    public async Task<StudentComprehensiveTrainingDto?> GetTrainingDetailsAsync(int trainingId)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/comprehensive-trainings/{trainingId}";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<StudentComprehensiveTrainingDto>(content, JsonOptions);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                System.Diagnostics.Debug.WriteLine($"综合训练不存在或无权限访问: {trainingId}");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"获取综合训练详情失败: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取综合训练详情异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 检查是否有权限访问指定综合训练
    /// </summary>
    public async Task<bool> HasAccessToTrainingAsync(int trainingId)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/comprehensive-trainings/{trainingId}/access";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<bool>(content, JsonOptions);
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"检查综合训练访问权限异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取学生可访问的综合训练总数
    /// </summary>
    public async Task<int> GetAvailableTrainingCountAsync()
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = "/api/student/comprehensive-trainings/count";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<int>(content, JsonOptions);
            }

            return 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取综合训练总数异常: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 获取学生综合训练进度统计
    /// </summary>
    public async Task<ComprehensiveTrainingProgressDto> GetTrainingProgressAsync()
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = "/api/student/comprehensive-trainings/progress";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                ComprehensiveTrainingProgressDto? progress = JsonSerializer.Deserialize<ComprehensiveTrainingProgressDto>(content, JsonOptions);
                return progress ?? new ComprehensiveTrainingProgressDto();
            }

            System.Diagnostics.Debug.WriteLine($"获取综合训练进度失败，状态码: {response.StatusCode}");
            return new ComprehensiveTrainingProgressDto();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取综合训练进度异常: {ex.Message}");
            return new ComprehensiveTrainingProgressDto();
        }
    }

    /// <summary>
    /// 确保用户已认证并设置Authorization头
    /// </summary>
    private async Task EnsureAuthenticatedAsync()
    {
        if (!_authenticationService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("用户未认证");
        }

        // 获取当前访问令牌
        string? accessToken = await _authenticationService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new UnauthorizedAccessException("无法获取访问令牌");
        }

        // 设置Authorization头
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
