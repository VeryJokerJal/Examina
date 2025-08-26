using System.Diagnostics;
using Examina.Models.Exam;
using Examina.Models.ExamAttempt;
using Examina.Services;

namespace Examina.Tests;

/// <summary>
/// 开始考试按钮功能测试
/// </summary>
public static class ExamButtonFunctionalityTest
{
    /// <summary>
    /// 测试考试权限检查功能
    /// </summary>
    public static async Task TestExamPermissionCheck()
    {
        Debug.WriteLine("=== 开始考试按钮功能测试 ===");
        
        try
        {
            // 获取服务实例
            IExamAttemptService? examAttemptService = AppServiceManager.GetService<IExamAttemptService>();
            IAuthenticationService? authService = AppServiceManager.GetService<IAuthenticationService>();
            
            if (examAttemptService == null)
            {
                Debug.WriteLine("✗ ExamAttemptService未注册");
                return;
            }
            
            if (authService == null)
            {
                Debug.WriteLine("✗ AuthenticationService未注册");
                return;
            }
            
            Debug.WriteLine("✓ 服务实例获取成功");
            
            // 获取当前用户
            UserInfo? currentUser = authService.CurrentUser;
            if (currentUser == null || !int.TryParse(currentUser.Id, out int studentId))
            {
                Debug.WriteLine("✗ 用户未登录或用户ID无效");
                return;
            }
            
            Debug.WriteLine($"✓ 当前用户: {currentUser.Username} (ID: {studentId})");
            
            // 测试考试ID（使用已知的考试）
            int testExamId = 1;
            
            Debug.WriteLine($"测试考试权限检查，ExamId: {testExamId}, StudentId: {studentId}");
            
            // 检查考试次数限制
            ExamAttemptLimitDto limitCheck = await examAttemptService.CheckExamAttemptLimitAsync(testExamId, studentId);
            
            Debug.WriteLine("=== 考试权限检查结果 ===");
            Debug.WriteLine($"CanStartExam: {limitCheck.CanStartExam}");
            Debug.WriteLine($"CanRetake: {limitCheck.CanRetake}");
            Debug.WriteLine($"CanPractice: {limitCheck.CanPractice}");
            Debug.WriteLine($"TotalAttempts: {limitCheck.TotalAttempts}");
            Debug.WriteLine($"RetakeAttempts: {limitCheck.RetakeAttempts}");
            Debug.WriteLine($"PracticeAttempts: {limitCheck.PracticeAttempts}");
            Debug.WriteLine($"HasCompletedFirstAttempt: {limitCheck.HasCompletedFirstAttempt}");
            Debug.WriteLine($"LimitReason: {limitCheck.LimitReason ?? "无"}");
            
            // 分析结果
            if (limitCheck.CanStartExam)
            {
                Debug.WriteLine("✓ 用户可以开始考试");
                
                if (limitCheck.HasCompletedFirstAttempt)
                {
                    if (limitCheck.CanRetake)
                    {
                        Debug.WriteLine("  - 可以进行重考");
                    }
                    if (limitCheck.CanPractice)
                    {
                        Debug.WriteLine("  - 可以进行练习");
                    }
                }
                else
                {
                    Debug.WriteLine("  - 可以进行首次考试");
                }
            }
            else
            {
                Debug.WriteLine($"✗ 用户无法开始考试: {limitCheck.LimitReason}");
            }
            
            // 测试不同考试模式的权限验证
            Debug.WriteLine("\n=== 测试不同考试模式权限 ===");
            
            var testModes = new[]
            {
                (ExamAttemptType.FirstAttempt, "首次考试"),
                (ExamAttemptType.Retake, "重考"),
                (ExamAttemptType.Practice, "练习")
            };
            
            foreach ((ExamAttemptType attemptType, string modeName) in testModes)
            {
                (bool isValid, string? errorMessage) = await examAttemptService.ValidateExamAttemptPermissionAsync(
                    testExamId, studentId, attemptType);
                
                if (isValid)
                {
                    Debug.WriteLine($"✓ {modeName}模式: 权限验证通过");
                }
                else
                {
                    Debug.WriteLine($"✗ {modeName}模式: {errorMessage}");
                }
            }
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ 考试权限检查测试失败: {ex.Message}");
            Debug.WriteLine($"异常详情: {ex}");
        }
        
        Debug.WriteLine("");
    }

    /// <summary>
    /// 测试考试历史记录获取
    /// </summary>
    public static async Task TestExamHistoryRetrieval()
    {
        Debug.WriteLine("=== 考试历史记录测试 ===");
        
        try
        {
            IExamAttemptService? examAttemptService = AppServiceManager.GetService<IExamAttemptService>();
            IAuthenticationService? authService = AppServiceManager.GetService<IAuthenticationService>();
            
            if (examAttemptService == null || authService == null)
            {
                Debug.WriteLine("✗ 服务实例获取失败");
                return;
            }
            
            UserInfo? currentUser = authService.CurrentUser;
            if (currentUser == null || !int.TryParse(currentUser.Id, out int studentId))
            {
                Debug.WriteLine("✗ 用户信息无效");
                return;
            }
            
            int testExamId = 1;
            
            // 获取考试历史
            List<ExamAttemptDto> history = await examAttemptService.GetExamAttemptHistoryAsync(testExamId, studentId);
            
            Debug.WriteLine($"考试历史记录数量: {history.Count}");
            
            if (history.Count > 0)
            {
                Debug.WriteLine("考试历史详情:");
                for (int i = 0; i < Math.Min(history.Count, 5); i++)
                {
                    ExamAttemptDto attempt = history[i];
                    Debug.WriteLine($"  记录 {i + 1}: ID={attempt.Id}, 类型={attempt.AttemptTypeDisplay}, 状态={attempt.Status}");
                    Debug.WriteLine($"    开始时间={attempt.StartedAt}, 完成时间={attempt.CompletedAt}");
                    Debug.WriteLine($"    分数={attempt.Score}/{attempt.MaxScore}");
                }
                
                if (history.Count > 5)
                {
                    Debug.WriteLine($"  ... 还有 {history.Count - 5} 条记录");
                }
            }
            else
            {
                Debug.WriteLine("没有找到考试历史记录");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ 考试历史记录测试失败: {ex.Message}");
        }
        
        Debug.WriteLine("");
    }

    /// <summary>
    /// 运行所有开始考试按钮功能测试
    /// </summary>
    public static async Task RunAllTests()
    {
        Debug.WriteLine("开始开始考试按钮功能测试...");
        Debug.WriteLine("");
        
        await TestExamPermissionCheck();
        await TestExamHistoryRetrieval();
        
        Debug.WriteLine("开始考试按钮功能测试完成。");
    }
}
