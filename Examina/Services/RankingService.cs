using System.Net;
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

            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized)
            {
                string jsonContent = await response.Content.ReadAsStringAsync();

                // 对于401状态码，尝试解析响应内容，如果解析失败则返回空的排行榜数据
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogInformation("获取上机统考排行榜时遇到401状态码，尝试返回空数据或解析可用数据");
                    try
                    {
                        RankingResponseDto? ranking = JsonSerializer.Deserialize<RankingResponseDto>(jsonContent, _jsonOptions);
                        if (ranking != null)
                        {
                            _logger.LogInformation("成功解析401响应中的排行榜数据，记录数: {Count}", ranking.Entries.Count);
                            return ranking;
                        }
                    }
                    catch (JsonException)
                    {
                        _logger.LogInformation("401响应无法解析为排行榜数据，返回空的排行榜");
                    }

                    // 返回空的排行榜数据而不是null，允许UI显示空状态
                    return new RankingResponseDto
                    {
                        Type = RankingType.ExamRanking,
                        TypeName = "上机统考排行榜",
                        Entries = [],
                        TotalCount = 0,
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalPages = 0
                    };
                }

                RankingResponseDto? successRanking = JsonSerializer.Deserialize<RankingResponseDto>(jsonContent, _jsonOptions);
                _logger.LogInformation("成功获取上机统考排行榜，记录数: {Count}", successRanking?.Entries.Count ?? 0);
                return successRanking;
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

            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized)
            {
                string jsonContent = await response.Content.ReadAsStringAsync();

                // 对于401状态码，尝试解析响应内容，如果解析失败则返回空的排行榜数据
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogInformation("获取模拟考试排行榜时遇到401状态码，尝试返回空数据或解析可用数据");
                    try
                    {
                        RankingResponseDto? ranking = JsonSerializer.Deserialize<RankingResponseDto>(jsonContent, _jsonOptions);
                        if (ranking != null)
                        {
                            _logger.LogInformation("成功解析401响应中的排行榜数据，记录数: {Count}", ranking.Entries.Count);
                            return ranking;
                        }
                    }
                    catch (JsonException)
                    {
                        _logger.LogInformation("401响应无法解析为排行榜数据，返回空的排行榜");
                    }

                    // 返回空的排行榜数据而不是null，允许UI显示空状态
                    return new RankingResponseDto
                    {
                        Type = RankingType.MockExamRanking,
                        TypeName = "模拟考试排行榜",
                        Entries = [],
                        TotalCount = 0,
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalPages = 0
                    };
                }

                RankingResponseDto? successRanking = JsonSerializer.Deserialize<RankingResponseDto>(jsonContent, _jsonOptions);
                _logger.LogInformation("成功获取模拟考试排行榜，记录数: {Count}", successRanking?.Entries.Count ?? 0);
                return successRanking;
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

            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized)
            {
                string jsonContent = await response.Content.ReadAsStringAsync();

                // 对于401状态码，尝试解析响应内容，如果解析失败则返回空的排行榜数据
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogInformation("获取综合实训排行榜时遇到401状态码，尝试返回空数据或解析可用数据");
                    try
                    {
                        RankingResponseDto? ranking = JsonSerializer.Deserialize<RankingResponseDto>(jsonContent, _jsonOptions);
                        if (ranking != null)
                        {
                            _logger.LogInformation("成功解析401响应中的排行榜数据，记录数: {Count}", ranking.Entries.Count);
                            return ranking;
                        }
                    }
                    catch (JsonException)
                    {
                        _logger.LogInformation("401响应无法解析为排行榜数据，返回空的排行榜");
                    }

                    // 返回空的排行榜数据而不是null，允许UI显示空状态
                    return new RankingResponseDto
                    {
                        Type = RankingType.TrainingRanking,
                        TypeName = "综合实训排行榜",
                        Entries = [],
                        TotalCount = 0,
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalPages = 0
                    };
                }

                RankingResponseDto? successRanking = JsonSerializer.Deserialize<RankingResponseDto>(jsonContent, _jsonOptions);
                _logger.LogInformation("成功获取综合实训排行榜，记录数: {Count}", successRanking?.Entries.Count ?? 0);
                return successRanking;
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

            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized)
            {
                string jsonContent = await response.Content.ReadAsStringAsync();

                // 对于401状态码，尝试解析响应内容，如果解析失败则返回空的排行榜数据
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogInformation("获取排行榜数据时遇到401状态码，类型: {Type}, 尝试返回空数据或解析可用数据", type);
                    try
                    {
                        RankingResponseDto? ranking = JsonSerializer.Deserialize<RankingResponseDto>(jsonContent, _jsonOptions);
                        if (ranking != null)
                        {
                            _logger.LogInformation("成功解析401响应中的排行榜数据，记录数: {Count}", ranking.Entries?.Count ?? 0);
                            return ranking;
                        }
                    }
                    catch (JsonException)
                    {
                        _logger.LogInformation("401响应无法解析为排行榜数据，返回空的排行榜");
                    }

                    // 返回空的排行榜数据而不是null，允许UI显示空状态
                    string typeName = type switch
                    {
                        RankingType.ExamRanking => "上机统考排行榜",
                        RankingType.MockExamRanking => "模拟考试排行榜",
                        RankingType.TrainingRanking => "综合实训排行榜",
                        _ => "排行榜"
                    };

                    return new RankingResponseDto
                    {
                        Type = type,
                        TypeName = typeName,
                        Entries = [],
                        TotalCount = 0,
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalPages = 0
                    };
                }

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
