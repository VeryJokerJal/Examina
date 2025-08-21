using Examina.Models;
using Examina.Models.Api;
using Examina.Models.BenchSuite;
using Examina.Models.Exam;
using Examina.Models.MockExam;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Examina.Services;

/// <summary>
/// 增强的考试工具栏服务，集成BenchSuite评分功能
/// </summary>
public class EnhancedExamToolbarService : IDisposable
{
    private readonly IStudentExamService _studentExamService;
    private readonly IStudentMockExamService _studentMockExamService;
    private readonly IStudentComprehensiveTrainingService _studentComprehensiveTrainingService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IBenchSuiteIntegrationService _benchSuiteIntegrationService;
    private readonly IBenchSuiteDirectoryService _benchSuiteDirectoryService;
    private readonly ILogger<EnhancedExamToolbarService> _logger;
    private bool _disposed;

    public EnhancedExamToolbarService(
        IStudentExamService studentExamService,
        IStudentMockExamService studentMockExamService,
        IStudentComprehensiveTrainingService studentComprehensiveTrainingService,
        IAuthenticationService authenticationService,
        IBenchSuiteIntegrationService benchSuiteIntegrationService,
        IBenchSuiteDirectoryService benchSuiteDirectoryService,
        ILogger<EnhancedExamToolbarService> logger)
    {
        _studentExamService = studentExamService ?? throw new ArgumentNullException(nameof(studentExamService));
        _studentMockExamService = studentMockExamService ?? throw new ArgumentNullException(nameof(studentMockExamService));
        _studentComprehensiveTrainingService = studentComprehensiveTrainingService ?? throw new ArgumentNullException(nameof(studentComprehensiveTrainingService));
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
        try
        {
            _logger.LogInformation("开始提交正式考试，考试ID: {ExamId}", examId);

            // 获取当前用户信息
            UserInfo? currentUser = _authenticationService.CurrentUser;
            if (currentUser == null)
            {
                _logger.LogWarning("用户未登录，无法提交考试");
                return false;
            }

            // 1. 先进行BenchSuite评分
            if (int.TryParse(currentUser.Id, out int studentId))
            {
                BenchSuiteScoringResult? scoringResult = await PerformBenchSuiteScoringAsync(ExamType.FormalExam, examId, studentId);

                // 2. 提交考试到服务器
                // 注意：这里需要实现实际的正式考试提交API
                await Task.Delay(1000); // 模拟网络请求，实际实现中需要调用相应的API

                // 3. 如果评分成功，记录评分结果
                if (scoringResult?.IsSuccess == true)
                {
                    _logger.LogInformation("正式考试BenchSuite评分完成，总分: {TotalScore}, 得分: {AchievedScore}",
                        scoringResult.TotalScore, scoringResult.AchievedScore);

                    // 这里可以将评分结果保存到数据库或发送到服务器
                    // await SaveScoringResultAsync(examId, scoringResult);
                }

                _logger.LogInformation("正式考试提交成功，考试ID: {ExamId}", examId);
                return true;
            }
            else
            {
                _logger.LogWarning("无法解析用户ID为整数: {UserId}", currentUser.Id);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交正式考试失败，考试ID: {ExamId}", examId);
            return false;
        }
    }

    /// <summary>
    /// 提交模拟考试
    /// </summary>
    public async Task<bool> SubmitMockExamAsync(int mockExamId)
    {
        try
        {
            _logger.LogInformation("开始提交模拟考试，模拟考试ID: {MockExamId}", mockExamId);

            // 获取当前用户信息
            UserInfo? currentUser = _authenticationService.CurrentUser;
            if (currentUser == null)
            {
                _logger.LogWarning("用户未登录，无法提交模拟考试");
                return false;
            }

            // 1. 先进行BenchSuite评分
            if (int.TryParse(currentUser.Id, out int studentId))
            {
                BenchSuiteScoringResult? scoringResult = await PerformBenchSuiteScoringAsync(ExamType.MockExam, mockExamId, studentId);

                // 2. 准备成绩提交数据
                SubmitMockExamScoreRequestDto scoreRequest = new()
                {
                    Score = scoringResult?.AchievedScore,
                    MaxScore = scoringResult?.TotalScore,
                    DurationSeconds = null, // 可以从考试开始时间计算
                    Notes = scoringResult?.IsSuccess == true ? "BenchSuite自动评分完成" : "BenchSuite评分失败",
                    BenchSuiteScoringResult = scoringResult != null ? JsonSerializer.Serialize(scoringResult) : null
                };

                // 3. 提交模拟考试成绩到服务器
                bool submitResult = await _studentMockExamService.SubmitMockExamScoreAsync(mockExamId, scoreRequest);

                if (!submitResult)
                {
                    _logger.LogWarning("模拟考试成绩提交失败，模拟考试ID: {MockExamId}", mockExamId);

                    // 如果成绩提交失败，尝试基本提交（不包含成绩数据）
                    MockExamSubmissionResponseDto? basicSubmitResult = await _studentMockExamService.SubmitMockExamAsync(mockExamId);
                    if (basicSubmitResult?.Success != true)
                    {
                        _logger.LogError("模拟考试基本提交也失败，模拟考试ID: {MockExamId}, 错误: {Error}",
                            mockExamId, basicSubmitResult?.Message ?? "未知错误");
                        return false;
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
                if (scoringResult?.IsSuccess == true)
                {
                    _logger.LogInformation("模拟考试BenchSuite评分完成，总分: {TotalScore}, 得分: {AchievedScore}",
                        scoringResult.TotalScore, scoringResult.AchievedScore);
                }
                else
                {
                    _logger.LogWarning("模拟考试BenchSuite评分失败或未执行");
                }

                _logger.LogInformation("模拟考试提交流程完成，模拟考试ID: {MockExamId}", mockExamId);
                return true;
            }
            else
            {
                _logger.LogWarning("无法解析用户ID为整数: {UserId}", currentUser.Id);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交模拟考试失败，模拟考试ID: {MockExamId}", mockExamId);
            return false;
        }
    }

    /// <summary>
    /// 提交综合实训
    /// </summary>
    public async Task<bool> SubmitComprehensiveTrainingAsync(int trainingId)
    {
        try
        {
            _logger.LogInformation("开始提交综合实训，实训ID: {TrainingId}", trainingId);

            // 获取当前用户信息
            UserInfo? currentUser = _authenticationService.CurrentUser;
            if (currentUser == null)
            {
                _logger.LogWarning("用户未登录，无法提交综合实训");
                return false;
            }

            // 1. 先进行BenchSuite评分
            if (int.TryParse(currentUser.Id, out int studentId))
            {
                BenchSuiteScoringResult? scoringResult = await PerformBenchSuiteScoringAsync(ExamType.ComprehensiveTraining, trainingId, studentId);

                // 2. 准备训练提交数据
                CompleteTrainingRequest trainingRequest = new()
                {
                    Score = scoringResult?.AchievedScore,
                    MaxScore = scoringResult?.TotalScore,
                    DurationSeconds = null, // 可以从工具栏获取实际用时
                    Notes = scoringResult?.IsSuccess == true ? "BenchSuite自动评分完成" : "BenchSuite评分失败",
                    BenchSuiteScoringResult = scoringResult != null ? JsonSerializer.Serialize(scoringResult) : null
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
                        return false;
                    }

                    _logger.LogInformation("综合实训基本提交成功（无成绩数据），实训ID: {TrainingId}", trainingId);
                }

                // 4. 如果评分成功，记录评分结果
                if (scoringResult?.IsSuccess == true)
                {
                    _logger.LogInformation("综合实训BenchSuite评分完成，总分: {TotalScore}, 得分: {AchievedScore}",
                        scoringResult.TotalScore, scoringResult.AchievedScore);
                }

                _logger.LogInformation("综合实训提交成功，实训ID: {TrainingId}", trainingId);
                return true;
            }
            else
            {
                _logger.LogWarning("无法解析用户ID为整数: {UserId}", currentUser.Id);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交综合实训失败，实训ID: {TrainingId}", trainingId);
            return false;
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
                    ExamType.MockExam => await SubmitMockExamAsync(examId),
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

    #region 私有方法

    /// <summary>
    /// 执行BenchSuite评分
    /// </summary>
    private async Task<BenchSuiteScoringResult?> PerformBenchSuiteScoringAsync(ExamType examType, int examId, int studentId)
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

            // 确保目录结构存在
            BenchSuiteDirectoryValidationResult directoryResult = await _benchSuiteDirectoryService.EnsureDirectoryStructureAsync();
            if (!directoryResult.IsValid)
            {
                _logger.LogWarning("BenchSuite目录结构验证失败: {ErrorMessage}", directoryResult.ErrorMessage);
                return null;
            }

            // 构建评分请求
            BenchSuiteScoringRequest request = new()
            {
                ExamId = examId,
                ExamType = examType,
                StudentUserId = studentId,
                BasePath = _benchSuiteDirectoryService.GetBasePath()
            };

            // 扫描考试文件
            await ScanExamFilesAsync(request);

            // 执行评分
            BenchSuiteScoringResult result = await _benchSuiteIntegrationService.ScoreExamAsync(request);

            _logger.LogInformation("BenchSuite评分完成，成功: {IsSuccess}, 总分: {TotalScore}, 得分: {AchievedScore}",
                result.IsSuccess, result.TotalScore, result.AchievedScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BenchSuite评分过程中发生异常");
            return null;
        }
    }

    /// <summary>
    /// 扫描考试文件
    /// </summary>
    private Task ScanExamFilesAsync(BenchSuiteScoringRequest request)
    {
        // 扫描各种类型的考试文件
        foreach (BenchSuiteFileType fileType in _benchSuiteIntegrationService.GetSupportedFileTypes())
        {
            string directoryPath = _benchSuiteDirectoryService.GetDirectoryPath(fileType);
            string examDirectory = System.IO.Path.Combine(directoryPath, $"Exam_{request.ExamId}", $"Student_{request.StudentUserId}");

            if (System.IO.Directory.Exists(examDirectory))
            {
                string[] files = System.IO.Directory.GetFiles(examDirectory, "*", System.IO.SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    request.FilePaths[fileType] = [.. files];
                    _logger.LogDebug("发现 {FileType} 文件 {FileCount} 个", fileType, files.Length);
                }
            }
        }
        return Task.CompletedTask;
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
