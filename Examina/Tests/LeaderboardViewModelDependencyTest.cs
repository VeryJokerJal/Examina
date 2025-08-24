using System;
using System.Diagnostics;
using Examina.Services;
using Examina.ViewModels.Pages;

namespace Examina.Tests;

/// <summary>
/// LeaderboardViewModel依赖注入测试
/// </summary>
public static class LeaderboardViewModelDependencyTest
{
    /// <summary>
    /// 测试LeaderboardViewModel的依赖注入是否正常工作
    /// </summary>
    public static void TestLeaderboardViewModelDependencies()
    {
        try
        {
            Debug.WriteLine("=== LeaderboardViewModel依赖注入测试 ===");
            
            // 测试通过App.GetService获取LeaderboardViewModel
            if (Avalonia.Application.Current is App app)
            {
                Debug.WriteLine("📱 获取App实例成功");
                
                // 测试获取LeaderboardViewModel
                LeaderboardViewModel? leaderboardViewModel = app.GetService<LeaderboardViewModel>();
                
                if (leaderboardViewModel != null)
                {
                    Debug.WriteLine("✅ LeaderboardViewModel获取成功");
                    
                    // 测试基本功能
                    TestBasicFunctionality(leaderboardViewModel);
                    
                    // 测试服务依赖
                    TestServiceDependencies();
                    
                    Debug.WriteLine("✅ LeaderboardViewModel依赖注入测试通过");
                }
                else
                {
                    Debug.WriteLine("❌ LeaderboardViewModel获取失败 - 服务为null");
                    Debug.WriteLine("可能的原因：");
                    Debug.WriteLine("1. LeaderboardViewModel未在DI容器中注册");
                    Debug.WriteLine("2. 依赖的服务未正确注册");
                    Debug.WriteLine("3. 服务提供者未正确初始化");
                }
            }
            else
            {
                Debug.WriteLine("❌ 无法获取App实例");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ 测试过程中发生异常: {ex.Message}");
            Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// 测试LeaderboardViewModel的基本功能
    /// </summary>
    private static void TestBasicFunctionality(LeaderboardViewModel viewModel)
    {
        try
        {
            Debug.WriteLine("🔧 测试LeaderboardViewModel基本功能...");
            
            // 测试属性初始化
            if (viewModel.LeaderboardTypes.Count > 0)
            {
                Debug.WriteLine($"✅ 排行榜类型初始化成功，数量: {viewModel.LeaderboardTypes.Count}");
            }
            else
            {
                Debug.WriteLine("⚠️ 排行榜类型列表为空");
            }
            
            if (viewModel.ExamFilters.Count > 0)
            {
                Debug.WriteLine($"✅ 试卷筛选初始化成功，数量: {viewModel.ExamFilters.Count}");
            }
            else
            {
                Debug.WriteLine("⚠️ 试卷筛选列表为空");
            }
            
            if (viewModel.SortTypes.Count > 0)
            {
                Debug.WriteLine($"✅ 排序类型初始化成功，数量: {viewModel.SortTypes.Count}");
            }
            else
            {
                Debug.WriteLine("⚠️ 排序类型列表为空");
            }
            
            // 测试命令初始化
            if (viewModel.RefreshLeaderboardCommand != null)
            {
                Debug.WriteLine("✅ 刷新命令初始化成功");
            }
            else
            {
                Debug.WriteLine("❌ 刷新命令未初始化");
            }
            
            Debug.WriteLine("✅ 基本功能测试完成");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ 基本功能测试失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 测试各个服务的依赖注入状态
    /// </summary>
    private static void TestServiceDependencies()
    {
        try
        {
            Debug.WriteLine("🔧 测试服务依赖注入状态...");
            
            if (Avalonia.Application.Current is App app)
            {
                // 测试RankingService
                RankingService? rankingService = app.GetService<RankingService>();
                Debug.WriteLine($"📊 RankingService: {(rankingService != null ? "✅ 可用" : "❌ null")}");
                
                // 测试IStudentComprehensiveTrainingService
                IStudentComprehensiveTrainingService? comprehensiveService = app.GetService<IStudentComprehensiveTrainingService>();
                Debug.WriteLine($"📚 IStudentComprehensiveTrainingService: {(comprehensiveService != null ? "✅ 可用" : "❌ null")}");
                
                // 测试IStudentExamService
                IStudentExamService? examService = app.GetService<IStudentExamService>();
                Debug.WriteLine($"📝 IStudentExamService: {(examService != null ? "✅ 可用" : "❌ null")}");
                
                // 测试IStudentMockExamService
                IStudentMockExamService? mockExamService = app.GetService<IStudentMockExamService>();
                Debug.WriteLine($"🎯 IStudentMockExamService: {(mockExamService != null ? "✅ 可用" : "❌ null")}");
                
                // 统计结果
                int availableServices = 0;
                if (rankingService != null) availableServices++;
                if (comprehensiveService != null) availableServices++;
                if (examService != null) availableServices++;
                if (mockExamService != null) availableServices++;
                
                Debug.WriteLine($"📈 服务可用性统计: {availableServices}/4 个服务可用");
                
                if (availableServices == 4)
                {
                    Debug.WriteLine("✅ 所有必需服务都已正确注入");
                }
                else
                {
                    Debug.WriteLine("⚠️ 部分服务未正确注入，可能影响功能");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ 服务依赖测试失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 测试LeaderboardViewModel的数据加载功能
    /// </summary>
    public static void TestDataLoading()
    {
        try
        {
            Debug.WriteLine("=== LeaderboardViewModel数据加载测试 ===");
            
            if (Avalonia.Application.Current is App app)
            {
                LeaderboardViewModel? viewModel = app.GetService<LeaderboardViewModel>();
                
                if (viewModel != null)
                {
                    Debug.WriteLine("🔄 测试初始数据加载...");
                    
                    // 触发初始数据加载
                    viewModel.LoadInitialData();
                    
                    // 等待一段时间让异步操作完成
                    System.Threading.Thread.Sleep(2000);
                    
                    // 检查数据加载结果
                    if (viewModel.LeaderboardData.Count > 0)
                    {
                        Debug.WriteLine($"✅ 数据加载成功，记录数: {viewModel.LeaderboardData.Count}");
                        
                        // 检查第一条记录的数据完整性
                        var firstEntry = viewModel.LeaderboardData[0];
                        Debug.WriteLine($"📊 第一条记录: 排名={firstEntry.Rank}, 用户={firstEntry.Username}, 分数={firstEntry.Score}");
                        Debug.WriteLine($"🏫 学校信息: {firstEntry.SchoolName}, 班级: {firstEntry.ClassName}");
                    }
                    else
                    {
                        Debug.WriteLine("⚠️ 数据加载后记录为空");
                    }
                    
                    if (!viewModel.IsLoading)
                    {
                        Debug.WriteLine("✅ 加载状态正确更新");
                    }
                    else
                    {
                        Debug.WriteLine("⚠️ 加载状态未正确更新");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ 数据加载测试失败: {ex.Message}");
        }
    }
}
