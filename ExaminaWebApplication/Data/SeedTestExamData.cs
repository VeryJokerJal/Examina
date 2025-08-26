using ExaminaWebApplication.Models.ImportedExam;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Data;

/// <summary>
/// 测试考试数据种子类
/// </summary>
public static class SeedTestExamData
{
    /// <summary>
    /// 创建测试考试数据
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <returns>异步任务</returns>
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // 检查是否已有测试数据
        if (await context.ImportedExams.AnyAsync())
        {
            return; // 已有数据，不需要种子数据
        }

        DateTime now = DateTime.UtcNow;

        // 创建测试考试数据
        List<ImportedExam> testExams = new()
        {
            // 全省统考 - 已发布状态
            new ImportedExam
            {
                OriginalExamId = "PROVINCIAL_001",
                Name = "2025年春季全省计算机统考",
                Description = "面向全省学生的计算机应用能力统一考试",
                ExamType = "UnifiedExam",
                Status = "Published",
                TotalScore = 100.0m,
                DurationMinutes = 150,
                StartTime = now.AddDays(7), // 7天后开始
                EndTime = now.AddDays(7).AddMinutes(150), // 开始后150分钟结束
                AllowRetake = false,
                MaxRetakeCount = 0,
                PassingScore = 60.0m,
                RandomizeQuestions = true,
                ShowScore = true,
                ShowAnswers = false,
                IsEnabled = true,
                ExamCategory = ExamCategory.Provincial,
                Tags = "计算机,统考,全省",
                ImportedBy = 1,
                ImportedAt = now,
                OriginalCreatedBy = 1,
                OriginalCreatedAt = now,
                ImportFileName = "provincial_exam_001.xml",
                ImportFileSize = 1024000,
                ImportVersion = "1.0",
                ImportStatus = "Success"
            },

            // 全省统考 - 草稿状态
            new ImportedExam
            {
                OriginalExamId = "PROVINCIAL_002",
                Name = "2025年夏季全省计算机统考",
                Description = "夏季学期全省计算机应用能力统一考试",
                ExamType = "UnifiedExam",
                Status = "Draft",
                TotalScore = 100.0m,
                DurationMinutes = 120,
                StartTime = null,
                EndTime = null,
                AllowRetake = false,
                MaxRetakeCount = 0,
                PassingScore = 60.0m,
                RandomizeQuestions = true,
                ShowScore = true,
                ShowAnswers = false,
                IsEnabled = true,
                ExamCategory = ExamCategory.Provincial,
                Tags = "计算机,统考,全省,夏季",
                ImportedBy = 1,
                ImportedAt = now,
                OriginalCreatedBy = 1,
                OriginalCreatedAt = now,
                ImportFileName = "provincial_exam_002.xml",
                ImportFileSize = 1024000,
                ImportVersion = "1.0",
                ImportStatus = "Success"
            },

            // 学校统考 - 已安排状态
            new ImportedExam
            {
                OriginalExamId = "SCHOOL_001",
                Name = "计算机基础期中考试",
                Description = "计算机基础课程期中考试",
                ExamType = "UnifiedExam",
                Status = "Scheduled",
                TotalScore = 100.0m,
                DurationMinutes = 90,
                StartTime = now.AddDays(3), // 3天后开始
                EndTime = now.AddDays(3).AddMinutes(90), // 开始后90分钟结束
                AllowRetake = true,
                MaxRetakeCount = 1,
                PassingScore = 60.0m,
                RandomizeQuestions = false,
                ShowScore = true,
                ShowAnswers = true,
                IsEnabled = true,
                ExamCategory = ExamCategory.School,
                Tags = "计算机基础,期中考试",
                ImportedBy = 1,
                ImportedAt = now,
                OriginalCreatedBy = 1,
                OriginalCreatedAt = now,
                ImportFileName = "school_exam_001.xml",
                ImportFileSize = 512000,
                ImportVersion = "1.0",
                ImportStatus = "Success"
            },

            // 学校统考 - 进行中状态
            new ImportedExam
            {
                OriginalExamId = "SCHOOL_002",
                Name = "Office应用技能测试",
                Description = "Microsoft Office应用技能综合测试",
                ExamType = "UnifiedExam",
                Status = "InProgress",
                TotalScore = 100.0m,
                DurationMinutes = 120,
                StartTime = now.AddMinutes(-30), // 30分钟前开始
                EndTime = now.AddMinutes(90), // 90分钟后结束
                AllowRetake = false,
                MaxRetakeCount = 0,
                PassingScore = 70.0m,
                RandomizeQuestions = true,
                ShowScore = false,
                ShowAnswers = false,
                IsEnabled = true,
                ExamCategory = ExamCategory.School,
                Tags = "Office,技能测试",
                ImportedBy = 1,
                ImportedAt = now,
                OriginalCreatedBy = 1,
                OriginalCreatedAt = now,
                ImportFileName = "school_exam_002.xml",
                ImportFileSize = 768000,
                ImportVersion = "1.0",
                ImportStatus = "Success"
            },

            // 学校统考 - 已结束状态
            new ImportedExam
            {
                OriginalExamId = "SCHOOL_003",
                Name = "程序设计基础考试",
                Description = "C#程序设计基础课程考试",
                ExamType = "UnifiedExam",
                Status = "Completed",
                TotalScore = 100.0m,
                DurationMinutes = 180,
                StartTime = now.AddDays(-2), // 2天前开始
                EndTime = now.AddDays(-2).AddMinutes(180), // 开始后180分钟结束
                AllowRetake = false,
                MaxRetakeCount = 0,
                PassingScore = 60.0m,
                RandomizeQuestions = false,
                ShowScore = true,
                ShowAnswers = true,
                IsEnabled = true,
                ExamCategory = ExamCategory.School,
                Tags = "程序设计,C#,基础",
                ImportedBy = 1,
                ImportedAt = now,
                OriginalCreatedBy = 1,
                OriginalCreatedAt = now,
                ImportFileName = "school_exam_003.xml",
                ImportFileSize = 2048000,
                ImportVersion = "1.0",
                ImportStatus = "Success"
            }
        };

        // 添加到数据库
        await context.ImportedExams.AddRangeAsync(testExams);
        await context.SaveChangesAsync();

        Console.WriteLine($"已创建 {testExams.Count} 个测试考试数据");
    }
}
