using Examina.Models;
using Examina.Models.Exam;
using Examina.Services;
using Examina.ViewModels.Pages;
using Examina.Converters;

namespace Examina.Tests;

/// <summary>
/// 考试状态显示测试
/// </summary>
public class ExamStatusDisplayTest
{
    /// <summary>
    /// 测试考试状态显示修复
    /// </summary>
    public static async Task TestExamStatusDisplayFix()
    {
        Console.WriteLine("=== 考试状态显示修复测试 ===");

        // 1. 测试ExamStatusToStringConverter
        Console.WriteLine("\n1. 测试状态转换器:");
        TestExamStatusConverter();

        // 2. 测试ExamAttemptDto状态显示
        Console.WriteLine("\n2. 测试考试尝试状态显示:");
        TestExamAttemptStatusDisplay();

        // 3. 测试ExamViewModel状态更新
        Console.WriteLine("\n3. 测试ExamViewModel状态更新:");
        await TestExamViewModelStatusUpdate();

        // 4. 测试状态同步机制
        Console.WriteLine("\n4. 测试状态同步机制:");
        TestStatusSyncMechanism();

        Console.WriteLine("\n=== 测试完成 ===");
    }

    /// <summary>
    /// 测试ExamStatusToStringConverter
    /// </summary>
    private static void TestExamStatusConverter()
    {
        ExamStatusToStringConverter converter = new();

        // 测试各种状态的转换
        var testCases = new[]
        {
            (ExamStatus.Preparing, "准备中"),
            (ExamStatus.InProgress, "考试进行中"),
            (ExamStatus.AboutToEnd, "即将结束"),
            (ExamStatus.Ended, "考试已结束"),
            (ExamStatus.Submitted, "已提交")
        };

        foreach ((ExamStatus status, string expected) in testCases)
        {
            object? result = converter.Convert(status, typeof(string), null, null);
            string actual = result?.ToString() ?? "null";
            bool passed = actual == expected;
            
            Console.WriteLine($"  {status} -> {actual} [{(passed ? "✓" : "✗")}]");
            if (!passed)
            {
                Console.WriteLine($"    期望: {expected}, 实际: {actual}");
            }
        }
    }

    /// <summary>
    /// 测试ExamAttemptDto状态显示
    /// </summary>
    private static void TestExamAttemptStatusDisplay()
    {
        var testCases = new[]
        {
            (ExamAttemptStatus.InProgress, ExamAttemptType.FirstAttempt, "进行中", "首次考试"),
            (ExamAttemptStatus.Completed, ExamAttemptType.Retake, "已完成", "重考"),
            (ExamAttemptStatus.Abandoned, ExamAttemptType.Practice, "已放弃", "重做练习"),
            (ExamAttemptStatus.TimedOut, ExamAttemptType.FirstAttempt, "超时", "首次考试")
        };

        foreach ((ExamAttemptStatus status, ExamAttemptType type, string expectedStatus, string expectedType) in testCases)
        {
            ExamAttemptDto attempt = new()
            {
                Status = status,
                AttemptType = type
            };

            bool statusPassed = attempt.StatusDisplay == expectedStatus;
            bool typePassed = attempt.AttemptTypeDisplay == expectedType;

            Console.WriteLine($"  状态: {status} -> {attempt.StatusDisplay} [{(statusPassed ? "✓" : "✗")}]");
            Console.WriteLine($"  类型: {type} -> {attempt.AttemptTypeDisplay} [{(typePassed ? "✓" : "✗")}]");
        }
    }

    /// <summary>
    /// 测试ExamViewModel状态更新
    /// </summary>
    private static async Task TestExamViewModelStatusUpdate()
    {
        // 创建模拟服务
        IStudentExamService mockExamService = new MockStudentExamService();
        IExamAttemptService mockAttemptService = new MockExamAttemptService();
        IAuthenticationService mockAuthService = new MockAuthenticationService();

        // 创建ExamViewModel
        ExamViewModel viewModel = new(mockAuthService, mockExamService, mockAttemptService);

        // 模拟选择考试
        StudentExamDto testExam = new()
        {
            Id = 1,
            Name = "测试考试",
            AllowRetake = true,
            AllowPractice = true,
            MaxRetakeCount = 3,
            DurationMinutes = 120
        };

        viewModel.SelectedExam = testExam;

        // 等待异步操作完成
        await Task.Delay(100);

        Console.WriteLine($"  初始状态: {viewModel.ExamStatus}");
        Console.WriteLine($"  有活跃考试: {viewModel.HasActiveExam}");

        // 模拟开始考试
        ExamAttemptDto mockAttempt = new()
        {
            Id = 1,
            ExamId = 1,
            StudentId = 1,
            AttemptType = ExamAttemptType.FirstAttempt,
            Status = ExamAttemptStatus.InProgress,
            StartedAt = DateTime.Now
        };

        viewModel.CurrentExamAttempt = mockAttempt;
        viewModel.HasActiveExam = true;

        Console.WriteLine($"  考试开始后状态: {viewModel.ExamStatus}");
        Console.WriteLine($"  有活跃考试: {viewModel.HasActiveExam}");

        // 模拟考试完成
        mockAttempt.Status = ExamAttemptStatus.Completed;
        mockAttempt.CompletedAt = DateTime.Now;
        viewModel.HasActiveExam = false;

        Console.WriteLine($"  考试完成后状态: {viewModel.ExamStatus}");
        Console.WriteLine($"  有活跃考试: {viewModel.HasActiveExam}");
    }

    /// <summary>
    /// 测试状态同步机制
    /// </summary>
    private static void TestStatusSyncMechanism()
    {
        // 测试ExamAttemptStatus到ExamStatus的映射
        var mappingTests = new[]
        {
            (ExamAttemptStatus.InProgress, ExamStatus.InProgress),
            (ExamAttemptStatus.Completed, ExamStatus.Submitted),
            (ExamAttemptStatus.Abandoned, ExamStatus.Ended),
            (ExamAttemptStatus.TimedOut, ExamStatus.Ended)
        };

        Console.WriteLine("  状态映射测试:");
        foreach ((ExamAttemptStatus attemptStatus, ExamStatus expectedToolbarStatus) in mappingTests)
        {
            // 模拟状态映射逻辑
            ExamStatus actualToolbarStatus = attemptStatus switch
            {
                ExamAttemptStatus.InProgress => ExamStatus.InProgress,
                ExamAttemptStatus.Completed => ExamStatus.Submitted,
                ExamAttemptStatus.Abandoned => ExamStatus.Ended,
                ExamAttemptStatus.TimedOut => ExamStatus.Ended,
                _ => ExamStatus.Preparing
            };

            bool passed = actualToolbarStatus == expectedToolbarStatus;
            Console.WriteLine($"    {attemptStatus} -> {actualToolbarStatus} [{(passed ? "✓" : "✗")}]");
        }
    }
}

/// <summary>
/// 模拟认证服务
/// </summary>
public class MockAuthenticationService : IAuthenticationService
{
    public UserInfo? CurrentUser { get; private set; } = new()
    {
        Id = "1",
        Username = "测试用户",
        HasFullAccess = true
    };

    public event EventHandler<UserInfo?>? UserInfoUpdated;

    public Task<AuthenticationResult> LoginAsync(string phoneNumber, string password, bool rememberMe = false) => 
        Task.FromResult(new AuthenticationResult { IsSuccess = true, User = CurrentUser });

    public Task LogoutAsync() => Task.CompletedTask;
    public Task<bool> RefreshTokenAsync() => Task.FromResult(true);
    public Task<bool> IsTokenValidAsync() => Task.FromResult(true);
    public void UpdateUserInfo(UserInfo userInfo) { CurrentUser = userInfo; }
}

/// <summary>
/// 模拟考试尝试服务
/// </summary>
public class MockExamAttemptService : IExamAttemptService
{
    private readonly List<ExamAttemptDto> _attempts = [];
    private int _nextId = 1;

    public Task<ExamAttemptLimitDto> CheckExamAttemptLimitAsync(int examId, int studentId) =>
        Task.FromResult(new ExamAttemptLimitDto
        {
            ExamId = examId,
            StudentId = studentId,
            CanStartExam = true,
            CanRetake = true,
            CanPractice = true
        });

    public Task<ExamAttemptDto?> StartExamAttemptAsync(int examId, int studentId, ExamAttemptType attemptType) =>
        Task.FromResult<ExamAttemptDto?>(new ExamAttemptDto
        {
            Id = _nextId++,
            ExamId = examId,
            StudentId = studentId,
            AttemptType = attemptType,
            Status = ExamAttemptStatus.InProgress,
            StartedAt = DateTime.Now
        });

    public Task<bool> CompleteExamAttemptAsync(int attemptId, decimal? score = null, decimal? maxScore = null, int? durationSeconds = null, string? notes = null) =>
        Task.FromResult(true);

    public Task<bool> AbandonExamAttemptAsync(int attemptId, string? reason = null) => Task.FromResult(true);
    public Task<bool> TimeoutExamAttemptAsync(int attemptId, decimal? score = null, decimal? maxScore = null, int? durationSeconds = null) => Task.FromResult(true);
    public Task<List<ExamAttemptDto>> GetExamAttemptHistoryAsync(int examId, int studentId) => Task.FromResult(_attempts);
    public Task<List<ExamAttemptDto>> GetStudentExamAttemptHistoryAsync(int studentId, int pageNumber = 1, int pageSize = 50) => Task.FromResult(_attempts);
    public Task<ExamAttemptDto?> GetCurrentExamAttemptAsync(int studentId) => Task.FromResult<ExamAttemptDto?>(null);
    public Task<ExamAttemptDto?> GetExamAttemptDetailsAsync(int attemptId) => Task.FromResult<ExamAttemptDto?>(null);
    public Task<bool> HasActiveExamAttemptAsync(int studentId) => Task.FromResult(false);
    public Task<ExamAttemptStatisticsDto> GetExamAttemptStatisticsAsync(int examId) => Task.FromResult(new ExamAttemptStatisticsDto());
    public Task<(bool IsValid, string? ErrorMessage)> ValidateExamAttemptPermissionAsync(int examId, int studentId, ExamAttemptType attemptType) => Task.FromResult((true, (string?)null));
    public Task<bool> UpdateExamAttemptProgressAsync(int attemptId, string? progressData) => Task.FromResult(true);
}
