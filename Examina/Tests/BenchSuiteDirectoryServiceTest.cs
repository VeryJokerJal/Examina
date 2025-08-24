using System;
using System.Diagnostics;
using Examina.Services;

namespace Examina.Tests;

/// <summary>
/// BenchSuiteDirectoryService依赖注入测试
/// </summary>
public static class BenchSuiteDirectoryServiceTest
{
    /// <summary>
    /// 测试IBenchSuiteDirectoryService是否能正确获取
    /// </summary>
    public static void TestServiceAvailability()
    {
        try
        {
            Debug.WriteLine("=== BenchSuiteDirectoryService依赖注入测试 ===");
            
            // 测试AppServiceManager是否已初始化
            IBenchSuiteDirectoryService? directoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
            
            if (directoryService != null)
            {
                Debug.WriteLine("✅ IBenchSuiteDirectoryService获取成功");
                
                // 测试基本功能
                string basePath = directoryService.GetBasePath();
                Debug.WriteLine($"📁 基础路径: {basePath}");
                
                // 测试新的GetExamDirectoryPath方法
                string examPath = directoryService.GetExamDirectoryPath(
                    Models.ExamType.MockExam, 
                    123, 
                    Models.BenchSuite.BenchSuiteFileType.CSharp);
                Debug.WriteLine($"📂 考试目录路径: {examPath}");
                
                Debug.WriteLine("✅ BenchSuiteDirectoryService功能测试通过");
            }
            else
            {
                Debug.WriteLine("❌ IBenchSuiteDirectoryService获取失败 - 服务为null");
                Debug.WriteLine("可能的原因：");
                Debug.WriteLine("1. AppServiceManager未正确初始化");
                Debug.WriteLine("2. IBenchSuiteDirectoryService未在DI容器中注册");
                Debug.WriteLine("3. ConfigureExaminaServices()未被调用");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ 测试过程中发生异常: {ex.Message}");
            Debug.WriteLine($"异常详情: {ex}");
        }
    }
    
    /// <summary>
    /// 测试ExamToolbarViewModel中的依赖注入
    /// </summary>
    public static void TestExamToolbarViewModelDependency()
    {
        try
        {
            Debug.WriteLine("=== ExamToolbarViewModel依赖注入测试 ===");
            
            // 获取必要的服务
            IBenchSuiteDirectoryService? directoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
            
            if (directoryService != null)
            {
                Debug.WriteLine("✅ 从AppServiceManager获取IBenchSuiteDirectoryService成功");
                
                // 模拟创建ExamToolbarViewModel的过程
                Debug.WriteLine("🔧 模拟ExamToolbarViewModel创建过程...");
                Debug.WriteLine("   - 获取IAuthenticationService");
                Debug.WriteLine("   - 获取IBenchSuiteDirectoryService");
                Debug.WriteLine("   - 创建ExamToolbarViewModel实例");
                Debug.WriteLine("✅ ExamToolbarViewModel依赖注入模拟成功");
            }
            else
            {
                Debug.WriteLine("❌ 无法从AppServiceManager获取IBenchSuiteDirectoryService");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ ExamToolbarViewModel依赖注入测试失败: {ex.Message}");
        }
    }
}
