using System.Diagnostics;
using System.Text.Json;
using Examina.Services;

namespace Examina.Tests;

/// <summary>
/// 数据一致性验证工具
/// </summary>
public static class DataConsistencyVerification
{
    /// <summary>
    /// 验证考试次数统计的数据一致性
    /// </summary>
    public static async Task VerifyExamAttemptConsistency()
    {
        Debug.WriteLine("=== 考试次数统计数据一致性验证 ===");
        
        try
        {
            IAuthenticationService? authService = AppServiceManager.GetService<IAuthenticationService>();
            IConfigurationService? configService = AppServiceManager.GetService<IConfigurationService>();
            
            if (authService == null || configService == null)
            {
                Debug.WriteLine("✗ 无法获取必要的服务实例");
                return;
            }

            // 检查认证状态
            bool isAuthenticated = authService.IsAuthenticated;
            if (!isAuthenticated)
            {
                Debug.WriteLine("⚠ 用户未认证，无法验证数据一致性");
                Debug.WriteLine("请先登录后再运行此验证");
                return;
            }

            string? token = await authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("✗ 无法获取认证令牌");
                return;
            }

            Debug.WriteLine("✓ 用户已认证，开始验证数据一致性");

            // 创建HttpClient来直接调用API
            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Examina-Desktop-Client/1.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            string baseUrl = configService.ApiBaseUrl;
            string apiUrl = $"{baseUrl}/api/student/exams/completions";

            Debug.WriteLine($"调用API: {apiUrl}");

            // 调用API获取考试完成记录
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
            
            if (response.IsSuccessStatusCode)
            {
                string jsonContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"API响应状态: {response.StatusCode}");
                Debug.WriteLine($"响应内容长度: {jsonContent.Length} 字符");

                // 尝试解析JSON
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(jsonContent);
                    JsonElement root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        int recordCount = root.GetArrayLength();
                        Debug.WriteLine($"✓ 成功获取到 {recordCount} 条考试完成记录");

                        if (recordCount > 0)
                        {
                            Debug.WriteLine("考试完成记录详情:");
                            int displayCount = Math.Min(recordCount, 5); // 只显示前5条
                            
                            for (int i = 0; i < displayCount; i++)
                            {
                                JsonElement record = root[i];
                                
                                int examId = record.TryGetProperty("examId", out JsonElement examIdElement) ? examIdElement.GetInt32() : 0;
                                string status = record.TryGetProperty("status", out JsonElement statusElement) ? statusElement.GetString() ?? "Unknown" : "Unknown";
                                string completedAt = record.TryGetProperty("completedAt", out JsonElement completedAtElement) ? completedAtElement.GetString() ?? "未完成" : "未完成";
                                
                                Debug.WriteLine($"  记录 {i + 1}: 考试ID={examId}, 状态={status}, 完成时间={completedAt}");
                            }

                            if (recordCount > 5)
                            {
                                Debug.WriteLine($"  ... 还有 {recordCount - 5} 条记录");
                            }

                            Debug.WriteLine("");
                            Debug.WriteLine("数据一致性分析:");
                            Debug.WriteLine($"  后端记录总数: {recordCount}");
                            Debug.WriteLine("  建议检查前端显示的考试次数是否与此数字一致");
                            
                            if (recordCount >= 10)
                            {
                                Debug.WriteLine("✓ 后端确实有大量考试记录，前端应该显示相应的次数");
                            }
                            else if (recordCount > 0)
                            {
                                Debug.WriteLine($"⚠ 后端有 {recordCount} 条记录，请确认这是否符合预期");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("⚠ 后端没有考试完成记录");
                            Debug.WriteLine("这可能说明:");
                            Debug.WriteLine("  1. 确实没有参加过考试");
                            Debug.WriteLine("  2. 考试记录存储在其他表中");
                            Debug.WriteLine("  3. API权限或查询条件有问题");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"⚠ API返回的不是数组格式: {root.ValueKind}");
                        Debug.WriteLine($"响应内容: {jsonContent}");
                    }
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"✗ JSON解析失败: {ex.Message}");
                    Debug.WriteLine($"响应内容: {jsonContent}");
                }
            }
            else
            {
                Debug.WriteLine($"✗ API调用失败: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"错误内容: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ 数据一致性验证失败: {ex.Message}");
            Debug.WriteLine($"异常详情: {ex}");
        }
        
        Debug.WriteLine("");
    }

    /// <summary>
    /// 验证不同类型考试的记录
    /// </summary>
    public static async Task VerifyDifferentExamTypes()
    {
        Debug.WriteLine("=== 不同类型考试记录验证 ===");
        
        try
        {
            IAuthenticationService? authService = AppServiceManager.GetService<IAuthenticationService>();
            IConfigurationService? configService = AppServiceManager.GetService<IConfigurationService>();
            
            if (authService == null || configService == null)
            {
                Debug.WriteLine("✗ 无法获取必要的服务实例");
                return;
            }

            string? token = await authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("✗ 无法获取认证令牌");
                return;
            }

            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Examina-Desktop-Client/1.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            string baseUrl = configService.ApiBaseUrl;

            // 检查模拟考试记录
            string mockExamUrl = $"{baseUrl}/api/student/mock-exams";
            Debug.WriteLine($"检查模拟考试记录: {mockExamUrl}");
            
            HttpResponseMessage mockResponse = await httpClient.GetAsync(mockExamUrl);
            if (mockResponse.IsSuccessStatusCode)
            {
                string mockContent = await mockResponse.Content.ReadAsStringAsync();
                using JsonDocument mockDoc = JsonDocument.Parse(mockContent);
                int mockCount = mockDoc.RootElement.ValueKind == JsonValueKind.Array ? mockDoc.RootElement.GetArrayLength() : 0;
                Debug.WriteLine($"模拟考试记录数: {mockCount}");
            }

            Debug.WriteLine("建议同时检查:");
            Debug.WriteLine("  1. 正式考试完成记录 (ExamCompletions)");
            Debug.WriteLine("  2. 模拟考试完成记录 (MockExamCompletions)");
            Debug.WriteLine("  3. 综合训练完成记录 (ComprehensiveTrainingCompletions)");
            Debug.WriteLine("  4. 专项训练完成记录 (SpecializedTrainingCompletions)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ 不同类型考试记录验证失败: {ex.Message}");
        }
        
        Debug.WriteLine("");
    }

    /// <summary>
    /// 运行所有数据一致性验证
    /// </summary>
    public static async Task RunAllVerifications()
    {
        Debug.WriteLine("开始数据一致性验证...");
        Debug.WriteLine("");
        
        await VerifyExamAttemptConsistency();
        await VerifyDifferentExamTypes();
        
        Debug.WriteLine("数据一致性验证完成。");
    }
}
