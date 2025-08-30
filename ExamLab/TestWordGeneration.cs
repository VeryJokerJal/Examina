using ExamLab.Services.DocumentGeneration;

namespace ExamLab;

/// <summary>
/// Word文档生成测试程序
/// </summary>
public static class TestWordGeneration
{
    /// <summary>
    /// 测试Word文档生成功能
    /// </summary>
    public static async Task TestAsync()
    {
        Console.WriteLine("=== ExamLab Word文档生成功能测试 ===");
        Console.WriteLine();

        try
        {
            // 设置输出文件路径
            string outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Word文档生成测试_{DateTime.Now:yyyyMMdd_HHmmss}.docx"
            );

            Console.WriteLine($"输出文件路径: {outputPath}");
            Console.WriteLine();

            // 运行测试
            DocumentGenerationResult result = await WordDocumentGeneratorTest.RunTestAsync(outputPath);

            Console.WriteLine();
            Console.WriteLine("=== 测试完成 ===");

            if (result.IsSuccess)
            {
                Console.WriteLine("✅ 测试成功!");
                Console.WriteLine($"📄 生成的文档包含了以下操作点的演示:");
                Console.WriteLine("   • 段落字体设置");
                Console.WriteLine("   • 段落字号设置");
                Console.WriteLine("   • 段落对齐方式");
                Console.WriteLine("   • 纸张大小设置");
                Console.WriteLine("   • 页边距设置");
                Console.WriteLine("   • 水印文字设置");
                Console.WriteLine("   • 表格创建");
                Console.WriteLine("   • 单元格内容设置");
                Console.WriteLine("   • 自选图形插入");
                Console.WriteLine("   • 文本框内容设置");
                Console.WriteLine("   • 查找替换操作");
                Console.WriteLine();
                Console.WriteLine($"📁 请查看生成的文档: {outputPath}");
            }
            else
            {
                Console.WriteLine("❌ 测试失败!");
                Console.WriteLine($"错误信息: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试过程中发生异常: {ex.Message}");
            Console.WriteLine($"详细错误: {ex}");
        }

        Console.WriteLine();
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}
