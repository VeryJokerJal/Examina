using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Examina.Models.SpecializedTraining;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// 学生端专项训练服务实现
/// </summary>
public class StudentSpecializedTrainingService : IStudentSpecializedTrainingService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<StudentSpecializedTrainingService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public StudentSpecializedTrainingService(
        HttpClient httpClient,
        IAuthenticationService authenticationService,
        ILogger<StudentSpecializedTrainingService> logger)
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
    /// 获取学生可访问的专项训练列表
    /// </summary>
    public async Task<List<StudentSpecializedTrainingDto>> GetAvailableTrainingsAsync(int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string url = $"/api/student/specialized-trainings?pageNumber={pageNumber}&pageSize={pageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                List<StudentSpecializedTrainingDto>? trainings = await response.Content.ReadFromJsonAsync<List<StudentSpecializedTrainingDto>>(_jsonOptions);
                return trainings ?? [];
            }

            _logger.LogWarning("获取专项训练列表失败，状态码: {StatusCode}", response.StatusCode);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取专项训练列表时发生异常");
            return [];
        }
    }

    /// <summary>
    /// 获取专项训练详情
    /// </summary>
    public async Task<StudentSpecializedTrainingDto?> GetTrainingDetailsAsync(int trainingId)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string url = $"/api/student/specialized-trainings/{trainingId}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                StudentSpecializedTrainingDto? training = await response.Content.ReadFromJsonAsync<StudentSpecializedTrainingDto>(_jsonOptions);
                return training;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("专项训练不存在或无权限访问，训练ID: {TrainingId}", trainingId);
                return null;
            }

            _logger.LogWarning("获取专项训练详情失败，训练ID: {TrainingId}, 状态码: {StatusCode}", trainingId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取专项训练详情时发生异常，训练ID: {TrainingId}", trainingId);
            return null;
        }
    }

    /// <summary>
    /// 检查是否有权限访问指定专项训练
    /// </summary>
    public async Task<bool> HasAccessToTrainingAsync(int trainingId)
    {
        try
        {
            StudentSpecializedTrainingDto? training = await GetTrainingDetailsAsync(trainingId);
            return training != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查专项训练访问权限时发生异常，训练ID: {TrainingId}", trainingId);
            return false;
        }
    }

    /// <summary>
    /// 获取学生可访问的专项训练总数
    /// </summary>
    public async Task<int> GetAvailableTrainingCountAsync()
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string url = "/api/student/specialized-trainings/count";
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                if (int.TryParse(content, out int count))
                {
                    return count;
                }
            }

            _logger.LogWarning("获取专项训练总数失败，状态码: {StatusCode}", response.StatusCode);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取专项训练总数时发生异常");
            return 0;
        }
    }

    /// <summary>
    /// 搜索专项训练
    /// </summary>
    public async Task<List<StudentSpecializedTrainingDto>> SearchTrainingsAsync(string searchKeyword, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string encodedKeyword = Uri.EscapeDataString(searchKeyword);
            string url = $"/api/student/specialized-trainings/search?keyword={encodedKeyword}&pageNumber={pageNumber}&pageSize={pageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                List<StudentSpecializedTrainingDto>? trainings = await response.Content.ReadFromJsonAsync<List<StudentSpecializedTrainingDto>>(_jsonOptions);
                return trainings ?? [];
            }

            _logger.LogWarning("搜索专项训练失败，关键词: {Keyword}, 状态码: {StatusCode}", searchKeyword, response.StatusCode);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索专项训练时发生异常，关键词: {Keyword}", searchKeyword);
            return [];
        }
    }

    /// <summary>
    /// 按模块类型筛选专项训练
    /// </summary>
    public async Task<List<StudentSpecializedTrainingDto>> GetTrainingsByModuleTypeAsync(string moduleType, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string encodedModuleType = Uri.EscapeDataString(moduleType);
            string url = $"/api/student/specialized-trainings/by-module-type?moduleType={encodedModuleType}&pageNumber={pageNumber}&pageSize={pageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                List<StudentSpecializedTrainingDto>? trainings = await response.Content.ReadFromJsonAsync<List<StudentSpecializedTrainingDto>>(_jsonOptions);
                return trainings ?? [];
            }

            _logger.LogWarning("按模块类型筛选专项训练失败，模块类型: {ModuleType}, 状态码: {StatusCode}", moduleType, response.StatusCode);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按模块类型筛选专项训练时发生异常，模块类型: {ModuleType}", moduleType);
            return [];
        }
    }

    /// <summary>
    /// 获取所有可用的模块类型列表
    /// </summary>
    public async Task<List<string>> GetAvailableModuleTypesAsync()
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string url = "/api/student/specialized-trainings/module-types";
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                List<string>? moduleTypes = await response.Content.ReadFromJsonAsync<List<string>>(_jsonOptions);
                return moduleTypes ?? [];
            }

            _logger.LogWarning("获取模块类型列表失败，状态码: {StatusCode}", response.StatusCode);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模块类型列表时发生异常");
            return [];
        }
    }



    /// <summary>
    /// 标记专项训练为开始状态
    /// </summary>
    public async Task<bool> StartSpecializedTrainingAsync(int trainingId)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string url = $"/api/student/specialized-trainings/{trainingId}/start";
            HttpResponseMessage response = await _httpClient.PostAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("专项训练开始成功，训练ID: {TrainingId}", trainingId);
                return true;
            }

            _logger.LogWarning("专项训练开始失败，训练ID: {TrainingId}, 状态码: {StatusCode}", trainingId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记专项训练开始时发生异常，训练ID: {TrainingId}", trainingId);
            return false;
        }
    }

    /// <summary>
    /// 提交专项训练成绩并标记为完成
    /// </summary>
    public async Task<bool> CompleteSpecializedTrainingAsync(int trainingId, decimal? score = null, decimal? maxScore = null, int? durationSeconds = null, string? notes = null)
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

            string url = $"/api/student/specialized-trainings/{trainingId}/complete";
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, requestData, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("专项训练完成成功，训练ID: {TrainingId}, 得分: {Score}", trainingId, score);
                return true;
            }

            _logger.LogWarning("专项训练完成失败，训练ID: {TrainingId}, 状态码: {StatusCode}", trainingId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交专项训练成绩时发生异常，训练ID: {TrainingId}", trainingId);
            return false;
        }
    }

    /// <summary>
    /// 获取专项训练进度统计
    /// </summary>
    public async Task<SpecializedTrainingProgressDto> GetTrainingProgressAsync()
    {
        try
        {
            await EnsureAuthenticatedAsync();

            string url = "/api/student/specialized-trainings/progress";
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                SpecializedTrainingProgressDto? progress = await response.Content.ReadFromJsonAsync<SpecializedTrainingProgressDto>(_jsonOptions);
                return progress ?? new SpecializedTrainingProgressDto();
            }

            _logger.LogWarning("获取专项训练进度统计失败，状态码: {StatusCode}", response.StatusCode);
            return new SpecializedTrainingProgressDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取专项训练进度统计时发生异常");
            return new SpecializedTrainingProgressDto();
        }
    }

    /// <summary>
    /// 确保用户已认证
    /// </summary>
    private async Task EnsureAuthenticatedAsync()
    {
        if (!_authenticationService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("用户未认证");
        }

        // 确保HTTP客户端包含认证头
        string? token = await _authenticationService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
}
