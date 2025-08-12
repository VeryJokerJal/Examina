using System;
using System.Collections.Generic;
using BenchSuite.Services;

namespace TestParameterResolver
{
    /// <summary>
    /// 参数解析器功能测试程序
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== PowerPoint参数解析器功能测试 ===");
            Console.WriteLine();

            // 测试1：编号参数识别
            TestIndexParameterRecognition();

            // 测试2：-1参数解析
            TestMinusOneParameterResolution();

            // 测试3：参数一致性
            TestParameterConsistency();

            // 测试4：多个编号参数
            TestMultipleParameters();

            // 测试5：边界情况
            TestEdgeCases();

            Console.WriteLine();
            Console.WriteLine("=== 所有测试完成 ===");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        /// <summary>
        /// 测试编号参数识别
        /// </summary>
        static void TestIndexParameterRecognition()
        {
            Console.WriteLine("测试1：编号参数识别");
            
            string[] indexParams = { "SlideIndex", "SlideNumber", "TextBoxIndex", "TextBoxOrder", "ElementIndex", "ShapeNumber" };
            string[] nonIndexParams = { "FontSize", "ColorValue", "TextContent", "Layout" };

            foreach (string param in indexParams)
            {
                bool isIndex = ParameterResolver.IsIndexParameter(param);
                Console.WriteLine($"  {param}: {(isIndex ? "✓" : "✗")} {(isIndex ? "编号参数" : "非编号参数")}");
            }

            foreach (string param in nonIndexParams)
            {
                bool isIndex = ParameterResolver.IsIndexParameter(param);
                Console.WriteLine($"  {param}: {(isIndex ? "✗" : "✓")} {(isIndex ? "编号参数" : "非编号参数")}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// 测试-1参数解析
        /// </summary>
        static void TestMinusOneParameterResolution()
        {
            Console.WriteLine("测试2：-1参数解析");
            
            ParameterResolutionContext context = new();

            // 测试幻灯片编号
            string slideResult = ParameterResolver.ResolveParameter("SlideIndex", "-1", 5, context);
            Console.WriteLine($"  SlideIndex(-1, max=5): {slideResult}");

            // 测试文本框编号
            string textBoxResult = ParameterResolver.ResolveParameter("TextBoxIndex", "-1", 3, context);
            Console.WriteLine($"  TextBoxIndex(-1, max=3): {textBoxResult}");

            // 测试普通数字
            string normalResult = ParameterResolver.ResolveParameter("SlideIndex", "2", 5, context);
            Console.WriteLine($"  SlideIndex(2, max=5): {normalResult}");

            // 测试非编号参数
            string nonIndexResult = ParameterResolver.ResolveParameter("FontSize", "-1", 5, context);
            Console.WriteLine($"  FontSize(-1, max=5): {nonIndexResult}");

            Console.WriteLine();
        }

        /// <summary>
        /// 测试参数一致性
        /// </summary>
        static void TestParameterConsistency()
        {
            Console.WriteLine("测试3：参数一致性");
            
            ParameterResolutionContext context = new();

            // 第一次解析
            string result1 = ParameterResolver.ResolveParameter("SlideIndex", "-1", 10, context);
            Console.WriteLine($"  第一次解析 SlideIndex(-1): {result1}");

            // 第二次解析相同参数
            string result2 = ParameterResolver.ResolveParameter("SlideIndex", "-1", 10, context);
            Console.WriteLine($"  第二次解析 SlideIndex(-1): {result2}");

            bool isConsistent = result1 == result2;
            Console.WriteLine($"  一致性检查: {(isConsistent ? "✓" : "✗")} {(isConsistent ? "一致" : "不一致")}");

            Console.WriteLine();
        }

        /// <summary>
        /// 测试多个编号参数
        /// </summary>
        static void TestMultipleParameters()
        {
            Console.WriteLine("测试4：多个编号参数");
            
            ParameterResolutionContext context = new();

            // 测试逗号分隔的多个参数
            string multiResult = ParameterResolver.ResolveMultipleParameters("SlideIndexes", "-1,2,-1,4", 8, context);
            Console.WriteLine($"  SlideIndexes(-1,2,-1,4, max=8): {multiResult}");

            // 解析为数组
            int[] intArray = ParameterResolver.GetResolvedIntArray("SlideIndexes", context);
            Console.WriteLine($"  解析为数组: [{string.Join(", ", intArray)}]");

            Console.WriteLine();
        }

        /// <summary>
        /// 测试边界情况
        /// </summary>
        static void TestEdgeCases()
        {
            Console.WriteLine("测试5：边界情况");
            
            ParameterResolutionContext context = new();

            try
            {
                // 测试最大值为0
                ParameterResolver.ResolveParameter("SlideIndex", "-1", 0, context);
                Console.WriteLine("  最大值为0: ✗ 应该抛出异常");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("  最大值为0: ✓ 正确抛出异常");
            }

            // 测试空参数名
            string emptyNameResult = ParameterResolver.ResolveParameter("", "-1", 5, context);
            Console.WriteLine($"  空参数名(-1): {emptyNameResult}");

            // 测试无效数字
            string invalidNumberResult = ParameterResolver.ResolveParameter("SlideIndex", "abc", 5, context);
            Console.WriteLine($"  无效数字(abc): {invalidNumberResult}");

            // 测试最大值为1的边界
            string boundaryResult = ParameterResolver.ResolveParameter("TestIndex", "-1", 1, context);
            Console.WriteLine($"  最大值为1(-1): {boundaryResult}");

            Console.WriteLine();
        }
    }
}
