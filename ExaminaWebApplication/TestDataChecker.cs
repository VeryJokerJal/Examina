using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.ImportedExam;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication;

/// <summary>
/// 测试数据检查器，用于验证数据库中的考试数据
/// </summary>
public class TestDataChecker
{
    private readonly ApplicationDbContext _context;

    public TestDataChecker(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 检查并显示数据库中的考试数据
    /// </summary>
    public async Task CheckTestDataAsync()
    {
        Console.WriteLine("=== 检查数据库中的考试数据 ===");
        
        List<ImportedExam> allExams = await _context.ImportedExams.ToListAsync();
        Console.WriteLine($"数据库中总共有 {allExams.Count} 个考试");
        
        if (allExams.Count == 0)
        {
            Console.WriteLine("数据库中没有考试数据，正在创建测试数据...");
            await SeedTestExamData.SeedAsync(_context);
            allExams = await _context.ImportedExams.ToListAsync();
            Console.WriteLine($"创建测试数据后，数据库中有 {allExams.Count} 个考试");
        }

        Console.WriteLine("\n=== 全省统考 ===");
        List<ImportedExam> provincialExams = allExams.Where(e => e.ExamCategory == ExamCategory.Provincial).ToList();
        foreach (ImportedExam exam in provincialExams)
        {
            Console.WriteLine($"- {exam.Name}");
            Console.WriteLine($"  状态: {exam.Status}");
            Console.WriteLine($"  开始时间: {exam.StartTime?.ToString("yyyy-MM-dd HH:mm") ?? "未设置"}");
            Console.WriteLine($"  结束时间: {exam.EndTime?.ToString("yyyy-MM-dd HH:mm") ?? "未设置"}");
            Console.WriteLine($"  是否启用: {exam.IsEnabled}");
            Console.WriteLine();
        }

        Console.WriteLine("=== 学校统考 ===");
        List<ImportedExam> schoolExams = allExams.Where(e => e.ExamCategory == ExamCategory.School).ToList();
        foreach (ImportedExam exam in schoolExams)
        {
            Console.WriteLine($"- {exam.Name}");
            Console.WriteLine($"  状态: {exam.Status}");
            Console.WriteLine($"  开始时间: {exam.StartTime?.ToString("yyyy-MM-dd HH:mm") ?? "未设置"}");
            Console.WriteLine($"  结束时间: {exam.EndTime?.ToString("yyyy-MM-dd HH:mm") ?? "未设置"}");
            Console.WriteLine($"  是否启用: {exam.IsEnabled}");
            Console.WriteLine();
        }

        Console.WriteLine($"当前时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        Console.WriteLine($"当前时间: {DateTime.Now:yyyy-MM-dd HH:mm} Local");
    }

    /// <summary>
    /// 强制重新创建测试数据
    /// </summary>
    public async Task RecreateTestDataAsync()
    {
        Console.WriteLine("=== 重新创建测试数据 ===");
        
        // 删除现有数据
        List<ImportedExam> existingExams = await _context.ImportedExams.ToListAsync();
        if (existingExams.Count > 0)
        {
            _context.ImportedExams.RemoveRange(existingExams);
            await _context.SaveChangesAsync();
            Console.WriteLine($"已删除 {existingExams.Count} 个现有考试");
        }

        // 创建新的测试数据
        await SeedTestExamData.SeedAsync(_context);
        Console.WriteLine("已重新创建测试数据");
        
        // 显示新数据
        await CheckTestDataAsync();
    }
}
