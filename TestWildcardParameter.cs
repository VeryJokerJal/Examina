using System;
using ExamLab.Models;
using ExamLab.Services;

namespace ExamLab.Tests
{
    /// <summary>
    /// 测试通配符参数功能
    /// </summary>
    public class TestWildcardParameter
    {
        /// <summary>
        /// 测试-1通配符参数验证
        /// </summary>
        public static void TestWildcardValidation()
        {
            Console.WriteLine("测试通配符参数验证功能");
            Console.WriteLine("=========================");

            // 创建测试参数
            ConfigurationParameter parameter = new()
            {
                Name = "TestParameter",
                DisplayName = "测试参数",
                Type = ParameterType.Number,
                MinValue = 1,
                MaxValue = 10,
                IsRequired = true
            };

            // 测试正常值
            parameter.Value = "5";
            ValidationResult result1 = ValidationService.ValidateParameter(parameter);
            Console.WriteLine($"正常值 5: {(result1.IsValid ? "通过" : "失败")} - {string.Join(", ", result1.Errors)}");

            // 测试-1通配符值
            parameter.Value = "-1";
            ValidationResult result2 = ValidationService.ValidateParameter(parameter);
            Console.WriteLine($"通配符值 -1: {(result2.IsValid ? "通过" : "失败")} - {string.Join(", ", result2.Errors)}");

            // 测试超出范围的值
            parameter.Value = "15";
            ValidationResult result3 = ValidationService.ValidateParameter(parameter);
            Console.WriteLine($"超出范围值 15: {(result3.IsValid ? "通过" : "失败")} - {string.Join(", ", result3.Errors)}");

            // 测试小于最小值的值（非-1）
            parameter.Value = "0";
            ValidationResult result4 = ValidationService.ValidateParameter(parameter);
            Console.WriteLine($"小于最小值 0: {(result4.IsValid ? "通过" : "失败")} - {string.Join(", ", result4.Errors)}");

            Console.WriteLine();
            Console.WriteLine("测试完成！");
        }

        /// <summary>
        /// 测试不同类型的参数
        /// </summary>
        public static void TestDifferentParameterTypes()
        {
            Console.WriteLine("测试不同类型参数的通配符支持");
            Console.WriteLine("===============================");

            // 测试整数参数
            ConfigurationParameter intParam = new()
            {
                Name = "IntParameter",
                DisplayName = "整数参数",
                Type = ParameterType.Number,
                MinValue = 1,
                MaxValue = 100,
                Value = "-1"
            };

            ValidationResult intResult = ValidationService.ValidateParameter(intParam);
            Console.WriteLine($"整数参数 -1: {(intResult.IsValid ? "通过" : "失败")} - {string.Join(", ", intResult.Errors)}");

            // 测试小数参数
            ConfigurationParameter decimalParam = new()
            {
                Name = "DecimalParameter",
                DisplayName = "小数参数",
                Type = ParameterType.Number,
                MinValue = 0.1,
                MaxValue = 99.9,
                Value = "-1"
            };

            ValidationResult decimalResult = ValidationService.ValidateParameter(decimalParam);
            Console.WriteLine($"小数参数 -1: {(decimalResult.IsValid ? "通过" : "失败")} - {string.Join(", ", decimalResult.Errors)}");

            Console.WriteLine();
        }

        /// <summary>
        /// 主测试方法
        /// </summary>
        public static void Main()
        {
            try
            {
                TestWildcardValidation();
                TestDifferentParameterTypes();
                
                Console.WriteLine("所有测试完成！按任意键退出...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试过程中发生错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                Console.ReadKey();
            }
        }
    }
}
