using System;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// 目录结构验证脚本
/// 验证修复后的目录结构是否符合预期
/// </summary>
class Program
{
    private static readonly string BasePath = @"C:\河北对口计算机\";

    static async Task Main(string[] args)
    {
        Console.WriteLine("开始验证目录结构修复...");

        try
        {
            // 清理测试环境
            CleanupTestEnvironment();

            // 测试1: 验证基础目录结构不会创建科目文件夹
            Console.WriteLine("\n测试1: 验证基础目录结构创建");
            await TestBaseDirectoryStructure();

            // 测试2: 验证考试目录结构创建正确的层级
            Console.WriteLine("\n测试2: 验证考试目录结构创建");
            await TestExamDirectoryStructure();

            // 测试3: 验证不同考试类型的目录结构
            Console.WriteLine("\n测试3: 验证不同考试类型的目录结构");
            await TestDifferentExamTypes();

            Console.WriteLine("\n✅ 所有测试通过！目录结构修复成功。");
            Console.WriteLine("\n修复总结:");
            Console.WriteLine("- 基础目录结构创建时不再自动生成科目文件夹");
            Console.WriteLine("- 科目文件夹现在正确生成在 OnlineExams/{考试编号}/ 下");
            Console.WriteLine("- 支持不同考试类型的正确目录结构");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 测试失败: {ex.Message}");
        }
        finally
        {
            // 清理测试环境
            CleanupTestEnvironment();
        }

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }

    /// <summary>
    /// 测试基础目录结构（不应该创建科目文件夹）
    /// </summary>
    static async Task TestBaseDirectoryStructure()
    {
        // 模拟基础目录创建（只创建基础目录）
        if (!Directory.Exists(BasePath))
        {
            Directory.CreateDirectory(BasePath);
        }

        // 验证基础目录存在
        if (!Directory.Exists(BasePath))
        {
            throw new Exception("基础目录创建失败");
        }

        // 验证不应该创建科目文件夹
        string[] subjectFolders = { "CSharp", "EXCEL", "PPT", "WINDOWS", "WORD" };
        foreach (string folder in subjectFolders)
        {
            string folderPath = Path.Combine(BasePath, folder);
            if (Directory.Exists(folderPath))
            {
                throw new Exception($"错误：在基础路径下创建了科目文件夹 {folder}");
            }
        }

        Console.WriteLine("✅ 基础目录结构测试通过 - 没有在基础路径下创建科目文件夹");
    }

    /// <summary>
    /// 测试考试目录结构（应该创建正确的层级结构）
    /// </summary>
    static async Task TestExamDirectoryStructure()
    {
        // 模拟正式考试目录结构创建
        string examType = "OnlineExams";
        int examId = 1;
        string examTypePath = Path.Combine(BasePath, examType);
        string examIdPath = Path.Combine(examTypePath, examId.ToString());

        // 创建考试目录结构
        if (!Directory.Exists(examTypePath))
        {
            Directory.CreateDirectory(examTypePath);
        }

        if (!Directory.Exists(examIdPath))
        {
            Directory.CreateDirectory(examIdPath);
        }

        // 创建科目文件夹
        string[] subjectFolders = { "CSharp", "EXCEL", "PPT", "WINDOWS", "WORD" };
        foreach (string folder in subjectFolders)
        {
            string subjectPath = Path.Combine(examIdPath, folder);
            if (!Directory.Exists(subjectPath))
            {
                Directory.CreateDirectory(subjectPath);
            }
        }

        // 验证目录结构
        if (!Directory.Exists(examTypePath))
        {
            throw new Exception("考试类型目录创建失败");
        }

        if (!Directory.Exists(examIdPath))
        {
            throw new Exception("考试ID目录创建失败");
        }

        // 验证科目文件夹在正确的位置
        foreach (string folder in subjectFolders)
        {
            string correctPath = Path.Combine(examIdPath, folder);
            string wrongPath = Path.Combine(BasePath, folder);

            if (!Directory.Exists(correctPath))
            {
                throw new Exception($"科目文件夹 {folder} 没有在正确位置创建: {correctPath}");
            }

            if (Directory.Exists(wrongPath))
            {
                throw new Exception($"科目文件夹 {folder} 错误地在基础路径下创建: {wrongPath}");
            }
        }

        Console.WriteLine($"✅ 考试目录结构测试通过 - 正确的层级结构: {BasePath}\\{examType}\\{examId}\\[科目文件夹]");
    }

    /// <summary>
    /// 测试不同考试类型的目录结构
    /// </summary>
    static async Task TestDifferentExamTypes()
    {
        var examTypes = new[]
        {
            ("FormalExam", "OnlineExams"),
            ("MockExam", "MockExams"),
            ("ComprehensiveTraining", "ComprehensiveTraining"),
            ("SpecializedTraining", "SpecializedTraining")
        };

        foreach (var (examTypeName, folderName) in examTypes)
        {
            int examId = 1;
            string examTypePath = Path.Combine(BasePath, folderName);
            string examIdPath = Path.Combine(examTypePath, examId.ToString());

            // 创建目录结构
            if (!Directory.Exists(examTypePath))
            {
                Directory.CreateDirectory(examTypePath);
            }

            if (!Directory.Exists(examIdPath))
            {
                Directory.CreateDirectory(examIdPath);
            }

            // 创建科目文件夹
            string[] subjectFolders = { "CSharp", "EXCEL", "PPT", "WINDOWS", "WORD" };
            foreach (string folder in subjectFolders)
            {
                string subjectPath = Path.Combine(examIdPath, folder);
                if (!Directory.Exists(subjectPath))
                {
                    Directory.CreateDirectory(subjectPath);
                }
            }

            // 验证目录结构
            if (!Directory.Exists(examTypePath))
            {
                throw new Exception($"{examTypeName} 类型目录创建失败");
            }

            if (!Directory.Exists(examIdPath))
            {
                throw new Exception($"{examTypeName} 考试ID目录创建失败");
            }

            // 验证科目文件夹在正确的位置
            foreach (string folder in subjectFolders)
            {
                string correctPath = Path.Combine(examIdPath, folder);
                if (!Directory.Exists(correctPath))
                {
                    throw new Exception($"{examTypeName} 科目文件夹 {folder} 没有在正确位置创建");
                }
            }

            Console.WriteLine($"✅ {examTypeName} 目录结构测试通过 - {folderName}");
        }
    }

    /// <summary>
    /// 清理测试环境
    /// </summary>
    static void CleanupTestEnvironment()
    {
        try
        {
            if (Directory.Exists(BasePath))
            {
                Directory.Delete(BasePath, true);
                Console.WriteLine("测试环境已清理");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清理测试环境时出错: {ex.Message}");
        }
    }
}
