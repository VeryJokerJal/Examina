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
        Debug.WriteLine($"验证开始时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        try
        {
            Debug.WriteLine("获取服务实例...");
            IAuthenticationService? authService = AppServiceManager.GetService<IAuthenticationService>();
            IConfigurationService? configService = AppServiceManager.GetService<IConfigurationService>();

            if (authService == null || configService == null)
            {
                Debug.WriteLine("✗ 无法获取必要的服务实例");
                Debug.WriteLine($"AuthService: {(authService != null ? "✓" : "✗")}");
                Debug.WriteLine($"ConfigService: {(configService != null ? "✓" : "✗")}");
                return;
            }
            Debug.WriteLine("✓ 服务实例获取成功");

            // 检查认证状态
            Debug.WriteLine("检查用户认证状态...");
            bool isAuthenticated = authService.IsAuthenticated;
            Debug.WriteLine($"认证状态: {(isAuthenticated ? "已认证" : "未认证")}");

            if (!isAuthenticated)
            {
                Debug.WriteLine("⚠ 用户未认证，无法验证数据一致性");
                Debug.WriteLine("请先登录后再运行此验证");

                // 尝试获取当前用户信息
                var currentUser = authService.CurrentUser;
                Debug.WriteLine($"当前用户: {(currentUser != null ? $"ID={currentUser.Id}, Username={currentUser.Username}" : "无")}");
                return;
            }

            Debug.WriteLine("获取认证令牌...");
            string? token = await authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("✗ 无法获取认证令牌");
                Debug.WriteLine($"令牌过期时间: {authService.TokenExpiresAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "未知"}");
                Debug.WriteLine($"需要刷新令牌: {authService.NeedsTokenRefresh}");
                return;
            }

            Debug.WriteLine($"✓ 成功获取认证令牌，长度: {token.Length} 字符");
            Debug.WriteLine($"令牌前缀: {token.Substring(0, Math.Min(20, token.Length))}...");
            Debug.WriteLine("✓ 用户已认证，开始验证数据一致性");

            // 创建HttpClient来直接调用API
            Debug.WriteLine("创建HTTP客户端...");
            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Examina-Desktop-Client/1.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            Debug.WriteLine("✓ HTTP请求头设置完成");

            string baseUrl = configService.ApiBaseUrl;
            string apiUrl = $"{baseUrl}/api/student/exams/completions";

            Debug.WriteLine($"API基础URL: {baseUrl}");
            Debug.WriteLine($"完整API URL: {apiUrl}");
            Debug.WriteLine("发送API请求...");

            // 调用API获取考试完成记录
            DateTime requestStart = DateTime.Now;
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
            TimeSpan requestDuration = DateTime.Now - requestStart;

            Debug.WriteLine($"API响应状态: {response.StatusCode} ({(int)response.StatusCode})");
            Debug.WriteLine($"请求耗时: {requestDuration.TotalMilliseconds:F0} 毫秒");
            Debug.WriteLine($"响应头信息:");
            foreach (var header in response.Headers)
            {
                Debug.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }

            if (response.IsSuccessStatusCode)
            {
                string jsonContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"✓ API调用成功");
                Debug.WriteLine($"响应内容长度: {jsonContent.Length} 字符");
                Debug.WriteLine($"Content-Type: {response.Content.Headers.ContentType?.ToString() ?? "未知"}");

                // 显示响应内容预览
                if (jsonContent.Length > 0)
                {
                    string preview = jsonContent.Length > 500 ? jsonContent.Substring(0, 500) + "..." : jsonContent;
                    Debug.WriteLine($"响应内容预览: {preview}");
                }

                // 尝试解析JSON
                Debug.WriteLine("开始解析JSON响应...");
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(jsonContent);
                    JsonElement root = doc.RootElement;
                    Debug.WriteLine($"JSON根元素类型: {root.ValueKind}");

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        int recordCount = root.GetArrayLength();
                        Debug.WriteLine($"✓ JSON解析成功，获取到 {recordCount} 条考试完成记录");

                        if (recordCount > 0)
                        {
                            Debug.WriteLine("考试完成记录详情:");
                            int displayCount = Math.Min(recordCount, 5); // 只显示前5条

                            for (int i = 0; i < displayCount; i++)
                            {
                                JsonElement record = root[i];

                                int id = record.TryGetProperty("id", out JsonElement idElement) ? idElement.GetInt32() : 0;
                                int examId = record.TryGetProperty("examId", out JsonElement examIdElement) ? examIdElement.GetInt32() : 0;
                                int studentUserId = record.TryGetProperty("studentUserId", out JsonElement studentUserIdElement) ? studentUserIdElement.GetInt32() : 0;
                                string status = record.TryGetProperty("status", out JsonElement statusElement) ? statusElement.GetString() ?? "Unknown" : "Unknown";
                                string startedAt = record.TryGetProperty("startedAt", out JsonElement startedAtElement) ? startedAtElement.GetString() ?? "未开始" : "未开始";
                                string completedAt = record.TryGetProperty("completedAt", out JsonElement completedAtElement) ? completedAtElement.GetString() ?? "未完成" : "未完成";
                                string createdAt = record.TryGetProperty("createdAt", out JsonElement createdAtElement) ? createdAtElement.GetString() ?? "未知" : "未知";

                                Debug.WriteLine($"  记录 {i + 1}: ID={id}, ExamId={examId}, StudentUserId={studentUserId}");
                                Debug.WriteLine($"    状态={status}, 开始时间={startedAt}, 完成时间={completedAt}");
                                Debug.WriteLine($"    创建时间={createdAt}");
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
                    Debug.WriteLine($"JSON异常位置: Line {ex.LineNumber}, Position {ex.BytePositionInLine}");
                    Debug.WriteLine($"原始响应内容: {jsonContent}");
                }
            }
            else
            {
                Debug.WriteLine($"✗ API调用失败: {response.StatusCode}");
                Debug.WriteLine($"状态码说明: {response.ReasonPhrase}");
                string errorContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"错误响应长度: {errorContent.Length} 字符");
                Debug.WriteLine($"错误内容: {errorContent}");

                // 显示响应头以便调试
                Debug.WriteLine("错误响应头:");
                foreach (var header in response.Headers)
                {
                    Debug.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== 数据一致性验证异常 ===");
            Debug.WriteLine($"异常类型: {ex.GetType().Name}");
            Debug.WriteLine($"异常消息: {ex.Message}");
            Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"内部异常: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            Debug.WriteLine($"=== 数据一致性验证异常结束 ===");
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
