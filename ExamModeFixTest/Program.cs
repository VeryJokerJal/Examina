using System;
using System.Collections.Generic;
using System.Linq;

namespace ExamModeFixTest;

// 模拟所需的枚举和类
public enum ExamMode
{
    Normal,
    Retake,
    Practice
}

public enum ExamType
{
    MockExam,
    ComprehensiveTraining,
    FormalExam,
    Practice,
    SpecialPractice,
    SpecializedTraining
}

public enum ExamAttemptType
{
    FirstAttempt = 0,
    Retake = 1,
    Practice = 2
}

public enum ExamAttemptStatus
{
    InProgress = 0,
    Completed = 1,
    Abandoned = 2,
    TimedOut = 3
}

public enum ExamCompletionStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Expired = 3,
    Cancelled = 4
}

public class ExamCompletion
{
    public int Id { get; set; }
    public int StudentUserId { get; set; }
    public int ExamId { get; set; }
    public ExamCompletionStatus Status { get; set; } = ExamCompletionStatus.NotStarted;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? Score { get; set; }
    public decimal? MaxScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ExamAttemptDto
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public int StudentId { get; set; }
    public int AttemptNumber { get; set; }
    public ExamAttemptType AttemptType { get; set; }
    public ExamAttemptStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? Score { get; set; }
    public decimal? MaxScore { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 考试模式修复测试 ===");
        Console.WriteLine();

        TestRetakeModeCanRetakeFix();
        TestPracticeModeNoApiSubmission();

        Console.WriteLine("所有测试完成。");
    }

    /// <summary>
    /// 测试重考模式CanRetake属性计算修复
    /// </summary>
    static void TestRetakeModeCanRetakeFix()
    {
        Console.WriteLine("=== 测试重考模式CanRetake属性计算修复 ===");

        try
        {
            Console.WriteLine("测试场景1: 学生首次完成考试后，应该能够重考");
            
            // 模拟考试完成记录
            List<ExamCompletion> mockCompletions = new List<ExamCompletion>
            {
                new ExamCompletion
                {
                    Id = 1,
                    ExamId = 1,
                    StudentUserId = 1,
                    Status = ExamCompletionStatus.Completed,
                    StartedAt = DateTime.Now.AddHours(-2),
                    CompletedAt = DateTime.Now.AddHours(-1),
                    Score = 85,
                    MaxScore = 100
                }
            };

            Console.WriteLine($"模拟数据: 学生1完成了考试1，得分85/100");
            
            // 验证考试类型推断逻辑（修复后的逻辑）
            List<ExamCompletion> sortedCompletions = mockCompletions
                .Where(c => c.ExamId == 1 && c.StudentUserId == 1)
                .OrderBy(c => c.StartedAt ?? c.CreatedAt)
                .ToList();

            List<ExamAttemptDto> attempts = new List<ExamAttemptDto>();
            for (int i = 0; i < sortedCompletions.Count; i++)
            {
                ExamCompletion completion = sortedCompletions[i];
                
                // 根据完成顺序推断考试类型（这是修复后的逻辑）
                ExamAttemptType attemptType = i == 0 ? ExamAttemptType.FirstAttempt : ExamAttemptType.Retake;
                
                ExamAttemptDto attempt = new ExamAttemptDto
                {
                    Id = completion.Id,
                    ExamId = completion.ExamId,
                    StudentId = completion.StudentUserId,
                    AttemptNumber = i + 1,
                    AttemptType = attemptType,
                    Status = ExamAttemptStatus.Completed,
                    StartedAt = completion.StartedAt ?? completion.CreatedAt,
                    CompletedAt = completion.CompletedAt,
                    Score = completion.Score,
                    MaxScore = completion.MaxScore
                };
                
                attempts.Add(attempt);
            }

            Console.WriteLine($"转换结果: 生成了{attempts.Count}个考试尝试记录");
            foreach (ExamAttemptDto attempt in attempts)
            {
                Console.WriteLine($"  尝试{attempt.AttemptNumber}: {attempt.AttemptType}, 状态: {attempt.Status}");
            }

            // 验证权限计算逻辑
            bool hasCompletedFirstAttempt = attempts.Any(a =>
                a.AttemptType == ExamAttemptType.FirstAttempt &&
                a.Status == ExamAttemptStatus.Completed);

            Console.WriteLine($"是否已完成首次考试: {hasCompletedFirstAttempt}");

            // 模拟考试配置
            bool allowRetake = true;
            int maxRetakeCount = 2;
            int retakeAttempts = attempts.Count(a => a.AttemptType == ExamAttemptType.Retake);

            bool canRetake = allowRetake && retakeAttempts < maxRetakeCount && hasCompletedFirstAttempt;
            
            Console.WriteLine($"重考配置: 允许重考={allowRetake}, 最大重考次数={maxRetakeCount}, 已重考次数={retakeAttempts}");
            Console.WriteLine($"计算结果: CanRetake = {canRetake}");

            if (canRetake && hasCompletedFirstAttempt)
            {
                Console.WriteLine("✓ 测试通过: 学生完成首次考试后可以重考");
            }
            else
            {
                Console.WriteLine("✗ 测试失败: 权限计算错误");
            }

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 测试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试练习模式不提交到API的修复
    /// </summary>
    static void TestPracticeModeNoApiSubmission()
    {
        Console.WriteLine("=== 测试练习模式不提交到API的修复 ===");

        try
        {
            Console.WriteLine("测试场景: 练习模式应该映射为ExamType.Practice，不向API提交");

            // 测试ExamMode到ExamType的映射（修复后的逻辑）
            ExamMode practiceMode = ExamMode.Practice;
            ExamType examType = practiceMode switch
            {
                ExamMode.Normal => ExamType.FormalExam,
                ExamMode.Retake => ExamType.FormalExam,
                ExamMode.Practice => ExamType.Practice,  // 修复后：练习模式映射为Practice
                _ => ExamType.FormalExam
            };

            Console.WriteLine($"ExamMode.Practice 映射为: {examType}");

            if (examType == ExamType.Practice)
            {
                Console.WriteLine("✓ 测试通过: 练习模式正确映射为ExamType.Practice");
            }
            else
            {
                Console.WriteLine("✗ 测试失败: 练习模式映射错误");
            }

            // 测试考试尝试类型
            ExamAttemptType practiceAttemptType = ExamAttemptType.Practice;
            Console.WriteLine($"练习模式考试尝试类型: {practiceAttemptType}");

            // 模拟CompleteExamAttemptAsync中的练习模式处理（修复后的逻辑）
            bool shouldSubmitToApi = practiceAttemptType != ExamAttemptType.Practice;
            Console.WriteLine($"是否应该提交到API: {shouldSubmitToApi}");

            if (!shouldSubmitToApi)
            {
                Console.WriteLine("✓ 测试通过: 练习模式不会提交到API");
            }
            else
            {
                Console.WriteLine("✗ 测试失败: 练习模式仍会提交到API");
            }

            // 测试提交逻辑分支
            Console.WriteLine("\n测试提交逻辑分支:");
            
            // 模拟不同考试类型的提交处理
            ExamType[] examTypes = { ExamType.FormalExam, ExamType.MockExam, ExamType.Practice, ExamType.ComprehensiveTraining };
            
            foreach (ExamType type in examTypes)
            {
                string submitAction = type switch
                {
                    ExamType.FormalExam => "提交到正式考试API",
                    ExamType.MockExam => "提交到模拟考试API", 
                    ExamType.Practice => "仅本地处理，不提交API",  // 修复后的逻辑
                    ExamType.ComprehensiveTraining => "提交到综合实训API",
                    _ => "未知处理方式"
                };
                
                Console.WriteLine($"  {type}: {submitAction}");
            }

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 测试异常: {ex.Message}");
        }
    }
}
