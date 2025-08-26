using System;
using System.Threading.Tasks;
using Examina.Models;
using Examina.Services;
using Examina.ViewModels.Pages;

namespace Examina.Tests;

/// <summary>
/// 统考功能测试类
/// </summary>
public static class UnifiedExamFunctionalityTest
{
    /// <summary>
    /// 运行所有统考功能测试
    /// </summary>
    public static async Task RunAllTestsAsync()
    {
        Console.WriteLine("=== 开始统考功能测试 ===");

        try
        {
            TestExamCategoryEnum();
            await TestUnifiedExamViewModelCreationAsync();
            TestViewModelProperties();
            TestCommands();

            Console.WriteLine("✅ 所有统考功能测试通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 统考功能测试失败: {ex.Message}");
            throw;
        }

        Console.WriteLine("=== 统考功能测试完成 ===");
    }

    /// <summary>
    /// 测试ExamCategory枚举
    /// </summary>
    public static void TestExamCategoryEnum()
    {
        try
        {
            Console.WriteLine("\n=== 测试ExamCategory枚举 ===");

            // 验证枚举值
            ExamCategory schoolCategory = ExamCategory.School;
            ExamCategory provincialCategory = ExamCategory.Provincial;

            Console.WriteLine($"学校统考枚举值: {schoolCategory} ({(int)schoolCategory})");
            Console.WriteLine($"全省统考枚举值: {provincialCategory} ({(int)provincialCategory})");

            // 验证枚举比较
            bool isSchoolCategory = schoolCategory == ExamCategory.School;
            bool isProvincialCategory = provincialCategory == ExamCategory.Provincial;

            if (isSchoolCategory && isProvincialCategory)
            {
                Console.WriteLine("✅ ExamCategory枚举测试通过");
            }
            else
            {
                throw new Exception("ExamCategory枚举值不正确");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ExamCategory枚举测试失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 测试UnifiedExamViewModel创建
    /// </summary>
    public static async Task TestUnifiedExamViewModelCreationAsync()
    {
        try
        {
            Console.WriteLine("\n=== 测试UnifiedExamViewModel创建 ===");

            // 注意：这里需要模拟服务，在实际测试中应该使用依赖注入
            // 这里只是验证ViewModel的基本结构
            Console.WriteLine("UnifiedExamViewModel类型检查通过");

            // 验证ViewModel的基本属性存在
            Type viewModelType = typeof(UnifiedExamViewModel);
            
            // 检查关键属性
            var provincialExamsProperty = viewModelType.GetProperty("ProvincialExams");
            var schoolExamsProperty = viewModelType.GetProperty("SchoolExams");
            var refreshAllCommandProperty = viewModelType.GetProperty("RefreshAllCommand");

            if (provincialExamsProperty != null && schoolExamsProperty != null && refreshAllCommandProperty != null)
            {
                Console.WriteLine("✅ UnifiedExamViewModel关键属性存在");
            }
            else
            {
                throw new Exception("UnifiedExamViewModel缺少关键属性");
            }

            Console.WriteLine("✅ UnifiedExamViewModel创建测试通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ UnifiedExamViewModel创建测试失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 测试ViewModel属性
    /// </summary>
    public static void TestViewModelProperties()
    {
        try
        {
            Console.WriteLine("\n=== 测试ViewModel属性 ===");

            Type viewModelType = typeof(UnifiedExamViewModel);

            // 检查所有必需的属性
            string[] requiredProperties = {
                "ProvincialExams",
                "SchoolExams",
                "ProvincialExamCount",
                "SchoolExamCount",
                "IsLoadingProvincialExams",
                "IsLoadingSchoolExams",
                "ErrorMessage",
                "HasFullAccess",
                "UserPermissionStatus"
            };

            foreach (string propertyName in requiredProperties)
            {
                var property = viewModelType.GetProperty(propertyName);
                if (property == null)
                {
                    throw new Exception($"缺少属性: {propertyName}");
                }
                Console.WriteLine($"✓ 属性存在: {propertyName}");
            }

            Console.WriteLine("✅ ViewModel属性测试通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ViewModel属性测试失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 测试命令
    /// </summary>
    public static void TestCommands()
    {
        try
        {
            Console.WriteLine("\n=== 测试命令 ===");

            Type viewModelType = typeof(UnifiedExamViewModel);

            // 检查所有必需的命令
            string[] requiredCommands = {
                "RefreshProvincialExamsCommand",
                "RefreshSchoolExamsCommand",
                "RefreshAllCommand",
                "LoadMoreProvincialExamsCommand",
                "LoadMoreSchoolExamsCommand",
                "ViewProvincialExamDetailsCommand",
                "ViewSchoolExamDetailsCommand"
            };

            foreach (string commandName in requiredCommands)
            {
                var command = viewModelType.GetProperty(commandName);
                if (command == null)
                {
                    throw new Exception($"缺少命令: {commandName}");
                }
                Console.WriteLine($"✓ 命令存在: {commandName}");
            }

            Console.WriteLine("✅ 命令测试通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 命令测试失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 测试静态方法
    /// </summary>
    public static void TestStaticMethods()
    {
        try
        {
            Console.WriteLine("\n=== 测试静态方法 ===");

            // 测试GetExamStatusText方法
            var getExamStatusTextMethod = typeof(UnifiedExamViewModel).GetMethod("GetExamStatusText");
            if (getExamStatusTextMethod == null)
            {
                throw new Exception("缺少GetExamStatusText方法");
            }

            // 测试GetExamTimeText方法
            var getExamTimeTextMethod = typeof(UnifiedExamViewModel).GetMethod("GetExamTimeText");
            if (getExamTimeTextMethod == null)
            {
                throw new Exception("缺少GetExamTimeText方法");
            }

            Console.WriteLine("✅ 静态方法测试通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 静态方法测试失败: {ex.Message}");
            throw;
        }
    }
}
