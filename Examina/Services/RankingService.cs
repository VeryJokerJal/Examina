using System.Net.Http;
using System.Text.Json;
using Examina.Models.Ranking;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// 排行榜服务
/// </summary>
public class RankingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RankingService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RankingService(HttpClient httpClient, ILogger<RankingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// 获取上机统考排行榜
    /// </summary>
    public async Task<RankingResponseDto?> GetExamRankingAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("获取上机统考排行榜，页码: {Page}, 每页: {PageSize}", page, pageSize);

            string endpoint = $"/api/ranking/exam?page={page}&pageSize={pageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string jsonContent = await response.Content.ReadAsStringAsync();
                RankingResponseDto? ranking = JsonSerializer.Deserialize<RankingResponseDto>(jsonContent, _jsonOptions);
                
                _logger.LogInformation("成功获取上机统考排行榜，记录数: {Count}", ranking?.Entries.Count ?? 0);
                return ranking;
            }
            else
            {
                _logger.LogWarning("获取上机统考排行榜失败，状态码: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上机统考排行榜时发生异常");
            return null;
        }
    }

    /// <summary>
    /// 获取模拟考试排行榜
    /// </summary>
    public async Task<RankingResponseDto?> GetMockExamRankingAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("获取模拟考试排行榜，页码: {Page}, 每页: {PageSize}", page, pageSize);

            string endpoint = $"/api/ranking/mock-exam?page={page}&pageSize={pageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string jsonContent = await response.Content.ReadAsStringAsync();
                RankingResponseDto? ranking = JsonSerializer.Deserialize<RankingResponseDto>(jsonContent, _jsonOptions);
                
                _logger.LogInformation("成功获取模拟考试排行榜，记录数: {Count}", ranking?.Entries.Count ?? 0);
                return ranking;
            }
            else
            {
                _logger.LogWarning("获取模拟考试排行榜失败，状态码: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模拟考试排行榜时发生异常");
            return null;
        }
    }

    /// <summary>
    /// 获取综合实训排行榜
    /// </summary>
    public async Task<RankingResponseDto?> GetTrainingRankingAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("获取综合实训排行榜，页码: {Page}, 每页: {PageSize}", page, pageSize);

            string endpoint = $"/api/ranking/training?page={page}&pageSize={pageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string jsonContent = await response.Content.ReadAsStringAsync();
                RankingResponseDto? ranking = JsonSerializer.Deserialize<RankingResponseDto>(jsonContent, _jsonOptions);
                
                _logger.LogInformation("成功获取综合实训排行榜，记录数: {Count}", ranking?.Entries.Count ?? 0);
                return ranking;
            }
            else
            {
                _logger.LogWarning("获取综合实训排行榜失败，状态码: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取综合实训排行榜时发生异常");
            return null;
        }
    }

    /// <summary>
    /// 根据排行榜类型获取排行榜数据
    /// </summary>
    public async Task<RankingResponseDto?> GetRankingByTypeAsync(RankingType type, int page = 1, int pageSize = 50)
    {
        return type switch
        {
            RankingType.ExamRanking => await GetExamRankingAsync(page, pageSize),
            RankingType.MockExamRanking => await GetMockExamRankingAsync(page, pageSize),
            RankingType.TrainingRanking => await GetTrainingRankingAsync(page, pageSize),
            _ => null
        };
    }

    /// <summary>
    /// 根据排行榜类型和试卷筛选获取排行榜数据
    /// </summary>
    public async Task<RankingResponseDto?> GetRankingByTypeAsync(RankingType type, int? examId = null, int page = 1, int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("获取排行榜数据，类型: {Type}, 试卷ID: {ExamId}, 页码: {Page}", type, examId, page);

            // 构建查询参数
            string queryParams = $"?page={page}&pageSize={pageSize}";
            if (examId.HasValue)
            {
                queryParams += $"&examId={examId.Value}";
            }

            string endpoint = type switch
            {
                RankingType.ExamRanking => $"/api/ranking/exam{queryParams}",
                RankingType.MockExamRanking => $"/api/ranking/mock-exam{queryParams}",
                RankingType.TrainingRanking => $"/api/ranking/training{queryParams}",
                _ => throw new ArgumentException($"不支持的排行榜类型: {type}")
            };

            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string jsonContent = await response.Content.ReadAsStringAsync();
                RankingResponseDto? result = JsonSerializer.Deserialize<RankingResponseDto>(jsonContent, _jsonOptions);

                _logger.LogInformation("成功获取排行榜数据，记录数: {Count}", result?.Entries?.Count ?? 0);
                return result;
            }
            else
            {
                _logger.LogWarning("获取排行榜数据失败，状态码: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取排行榜数据时发生异常，类型: {Type}, 试卷ID: {ExamId}", type, examId);
            return null;
        }
    }
}
