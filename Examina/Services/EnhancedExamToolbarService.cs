using Examina.Models;
using Examina.Models.Api;
using BenchSuite.Models;
using Examina.Models.MockExam;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// 增强的考试工具栏服务，集成BenchSuite评分功能
/// </summary>
public class EnhancedExamToolbarService : IDisposable
{
    private readonly IStudentExamService _studentExamService;
    private readonly IStudentMockExamService _studentMockExamService;
    private readonly IStudentComprehensiveTrainingService _studentComprehensiveTrainingService;
    private readonly IStudentFormalExamService _studentFormalExamService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IBenchSuiteIntegrationService _benchSuiteIntegrationService;
    private readonly IBenchSuiteDirectoryService _benchSuiteDirectoryService;
    private readonly ILogger<EnhancedExamToolbarService> _logger;
    private bool _disposed;

    public EnhancedExamToolbarService(
        IStudentExamService studentExamService,
        IStudentMockExamService studentMockExamService,
        IStudentComprehensiveTrainingService studentComprehensiveTrainingService,
        IStudentFormalExamService studentFormalExamService,
        IAuthenticationService authenticationService,
        IBenchSuiteIntegrationService benchSuiteIntegrationService,
        IBenchSuiteDirectoryService benchSuiteDirectoryService,
        ILogger<EnhancedExamToolbarService> logger)
    {
        _studentExamService = studentExamService ?? throw new ArgumentNullException(nameof(studentExamService));
        _studentMockExamService = studentMockExamService ?? throw new ArgumentNullException(nameof(studentMockExamService));
        _studentComprehensiveTrainingService = studentComprehensiveTrainingService ?? throw new ArgumentNullException(nameof(studentComprehensiveTrainingService));
        _studentFormalExamService = studentFormalExamService ?? throw new ArgumentNullException(nameof(studentFormalExamService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _benchSuiteIntegrationService = benchSuiteIntegrationService ?? throw new ArgumentNullException(nameof(benchSuiteIntegrationService));
        _benchSuiteDirectoryService = benchSuiteDirectoryService ?? throw new ArgumentNullException(nameof(benchSuiteDirectoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 提交正式考试（上机统考）
    /// </summary>
    public async Task<bool> SubmitFormalExamAsync(int examId)
    {
        Dictionary<ModuleType, ScoringResult>? result = await SubmitFormalExamWithResultAsync(examId);
        return result != null && result.Count > 0;
    }

    /// <summary>
    /// 提交正式考试并返回评分结果
    /// </summary>
    public async Task<Dictionary<ModuleType, ScoringResult>?> SubmitFormalExamWithResultAsync(int examId)
    {
        try
        {
            _logger.LogInformation("开始提交正式考试，考试ID: {ExamId}", examId);

            // 获取当前用户信息
            UserInfo? currentUser = _authenticationService.CurrentUser;
            if (currentUser == null)
            {
                _logger.LogWarning("用户未登录，无法提交考试");
                return null;
            }

            // 1. 先进行BenchSuite评分
            if (int.TryParse(currentUser.Id, out int studentId))
            {
                Dictionary<ModuleType, ScoringResult>? scoringResults = await PerformBenchSuiteScoringAsync(ExamType.FormalExam, examId, studentId);

                // 计算总分和得分
                double totalScore = scoringResults?.Values.Sum(r => r.TotalScore) ?? 0;
                double achievedScore = scoringResults?.Values.Sum(r => r.AchievedScore) ?? 0;
                bool isSuccess = scoringResults?.Values.All(r => r.IsSuccess) ?? false;

                // 2. 准备成绩提交数据
                SubmitExamScoreRequestDto scoreRequest = new()
                {
                    Score = achievedScore,
                    MaxScore = totalScore,
                    DurationSeconds = null, // 可以从考试开始时间计算
                    Notes = isSuccess ? "BenchSuite自动评分完成" : "BenchSuite评分失败",
                    BenchSuiteScoringResult = scoringResults != null ? JsonSerializer.Serialize(scoringResults) : null,
                    CompletedAt = DateTime.Now
                };

                // 3. 提交正式考试成绩到服务器
                bool submitResult = await _studentFormalExamService.SubmitExamScoreAsync(examId, scoreRequest);

                if (!submitResult)
                {
                    _logger.LogWarning("正式考试成绩提交失败，考试ID: {ExamId}", examId);

                    // 如果成绩提交失败，尝试基本完成（不包含成绩数据）
                    bool basicCompleteResult = await _studentFormalExamService.CompleteExamAsync(examId);
                    if (!basicCompleteResult)
                    {
                        _logger.LogError("正式考试基本完成也失败，考试ID: {ExamId}", examId);
                        return null;
                    }

                    _logger.LogInformation("正式考试基本完成成功（无成绩数据），考试ID: {ExamId}", examId);
                }
                else
                {
                    _logger.LogInformation("正式考试成绩提交成功，考试ID: {ExamId}, 得分: {Score}/{MaxScore}",
                        examId, achievedScore, totalScore);
                }

                return scoringResults;
            }
            else
            {
                _logger.LogWarning("无法解析用户ID为整数: {UserId}", currentUser.Id);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交正式考试失败，考试ID: {ExamId}", examId);
            return null;
        }
    }

    /// <summary>
    /// 提交模拟考试
    /// </summary>
    public async Task<Dictionary<ModuleType, ScoringResult>?> SubmitMockExamAsync(int mockExamId, int? actualDurationSeconds = null)
    {
        try
        {
            _logger.LogInformation("开始提交模拟考试，模拟考试ID: {MockExamId}, 实际用时: {Duration}秒", mockExamId, actualDurationSeconds);

            // 获取当前用户信息
            UserInfo? currentUser = _authenticationService.CurrentUser;
            if (currentUser == null)
            {
                _logger.LogWarning("用户未登录，无法提交模拟考试");
                return null;
            }

            // 1. 先进行BenchSuite评分
            if (int.TryParse(currentUser.Id, out int studentId))
            {
                Dictionary<ModuleType, ScoringResult>? scoringResults = await PerformBenchSuiteScoringAsync(ExamType.MockExam, mockExamId, studentId);

                // 计算总分和得分
                double totalScore = scoringResults?.Values.Sum(r => r.TotalScore) ?? 0;
                double achievedScore = scoringResults?.Values.Sum(r => r.AchievedScore) ?? 0;
                bool isSuccess = scoringResults?.Values.All(r => r.IsSuccess) ?? false;

                // 2. 准备成绩提交数据
                SubmitMockExamScoreRequestDto scoreRequest = new()
                {
                    Score = achievedScore,
                    MaxScore = totalScore,
                    DurationSeconds = actualDurationSeconds, // 使用传递的实际用时
                    Notes = isSuccess ? "BenchSuite自动评分完成" : "BenchSuite评分失败",
                    BenchSuiteScoringResult = scoringResults != null ? JsonSerializer.Serialize(scoringResults) : null
                };

                // 3. 提交模拟考试成绩到服务器
                bool submitResult = await _studentMockExamService.SubmitMockExamScoreAsync(mockExamId, scoreRequest);

                if (!submitResult)
                {
                    _logger.LogWarning("模拟考试成绩提交失败，模拟考试ID: {MockExamId}", mockExamId);

                    // 如果成绩提交失败，尝试基本提交（不包含成绩数据）
                    MockExamSubmissionResponseDto? basicSubmitResult = await _studentMockExamService.SubmitMockExamAsync(mockExamId, actualDurationSeconds);
                    if (basicSubmitResult?.Success != true)
                    {
                        _logger.LogError("模拟考试基本提交也失败，模拟考试ID: {MockExamId}, 错误: {Error}",
                            mockExamId, basicSubmitResult?.Message ?? "未知错误");
                        return null;
                    }

                    _logger.LogInformation("模拟考试基本提交成功（无成绩数据），模拟考试ID: {MockExamId}, 时间状态: {TimeStatus}",
                        mockExamId, basicSubmitResult.TimeStatusDescription);
                }
                else
                {
                    _logger.LogInformation("模拟考试成绩提交成功，模拟考试ID: {MockExamId}, 得分: {Score}/{MaxScore}",
                        mockExamId, scoreRequest.Score, scoreRequest.MaxScore);
                }

                // 4. 记录评分结果
                if (isSuccess)
                {
                    _logger.LogInformation("模拟考试BenchSuite评分完成，总分: {TotalScore}, 得分: {AchievedScore}",
                        totalScore, achievedScore);
                }
                else
                {
                    _logger.LogWarning("模拟考试BenchSuite评分失败或未执行");
                }

                _logger.LogInformation("模拟考试提交流程完成，模拟考试ID: {MockExamId}", mockExamId);
                return scoringResults;
            }
            else
            {
                _logger.LogWarning("无法解析用户ID为整数: {UserId}", currentUser.Id);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交模拟考试失败，模拟考试ID: {MockExamId}", mockExamId);
            return null;
        }
    }

    /// <summary>
    /// 提交综合实训
    /// </summary>
    public async Task<bool> SubmitComprehensiveTrainingAsync(int trainingId)
    {
        Dictionary<ModuleType, ScoringResult>? result = await SubmitComprehensiveTrainingWithResultAsync(trainingId);
        return result != null && result.Count > 0;
    }

    /// <summary>
    /// 提交综合实训并返回评分结果
    /// </summary>
    public async Task<Dictionary<ModuleType, ScoringResult>?> SubmitComprehensiveTrainingWithResultAsync(int trainingId)
    {
        try
        {
            _logger.LogInformation("开始提交综合实训，实训ID: {TrainingId}", trainingId);

            // 获取当前用户信息
            UserInfo? currentUser = _authenticationService.CurrentUser;
            if (currentUser == null)
            {
                _logger.LogWarning("用户未登录，无法提交综合实训");
                return null;
            }

            // 1. 先进行BenchSuite评分
            if (int.TryParse(currentUser.Id, out int studentId))
            {
                Dictionary<ModuleType, ScoringResult>? scoringResults = await PerformBenchSuiteScoringAsync(ExamType.ComprehensiveTraining, trainingId, studentId);

                // 计算总分和得分
                double totalScore = scoringResults?.Values.Sum(r => r.TotalScore) ?? 0;
                double achievedScore = scoringResults?.Values.Sum(r => r.AchievedScore) ?? 0;
                bool isSuccess = scoringResults?.Values.All(r => r.IsSuccess) ?? false;

                // 2. 准备训练提交数据
                CompleteTrainingRequest trainingRequest = new()
                {
                    Score = achievedScore,
                    MaxScore = totalScore,
                    DurationSeconds = null, // 可以从工具栏获取实际用时
                    Notes = isSuccess ? "BenchSuite自动评分完成" : "BenchSuite评分失败",
                    BenchSuiteScoringResult = scoringResults != null ? JsonSerializer.Serialize(scoringResults) : null,
                    CompletedAt = DateTime.Now // 记录精确的提交时间
                };

                // 3. 提交综合实训成绩到服务器
                bool submitResult = await _studentComprehensiveTrainingService.CompleteComprehensiveTrainingAsync(trainingId, trainingRequest);

                if (!submitResult)
                {
                    _logger.LogWarning("综合实训成绩提交失败，实训ID: {TrainingId}", trainingId);

                    // 如果成绩提交失败，尝试基本提交（不包含成绩数据）
                    bool basicSubmitResult = await _studentComprehensiveTrainingService.MarkTrainingAsCompletedAsync(trainingId);
                    if (!basicSubmitResult)
                    {
                        _logger.LogError("综合实训基本提交也失败，实训ID: {TrainingId}", trainingId);
                        return null;
                    }

                    _logger.LogInformation("综合实训基本提交成功（无成绩数据），实训ID: {TrainingId}", trainingId);
                }

                // 4. 如果评分成功，记录评分结果
                if (isSuccess)
                {
                    _logger.LogInformation("综合实训BenchSuite评分完成，总分: {TotalScore}, 得分: {AchievedScore}",
                        totalScore, achievedScore);
                }

                _logger.LogInformation("综合实训提交成功，实训ID: {TrainingId}", trainingId);
                return scoringResults;
            }
            else
            {
                _logger.LogWarning("无法解析用户ID为整数: {UserId}", currentUser.Id);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交综合实训失败，实训ID: {TrainingId}", trainingId);
            return null;
        }
    }

    /// <summary>
    /// 检查网络连接状态
    /// </summary>
    public async Task<bool> CheckNetworkConnectionAsync()
    {
        try
        {
            // 这里可以实现实际的网络连接检查
            // 例如ping服务器或调用一个轻量级的API
            await Task.Delay(500); // 模拟网络检查

            _logger.LogInformation("网络连接检查完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "网络连接检查失败");
            return false;
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    public UserInfo? GetCurrentUser()
    {
        return _authenticationService.CurrentUser;
    }

    /// <summary>
    /// 重试提交考试
    /// </summary>
    public async Task<bool> RetrySubmitExamAsync(ExamType examType, int examId, int maxRetries = 3)
    {
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogInformation("重试提交考试，类型: {ExamType}, ID: {ExamId}, 重试次数: {RetryCount}/{MaxRetries}",
                    examType, examId, retryCount + 1, maxRetries);

                bool result = examType switch
                {
                    ExamType.FormalExam => await SubmitFormalExamAsync(examId),
                    ExamType.MockExam => (await SubmitMockExamAsync(examId)) != null,
                    ExamType.ComprehensiveTraining => await SubmitComprehensiveTrainingAsync(examId),
                    _ => false
                };

                if (result)
                {
                    _logger.LogInformation("重试提交考试成功，类型: {ExamType}, ID: {ExamId}", examType, examId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重试提交考试失败，类型: {ExamType}, ID: {ExamId}, 重试次数: {RetryCount}",
                    examType, examId, retryCount + 1);
            }

            retryCount++;
            if (retryCount < maxRetries)
            {
                // 等待一段时间后重试
                await Task.Delay(2000 * retryCount); // 递增延迟
            }
        }

        _logger.LogError("重试提交考试最终失败，类型: {ExamType}, ID: {ExamId}", examType, examId);
        return false;
    }

    /// <summary>
    /// 执行本地BenchSuite评分（仅评分，不提交到服务器）
    /// </summary>
    public async Task<Dictionary<ModuleType, ScoringResult>?> PerformLocalScoringAsync(ExamType examType, int examId, int studentId)
    {
        try
        {
            _logger.LogInformation("开始本地BenchSuite评分，考试类型: {ExamType}, 考试ID: {ExamId}, 学生ID: {StudentId}",
                examType, examId, studentId);

            // 检查BenchSuite服务是否可用
            bool serviceAvailable = await _benchSuiteIntegrationService.IsServiceAvailableAsync();
            if (!serviceAvailable)
            {
                _logger.LogWarning("BenchSuite服务不可用，跳过本地评分");
                return null;
            }

            // 确保考试目录结构存在
            bool directoryResult = await _benchSuiteDirectoryService.EnsureExamDirectoryStructureAsync(examType, examId);
            if (!directoryResult)
            {
                _logger.LogWarning("BenchSuite考试目录结构验证失败");
                return null;
            }

            // 构建文件路径字典
            Dictionary<ModuleType, List<string>> filePaths = new();

            // 扫描各种类型的考试文件
            foreach (ModuleType moduleType in _benchSuiteIntegrationService.GetSupportedModuleTypes())
            {
                string directoryPath = _benchSuiteDirectoryService.GetExamDirectoryPath(examType, examId, moduleType);
                string examDirectory = System.IO.Path.Combine(directoryPath, $"Student_{studentId}");

                if (System.IO.Directory.Exists(examDirectory))
                {
                    string[] files = System.IO.Directory.GetFiles(examDirectory, "*", System.IO.SearchOption.AllDirectories);
                    filePaths[moduleType] = [.. files];
                }
                else
                {
                    filePaths[moduleType] = new List<string>();
                }
            }

            // 执行评分（仅本地评分，不提交）
            Dictionary<ModuleType, ScoringResult> results = await _benchSuiteIntegrationService.ScoreExamAsync(examType, examId, studentId, filePaths);

            _logger.LogInformation("本地BenchSuite评分完成，模块数量: {ModuleCount}", results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "本地BenchSuite评分过程中发生异常");
            return null;
        }
    }

    #region 私有方法

    /// <summary>
    /// 执行BenchSuite评分
    /// </summary>
    private async Task<Dictionary<ModuleType, ScoringResult>?> PerformBenchSuiteScoringAsync(ExamType examType, int examId, int studentId)
    {
        try
        {
            _logger.LogInformation("开始BenchSuite评分，考试类型: {ExamType}, 考试ID: {ExamId}, 学生ID: {StudentId}",
                examType, examId, studentId);

            // 检查BenchSuite服务是否可用
            bool serviceAvailable = await _benchSuiteIntegrationService.IsServiceAvailableAsync();
            if (!serviceAvailable)
            {
                _logger.LogWarning("BenchSuite服务不可用，跳过评分");
                return null;
            }

            // 确保考试目录结构存在
            bool directoryResult = await _benchSuiteDirectoryService.EnsureExamDirectoryStructureAsync(examType, examId);
            if (!directoryResult)
            {
                _logger.LogWarning("BenchSuite考试目录结构验证失败");
                return null;
            }

            // 构建文件路径字典
            Dictionary<ModuleType, List<string>> filePaths = new();

            // 扫描各种类型的考试文件
            foreach (ModuleType moduleType in _benchSuiteIntegrationService.GetSupportedModuleTypes())
            {
                string directoryPath = _benchSuiteDirectoryService.GetExamDirectoryPath(examType, examId, moduleType);
                string examDirectory = System.IO.Path.Combine(directoryPath, $"Student_{studentId}");

                if (System.IO.Directory.Exists(examDirectory))
                {
                    string[] files = System.IO.Directory.GetFiles(examDirectory, "*", System.IO.SearchOption.AllDirectories);
                    filePaths[moduleType] = [.. files];
                }
                else
                {
                    filePaths[moduleType] = new List<string>();
                }
            }

            // 执行评分
            Dictionary<ModuleType, ScoringResult> results = await _benchSuiteIntegrationService.ScoreExamAsync(examType, examId, studentId, filePaths);

            _logger.LogInformation("BenchSuite评分完成，模块数量: {ModuleCount}", results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BenchSuite评分过程中发生异常");
            return null;
        }
    }



    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
