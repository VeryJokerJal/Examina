using Examina.Configuration;
using Examina.Models;
using Examina.Services;

namespace Examina.Examples;

/// <summary>
/// BenchSuite集成使用示例
/// </summary>
public static class BenchSuiteIntegrationExample
{
    /// <summary>
    /// 应用程序启动时的BenchSuite集成示例
    /// </summary>
    public static async Task<bool> InitializeBenchSuiteIntegrationAsync()
    {
        try
        {
            Console.WriteLine("🚀 开始初始化BenchSuite集成...");

            // 1. 配置BenchSuite集成服务
            IServiceProvider serviceProvider = await BenchSuiteIntegrationSetup.ConfigureBenchSuiteIntegrationAsync();
            Console.WriteLine("✅ BenchSuite服务配置完成");

            // 2. 验证集成配置
            BenchSuiteIntegrationValidationResult validationResult = await BenchSuiteIntegrationSetup.ValidateIntegrationAsync();
            Console.WriteLine($"📋 集成验证结果:\n{validationResult.GetValidationSummary()}");

            // 3. 如果验证通过，显示集成状态
            if (validationResult.OverallValid)
            {
                await DisplayIntegrationStatusAsync();
                return true;
            }
            else
            {
                Console.WriteLine("❌ BenchSuite集成验证失败，将使用原有考试提交逻辑");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BenchSuite集成初始化失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 显示集成状态信息
    /// </summary>
    private static async Task DisplayIntegrationStatusAsync()
    {
        try
        {
            Console.WriteLine("\n📊 BenchSuite集成状态:");

            // 获取目录服务并显示目录信息
            IBenchSuiteDirectoryService? directoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
            if (directoryService != null)
            {
                string basePath = directoryService.GetBasePath();
                Console.WriteLine($"📁 基础目录: {basePath}");

                // 显示各文件类型目录
                foreach (Models.BenchSuite.BenchSuiteFileType fileType in Enum.GetValues<Models.BenchSuite.BenchSuiteFileType>())
                {
                    string directoryPath = directoryService.GetDirectoryPath(fileType);
                    bool exists = System.IO.Directory.Exists(directoryPath);
                    Console.WriteLine($"   {fileType}: {directoryPath} {(exists ? "✅" : "❌")}");
                }

                // 获取目录使用情况
                Models.BenchSuite.BenchSuiteDirectoryUsageInfo usageInfo = await directoryService.GetDirectoryUsageAsync();
                Console.WriteLine($"📈 目录使用情况: {usageInfo.TotalFileCount} 个文件, {usageInfo.TotalSizeBytes / 1024 / 1024:F2} MB");
            }

            // 获取集成服务并显示支持的文件类型
            IBenchSuiteIntegrationService? integrationService = AppServiceManager.GetService<IBenchSuiteIntegrationService>();
            if (integrationService != null)
            {
                IEnumerable<Models.BenchSuite.BenchSuiteFileType> supportedTypes = integrationService.GetSupportedFileTypes();
                Console.WriteLine($"🎯 支持的文件类型: {string.Join(", ", supportedTypes)}");
            }

            Console.WriteLine("✅ BenchSuite集成已就绪，考试提交时将自动进行评分");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 显示集成状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 模拟考试提交的BenchSuite集成示例
    /// </summary>
    public static async Task<bool> SimulateExamSubmissionAsync(int examId, ExamType examType)
    {
        try
        {
            Console.WriteLine($"\n🎯 模拟考试提交 - ID: {examId}, 类型: {examType}");

            // 获取增强考试工具栏服务
            EnhancedExamToolbarService? enhancedService = AppServiceManager.GetService<EnhancedExamToolbarService>();
            if (enhancedService == null)
            {
                Console.WriteLine("❌ EnhancedExamToolbarService不可用");
                return false;
            }

            // 根据考试类型调用相应的提交方法
            bool submitResult = examType switch
            {
                ExamType.MockExam => await enhancedService.SubmitMockExamAsync(examId),
                ExamType.FormalExam => await enhancedService.SubmitFormalExamAsync(examId),
                ExamType.ComprehensiveTraining => await enhancedService.SubmitComprehensiveTrainingAsync(examId),
                _ => false
            };

            if (submitResult)
            {
                Console.WriteLine("✅ 考试提交成功，BenchSuite评分已完成");
                return true;
            }
            else
            {
                Console.WriteLine("❌ 考试提交失败");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 模拟考试提交失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 测试BenchSuite集成功能
    /// </summary>
    public static async Task RunIntegrationTestAsync()
    {
        try
        {
            Console.WriteLine("🧪 开始BenchSuite集成功能测试...");

            // 获取测试器
            BenchSuiteIntegrationTester? tester = AppServiceManager.GetService<BenchSuiteIntegrationTester>();
            if (tester == null)
            {
                Console.WriteLine("❌ BenchSuiteIntegrationTester不可用");
                return;
            }

            // 执行集成测试
            BenchSuiteTestResult testResult = await tester.TestIntegrationAsync();

            Console.WriteLine($"\n📋 集成测试结果:");
            Console.WriteLine($"总体结果: {(testResult.OverallSuccess ? "✅ 通过" : "❌ 失败")}");
            Console.WriteLine($"测试耗时: {testResult.ElapsedMilliseconds} ms");

            if (!string.IsNullOrEmpty(testResult.ErrorMessage))
            {
                Console.WriteLine($"错误信息: {testResult.ErrorMessage}");
            }

            // 显示各项测试结果
            Console.WriteLine($"\n📊 详细测试结果:");
            Console.WriteLine($"目录结构测试: {(testResult.DirectoryTestResult.IsSuccess ? "✅" : "❌")} - {testResult.DirectoryTestResult.TestName}");
            Console.WriteLine($"服务可用性测试: {(testResult.ServiceAvailabilityResult.IsSuccess ? "✅" : "❌")} - {testResult.ServiceAvailabilityResult.TestName}");
            Console.WriteLine($"评分功能测试: {(testResult.ScoringTestResult.IsSuccess ? "✅" : "❌")} - {testResult.ScoringTestResult.TestName}");
            Console.WriteLine($"文件类型支持测试: {(testResult.FileTypeSupportResult.IsSuccess ? "✅" : "❌")} - {testResult.FileTypeSupportResult.TestName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 集成测试失败: {ex.Message}");
        }
    }
}

/// <summary>
/// 应用程序启动集成示例
/// </summary>
public static class AppStartupExample
{
    /// <summary>
    /// 在应用程序Main方法中调用的示例
    /// </summary>
    public static async Task<int> MainAsync(string[] args)
    {
        try
        {
            Console.WriteLine("🎓 Examina应用程序启动");

            // 初始化BenchSuite集成
            bool integrationSuccess = await BenchSuiteIntegrationExample.InitializeBenchSuiteIntegrationAsync();

            if (integrationSuccess)
            {
                Console.WriteLine("✅ BenchSuite集成初始化成功");

                // 可选：运行集成测试
                if (args.Contains("--test-integration"))
                {
                    await BenchSuiteIntegrationExample.RunIntegrationTestAsync();
                }

                // 可选：模拟考试提交测试
                if (args.Contains("--test-submit"))
                {
                    _ = await BenchSuiteIntegrationExample.SimulateExamSubmissionAsync(999, ExamType.MockExam);
                }
            }
            else
            {
                Console.WriteLine("⚠️ BenchSuite集成初始化失败，应用程序将使用原有考试提交逻辑");
            }

            Console.WriteLine("🚀 应用程序启动完成");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 应用程序启动失败: {ex.Message}");
            return 1;
        }
    }
}
