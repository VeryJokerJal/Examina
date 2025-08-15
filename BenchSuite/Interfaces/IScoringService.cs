using BenchSuite.Models;

namespace BenchSuite.Interfaces;

/// <summary>
/// 打分服务接口
/// </summary>
public interface IScoringService
{
    /// <summary>
    /// 对指定文件进行打分
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="examModel">试卷模型</param>
    /// <param name="configuration">打分配置</param>
    /// <returns>打分结果</returns>
    Task<ScoringResult> ScoreFileAsync(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null);

    /// <summary>
    /// 对指定文件进行打分（同步版本）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="examModel">试卷模型</param>
    /// <param name="configuration">打分配置</param>
    /// <returns>打分结果</returns>
    ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null);

    /// <summary>
    /// 对单个题目进行评分
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="question">题目模型</param>
    /// <param name="configuration">评分配置</param>
    /// <returns>该题目的评分结果</returns>
    Task<ScoringResult> ScoreQuestionAsync(string filePath, QuestionModel question, ScoringConfiguration? configuration = null);

    /// <summary>
    /// 验证文件是否可以被处理
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否可以处理</returns>
    bool CanProcessFile(string filePath);

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    /// <returns>支持的文件扩展名列表</returns>
    IEnumerable<string> GetSupportedExtensions();
}

/// <summary>
/// PPT打分服务接口
/// </summary>
public interface IPowerPointScoringService : IScoringService
{
    /// <summary>
    /// 检测PPT中的特定知识点
    /// </summary>
    /// <param name="filePath">PPT文件路径</param>
    /// <param name="knowledgePointType">知识点类型</param>
    /// <param name="parameters">检测参数</param>
    /// <returns>知识点检测结果</returns>
    Task<KnowledgePointResult> DetectKnowledgePointAsync(string filePath, string knowledgePointType, Dictionary<string, string> parameters);

    /// <summary>
    /// 批量检测PPT中的知识点
    /// </summary>
    /// <param name="filePath">PPT文件路径</param>
    /// <param name="knowledgePoints">要检测的知识点列表</param>
    /// <returns>知识点检测结果列表</returns>
    Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(string filePath, List<OperationPointModel> knowledgePoints);
}

/// <summary>
/// Word打分服务接口
/// </summary>
public interface IWordScoringService : IScoringService
{
    /// <summary>
    /// 检测Word文档中的特定知识点
    /// </summary>
    /// <param name="filePath">Word文件路径</param>
    /// <param name="knowledgePointType">知识点类型</param>
    /// <param name="parameters">检测参数</param>
    /// <returns>知识点检测结果</returns>
    Task<KnowledgePointResult> DetectKnowledgePointAsync(string filePath, string knowledgePointType, Dictionary<string, string> parameters);

    /// <summary>
    /// 批量检测Word文档中的知识点
    /// </summary>
    /// <param name="filePath">Word文件路径</param>
    /// <param name="knowledgePoints">要检测的知识点列表</param>
    /// <returns>知识点检测结果列表</returns>
    Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(string filePath, List<OperationPointModel> knowledgePoints);
}

/// <summary>
/// Excel打分服务接口
/// </summary>
public interface IExcelScoringService : IScoringService
{
    /// <summary>
    /// 检测Excel文档中的特定知识点
    /// </summary>
    /// <param name="filePath">Excel文件路径</param>
    /// <param name="knowledgePointType">知识点类型</param>
    /// <param name="parameters">检测参数</param>
    /// <returns>知识点检测结果</returns>
    Task<KnowledgePointResult> DetectKnowledgePointAsync(string filePath, string knowledgePointType, Dictionary<string, string> parameters);

    /// <summary>
    /// 批量检测Excel文档中的知识点
    /// </summary>
    /// <param name="filePath">Excel文件路径</param>
    /// <param name="knowledgePoints">要检测的知识点列表</param>
    /// <returns>知识点检测结果列表</returns>
    Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(string filePath, List<OperationPointModel> knowledgePoints);
}

/// <summary>
/// C#编程题打分服务接口
/// </summary>
public interface ICSharpScoringService : IScoringService
{
    /// <summary>
    /// 对C#代码进行评分
    /// </summary>
    /// <param name="templateCode">模板代码（包含NotImplementedException的填空）</param>
    /// <param name="studentCode">学生提交的代码</param>
    /// <param name="expectedImplementations">期望的实现代码列表</param>
    /// <param name="mode">评分模式</param>
    /// <returns>评分结果</returns>
    Task<CSharpScoringResult> ScoreCodeAsync(string templateCode, string studentCode, List<string> expectedImplementations, CSharpScoringMode mode);

    /// <summary>
    /// 检测代码补全填空
    /// </summary>
    /// <param name="templateCode">模板代码</param>
    /// <param name="studentCode">学生代码</param>
    /// <param name="expectedImplementations">期望实现</param>
    /// <returns>填空检测结果</returns>
    Task<List<FillBlankResult>> DetectFillBlanksAsync(string templateCode, string studentCode, List<string> expectedImplementations);

    /// <summary>
    /// 编译检查代码
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="references">引用程序集</param>
    /// <returns>编译结果</returns>
    Task<CompilationResult> CompileCodeAsync(string sourceCode, List<string>? references = null);

    /// <summary>
    /// 运行单元测试
    /// </summary>
    /// <param name="studentCode">学生代码</param>
    /// <param name="testCode">测试代码</param>
    /// <param name="references">引用程序集</param>
    /// <returns>测试结果</returns>
    Task<UnitTestResult> RunUnitTestsAsync(string studentCode, string testCode, List<string>? references = null);
}
