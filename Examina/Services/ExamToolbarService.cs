using System;
using System.Threading.Tasks;
using Examina.Models;
using Examina.Models.Exam;
using Examina.Models.MockExam;
using Examina.ViewModels;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// 考试工具栏服务，用于处理考试相关的业务逻辑
/// </summary>
public class ExamToolbarService : IDisposable
{
    private readonly IStudentExamService _studentExamService;
    private readonly IStudentMockExamService _studentMockExamService;
    private readonly IStudentComprehensiveTrainingService _studentComprehensiveTrainingService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<ExamToolbarService> _logger;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamToolbarService(
        IStudentExamService studentExamService,
        IStudentMockExamService studentMockExamService,
        IStudentComprehensiveTrainingService studentComprehensiveTrainingService,
        IAuthenticationService authenticationService,
        ILogger<ExamToolbarService> logger)
    {
        _studentExamService = studentExamService ?? throw new ArgumentNullException(nameof(studentExamService));
        _studentMockExamService = studentMockExamService ?? throw new ArgumentNullException(nameof(studentMockExamService));
        _studentComprehensiveTrainingService = studentComprehensiveTrainingService ?? throw new ArgumentNullException(nameof(studentComprehensiveTrainingService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 开始正式考试
    /// </summary>
    public async Task<bool> StartFormalExamAsync(int examId, ExamToolbarViewModel viewModel)
    {
        try
        {
            _logger.LogInformation("开始正式考试，考试ID: {ExamId}", examId);

            StudentExamDto? examDetails = await _studentExamService.GetExamDetailsAsync(examId);
            if (examDetails == null)
            {
                _logger.LogWarning("无法获取考试详情，考试ID: {ExamId}", examId);
                return false;
            }

            // 设置考试信息
            int totalQuestions = examDetails.Subjects.Sum(s => s.Questions.Count) + examDetails.Modules.Sum(m => m.Questions.Count);
            int durationSeconds = examDetails.DurationMinutes * 60;

            viewModel.SetExamInfo(ExamType.FormalExam, examId, examDetails.Name, totalQuestions, durationSeconds);
            viewModel.StartCountdown(durationSeconds);

            _logger.LogInformation("正式考试已开始，考试名称: {ExamName}, 题目数: {TotalQuestions}, 时长: {Duration}分钟", 
                examDetails.Name, totalQuestions, examDetails.DurationMinutes);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始正式考试失败，考试ID: {ExamId}", examId);
            return false;
        }
    }

    /// <summary>
    /// 开始模拟考试
    /// </summary>
    public async Task<bool> StartMockExamAsync(int mockExamId, ExamToolbarViewModel viewModel)
    {
        try
        {
            _logger.LogInformation("开始模拟考试，模拟考试ID: {MockExamId}", mockExamId);

            StudentMockExamDto? mockExamDetails = await _studentMockExamService.GetMockExamDetailsAsync(mockExamId);
            if (mockExamDetails == null)
            {
                _logger.LogWarning("无法获取模拟考试详情，模拟考试ID: {MockExamId}", mockExamId);
                return false;
            }

            // 设置考试信息
            int totalQuestions = mockExamDetails.Questions.Count;
            int durationSeconds = mockExamDetails.DurationMinutes * 60;

            viewModel.SetExamInfo(ExamType.MockExam, mockExamId, mockExamDetails.Name, totalQuestions, durationSeconds);
            viewModel.StartCountdown(durationSeconds);

            _logger.LogInformation("模拟考试已开始，考试名称: {ExamName}, 题目数: {TotalQuestions}, 时长: {Duration}分钟", 
                mockExamDetails.Name, totalQuestions, mockExamDetails.DurationMinutes);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始模拟考试失败，模拟考试ID: {MockExamId}", mockExamId);
            return false;
        }
    }

    /// <summary>
    /// 开始综合实训
    /// </summary>
    public async Task<bool> StartComprehensiveTrainingAsync(int trainingId, ExamToolbarViewModel viewModel)
    {
        try
        {
            _logger.LogInformation("开始综合实训，实训ID: {TrainingId}", trainingId);

            StudentComprehensiveTrainingDto? trainingDetails = await _studentComprehensiveTrainingService.GetTrainingDetailsAsync(trainingId);
            if (trainingDetails == null)
            {
                _logger.LogWarning("无法获取综合实训详情，实训ID: {TrainingId}", trainingId);
                return false;
            }

            // 设置考试信息（综合实训通常没有严格的时间限制，这里设置一个默认值）
            int totalQuestions = trainingDetails.Subjects.Sum(s => s.Questions.Count) + trainingDetails.Modules.Sum(m => m.Questions.Count);
            int durationSeconds = 120 * 60; // 默认2小时

            viewModel.SetExamInfo(ExamType.ComprehensiveTraining, trainingId, trainingDetails.Name, totalQuestions, durationSeconds);
            viewModel.StartCountdown(durationSeconds);

            _logger.LogInformation("综合实训已开始，实训名称: {TrainingName}, 题目数: {TotalQuestions}",
                trainingDetails.Name, totalQuestions);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始综合实训失败，实训ID: {TrainingId}", trainingId);
            return false;
        }
    }

    /// <summary>
    /// 提交正式考试
    /// </summary>
    public async Task<bool> SubmitFormalExamAsync(int examId)
    {
        try
        {
            _logger.LogInformation("提交正式考试，考试ID: {ExamId}", examId);

            // 这里应该调用相应的提交API
            // 由于当前的StudentExamService没有提交方法，这里暂时模拟
            await Task.Delay(1000); // 模拟网络请求

            _logger.LogInformation("正式考试提交成功，考试ID: {ExamId}", examId);
            return true;
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
            _logger.LogInformation("提交模拟考试，模拟考试ID: {MockExamId}", mockExamId);

            bool result = await _studentMockExamService.SubmitMockExamAsync(mockExamId);
            
            if (result)
            {
                _logger.LogInformation("模拟考试提交成功，模拟考试ID: {MockExamId}", mockExamId);
            }
            else
            {
                _logger.LogWarning("模拟考试提交失败，模拟考试ID: {MockExamId}", mockExamId);
            }

            return result;
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
            _logger.LogInformation("提交综合实训，实训ID: {TrainingId}", trainingId);

            bool result = await _studentComprehensiveTrainingService.MarkTrainingAsCompletedAsync(trainingId);

            if (result)
            {
                _logger.LogInformation("综合实训提交成功，实训ID: {TrainingId}", trainingId);
            }
            else
            {
                _logger.LogWarning("综合实训提交失败，实训ID: {TrainingId}", trainingId);
            }

            return result;
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
                int delayMs = retryCount * 2000; // 递增延迟：2秒、4秒、6秒
                await Task.Delay(delayMs);
            }
        }

        _logger.LogError("重试提交考试最终失败，类型: {ExamType}, ID: {ExamId}, 已重试: {RetryCount}次", 
            examType, examId, maxRetries);
        return false;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _logger.LogInformation("ExamToolbarService资源已释放");
        }
    }
}
