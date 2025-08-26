using System.Diagnostics;
using Examina.Models.Exam;
using Examina.Services;
using Examina.Services.Interfaces;

namespace Examina.Tests;

/// <summary>
/// ExamAttemptService功能测试
/// </summary>
public static class ExamAttemptServiceTest
{
    /// <summary>
    /// 测试ExamAttemptService是否正确注册
    /// </summary>
    public static void TestExamAttemptServiceRegistration()
    {
        Debug.WriteLine("=== ExamAttemptService注册测试 ===");
        
        try
        {
            IExamAttemptService? service = AppServiceManager.GetService<IExamAttemptService>();
            
            if (service != null)
            {
                Debug.WriteLine("✓ ExamAttemptService已正确注册");
                Debug.WriteLine($"服务类型: {service.GetType().Name}");
            }
            else
            {
                Debug.WriteLine("✗ ExamAttemptService未注册或注册失败");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ ExamAttemptService注册测试失败: {ex.Message}");
        }
        
        Debug.WriteLine("");
    }

    /// <summary>
    /// 测试考试次数统计功能
    /// </summary>
    public static async Task TestExamAttemptLimitCheck()
    {
        Debug.WriteLine("=== 考试次数统计功能测试 ===");
        
        try
        {
            IExamAttemptService? service = AppServiceManager.GetService<IExamAttemptService>();
            
            if (service == null)
            {
                Debug.WriteLine("✗ 无法获取ExamAttemptService实例");
                return;
            }

            // 测试参数（使用示例数据）
            int testExamId = 1;
            int testStudentId = 1;

            Debug.WriteLine($"测试参数: ExamId={testExamId}, StudentId={testStudentId}");

            // 调用考试次数检查
            ExamAttemptLimitDto result = await service.CheckExamAttemptLimitAsync(testExamId, testStudentId);

            Debug.WriteLine("考试次数统计结果:");
            Debug.WriteLine($"  总尝试次数: {result.TotalAttempts}");
            Debug.WriteLine($"  重考次数: {result.RetakeAttempts}");
            Debug.WriteLine($"  练习次数: {result.PracticeAttempts}");
            Debug.WriteLine($"  是否可以开始考试: {result.CanStartExam}");
            Debug.WriteLine($"  是否可以重考: {result.CanRetake}");
            Debug.WriteLine($"  是否可以练习: {result.CanPractice}");
            Debug.WriteLine($"  限制原因: {result.LimitReason ?? "无"}");
            Debug.WriteLine($"  是否已完成首次考试: {result.HasCompletedFirstAttempt}");

            if (result.TotalAttempts > 0)
            {
                Debug.WriteLine("✓ 成功从后端API获取到考试记录");
            }
            else
            {
                Debug.WriteLine("⚠ 未获取到考试记录，可能是:");
                Debug.WriteLine("  1. 该学生确实没有参加过考试");
                Debug.WriteLine("  2. API调用失败，回退到本地缓存");
                Debug.WriteLine("  3. 认证令牌无效");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ 考试次数统计功能测试失败: {ex.Message}");
            Debug.WriteLine($"异常详情: {ex}");
        }
        
        Debug.WriteLine("");
    }

    /// <summary>
    /// 测试学生考试历史记录获取
    /// </summary>
    public static async Task TestStudentExamHistory()
    {
        Debug.WriteLine("=== 学生考试历史记录测试 ===");
        
        try
        {
            IExamAttemptService? service = AppServiceManager.GetService<IExamAttemptService>();
            
            if (service == null)
            {
                Debug.WriteLine("✗ 无法获取ExamAttemptService实例");
                return;
            }

            // 测试参数
            int testStudentId = 1;
            int pageNumber = 1;
            int pageSize = 10;

            Debug.WriteLine($"测试参数: StudentId={testStudentId}, Page={pageNumber}, Size={pageSize}");

            // 获取学生考试历史
            List<ExamAttemptDto> history = await service.GetStudentExamAttemptHistoryAsync(testStudentId, pageNumber, pageSize);

            Debug.WriteLine($"获取到 {history.Count} 条考试历史记录:");
            
            foreach (ExamAttemptDto attempt in history)
            {
                Debug.WriteLine($"  考试ID: {attempt.ExamId}, 状态: {attempt.StatusDisplay}, 开始时间: {attempt.StartedAt:yyyy-MM-dd HH:mm:ss}");
            }

            if (history.Count > 0)
            {
                Debug.WriteLine("✓ 成功获取学生考试历史记录");
            }
            else
            {
                Debug.WriteLine("⚠ 未获取到考试历史记录");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ 学生考试历史记录测试失败: {ex.Message}");
        }
        
        Debug.WriteLine("");
    }

    /// <summary>
    /// 运行所有ExamAttemptService测试
    /// </summary>
    public static async Task RunAllTests()
    {
        Debug.WriteLine("开始ExamAttemptService功能测试...");
        Debug.WriteLine("");
        
        TestExamAttemptServiceRegistration();
        await TestExamAttemptLimitCheck();
        await TestStudentExamHistory();
        
        Debug.WriteLine("ExamAttemptService功能测试完成。");
    }
}
