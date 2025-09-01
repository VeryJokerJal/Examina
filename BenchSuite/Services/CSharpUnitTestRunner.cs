using BenchSuite.Models;
using System.Reflection;
using System.Text;

namespace BenchSuite.Services;

/// <summary>
/// C#单元测试运行器 - 自动运行测试用例并检查结果
/// 现在使用MSBuild编译机制，提供更好的项目支持和测试框架兼容性
/// </summary>
public static class CSharpUnitTestRunner
{
    /// <summary>
    /// 运行单元测试
    /// </summary>
    /// <param name="studentCode">学生代码</param>
    /// <param name="testCode">测试代码</param>
    /// <param name="references">引用程序集路径列表</param>
    /// <returns>单元测试结果</returns>
    public static async Task<UnitTestResult> RunUnitTestsAsync(string studentCode, string testCode, List<string>? references = null)
    {
        UnitTestResult result = new()
        {
            IsSuccess = false
        };

        DateTime startTime = DateTime.Now;

        try
        {
            // 合并学生代码和测试代码
            string combinedCode = CombineCodeWithTests(studentCode, testCode);

            // 使用MSBuild编译代码
            CompilationResult compilationResult = await CSharpCompilationChecker.CompileCodeAsync(
                combinedCode,
                GetTestReferences(references),
                "net9.0");

            if (!compilationResult.IsSuccess)
            {
                result.ErrorMessage = "代码编译失败";
                result.Details = string.Join("\n", compilationResult.Errors.Select(e => e.Message));
                return result;
            }

            // 运行测试
            result = await ExecuteTestsAsync(compilationResult.AssemblyBytes!);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"运行测试时发生异常: {ex.Message}";
            result.Details = ex.StackTrace ?? "";
        }
        finally
        {
            DateTime endTime = DateTime.Now;
            result.ExecutionTimeMs = (long)(endTime - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 执行测试程序集
    /// </summary>
    /// <param name="assemblyBytes">程序集字节</param>
    /// <returns>测试结果</returns>
    private static async Task<UnitTestResult> ExecuteTestsAsync(byte[] assemblyBytes)
    {
        UnitTestResult result = new();

        try
        {
            // 加载程序集
            Assembly assembly = Assembly.Load(assemblyBytes);

            // 查找测试类和测试方法
            List<TestCaseResult> testCaseResults = [];

            foreach (Type type in assembly.GetTypes())
            {
                // 查找带有测试属性的方法
                MethodInfo[] testMethods = [.. type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(m => HasTestAttribute(m))];

                if (testMethods.Length > 0)
                {
                    // 创建类实例（如果需要）
                    object? instance = null;
                    if (testMethods.Any(m => !m.IsStatic))
                    {
                        try
                        {
                            instance = Activator.CreateInstance(type);
                        }
                        catch (Exception ex)
                        {
                            testCaseResults.Add(new TestCaseResult
                            {
                                TestName = $"{type.Name}..ctor",
                                Passed = false,
                                ErrorMessage = $"无法创建测试类实例: {ex.Message}",
                                ExecutionTimeMs = 0
                            });
                            continue;
                        }
                    }

                    // 运行每个测试方法
                    foreach (MethodInfo method in testMethods)
                    {
                        TestCaseResult testCase = await RunSingleTestAsync(method, instance);
                        testCaseResults.Add(testCase);
                    }
                }
            }

            // 汇总结果
            result.TestCaseResults = testCaseResults;
            result.TotalTests = testCaseResults.Count;
            result.PassedTests = testCaseResults.Count(t => t.Passed);
            result.FailedTests = testCaseResults.Count(t => !t.Passed);
            result.SkippedTests = 0; // 简化实现，暂不支持跳过的测试
            result.IsSuccess = result.FailedTests == 0 && result.TotalTests > 0;
            result.Details = $"总计: {result.TotalTests}, 通过: {result.PassedTests}, 失败: {result.FailedTests}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"执行测试时发生异常: {ex.Message}";
            result.Details = ex.StackTrace ?? "";
        }

        return result;
    }

    /// <summary>
    /// 运行单个测试方法
    /// </summary>
    /// <param name="method">测试方法</param>
    /// <param name="instance">类实例</param>
    /// <returns>测试用例结果</returns>
    private static async Task<TestCaseResult> RunSingleTestAsync(MethodInfo method, object? instance)
    {
        TestCaseResult result = new()
        {
            TestName = $"{method.DeclaringType?.Name}.{method.Name}"
        };

        DateTime startTime = DateTime.Now;
        StringBuilder output = new();

        try
        {
            // 捕获控制台输出
            using StringWriter outputWriter = new(output);
            TextWriter originalOut = Console.Out;

            try
            {
                Console.SetOut(outputWriter);

                // 调用测试方法
                object? methodResult = method.Invoke(instance, null);

                // 如果方法返回Task，等待完成
                if (methodResult is Task task)
                {
                    await task;
                }

                result.Passed = true;
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        catch (TargetInvocationException ex)
        {
            result.Passed = false;
            Exception innerException = ex.InnerException ?? ex;
            result.ErrorMessage = innerException.Message;
            result.StackTrace = innerException.StackTrace;
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = ex.Message;
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            DateTime endTime = DateTime.Now;
            result.ExecutionTimeMs = (long)(endTime - startTime).TotalMilliseconds;
            result.Output = output.ToString();
        }

        return result;
    }

    /// <summary>
    /// 检查方法是否有测试属性
    /// </summary>
    /// <param name="method">方法信息</param>
    /// <returns>是否为测试方法</returns>
    private static bool HasTestAttribute(MethodInfo method)
    {
        // 检查常见的测试属性
        string[] testAttributes = 
        [
            "Test",
            "TestMethod", 
            "Fact",
            "Theory"
        ];

        return method.GetCustomAttributes(false)
            .Any(attr => testAttributes.Contains(attr.GetType().Name));
    }

    /// <summary>
    /// 合并学生代码和测试代码
    /// </summary>
    /// <param name="studentCode">学生代码</param>
    /// <param name="testCode">测试代码</param>
    /// <returns>合并后的代码</returns>
    private static string CombineCodeWithTests(string studentCode, string testCode)
    {
        StringBuilder combined = new();

        // 添加必要的using语句
        combined.AppendLine("using System;");
        combined.AppendLine("using System.Collections.Generic;");
        combined.AppendLine("using System.Linq;");
        combined.AppendLine("using System.Threading.Tasks;");
        combined.AppendLine();

        // 添加学生代码
        combined.AppendLine("// === 学生代码 ===");
        combined.AppendLine(studentCode);
        combined.AppendLine();

        // 添加测试代码
        combined.AppendLine("// === 测试代码 ===");
        combined.AppendLine(testCode);

        return combined.ToString();
    }

    /// <summary>
    /// 获取测试相关的引用
    /// </summary>
    /// <param name="additionalReferences">额外引用</param>
    /// <returns>引用列表</returns>
    private static List<string> GetTestReferences(List<string>? additionalReferences = null)
    {
        List<string> references = additionalReferences?.ToList() ?? [];

        // 添加测试框架引用（如果可用）
        try
        {
            string[] testAssemblies = 
            [
                "xunit.core",
                "xunit.assert",
                "Microsoft.VisualStudio.TestPlatform.TestFramework"
            ];

            foreach (string assemblyName in testAssemblies)
            {
                try
                {
                    Assembly? assembly = Assembly.Load(assemblyName);
                    if (assembly != null && !string.IsNullOrEmpty(assembly.Location))
                    {
                        references.Add(assembly.Location);
                    }
                }
                catch
                {
                    // 忽略加载失败的测试框架
                }
            }
        }
        catch
        {
            // 忽略错误
        }

        return references;
    }

    /// <summary>
    /// 创建简单的断言测试代码模板
    /// </summary>
    /// <param name="className">要测试的类名</param>
    /// <param name="methodName">要测试的方法名</param>
    /// <param name="testCases">测试用例</param>
    /// <returns>测试代码</returns>
    public static string CreateSimpleTestTemplate(string className, string methodName, List<(object[] inputs, object expected)> testCases)
    {
        StringBuilder testCode = new();

        testCode.AppendLine("public class SimpleTests");
        testCode.AppendLine("{");

        for (int i = 0; i < testCases.Count; i++)
        {
            (object[] inputs, object expected) = testCases[i];
            
            testCode.AppendLine($"    [Test]");
            testCode.AppendLine($"    public void Test{methodName}_{i + 1}()");
            testCode.AppendLine("    {");
            
            // 创建实例（如果需要）
            testCode.AppendLine($"        var instance = new {className}();");
            
            // 调用方法
            string inputsStr = string.Join(", ", inputs.Select(FormatValue));
            testCode.AppendLine($"        var result = instance.{methodName}({inputsStr});");
            
            // 断言
            testCode.AppendLine($"        if (!result.Equals({FormatValue(expected)}))");
            testCode.AppendLine($"            throw new Exception($\"Expected {FormatValue(expected)}, but got {{result}}\");");
            
            testCode.AppendLine("    }");
            testCode.AppendLine();
        }

        testCode.AppendLine("}");

        // 添加简单的Test属性
        testCode.AppendLine();
        testCode.AppendLine("public class TestAttribute : Attribute { }");

        return testCode.ToString();
    }

    /// <summary>
    /// 格式化值为C#代码
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>格式化后的字符串</returns>
    private static string FormatValue(object value)
    {
        return value switch
        {
            string s => $"\"{s}\"",
            char c => $"'{c}'",
            bool b => b.ToString().ToLower(),
            null => "null",
            _ => value.ToString() ?? "null"
        };
    }
}
