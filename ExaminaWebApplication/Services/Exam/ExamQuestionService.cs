using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Exam;
using ExaminaWebApplication.Models.Excel;
using ExaminaWebApplication.Services.Excel;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Services.Exam;

/// <summary>
/// 试卷题目管理服务类
/// </summary>
public class ExamQuestionService
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelOperationService _excelOperationService;
    private readonly ExcelQuestionService _excelQuestionService;

    public ExamQuestionService(
        ApplicationDbContext context,
        ExcelOperationService excelOperationService,
        ExcelQuestionService excelQuestionService)
    {
        _context = context;
        _excelOperationService = excelOperationService;
        _excelQuestionService = excelQuestionService;
    }

    /// <summary>
    /// 获取试卷的所有题目
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <returns></returns>
    public async Task<List<ExamQuestion>> GetExamQuestionsAsync(int examId)
    {
        return await _context.ExamQuestions
            .Include(q => q.ExamSubject)
            .Include(q => q.ExcelOperationPoint)
            .Include(q => q.ExcelQuestionTemplate)
            .Include(q => q.ExcelQuestionInstance)
            .Where(q => q.ExamId == examId)
            .OrderBy(q => q.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 获取科目的所有题目
    /// </summary>
    /// <param name="examSubjectId">科目ID</param>
    /// <returns></returns>
    public async Task<List<ExamQuestion>> GetSubjectQuestionsAsync(int examSubjectId)
    {
        return await _context.ExamQuestions
            .Include(q => q.ExcelOperationPoint)
            .Include(q => q.ExcelQuestionTemplate)
            .Include(q => q.ExcelQuestionInstance)
            .Where(q => q.ExamSubjectId == examSubjectId)
            .OrderBy(q => q.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 根据ID获取题目详情
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns></returns>
    public async Task<ExamQuestion?> GetQuestionByIdAsync(int questionId)
    {
        return await _context.ExamQuestions
            .Include(q => q.Exam)
            .Include(q => q.ExamSubject)
            .Include(q => q.ExcelOperationPoint)
            .ThenInclude(op => op!.Parameters)
            .Include(q => q.ExcelQuestionTemplate)
            .Include(q => q.ExcelQuestionInstance)
            .FirstOrDefaultAsync(q => q.Id == questionId);
    }

    /// <summary>
    /// 从Excel操作点创建题目
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns></returns>
    public async Task<ExamQuestion> CreateQuestionFromExcelOperationAsync(CreateExcelQuestionRequest request)
    {
        // 验证试卷和科目是否存在
        ExamSubject? examSubject = await _context.ExamSubjects
            .Include(es => es.Exam)
            .FirstOrDefaultAsync(es => es.Id == request.ExamSubjectId);

        if (examSubject == null)
        {
            throw new ArgumentException($"科目 ID {request.ExamSubjectId} 不存在");
        }

        // 验证Excel操作点是否存在
        ExcelOperationPoint? operationPoint = await _excelOperationService
            .GetOperationPointByNumberAsync(request.OperationNumber);

        if (operationPoint == null)
        {
            throw new ArgumentException($"Excel操作点 {request.OperationNumber} 不存在");
        }

        // 验证参数配置
        ParameterValidationResult validationResult = await _excelOperationService
            .ValidateOperationParametersAsync(request.OperationNumber, request.Parameters);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"参数配置无效: {string.Join(", ", validationResult.Errors)}");
        }

        // 生成题目内容
        string questionContent = GenerateQuestionContent(operationPoint, request.Parameters);

        // 获取下一个题目编号
        int nextQuestionNumber = await GetNextQuestionNumberAsync(examSubject.ExamId);

        // 获取下一个排序号
        int nextSortOrder = await GetNextSortOrderAsync(request.ExamSubjectId);

        // 创建题目
        ExamQuestion question = new()
        {
            ExamId = examSubject.ExamId,
            ExamSubjectId = request.ExamSubjectId,
            QuestionNumber = nextQuestionNumber,
            Title = $"{operationPoint.Name}",
            Content = questionContent,
            QuestionType = QuestionType.ExcelOperation,
            Score = request.Score,
            DifficultyLevel = request.DifficultyLevel,
            EstimatedMinutes = request.EstimatedMinutes,
            SortOrder = nextSortOrder,
            IsRequired = request.IsRequired,
            ExcelOperationPointId = operationPoint.Id,
            QuestionConfig = JsonSerializer.Serialize(new ExcelQuestionConfig
            {
                OperationNumber = request.OperationNumber,
                Parameters = request.Parameters,
                AllowMultipleSolutions = request.AllowMultipleSolutions,
                PartialScoring = request.PartialScoring,
                Hints = request.Hints
            }),
            AnswerValidationRules = GenerateAnswerValidationRules(operationPoint, request.Parameters),
            StandardAnswer = JsonSerializer.Serialize(request.Parameters),
            Tags = request.Tags,
            Remarks = request.Remarks,
            CreatedAt = DateTime.UtcNow
        };

        _ = _context.ExamQuestions.Add(question);
        _ = await _context.SaveChangesAsync();

        return question;
    }

    /// <summary>
    /// 从Excel题目模板创建题目
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns></returns>
    public async Task<ExamQuestion> CreateQuestionFromExcelTemplateAsync(CreateExcelTemplateQuestionRequest request)
    {
        // 验证科目是否存在
        ExamSubject? examSubject = await _context.ExamSubjects
            .Include(es => es.Exam)
            .FirstOrDefaultAsync(es => es.Id == request.ExamSubjectId);

        if (examSubject == null)
        {
            throw new ArgumentException($"科目 ID {request.ExamSubjectId} 不存在");
        }

        // 生成Excel题目实例
        ExcelQuestionInstance questionInstance = await _excelQuestionService
            .GenerateQuestionInstanceAsync(request.TemplateId, request.CustomParameters);

        // 获取关联的操作点
        ExcelQuestionTemplate? template = await _context.ExcelQuestionTemplates
            .Include(t => t.OperationPoint)
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId);

        if (template == null)
        {
            throw new ArgumentException($"题目模板 ID {request.TemplateId} 不存在");
        }

        // 获取下一个题目编号和排序号
        int nextQuestionNumber = await GetNextQuestionNumberAsync(examSubject.ExamId);
        int nextSortOrder = await GetNextSortOrderAsync(request.ExamSubjectId);

        // 创建题目
        ExamQuestion question = new()
        {
            ExamId = examSubject.ExamId,
            ExamSubjectId = request.ExamSubjectId,
            QuestionNumber = nextQuestionNumber,
            Title = questionInstance.QuestionTitle,
            Content = questionInstance.QuestionDescription,
            QuestionType = QuestionType.ExcelOperation,
            Score = request.Score,
            DifficultyLevel = request.DifficultyLevel,
            EstimatedMinutes = request.EstimatedMinutes,
            SortOrder = nextSortOrder,
            IsRequired = request.IsRequired,
            ExcelOperationPointId = template.OperationPoint.Id,
            ExcelQuestionTemplateId = request.TemplateId,
            ExcelQuestionInstanceId = questionInstance.Id,
            QuestionConfig = questionInstance.ActualParameters,
            AnswerValidationRules = questionInstance.AnswerValidationRules,
            StandardAnswer = questionInstance.ActualParameters,
            Tags = request.Tags,
            Remarks = request.Remarks,
            CreatedAt = DateTime.UtcNow
        };

        _ = _context.ExamQuestions.Add(question);
        _ = await _context.SaveChangesAsync();

        return question;
    }

    /// <summary>
    /// 更新题目信息
    /// </summary>
    /// <param name="question">题目信息</param>
    /// <returns></returns>
    public async Task<ExamQuestion> UpdateQuestionAsync(ExamQuestion question)
    {
        ExamQuestion? existingQuestion = await _context.ExamQuestions.FindAsync(question.Id);
        if (existingQuestion == null)
        {
            throw new ArgumentException($"题目 ID {question.Id} 不存在");
        }

        // 检查试卷状态
        Models.Exam.Exam? exam = await _context.Exams.FindAsync(existingQuestion.ExamId);
        if (exam != null && (exam.Status == ExamStatus.Published || exam.Status == ExamStatus.InProgress))
        {
            throw new InvalidOperationException("已发布或进行中的试卷题目不能编辑");
        }

        // 更新题目信息
        existingQuestion.Title = question.Title;
        existingQuestion.Content = question.Content;
        existingQuestion.Score = question.Score;
        existingQuestion.DifficultyLevel = question.DifficultyLevel;
        existingQuestion.EstimatedMinutes = question.EstimatedMinutes;
        existingQuestion.IsRequired = question.IsRequired;
        existingQuestion.QuestionConfig = question.QuestionConfig;
        existingQuestion.AnswerValidationRules = question.AnswerValidationRules;
        existingQuestion.StandardAnswer = question.StandardAnswer;
        existingQuestion.ScoringRules = question.ScoringRules;
        existingQuestion.Tags = question.Tags;
        existingQuestion.Remarks = question.Remarks;
        existingQuestion.UpdatedAt = DateTime.UtcNow;

        _ = await _context.SaveChangesAsync();
        return existingQuestion;
    }

    /// <summary>
    /// 删除题目
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns></returns>
    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        ExamQuestion? question = await _context.ExamQuestions.FindAsync(questionId);
        if (question == null)
        {
            return false;
        }

        // 检查试卷状态
        Models.Exam.Exam? exam = await _context.Exams.FindAsync(question.ExamId);
        if (exam != null && (exam.Status == ExamStatus.Published || exam.Status == ExamStatus.InProgress))
        {
            throw new InvalidOperationException("已发布或进行中的试卷题目不能删除");
        }

        _ = _context.ExamQuestions.Remove(question);
        _ = await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// 调整题目顺序
    /// </summary>
    /// <param name="examSubjectId">科目ID</param>
    /// <param name="questionOrders">题目顺序列表</param>
    /// <returns></returns>
    public async Task ReorderQuestionsAsync(int examSubjectId, List<QuestionOrderItem> questionOrders)
    {
        List<ExamQuestion> questions = await _context.ExamQuestions
            .Where(q => q.ExamSubjectId == examSubjectId)
            .ToListAsync();

        foreach (QuestionOrderItem orderItem in questionOrders)
        {
            ExamQuestion? question = questions.FirstOrDefault(q => q.Id == orderItem.QuestionId);
            if (question != null)
            {
                question.SortOrder = orderItem.SortOrder;
                question.UpdatedAt = DateTime.UtcNow;
            }
        }

        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 生成题目内容
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    /// <param name="parameters">参数</param>
    /// <returns></returns>
    private static string GenerateQuestionContent(ExcelOperationPoint operationPoint, Dictionary<string, object?> parameters)
    {
        string content = $"请完成以下Excel操作：{operationPoint.Name}\n\n";
        content += $"操作描述：{operationPoint.Description}\n\n";

        if (parameters.Count != 0)
        {
            content += "操作要求：\n";
            foreach (KeyValuePair<string, object?> param in parameters)
            {
                content += $"- {param.Key}：{param.Value}\n";
            }
        }

        return content;
    }

    /// <summary>
    /// 生成答案验证规则
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    /// <param name="parameters">参数</param>
    /// <returns></returns>
    private static string GenerateAnswerValidationRules(ExcelOperationPoint operationPoint, Dictionary<string, object?> parameters)
    {
        ExcelAnswerValidationRules rules = new()
        {
            OperationNumber = operationPoint.OperationNumber,
            OperationType = operationPoint.OperationType,
            TargetType = operationPoint.TargetType.ToString(),
            ExpectedParameters = parameters,
            ValidationStrategy = "ExactMatch"
        };

        return JsonSerializer.Serialize(rules);
    }

    /// <summary>
    /// 获取下一个题目编号
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <returns></returns>
    private async Task<int> GetNextQuestionNumberAsync(int examId)
    {
        int? maxNumber = await _context.ExamQuestions
            .Where(q => q.ExamId == examId)
            .MaxAsync(q => (int?)q.QuestionNumber);

        return (maxNumber ?? 0) + 1;
    }

    /// <summary>
    /// 获取下一个排序号
    /// </summary>
    /// <param name="examSubjectId">科目ID</param>
    /// <returns></returns>
    private async Task<int> GetNextSortOrderAsync(int examSubjectId)
    {
        int? maxOrder = await _context.ExamQuestions
            .Where(q => q.ExamSubjectId == examSubjectId)
            .MaxAsync(q => (int?)q.SortOrder);

        return (maxOrder ?? 0) + 1;
    }


}

/// <summary>
/// 从Excel操作点创建题目请求
/// </summary>
public class CreateExcelQuestionRequest
{
    /// <summary>
    /// 科目ID
    /// </summary>
    public int ExamSubjectId { get; set; }

    /// <summary>
    /// Excel操作点编号
    /// </summary>
    public int OperationNumber { get; set; }

    /// <summary>
    /// 题目参数
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = [];

    /// <summary>
    /// 题目分值
    /// </summary>
    [Range(0.1, 100.0)]
    public decimal Score { get; set; } = 10.0m;

    /// <summary>
    /// 难度级别
    /// </summary>
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 预计完成时间
    /// </summary>
    public int EstimatedMinutes { get; set; } = 5;

    /// <summary>
    /// 是否必答
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 是否允许多种解法
    /// </summary>
    public bool AllowMultipleSolutions { get; set; } = false;

    /// <summary>
    /// 部分分值配置
    /// </summary>
    public Dictionary<string, int> PartialScoring { get; set; } = [];

    /// <summary>
    /// 提示信息
    /// </summary>
    public List<string> Hints { get; set; } = [];

    /// <summary>
    /// 题目标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 题目备注
    /// </summary>
    public string? Remarks { get; set; }
}

/// <summary>
/// 从Excel模板创建题目请求
/// </summary>
public class CreateExcelTemplateQuestionRequest
{
    /// <summary>
    /// 科目ID
    /// </summary>
    public int ExamSubjectId { get; set; }

    /// <summary>
    /// Excel题目模板ID
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// 自定义参数
    /// </summary>
    public Dictionary<string, object?>? CustomParameters { get; set; }

    /// <summary>
    /// 题目分值
    /// </summary>
    public int Score { get; set; } = 10;

    /// <summary>
    /// 难度级别
    /// </summary>
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 预计完成时间
    /// </summary>
    public int EstimatedMinutes { get; set; } = 5;

    /// <summary>
    /// 是否必答
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 题目标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 题目备注
    /// </summary>
    public string? Remarks { get; set; }
}

/// <summary>
/// 题目顺序项
/// </summary>
public class QuestionOrderItem
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public int QuestionId { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Excel答案验证规则
/// </summary>
public class ExcelAnswerValidationRules
{
    /// <summary>
    /// 操作点编号
    /// </summary>
    public int OperationNumber { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 目标类型
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// 期望的参数值
    /// </summary>
    public Dictionary<string, object?> ExpectedParameters { get; set; } = [];

    /// <summary>
    /// 验证策略
    /// </summary>
    public string ValidationStrategy { get; set; } = "ExactMatch";

    /// <summary>
    /// 容错设置
    /// </summary>
    public Dictionary<string, object> ToleranceSettings { get; set; } = [];
}
