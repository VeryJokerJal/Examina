using Examina.Models;
using Examina.Models.Exam;
using Examina.Services;

namespace Examina.Tests;

/// <summary>
/// 考试尝试服务测试
/// </summary>
public class ExamAttemptServiceTest
{
    /// <summary>
    /// 测试考试次数限制验证逻辑
    /// </summary>
    public static async Task TestExamAttemptLimitValidation()
    {
        // 模拟服务
        IStudentExamService mockStudentExamService = new MockStudentExamService();
        IConfigurationService mockConfigurationService = new MockConfigurationService();
        
        ExamAttemptService examAttemptService = new(mockStudentExamService, mockConfigurationService);

        // 测试场景1：首次考试
        Console.WriteLine("=== 测试场景1：首次考试 ===");
        ExamAttemptLimitDto firstAttemptLimit = await examAttemptService.CheckExamAttemptLimitAsync(1, 1);
        Console.WriteLine($"可以开始考试: {firstAttemptLimit.CanStartExam}");
        Console.WriteLine($"可以重考: {firstAttemptLimit.CanRetake}");
        Console.WriteLine($"可以练习: {firstAttemptLimit.CanPractice}");
        Console.WriteLine($"状态描述: {firstAttemptLimit.StatusDisplay}");

        // 开始首次考试
        ExamAttemptDto? firstAttempt = await examAttemptService.StartExamAttemptAsync(1, 1, ExamAttemptType.FirstAttempt);
        if (firstAttempt != null)
        {
            Console.WriteLine($"首次考试开始成功，尝试ID: {firstAttempt.Id}");
            
            // 完成首次考试
            bool completed = await examAttemptService.CompleteExamAttemptAsync(firstAttempt.Id, 85, 100, 3600, "首次考试完成");
            Console.WriteLine($"首次考试完成: {completed}");
        }

        // 测试场景2：重考权限检查
        Console.WriteLine("\n=== 测试场景2：重考权限检查 ===");
        ExamAttemptLimitDto retakeLimit = await examAttemptService.CheckExamAttemptLimitAsync(1, 1);
        Console.WriteLine($"可以开始考试: {retakeLimit.CanStartExam}");
        Console.WriteLine($"可以重考: {retakeLimit.CanRetake}");
        Console.WriteLine($"可以练习: {retakeLimit.CanPractice}");
        Console.WriteLine($"剩余重考次数: {retakeLimit.RemainingRetakeCount}");
        Console.WriteLine($"状态描述: {retakeLimit.StatusDisplay}");

        // 测试场景3：重考
        if (retakeLimit.CanRetake)
        {
            Console.WriteLine("\n=== 测试场景3：重考 ===");
            ExamAttemptDto? retakeAttempt = await examAttemptService.StartExamAttemptAsync(1, 1, ExamAttemptType.Retake);
            if (retakeAttempt != null)
            {
                Console.WriteLine($"重考开始成功，尝试ID: {retakeAttempt.Id}");
                
                // 完成重考
                bool retakeCompleted = await examAttemptService.CompleteExamAttemptAsync(retakeAttempt.Id, 92, 100, 3200, "重考完成");
                Console.WriteLine($"重考完成: {retakeCompleted}");
            }
        }

        // 测试场景4：练习
        Console.WriteLine("\n=== 测试场景4：练习 ===");
        ExamAttemptDto? practiceAttempt = await examAttemptService.StartExamAttemptAsync(1, 1, ExamAttemptType.Practice);
        if (practiceAttempt != null)
        {
            Console.WriteLine($"练习开始成功，尝试ID: {practiceAttempt.Id}");
            
            // 完成练习
            bool practiceCompleted = await examAttemptService.CompleteExamAttemptAsync(practiceAttempt.Id, 88, 100, 2800, "练习完成");
            Console.WriteLine($"练习完成: {practiceCompleted}");
        }

        // 测试场景5：查看历史记录
        Console.WriteLine("\n=== 测试场景5：查看历史记录 ===");
        List<ExamAttemptDto> history = await examAttemptService.GetExamAttemptHistoryAsync(1, 1);
        Console.WriteLine($"考试历史记录数量: {history.Count}");
        foreach (ExamAttemptDto attempt in history)
        {
            Console.WriteLine($"- {attempt.AttemptTypeDisplay}: {attempt.StatusDisplay}, 得分: {attempt.Score}/{attempt.MaxScore}, 用时: {attempt.DurationDisplay}");
        }

        // 测试场景6：统计信息
        Console.WriteLine("\n=== 测试场景6：统计信息 ===");
        ExamAttemptStatisticsDto statistics = await examAttemptService.GetExamAttemptStatisticsAsync(1);
        Console.WriteLine($"总参与人数: {statistics.TotalParticipants}");
        Console.WriteLine($"总尝试次数: {statistics.TotalAttempts}");
        Console.WriteLine($"完成率: {statistics.CompletionRate:F1}%");
        Console.WriteLine($"平均得分: {statistics.AverageScore:F1}");
        Console.WriteLine($"平均用时: {statistics.AverageDurationDisplay}");

        Console.WriteLine("\n=== 测试完成 ===");
    }
}

/// <summary>
/// 模拟学生考试服务
/// </summary>
public class MockStudentExamService : IStudentExamService
{
    public Task<List<StudentExamDto>> GetAvailableExamsAsync(int pageNumber = 1, int pageSize = 50)
    {
        return Task.FromResult(new List<StudentExamDto>
        {
            new()
            {
                Id = 1,
                Name = "测试考试",
                AllowRetake = true,
                AllowPractice = true,
                MaxRetakeCount = 3
            }
        });
    }

    public Task<StudentExamDto?> GetExamDetailsAsync(int examId)
    {
        if (examId == 1)
        {
            return Task.FromResult<StudentExamDto?>(new StudentExamDto
            {
                Id = 1,
                Name = "测试考试",
                AllowRetake = true,
                AllowPractice = true,
                MaxRetakeCount = 3
            });
        }
        return Task.FromResult<StudentExamDto?>(null);
    }

    public Task<bool> HasAccessToExamAsync(int examId) => Task.FromResult(true);
    public Task<int> GetAvailableExamCountAsync() => Task.FromResult(1);
    public Task<List<StudentExamDto>> GetAvailableExamsByCategoryAsync(ExamCategory examCategory, int pageNumber = 1, int pageSize = 50) => Task.FromResult(new List<StudentExamDto>());
    public Task<int> GetAvailableExamCountByCategoryAsync(ExamCategory examCategory) => Task.FromResult(0);
    public Task<SpecialPracticeProgressDto> GetSpecialPracticeProgressAsync() => Task.FromResult(new SpecialPracticeProgressDto());
    public Task<int> GetAvailableSpecialPracticeCountAsync() => Task.FromResult(0);
    public Task<bool> StartSpecialPracticeAsync(int practiceId) => Task.FromResult(false);
    public Task<bool> CompleteSpecialPracticeAsync(int practiceId, CompletePracticeRequest request) => Task.FromResult(false);
    public Task<List<SpecialPracticeCompletionDto>> GetSpecialPracticeCompletionsAsync(int pageNumber = 1, int pageSize = 20) => Task.FromResult(new List<SpecialPracticeCompletionDto>());
}

/// <summary>
/// 模拟配置服务
/// </summary>
public class MockConfigurationService : IConfigurationService
{
    public string ApiBaseUrl => "http://localhost:5000";
    public string StudentAuthEndpoint => "/api/auth/student";
    public string AdminAuthEndpoint => "/api/auth/admin";
    public string ApplicationName => "Examina Test";
    public string ApplicationVersion => "1.0.0";
    public bool IsDebugMode => true;
    public int TokenRefreshThresholdMinutes => 5;
    public bool AutoLoginEnabled => false;
}
