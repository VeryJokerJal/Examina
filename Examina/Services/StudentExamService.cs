using System.Net.Http.Headers;
using Examina.Models;
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
            bool authSuccess = await EnsureAuthenticatedAsync();
            if (!authSuccess)
            {
                return [];
            }

            string endpoint = $"/api/student/exams?pageNumber={pageNumber}&pageSize={pageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                List<StudentExamDto>? exams = JsonSerializer.Deserialize<List<StudentExamDto>>(content, JsonOptions);
                return exams ?? [];
            }

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
            _ = await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/exams/{examId}";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<StudentExamDto>(content, JsonOptions);
            }

            return response.StatusCode == System.Net.HttpStatusCode.NotFound ? null : null;
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
            _ = await EnsureAuthenticatedAsync();

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
            _ = await EnsureAuthenticatedAsync();

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
    /// 按考试类型获取学生可访问的考试列表
    /// </summary>
    public async Task<List<StudentExamDto>> GetAvailableExamsByCategoryAsync(ExamCategory examCategory, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            _ = await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/exams/category/{(int)examCategory}?pageNumber={pageNumber}&pageSize={pageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                List<StudentExamDto>? exams = JsonSerializer.Deserialize<List<StudentExamDto>>(content, JsonOptions);
                return exams ?? [];
            }

            return [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StudentExamService] 按类型获取考试列表异常: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[StudentExamService] 异常堆栈: {ex.StackTrace}");
            return [];
        }
    }

    /// <summary>
    /// 按考试类型获取学生可访问的考试总数
    /// </summary>
    public async Task<int> GetAvailableExamCountByCategoryAsync(ExamCategory examCategory)
    {
        try
        {
            _ = await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/exams/category/{(int)examCategory}/count";
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
            System.Diagnostics.Debug.WriteLine($"按类型获取考试总数异常: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 确保用户已认证并设置Authorization头
    /// </summary>
    private async Task<bool> EnsureAuthenticatedAsync()
    {
        string? accessToken = await _authenticationService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            return false;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return true;
    }

    /// <summary>
    /// 获取学生专项练习进度统计
    /// </summary>
    public async Task<SpecialPracticeProgressDto> GetSpecialPracticeProgressAsync()
    {
        try
        {
            _ = await EnsureAuthenticatedAsync();

            string endpoint = "/api/student/special-practices/progress";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                SpecialPracticeProgressDto? progress = JsonSerializer.Deserialize<SpecialPracticeProgressDto>(content, JsonOptions);
                return progress ?? new SpecialPracticeProgressDto();
            }

            return new SpecialPracticeProgressDto();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取专项练习进度异常: {ex.Message}");
            return new SpecialPracticeProgressDto();
        }
    }

    /// <summary>
    /// 获取学生可访问的专项练习总数
    /// </summary>
    public async Task<int> GetAvailableSpecialPracticeCountAsync()
    {
        try
        {
            _ = await EnsureAuthenticatedAsync();

            string endpoint = "/api/student/special-practices/count";
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
            System.Diagnostics.Debug.WriteLine($"获取专项练习总数异常: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 标记专项练习为开始状态
    /// </summary>
    public async Task<bool> StartSpecialPracticeAsync(int practiceId)
    {
        try
        {
            _ = await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/special-practices/{practiceId}/start";
            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, null);

            bool success = response.IsSuccessStatusCode;
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"标记专项练习开始异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 提交专项练习成绩并标记为完成
    /// </summary>
    public async Task<bool> CompleteSpecialPracticeAsync(int practiceId, CompletePracticeRequest request)
    {
        try
        {
            _ = await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/special-practices/{practiceId}/complete";
            string jsonContent = JsonSerializer.Serialize(request, JsonOptions);
            StringContent content = new(jsonContent, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);

            bool success = response.IsSuccessStatusCode;
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"提交专项练习成绩异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取专项练习完成记录
    /// </summary>
    public async Task<List<SpecialPracticeCompletionDto>> GetSpecialPracticeCompletionsAsync(int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _ = await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/special-practices/completions?pageNumber={pageNumber}&pageSize={pageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                List<SpecialPracticeCompletionDto>? completions = JsonSerializer.Deserialize<List<SpecialPracticeCompletionDto>>(content, JsonOptions);
                return completions ?? [];
            }

            return [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取专项练习完成记录异常: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            return [];
        }
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

            return response.StatusCode == System.Net.HttpStatusCode.NotFound ? null : null;
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

            return new ComprehensiveTrainingProgressDto();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取综合训练进度异常: {ex.Message}");
            return new ComprehensiveTrainingProgressDto();
        }
    }

    /// <summary>
    /// 获取学生专项练习进度统计
    /// </summary>
    public async Task<SpecialPracticeProgressDto> GetSpecialPracticeProgressAsync()
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = "/api/student/special-practices/progress";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                SpecialPracticeProgressDto? progress = JsonSerializer.Deserialize<SpecialPracticeProgressDto>(content, JsonOptions);
                return progress ?? new SpecialPracticeProgressDto();
            }

            return new SpecialPracticeProgressDto();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取专项练习进度异常: {ex.Message}");
            return new SpecialPracticeProgressDto();
        }
    }

    /// <summary>
    /// 获取学生可访问的专项练习总数
    /// </summary>
    public async Task<int> GetAvailableSpecialPracticeCountAsync()
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = "/api/student/special-practices/count";
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
            System.Diagnostics.Debug.WriteLine($"获取专项练习总数异常: {ex.Message}");
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

        string? accessToken = await _authenticationService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new UnauthorizedAccessException("无法获取访问令牌");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    /// <summary>
    /// 标记综合训练为开始状态
    /// </summary>
    public async Task<bool> StartComprehensiveTrainingAsync(int trainingId)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/comprehensive-trainings/{trainingId}/start";
            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, null);

            bool success = response.IsSuccessStatusCode;
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"标记综合训练开始异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 提交综合训练成绩并标记为完成
    /// </summary>
    public async Task<bool> CompleteComprehensiveTrainingAsync(int trainingId, CompleteTrainingRequest request)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/comprehensive-trainings/{trainingId}/complete";
            string jsonContent = JsonSerializer.Serialize(request, JsonOptions);
            StringContent content = new(jsonContent, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);

            bool success = response.IsSuccessStatusCode;
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"提交综合训练成绩异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 标记综合训练为已完成
    /// </summary>
    public async Task<bool> MarkTrainingAsCompletedAsync(int trainingId, double? score = null, double? maxScore = null, int? durationSeconds = null, string? notes = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            object requestData = new
            {
                score,
                maxScore,
                durationSeconds,
                notes
            };

            string endpoint = $"/api/student/comprehensive-trainings/{trainingId}/mark-completed";
            string jsonContent = JsonSerializer.Serialize(requestData, JsonOptions);
            StringContent content = new(jsonContent, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);

            bool success = response.IsSuccessStatusCode;
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"标记综合训练为已完成异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取综合训练完成记录
    /// </summary>
    public async Task<List<ComprehensiveTrainingCompletionDto>> GetComprehensiveTrainingCompletionsAsync(int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string endpoint = $"/api/student/comprehensive-trainings/completions?pageNumber={pageNumber}&pageSize={pageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                List<ComprehensiveTrainingCompletionDto>? completions = JsonSerializer.Deserialize<List<ComprehensiveTrainingCompletionDto>>(content, JsonOptions);
                return completions ?? [];
            }

            return [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取综合训练完成记录异常: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            return [];
        }
    }
}
