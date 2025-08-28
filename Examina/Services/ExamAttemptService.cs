using Examina.Models;
using Examina.Models.Exam;

namespace Examina.Services;

/// <summary>
/// 考试尝试服务实现
/// </summary>
public class ExamAttemptService : IExamAttemptService
{
    private readonly IStudentExamService _studentExamService;
    private readonly IConfigurationService _configurationService;
    private readonly IAuthenticationService _authenticationService;
    private readonly HttpClient _httpClient;
    private readonly List<ExamAttemptDto> _examAttempts; // 本地缓存，用于临时存储
    private int _nextAttemptId = 1;

    /// <summary>
    /// JSON序列化选项
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExamAttemptService(
        IStudentExamService studentExamService,
        IConfigurationService configurationService,
        IAuthenticationService authenticationService,
        HttpClient httpClient)
    {
        _studentExamService = studentExamService;
        _configurationService = configurationService;
        _authenticationService = authenticationService;
        _httpClient = httpClient;
        _examAttempts = [];
    }

    /// <summary>
    /// 检查学生是否可以开始考试
    /// </summary>
    public async Task<ExamAttemptLimitDto> CheckExamAttemptLimitAsync(int examId, int studentId)
    {
        try
        {
            // 获取考试详情
            StudentExamDto? exam = await _studentExamService.GetExamDetailsAsync(examId);
            if (exam == null)
            {
                return new ExamAttemptLimitDto
                {
                    ExamId = examId,
                    StudentId = studentId,
                    CanStartExam = false,
                    CanRetake = false,
                    CanPractice = false,
                    LimitReason = "考试不存在或无权限访问"
                };
            }

            // 获取学生的考试尝试历史
            List<ExamAttemptDto> attempts = await GetExamAttemptHistoryAsync(examId, studentId);

            // 统计各类型尝试次数
            int totalAttempts = attempts.Count;
            int retakeAttempts = attempts.Count(a => a.AttemptType == ExamAttemptType.Retake);
            int practiceAttempts = attempts.Count(a => a.AttemptType == ExamAttemptType.Practice);
            ExamAttemptDto? lastAttempt = attempts.OrderByDescending(a => a.StartedAt).FirstOrDefault();

            // 检查是否有进行中的考试
            bool hasActiveAttempt = attempts.Any(a => a.Status == ExamAttemptStatus.InProgress);

            // 判断是否可以开始考试
            bool canStartExam = !hasActiveAttempt;
            bool canRetake = false;
            bool canPractice = false;
            string? limitReason = null;

            if (hasActiveAttempt)
            {
                canStartExam = false;
                limitReason = "有正在进行的考试，请先完成当前考试";
            }
            else
            {
                // 是否已完成首次考试
                bool hasCompletedFirstAttempt = attempts.Any(a =>
                    a.AttemptType == ExamAttemptType.FirstAttempt &&
                    a.Status == ExamAttemptStatus.Completed);

                // 重考权限
                if (exam.AllowRetake && retakeAttempts < exam.MaxRetakeCount)
                {
                    canRetake = hasCompletedFirstAttempt;
                }

                // 练习权限
                if (exam.AllowPractice)
                {
                    canPractice = hasCompletedFirstAttempt;
                }

                // 首次考试未完成 -> 可开始首次考试
                if (!hasCompletedFirstAttempt)
                {
                    canStartExam = true;
                }
                else
                {
                    // 已完成首次考试 -> 看是否还能重考或练习
                    canStartExam = canRetake || canPractice;

                    if (!canStartExam)
                    {
                        if (!exam.AllowRetake && !exam.AllowPractice)
                        {
                            limitReason = "考试不允许重考和重做练习";
                        }
                        else if (exam.AllowRetake && retakeAttempts >= exam.MaxRetakeCount)
                        {
                            limitReason = $"重考次数已达上限 ({exam.MaxRetakeCount}次)";
                        }
                        else if (!exam.AllowPractice)
                        {
                            limitReason = "考试不允许练习模式";
                        }
                    }
                }
            }

            ExamAttemptLimitDto result = new()
            {
                ExamId = examId,
                StudentId = studentId,
                CanStartExam = canStartExam,
                CanRetake = canRetake,
                CanPractice = canPractice,
                TotalAttempts = totalAttempts,
                RetakeAttempts = retakeAttempts,
                PracticeAttempts = practiceAttempts,
                MaxRetakeCount = exam.MaxRetakeCount,
                AllowRetake = exam.AllowRetake,
                AllowPractice = exam.AllowPractice,
                LimitReason = limitReason,
                LastAttempt = lastAttempt
            };

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== CheckExamAttemptLimitAsync 异常 ===");
            System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"异常消息: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            System.Diagnostics.Debug.WriteLine($"=== CheckExamAttemptLimitAsync 异常结束 ===");

            return new ExamAttemptLimitDto
            {
                ExamId = examId,
                StudentId = studentId,
                CanStartExam = false,
                CanRetake = false,
                CanPractice = false,
                LimitReason = $"检查考试权限时发生错误: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 开始考试尝试
    /// </summary>
    public async Task<ExamAttemptDto?> StartExamAttemptAsync(int examId, int studentId, ExamAttemptType attemptType)
    {
        try
        {
            // 验证权限
            (bool isValid, string? errorMessage) = await ValidateExamAttemptPermissionAsync(examId, studentId, attemptType);
            if (!isValid)
            {
                return null;
            }

            // 获取下一个尝试编号
            List<ExamAttemptDto> existingAttempts = await GetExamAttemptHistoryAsync(examId, studentId);
            int nextAttemptNumber = existingAttempts.Count + 1;

            // 创建新的考试尝试记录
            ExamAttemptDto attempt = new()
            {
                Id = _nextAttemptId++,
                ExamId = examId,
                StudentId = studentId,
                AttemptNumber = nextAttemptNumber,
                AttemptType = attemptType,
                Status = ExamAttemptStatus.InProgress,
                StartedAt = DateTime.Now,
                IsRanked = attemptType != ExamAttemptType.Practice
            };

            _examAttempts.Add(attempt);

            return attempt;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StartExamAttemptAsync 异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 完成考试尝试
    /// </summary>
    public async Task<bool> CompleteExamAttemptAsync(int attemptId, double? score = null, double? maxScore = null, int? durationSeconds = null, string? notes = null)
    {
        try
        {
            ExamAttemptDto? attempt = await GetExamAttemptDetailsAsync(attemptId);
            if (attempt == null || attempt.Status != ExamAttemptStatus.InProgress)
            {
                return false;
            }

            attempt.Status = ExamAttemptStatus.Completed;
            attempt.CompletedAt = DateTime.Now;
            attempt.Score = score;
            attempt.MaxScore = maxScore;
            attempt.DurationSeconds = durationSeconds;
            attempt.Notes = notes;

            // 练习模式的考试尝试仅在本地记录，不提交到API
            if (attempt.AttemptType == ExamAttemptType.Practice)
            {
                System.Diagnostics.Debug.WriteLine($"CompleteExamAttemptAsync: 练习模式考试尝试完成，仅本地记录，不提交到API");
                return true;
            }

            // 正式考试和重考需要提交到API（这里可以添加API提交逻辑）
            System.Diagnostics.Debug.WriteLine($"CompleteExamAttemptAsync: 正式考试尝试完成，类型: {attempt.AttemptType}");

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CompleteExamAttemptAsync 异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 放弃考试尝试
    /// </summary>
    public async Task<bool> AbandonExamAttemptAsync(int attemptId, string? reason = null)
    {
        try
        {
            ExamAttemptDto? attempt = await GetExamAttemptDetailsAsync(attemptId);
            if (attempt == null || attempt.Status != ExamAttemptStatus.InProgress)
            {
                return false;
            }

            attempt.Status = ExamAttemptStatus.Abandoned;
            attempt.CompletedAt = DateTime.Now;
            attempt.Notes = reason;

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AbandonExamAttemptAsync 异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 标记考试尝试为超时
    /// </summary>
    public async Task<bool> TimeoutExamAttemptAsync(int attemptId, double? score = null, double? maxScore = null, int? durationSeconds = null)
    {
        try
        {
            ExamAttemptDto? attempt = await GetExamAttemptDetailsAsync(attemptId);
            if (attempt == null || attempt.Status != ExamAttemptStatus.InProgress)
            {
                return false;
            }

            attempt.Status = ExamAttemptStatus.TimedOut;
            attempt.CompletedAt = DateTime.Now;
            attempt.Score = score;
            attempt.MaxScore = maxScore;
            attempt.DurationSeconds = durationSeconds;
            attempt.Notes = "考试超时自动提交";

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TimeoutExamAttemptAsync 异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取学生的考试尝试历史
    /// </summary>
    public async Task<List<ExamAttemptDto>> GetExamAttemptHistoryAsync(int examId, int studentId)
    {
        try
        {
            // 从后端API获取考试完成记录
            List<ExamCompletion> completions = await GetExamCompletionsFromApiAsync(examId);

            // 将ExamCompletion转换为ExamAttemptDto，并根据完成顺序推断考试类型
            List<ExamCompletion> sortedCompletions = [.. completions
                .Where(c => c.ExamId == examId && c.StudentUserId == studentId)
                .OrderBy(c => c.StartedAt ?? c.CreatedAt)];

            List<ExamAttemptDto> attempts = new List<ExamAttemptDto>();
            for (int i = 0; i < sortedCompletions.Count; i++)
            {
                ExamCompletion completion = sortedCompletions[i];

                // 根据完成顺序推断考试类型
                ExamAttemptType attemptType = i == 0 ? ExamAttemptType.FirstAttempt : ExamAttemptType.Retake;

                ExamAttemptDto attempt = new ExamAttemptDto
                {
                    Id = completion.Id,
                    ExamId = completion.ExamId,
                    StudentId = completion.StudentUserId,
                    AttemptNumber = i + 1,
                    AttemptType = attemptType,
                    Status = MapCompletionStatusToAttemptStatus(completion.Status),
                    StartedAt = completion.StartedAt ?? completion.CreatedAt,
                    CompletedAt = completion.CompletedAt,
                    Score = completion.Score,
                    MaxScore = completion.MaxScore,
                    DurationSeconds = completion.DurationSeconds,
                    Notes = completion.Notes,
                    IsRanked = true
                };

                attempts.Add(attempt);
            }

            // 合并本地缓存的数据（用于正在进行的考试）
            List<ExamAttemptDto> localAttempts = [.. _examAttempts.Where(a => a.ExamId == examId && a.StudentId == studentId)];

            attempts.AddRange(localAttempts);

            return [.. attempts.OrderBy(a => a.StartedAt)];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== GetExamAttemptHistoryAsync 异常 ===");
            System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"异常消息: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"回退到本地缓存数据...");

            // 如果API调用失败，返回本地缓存的数据
            List<ExamAttemptDto> fallbackResult = [.. _examAttempts
                .Where(a => a.ExamId == examId && a.StudentId == studentId)
                .OrderBy(a => a.StartedAt)];

            System.Diagnostics.Debug.WriteLine($"本地缓存返回 {fallbackResult.Count} 条记录");
            System.Diagnostics.Debug.WriteLine($"=== GetExamAttemptHistoryAsync 异常结束 ===");

            return fallbackResult;
        }
    }

    /// <summary>
    /// 获取学生的所有考试尝试历史
    /// </summary>
    public async Task<List<ExamAttemptDto>> GetStudentExamAttemptHistoryAsync(int studentId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            // 从后端API获取所有考试完成记录
            List<ExamCompletion> completions = await GetExamCompletionsFromApiAsync();

            // 将ExamCompletion转换为ExamAttemptDto，并根据完成顺序推断考试类型
            List<ExamCompletion> sortedCompletions = [.. completions
                .Where(c => c.StudentUserId == studentId)
                .OrderBy(c => c.StartedAt ?? c.CreatedAt)];

            List<ExamAttemptDto> attempts = new List<ExamAttemptDto>();

            // 按考试ID分组，为每个考试单独计算尝试次数
            var examGroups = sortedCompletions.GroupBy(c => c.ExamId);

            foreach (var examGroup in examGroups)
            {
                List<ExamCompletion> examCompletions = [.. examGroup.OrderBy(c => c.StartedAt ?? c.CreatedAt)];

                for (int i = 0; i < examCompletions.Count; i++)
                {
                    ExamCompletion completion = examCompletions[i];

                    // 根据完成顺序推断考试类型
                    ExamAttemptType attemptType = i == 0 ? ExamAttemptType.FirstAttempt : ExamAttemptType.Retake;

                    ExamAttemptDto attempt = new ExamAttemptDto
                    {
                        Id = completion.Id,
                        ExamId = completion.ExamId,
                        StudentId = completion.StudentUserId,
                        AttemptNumber = i + 1,
                        AttemptType = attemptType,
                        Status = MapCompletionStatusToAttemptStatus(completion.Status),
                        StartedAt = completion.StartedAt ?? completion.CreatedAt,
                        CompletedAt = completion.CompletedAt,
                        Score = completion.Score,
                        MaxScore = completion.MaxScore,
                        DurationSeconds = completion.DurationSeconds,
                        Notes = completion.Notes,
                        IsRanked = true
                    };

                    attempts.Add(attempt);
                }
            }

            // 合并本地缓存的数据
            List<ExamAttemptDto> localAttempts = [.. _examAttempts.Where(a => a.StudentId == studentId)];

            attempts.AddRange(localAttempts);

            return [.. attempts
                .OrderByDescending(a => a.StartedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetStudentExamAttemptHistoryAsync 异常: {ex.Message}");
            // 如果API调用失败，返回本地缓存的数据
            return [.. _examAttempts
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.StartedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)];
        }
    }

    /// <summary>
    /// 获取当前进行中的考试尝试
    /// </summary>
    public async Task<ExamAttemptDto?> GetCurrentExamAttemptAsync(int studentId)
    {
        await Task.CompletedTask;
        return _examAttempts
            .Where(a => a.StudentId == studentId && a.Status == ExamAttemptStatus.InProgress)
            .OrderByDescending(a => a.StartedAt)
            .FirstOrDefault();
    }

    /// <summary>
    /// 获取指定考试尝试的详细信息
    /// </summary>
    public async Task<ExamAttemptDto?> GetExamAttemptDetailsAsync(int attemptId)
    {
        await Task.CompletedTask;
        return _examAttempts.FirstOrDefault(a => a.Id == attemptId);
    }

    /// <summary>
    /// 检查学生是否有进行中的考试
    /// </summary>
    public async Task<bool> HasActiveExamAttemptAsync(int studentId)
    {
        ExamAttemptDto? currentAttempt = await GetCurrentExamAttemptAsync(studentId);
        return currentAttempt != null;
    }

    /// <summary>
    /// 获取考试的统计信息
    /// </summary>
    public async Task<ExamAttemptStatisticsDto> GetExamAttemptStatisticsAsync(int examId)
    {
        await Task.CompletedTask;

        List<ExamAttemptDto> examAttempts = [.. _examAttempts.Where(a => a.ExamId == examId)];
        List<ExamAttemptDto> completedAttempts = [.. examAttempts.Where(a => a.Status == ExamAttemptStatus.Completed && a.Score.HasValue)];

        return new ExamAttemptStatisticsDto
        {
            ExamId = examId,
            TotalParticipants = examAttempts.Select(a => a.StudentId).Distinct().Count(),
            TotalAttempts = examAttempts.Count,
            FirstAttempts = examAttempts.Count(a => a.AttemptType == ExamAttemptType.FirstAttempt),
            RetakeAttempts = examAttempts.Count(a => a.AttemptType == ExamAttemptType.Retake),
            PracticeAttempts = examAttempts.Count(a => a.AttemptType == ExamAttemptType.Practice),
            CompletedAttempts = examAttempts.Count(a => a.Status == ExamAttemptStatus.Completed),
            InProgressAttempts = examAttempts.Count(a => a.Status == ExamAttemptStatus.InProgress),
            AbandonedAttempts = examAttempts.Count(a => a.Status == ExamAttemptStatus.Abandoned),
            TimedOutAttempts = examAttempts.Count(a => a.Status == ExamAttemptStatus.TimedOut),
            AverageScore = completedAttempts.Count > 0 ? completedAttempts.Average(a => a.Score!.Value) : null,
            HighestScore = completedAttempts.Count > 0 ? completedAttempts.Max(a => a.Score!.Value) : null,
            LowestScore = completedAttempts.Count > 0 ? completedAttempts.Min(a => a.Score!.Value) : null,
            AverageDurationSeconds = completedAttempts.Where(a => a.DurationSeconds.HasValue).Count() > 0
                ? (int?)completedAttempts.Where(a => a.DurationSeconds.HasValue).Average(a => a.DurationSeconds!.Value)
                : null
        };
    }

    /// <summary>
    /// 验证考试尝试权限
    /// </summary>
    public async Task<(bool IsValid, string? ErrorMessage)> ValidateExamAttemptPermissionAsync(int examId, int studentId, ExamAttemptType attemptType)
    {
        try
        {
            // 检查考试权限
            ExamAttemptLimitDto limitCheck = await CheckExamAttemptLimitAsync(examId, studentId);

            if (!limitCheck.CanStartExam)
            {
                return (false, limitCheck.LimitReason ?? "无法开始考试");
            }

            // 根据尝试类型进行具体验证
            switch (attemptType)
            {
                case ExamAttemptType.FirstAttempt:
                    if (limitCheck.HasCompletedFirstAttempt)
                    {
                        return (false, "已完成首次考试，不能重复进行首次考试");
                    }
                    break;

                case ExamAttemptType.Retake:
                    if (!limitCheck.CanRetake)
                    {
                        return (false, "不允许重考或重考次数已用完");
                    }
                    break;

                case ExamAttemptType.Practice:
                    if (!limitCheck.CanPractice)
                    {
                        return (false, "不允许重做练习");
                    }
                    break;

                default:
                    return (false, "未知的考试尝试类型");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ValidateExamAttemptPermissionAsync 异常: {ex.Message}");
            return (false, $"验证权限时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新考试尝试的进度信息
    /// </summary>
    public async Task<bool> UpdateExamAttemptProgressAsync(int attemptId, string? progressData)
    {
        try
        {
            ExamAttemptDto? attempt = await GetExamAttemptDetailsAsync(attemptId);
            if (attempt == null || attempt.Status != ExamAttemptStatus.InProgress)
            {
                return false;
            }

            // 这里可以存储进度数据，例如答题进度、当前题目等
            // 由于是模拟实现，这里只是简单返回成功
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateExamAttemptProgressAsync 异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从后端API获取考试完成记录
    /// </summary>
    private async Task<List<ExamCompletion>> GetExamCompletionsFromApiAsync(int? examId = null)
    {
        try
        {
            // 获取认证令牌
            string? token = await _authenticationService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return [];
            }

            // 设置请求头
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            // 构建API URL
            string baseUrl = _configurationService.ApiBaseUrl;
            string url = $"{baseUrl}/api/student/exams/completions";
            if (examId.HasValue)
            {
                url += $"?examId={examId.Value}";
            }

            // 发送请求
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string jsonContent = await response.Content.ReadAsStringAsync();

                try
                {
                    List<ExamCompletion>? completions = JsonSerializer.Deserialize<List<ExamCompletion>>(jsonContent, JsonOptions);
                    return completions ?? [];
                }
                catch (JsonException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"GetExamCompletionsFromApiAsync JSON解析失败: {jsonEx.Message}");
                    return [];
                }
            }
            else
            {
                // 非成功状态码，静默返回空（非异常，不输出调试信息）
                return [];
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== GetExamCompletionsFromApiAsync 异常 ===");
            System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"异常消息: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            System.Diagnostics.Debug.WriteLine($"=== GetExamCompletionsFromApiAsync 异常结束 ===");
            return [];
        }
    }

    /// <summary>
    /// 将ExamCompletionStatus转换为ExamAttemptStatus
    /// </summary>
    private static ExamAttemptStatus MapCompletionStatusToAttemptStatus(ExamCompletionStatus status)
    {
        return status switch
        {
            ExamCompletionStatus.NotStarted => ExamAttemptStatus.InProgress,
            ExamCompletionStatus.InProgress => ExamAttemptStatus.InProgress,
            ExamCompletionStatus.Completed => ExamAttemptStatus.Completed,
            ExamCompletionStatus.Expired => ExamAttemptStatus.TimedOut,
            ExamCompletionStatus.Cancelled => ExamAttemptStatus.Abandoned,
            _ => ExamAttemptStatus.InProgress
        };
    }
}
