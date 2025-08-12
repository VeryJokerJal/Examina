using System;
using System.Collections.Generic;
using BenchSuite.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BenchSuite.Tests
{
    /// <summary>
    /// 参数解析器测试
    /// </summary>
    [TestClass]
    public class ParameterResolverTests
    {
        /// <summary>
        /// 测试编号参数识别
        /// </summary>
        [TestMethod]
        public void TestIsIndexParameter()
        {
            // 测试正确识别编号参数
            Assert.IsTrue(ParameterResolver.IsIndexParameter("SlideIndex"));
            Assert.IsTrue(ParameterResolver.IsIndexParameter("SlideNumber"));
            Assert.IsTrue(ParameterResolver.IsIndexParameter("TextBoxIndex"));
            Assert.IsTrue(ParameterResolver.IsIndexParameter("TextBoxOrder"));
            Assert.IsTrue(ParameterResolver.IsIndexParameter("ElementIndex"));
            Assert.IsTrue(ParameterResolver.IsIndexParameter("ShapeNumber"));
            
            // 测试不识别非编号参数
            Assert.IsFalse(ParameterResolver.IsIndexParameter("FontSize"));
            Assert.IsFalse(ParameterResolver.IsIndexParameter("ColorValue"));
            Assert.IsFalse(ParameterResolver.IsIndexParameter("TextContent"));
            Assert.IsFalse(ParameterResolver.IsIndexParameter("Layout"));
        }

        /// <summary>
        /// 测试-1参数解析
        /// </summary>
        [TestMethod]
        public void TestResolveMinusOneParameter()
        {
            ParameterResolutionContext context = new();
            
            // 测试-1解析为随机值
            string result1 = ParameterResolver.ResolveParameter("SlideIndex", "-1", 5, context);
            Assert.IsTrue(int.TryParse(result1, out int value1));
            Assert.IsTrue(value1 >= 1 && value1 <= 5);
            
            // 测试相同参数返回相同值（一致性）
            string result2 = ParameterResolver.ResolveParameter("SlideIndex", "-1", 5, context);
            Assert.AreEqual(result1, result2);
            
            // 测试不同参数返回不同的随机值
            string result3 = ParameterResolver.ResolveParameter("TextBoxIndex", "-1", 3, context);
            Assert.IsTrue(int.TryParse(result3, out int value3));
            Assert.IsTrue(value3 >= 1 && value3 <= 3);
        }

        /// <summary>
        /// 测试普通数字参数
        /// </summary>
        [TestMethod]
        public void TestResolveNormalParameter()
        {
            ParameterResolutionContext context = new();
            
            // 测试普通数字参数不变
            string result = ParameterResolver.ResolveParameter("SlideIndex", "3", 5, context);
            Assert.AreEqual("3", result);
            
            // 测试非编号参数不变
            string result2 = ParameterResolver.ResolveParameter("FontSize", "-1", 5, context);
            Assert.AreEqual("-1", result2);
        }

        /// <summary>
        /// 测试多个编号参数解析
        /// </summary>
        [TestMethod]
        public void TestResolveMultipleParameters()
        {
            ParameterResolutionContext context = new();
            
            // 测试逗号分隔的多个-1参数
            string result = ParameterResolver.ResolveMultipleParameters("SlideIndexes", "-1,2,-1", 5, context);
            string[] parts = result.Split(',');
            
            Assert.AreEqual(3, parts.Length);
            Assert.IsTrue(int.TryParse(parts[0], out int value1));
            Assert.IsTrue(value1 >= 1 && value1 <= 5);
            Assert.AreEqual("2", parts[1]);
            Assert.IsTrue(int.TryParse(parts[2], out int value3));
            Assert.IsTrue(value3 >= 1 && value3 <= 5);
        }

        /// <summary>
        /// 测试边界情况
        /// </summary>
        [TestMethod]
        public void TestEdgeCases()
        {
            ParameterResolutionContext context = new();
            
            // 测试最大值为0的情况
            Assert.ThrowsException<ArgumentException>(() =>
            {
                ParameterResolver.ResolveParameter("SlideIndex", "-1", 0, context);
            });
            
            // 测试空参数名
            string result = ParameterResolver.ResolveParameter("", "-1", 5, context);
            Assert.AreEqual("-1", result);
            
            // 测试无效数字
            string result2 = ParameterResolver.ResolveParameter("SlideIndex", "abc", 5, context);
            Assert.AreEqual("abc", result2);
        }

        /// <summary>
        /// 测试解析后的整数值获取
        /// </summary>
        [TestMethod]
        public void TestGetResolvedIntValue()
        {
            ParameterResolutionContext context = new();
            
            // 先解析一个参数
            ParameterResolver.ResolveParameter("SlideIndex", "-1", 5, context);
            
            // 获取解析后的整数值
            int value = ParameterResolver.GetResolvedIntValue("SlideIndex", context);
            Assert.IsTrue(value >= 1 && value <= 5);
            
            // 测试不存在的参数返回默认值
            int defaultValue = ParameterResolver.GetResolvedIntValue("NonExistent", context, 99);
            Assert.AreEqual(99, defaultValue);
        }

        /// <summary>
        /// 测试解析后的整数数组获取
        /// </summary>
        [TestMethod]
        public void TestGetResolvedIntArray()
        {
            ParameterResolutionContext context = new();
            
            // 先解析多个参数
            ParameterResolver.ResolveMultipleParameters("SlideIndexes", "1,-1,3", 5, context);
            
            // 获取解析后的整数数组
            int[] values = ParameterResolver.GetResolvedIntArray("SlideIndexes", context);
            Assert.AreEqual(3, values.Length);
            Assert.AreEqual(1, values[0]);
            Assert.IsTrue(values[1] >= 1 && values[1] <= 5);
            Assert.AreEqual(3, values[2]);
        }

        /// <summary>
        /// 测试参数解析上下文
        /// </summary>
        [TestMethod]
        public void TestParameterResolutionContext()
        {
            ParameterResolutionContext context = new();
            
            // 测试设置和获取参数
            context.SetResolvedParameter("TestParam", "TestValue");
            Assert.AreEqual("TestValue", context.GetResolvedParameter("TestParam"));
            Assert.IsTrue(context.IsParameterResolved("TestParam"));
            
            // 测试不存在的参数
            Assert.AreEqual(string.Empty, context.GetResolvedParameter("NonExistent"));
            Assert.IsFalse(context.IsParameterResolved("NonExistent"));
            
            // 测试随机数生成
            int random1 = context.GenerateRandomNumber(1, 10);
            Assert.IsTrue(random1 >= 1 && random1 <= 10);
            
            // 测试无效范围
            Assert.ThrowsException<ArgumentException>(() =>
            {
                context.GenerateRandomNumber(10, 1);
            });
        }
    }
}
