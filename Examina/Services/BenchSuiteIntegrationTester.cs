using Examina.Models;
using Examina.Models.BenchSuite;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// BenchSuite集成功能测试器
/// </summary>
public class BenchSuiteIntegrationTester
{
    private readonly IBenchSuiteIntegrationService _integrationService;
    private readonly IBenchSuiteDirectoryService _directoryService;
    private readonly ILogger<BenchSuiteIntegrationTester> _logger;

    public BenchSuiteIntegrationTester(
        IBenchSuiteIntegrationService integrationService,
        IBenchSuiteDirectoryService directoryService,
        ILogger<BenchSuiteIntegrationTester> logger)
    {
        _integrationService = integrationService ?? throw new ArgumentNullException(nameof(integrationService));
        _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 测试BenchSuite集成功能
    /// </summary>
    public async Task<BenchSuiteTestResult> TestIntegrationAsync()
    {
        BenchSuiteTestResult result = new()
        {
            StartTime = DateTime.Now
        };

        try
        {
            _logger.LogInformation("开始测试BenchSuite集成功能");

            // 1. 测试目录结构
            result.DirectoryTestResult = await TestDirectoryStructureAsync();

            // 2. 测试服务可用性
            result.ServiceAvailabilityResult = await TestServiceAvailabilityAsync();

            // 3. 测试评分功能
            result.ScoringTestResult = await TestScoringFunctionalityAsync();

            // 4. 测试文件类型支持
            result.FileTypeSupportResult = TestFileTypeSupport();

            // 计算总体结果
            result.OverallSuccess = result.DirectoryTestResult.IsSuccess &&
                                  result.ServiceAvailabilityResult.IsSuccess &&
                                  result.ScoringTestResult.IsSuccess &&
                                  result.FileTypeSupportResult.IsSuccess;

            _logger.LogInformation("BenchSuite集成功能测试完成，总体结果: {OverallSuccess}", result.OverallSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BenchSuite集成功能测试过程中发生异常");
            result.OverallSuccess = false;
            result.ErrorMessage = $"测试过程中发生异常: {ex.Message}";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 测试目录结构
    /// </summary>
    private async Task<TestStepResult> TestDirectoryStructureAsync()
    {
        TestStepResult result = new() { TestName = "目录结构测试" };

        try
        {
            _logger.LogInformation("测试目录结构");

            // 获取基础路径
            string basePath = _directoryService.GetBasePath();
            result.Details.Add($"基础路径: {basePath}");

            // 确保目录结构存在
            BenchSuiteDirectoryValidationResult validationResult = await _directoryService.EnsureDirectoryStructureAsync();
            result.IsSuccess = validationResult.IsValid;
            result.Details.Add($"目录验证结果: {validationResult.Details}");

            if (!validationResult.IsValid)
            {
                result.ErrorMessage = validationResult.ErrorMessage;
            }

            // 测试各文件类型目录
            foreach (BenchSuiteFileType fileType in Enum.GetValues<BenchSuiteFileType>())
            {
                string directoryPath = _directoryService.GetDirectoryPath(fileType);
                bool exists = System.IO.Directory.Exists(directoryPath);
                result.Details.Add($"{fileType} 目录: {directoryPath} - {(exists ? "存在" : "不存在")}");
            }

            _logger.LogInformation("目录结构测试完成，结果: {IsSuccess}", result.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "目录结构测试失败");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 测试服务可用性
    /// </summary>
    private async Task<TestStepResult> TestServiceAvailabilityAsync()
    {
        TestStepResult result = new() { TestName = "服务可用性测试" };

        try
        {
            _logger.LogInformation("测试服务可用性");

            bool serviceAvailable = await _integrationService.IsServiceAvailableAsync();
            result.IsSuccess = serviceAvailable;
            result.Details.Add($"BenchSuite服务可用性: {serviceAvailable}");

            if (!serviceAvailable)
            {
                result.ErrorMessage = "BenchSuite服务不可用";
            }

            _logger.LogInformation("服务可用性测试完成，结果: {IsSuccess}", result.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "服务可用性测试失败");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 测试评分功能
    /// </summary>
    private async Task<TestStepResult> TestScoringFunctionalityAsync()
    {
        TestStepResult result = new() { TestName = "评分功能测试" };

        try
        {
            _logger.LogInformation("测试评分功能");

            // 创建测试评分请求
            BenchSuiteScoringRequest request = new()
            {
                ExamId = 999,
                ExamType = ExamType.MockExam,
                StudentUserId = 1,
                BasePath = _directoryService.GetBasePath()
            };

            // 执行评分测试
            BenchSuiteScoringResult scoringResult = await _integrationService.ScoreExamAsync(request);
            result.IsSuccess = scoringResult.IsSuccess;
            result.Details.Add($"评分结果: {(scoringResult.IsSuccess ? "成功" : "失败")}");
            result.Details.Add($"总分: {scoringResult.TotalScore}");
            result.Details.Add($"得分: {scoringResult.AchievedScore}");

            if (!scoringResult.IsSuccess)
            {
                result.ErrorMessage = scoringResult.ErrorMessage;
            }

            _logger.LogInformation("评分功能测试完成，结果: {IsSuccess}", result.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "评分功能测试失败");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 测试文件类型支持
    /// </summary>
    private TestStepResult TestFileTypeSupport()
    {
        TestStepResult result = new() { TestName = "文件类型支持测试" };

        try
        {
            _logger.LogInformation("测试文件类型支持");

            IEnumerable<BenchSuiteFileType> supportedTypes = _integrationService.GetSupportedFileTypes();
            result.IsSuccess = supportedTypes.Any();
            result.Details.Add($"支持的文件类型数量: {supportedTypes.Count()}");

            foreach (BenchSuiteFileType fileType in supportedTypes)
            {
                result.Details.Add($"支持的文件类型: {fileType}");
            }

            if (!result.IsSuccess)
            {
                result.ErrorMessage = "没有支持的文件类型";
            }

            _logger.LogInformation("文件类型支持测试完成，结果: {IsSuccess}", result.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件类型支持测试失败");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

/// <summary>
/// BenchSuite测试结果
/// </summary>
public class BenchSuiteTestResult
{
    /// <summary>
    /// 总体是否成功
    /// </summary>
    public bool OverallSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 目录测试结果
    /// </summary>
    public TestStepResult DirectoryTestResult { get; set; } = new();

    /// <summary>
    /// 服务可用性测试结果
    /// </summary>
    public TestStepResult ServiceAvailabilityResult { get; set; } = new();

    /// <summary>
    /// 评分功能测试结果
    /// </summary>
    public TestStepResult ScoringTestResult { get; set; } = new();

    /// <summary>
    /// 文件类型支持测试结果
    /// </summary>
    public TestStepResult FileTypeSupportResult { get; set; } = new();

    /// <summary>
    /// 测试耗时（毫秒）
    /// </summary>
    public long ElapsedMilliseconds => (long)(EndTime - StartTime).TotalMilliseconds;
}

/// <summary>
/// 测试步骤结果
/// </summary>
public class TestStepResult
{
    /// <summary>
    /// 测试名称
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    public List<string> Details { get; set; } = [];
}
