using BenchSuite.Services.OpenXml;
using BenchSuite.Models;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BenchSuite.Tests
{
    /// <summary>
    /// 参数验证功能测试
    /// </summary>
    [TestClass]
    public class ParameterValidationTest
    {
        private ExcelOpenXmlScoringService _scoringService;
        private string _testFilePath;

        [TestInitialize]
        public void Setup()
        {
            _scoringService = new ExcelOpenXmlScoringService();
            
            // 创建一个简单的测试Excel文件路径
            _testFilePath = Path.Combine(Path.GetTempPath(), "test.xlsx");
            
            // 创建一个简单的Excel文件用于测试
            CreateTestExcelFile(_testFilePath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        /// <summary>
        /// 测试缺少必需参数时的调试信息输出
        /// </summary>
        [TestMethod]
        public void TestMissingRequiredParameters_ShouldOutputDebugInfo()
        {
            // Arrange
            Dictionary<string, string> parameters = new()
            {
                // 故意不提供必需的参数
            };

            // 创建一个简单的操作点来测试
            OperationPointModel operationPoint = new()
            {
                Id = "test-op-1",
                Name = "填充或复制单元格内容",
                ModuleType = ModuleType.Excel,
                Score = 10,
                IsEnabled = true,
                Parameters = []
            };

            List<OperationPointModel> operationPoints = [operationPoint];

            // 设置调试监听器来捕获Debug.WriteLine输出
            TestDebugListener debugListener = new();
            Debug.Listeners.Add(debugListener);

            try
            {
                // Act
                Task<List<KnowledgePointResult>> task = _scoringService.DetectKnowledgePointsAsync(_testFilePath, operationPoints);
                List<KnowledgePointResult> results = task.Result;

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);

                KnowledgePointResult result = results[0];
                Assert.IsFalse(result.IsCorrect);
                Assert.AreEqual(0, result.AchievedScore);
                Assert.IsTrue(result.Details.Contains("缺少必需参数"));

                // 验证调试信息是否输出
                bool hasDebugOutput = debugListener.Messages.Any(msg => 
                    msg.Contains("[ExcelOpenXmlScoringService] 知识点检测失败") &&
                    msg.Contains("FillOrCopyCellContent") &&
                    msg.Contains("缺少必需参数"));

                Assert.IsTrue(hasDebugOutput, "应该输出包含知识点类型和缺少参数信息的调试信息");
            }
            finally
            {
                Debug.Listeners.Remove(debugListener);
            }
        }

        /// <summary>
        /// 测试多个缺少参数的情况
        /// </summary>
        [TestMethod]
        public void TestMultipleMissingParameters_ShouldListAllMissing()
        {
            // Arrange
            Dictionary<string, string> parameters = new()
            {
                // 只提供部分参数
                ["CellRange"] = "A1"
                // 缺少 ExpectedValue 参数
            };

            OperationPointModel operationPoint = new()
            {
                Id = "test-op-2",
                Name = "填充或复制单元格内容",
                ModuleType = ModuleType.Excel,
                Score = 10,
                IsEnabled = true,
                Parameters = [new ParameterModel { Name = "CellRange", Value = "A1" }]
            };

            List<OperationPointModel> operationPoints = [operationPoint];

            TestDebugListener debugListener = new();
            Debug.Listeners.Add(debugListener);

            try
            {
                // Act
                Task<List<KnowledgePointResult>> task = _scoringService.DetectKnowledgePointsAsync(_testFilePath, operationPoints);
                List<KnowledgePointResult> results = task.Result;

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);

                KnowledgePointResult result = results[0];
                Assert.IsFalse(result.IsCorrect);
                Assert.IsTrue(result.Details.Contains("ExpectedValue"));

                // 验证调试信息包含具体的缺少参数名称
                bool hasSpecificDebugOutput = debugListener.Messages.Any(msg => 
                    msg.Contains("ExpectedValue"));

                Assert.IsTrue(hasSpecificDebugOutput, "调试信息应该包含具体缺少的参数名称");
            }
            finally
            {
                Debug.Listeners.Remove(debugListener);
            }
        }

        /// <summary>
        /// 创建测试用的Excel文件
        /// </summary>
        private void CreateTestExcelFile(string filePath)
        {
            try
            {
                using var package = new DocumentFormat.OpenXml.Packaging.SpreadsheetDocument();
                // 这里只是创建一个最基本的文件结构用于测试
                // 实际实现可能需要更复杂的Excel文件创建逻辑
                File.WriteAllBytes(filePath, []);
            }
            catch
            {
                // 如果创建失败，创建一个空文件
                File.WriteAllBytes(filePath, []);
            }
        }
    }

    /// <summary>
    /// 测试用的调试监听器
    /// </summary>
    public class TestDebugListener : TraceListener
    {
        public List<string> Messages { get; } = [];

        public override void Write(string? message)
        {
            if (message != null)
            {
                Messages.Add(message);
            }
        }

        public override void WriteLine(string? message)
        {
            if (message != null)
            {
                Messages.Add(message);
            }
        }
    }
}
