using System.Diagnostics;
using Examina.Services;

namespace Examina.Tests;

/// <summary>
/// API URL验证测试
/// </summary>
public static class ApiUrlVerificationTest
{
    /// <summary>
    /// 验证API URL构建是否正确
    /// </summary>
    public static void VerifyApiUrlConstruction()
    {
        Debug.WriteLine("=== API URL构建验证测试 ===");
        
        try
        {
            // 测试ConfigurationService
            IConfigurationService? configService = AppServiceManager.GetService<IConfigurationService>();
            if (configService != null)
            {
                string baseUrl = configService.ApiBaseUrl;
                Debug.WriteLine($"ConfigurationService.ApiBaseUrl: {baseUrl}");
                
                // 验证base URL不包含/api
                if (baseUrl.EndsWith("/api"))
                {
                    Debug.WriteLine("✗ 错误：ApiBaseUrl仍然包含/api后缀");
                }
                else
                {
                    Debug.WriteLine("✓ 正确：ApiBaseUrl不包含/api后缀");
                }
                
                // 构建示例URL
                string examCompletionsUrl = $"{baseUrl}/api/student/exams/completions";
                string mockExamsUrl = $"{baseUrl}/api/student/mock-exams";
                string authUrl = $"{baseUrl}/api/student/auth/login";
                
                Debug.WriteLine($"考试完成记录URL: {examCompletionsUrl}");
                Debug.WriteLine($"模拟考试URL: {mockExamsUrl}");
                Debug.WriteLine($"认证URL: {authUrl}");
                
                // 验证URL格式
                bool isValidExamUrl = examCompletionsUrl == "https://qiuzhenbd.com/api/student/exams/completions";
                bool isValidMockUrl = mockExamsUrl == "https://qiuzhenbd.com/api/student/mock-exams";
                bool isValidAuthUrl = authUrl == "https://qiuzhenbd.com/api/student/auth/login";
                
                Debug.WriteLine($"考试URL格式正确: {(isValidExamUrl ? "✓" : "✗")}");
                Debug.WriteLine($"模拟考试URL格式正确: {(isValidMockUrl ? "✓" : "✗")}");
                Debug.WriteLine($"认证URL格式正确: {(isValidAuthUrl ? "✓" : "✗")}");
                
                if (isValidExamUrl && isValidMockUrl && isValidAuthUrl)
                {
                    Debug.WriteLine("✓ 所有API URL格式验证通过");
                }
                else
                {
                    Debug.WriteLine("✗ 部分API URL格式验证失败");
                }
            }
            else
            {
                Debug.WriteLine("✗ 无法获取ConfigurationService实例");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ API URL验证测试失败: {ex.Message}");
        }
        
        Debug.WriteLine("");
    }

    /// <summary>
    /// 验证各服务的URL构建
    /// </summary>
    public static void VerifyServiceUrlConstruction()
    {
        Debug.WriteLine("=== 服务URL构建验证 ===");
        
        try
        {
            // 测试ExamAttemptService
            IExamAttemptService? examAttemptService = AppServiceManager.GetService<IExamAttemptService>();
            if (examAttemptService != null)
            {
                Debug.WriteLine("✓ ExamAttemptService已注册");
                Debug.WriteLine("  预期API调用: /api/student/exams/completions");
            }
            else
            {
                Debug.WriteLine("✗ ExamAttemptService未注册");
            }

            // 测试StudentMockExamService
            IStudentMockExamService? mockExamService = AppServiceManager.GetService<IStudentMockExamService>();
            if (mockExamService != null)
            {
                Debug.WriteLine("✓ StudentMockExamService已注册");
                Debug.WriteLine("  预期API调用: /api/student/mock-exams");
            }
            else
            {
                Debug.WriteLine("✗ StudentMockExamService未注册");
            }

            // 测试AuthenticationService
            IAuthenticationService? authService = AppServiceManager.GetService<IAuthenticationService>();
            if (authService != null)
            {
                Debug.WriteLine("✓ AuthenticationService已注册");
                Debug.WriteLine("  预期API调用: /api/student/auth/*");
            }
            else
            {
                Debug.WriteLine("✗ AuthenticationService未注册");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ 服务URL构建验证失败: {ex.Message}");
        }
        
        Debug.WriteLine("");
    }

    /// <summary>
    /// 验证URL中是否存在重复的/api路径
    /// </summary>
    public static void VerifyNoDuplicateApiPaths()
    {
        Debug.WriteLine("=== 重复API路径检查 ===");
        
        try
        {
            IConfigurationService? configService = AppServiceManager.GetService<IConfigurationService>();
            if (configService != null)
            {
                string baseUrl = configService.ApiBaseUrl;
                
                // 构建各种API URL并检查是否有重复的/api
                string[] testEndpoints = {
                    "/api/student/exams/completions",
                    "/api/student/mock-exams",
                    "/api/student/auth/login",
                    "/api/student/specialized-trainings",
                    "/api/student/comprehensive-trainings"
                };

                bool hasErrors = false;
                foreach (string endpoint in testEndpoints)
                {
                    string fullUrl = $"{baseUrl}{endpoint}";
                    
                    // 检查是否包含重复的/api
                    if (fullUrl.Contains("/api/api/"))
                    {
                        Debug.WriteLine($"✗ 发现重复/api路径: {fullUrl}");
                        hasErrors = true;
                    }
                    else
                    {
                        Debug.WriteLine($"✓ URL格式正确: {fullUrl}");
                    }
                }

                if (!hasErrors)
                {
                    Debug.WriteLine("✓ 所有API URL都没有重复的/api路径");
                }
                else
                {
                    Debug.WriteLine("✗ 发现重复的/api路径问题");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ 重复API路径检查失败: {ex.Message}");
        }
        
        Debug.WriteLine("");
    }

    /// <summary>
    /// 运行所有API URL验证测试
    /// </summary>
    public static void RunAllTests()
    {
        Debug.WriteLine("开始API URL验证测试...");
        Debug.WriteLine("");
        
        VerifyApiUrlConstruction();
        VerifyServiceUrlConstruction();
        VerifyNoDuplicateApiPaths();
        
        Debug.WriteLine("API URL验证测试完成。");
    }
}
