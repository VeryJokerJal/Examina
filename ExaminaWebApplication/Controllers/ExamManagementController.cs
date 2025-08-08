using ExaminaWebApplication.Models.Exam;
using ExaminaWebApplication.Services.Exam;
using ExaminaWebApplication.Services.Excel;
using ExaminaWebApplication.Services.Windows;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 试卷管理页面控制器
/// </summary>
public class ExamManagementController : Controller
{
    private readonly ExamService _examService;
    private readonly ExamSubjectService _examSubjectService;
    private readonly ExamQuestionService _examQuestionService;
    private readonly SimplifiedQuestionService _simplifiedQuestionService;
    private readonly ExcelOperationService _excelOperationService;
    private readonly WindowsOperationService _windowsOperationService;

    public ExamManagementController(
        ExamService examService,
        ExamSubjectService examSubjectService,
        ExamQuestionService examQuestionService,
        SimplifiedQuestionService simplifiedQuestionService,
        ExcelOperationService excelOperationService,
        WindowsOperationService windowsOperationService)
    {
        _examService = examService;
        _examSubjectService = examSubjectService;
        _examQuestionService = examQuestionService;
        _simplifiedQuestionService = simplifiedQuestionService;
        _excelOperationService = excelOperationService;
        _windowsOperationService = windowsOperationService;
    }

    /// <summary>
    /// 管理面板首页
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "试卷管理 - 管理面板";

        // 获取统计数据
        ExamStatistics examStats = await _examService.GetExamStatisticsAsync();
        ExcelOperationStatistics excelStats = await _excelOperationService.GetOperationStatisticsAsync();

        ViewBag.ExamStatistics = examStats;
        ViewBag.ExcelStatistics = excelStats;

        return View();
    }

    /// <summary>
    /// 试卷列表页面
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> ExamList()
    {
        ViewData["Title"] = "试卷管理 - 试卷列表";

        List<Models.Exam.Exam> exams = await _examService.GetAllExamsAsync(true);
        return View(exams);
    }

    /// <summary>
    /// 创建试卷页面
    /// </summary>
    /// <returns></returns>
    public IActionResult CreateExam()
    {
        ViewData["Title"] = "试卷管理 - 创建试卷";
        return View();
    }

    /// <summary>
    /// 试卷详情页面
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns></returns>
    public async Task<IActionResult> ExamDetails(int id)
    {
        Models.Exam.Exam? exam = await _examService.GetExamByIdAsync(id);
        if (exam == null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"试卷管理 - {exam.Name}";
        return View(exam);
    }

    /// <summary>
    /// 编辑试卷页面
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns></returns>
    public async Task<IActionResult> EditExam(int id)
    {
        Models.Exam.Exam? exam = await _examService.GetExamByIdAsync(id);
        if (exam == null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"试卷管理 - 编辑 {exam.Name}";
        return View(exam);
    }









    /// <summary>
    /// 获取科目可用的操作点
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetSubjectOperationPoints(int subjectId)
    {
        try
        {
            ExamSubject? subject = await _examSubjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null)
            {
                return NotFound(new { success = false, message = "科目不存在" });
            }

            object operationPoints = subject.SubjectType switch
            {
                SubjectType.Excel => new
                {
                    basic = await _excelOperationService.GetOperationPointsByCategoryAsync(Models.Excel.ExcelOperationCategory.BasicOperation),
                    dataList = await _excelOperationService.GetOperationPointsByCategoryAsync(Models.Excel.ExcelOperationCategory.DataListOperation),
                    chart = await _excelOperationService.GetOperationPointsByCategoryAsync(Models.Excel.ExcelOperationCategory.ChartOperation)
                },
                SubjectType.Windows => new
                {
                    create = await _windowsOperationService.GetOperationPointsByTypeAsync(Models.Windows.WindowsOperationType.Create),
                    copy = await _windowsOperationService.GetOperationPointsByTypeAsync(Models.Windows.WindowsOperationType.Copy),
                    move = await _windowsOperationService.GetOperationPointsByTypeAsync(Models.Windows.WindowsOperationType.Move),
                    delete = await _windowsOperationService.GetOperationPointsByTypeAsync(Models.Windows.WindowsOperationType.Delete),
                    rename = await _windowsOperationService.GetOperationPointsByTypeAsync(Models.Windows.WindowsOperationType.Rename),
                    shortcut = await _windowsOperationService.GetOperationPointsByTypeAsync(Models.Windows.WindowsOperationType.CreateShortcut),
                    property = await _windowsOperationService.GetOperationPointsByTypeAsync(Models.Windows.WindowsOperationType.ModifyProperties),
                    copyRename = await _windowsOperationService.GetOperationPointsByTypeAsync(Models.Windows.WindowsOperationType.CopyAndRename)
                },
                _ => new { message = "此科目类型暂不支持操作点配置" }
            };

            return Json(new { success = true, subjectType = subject.SubjectType, operationPoints });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 获取操作点详细信息
    /// </summary>
    /// <param name="subjectType">科目类型</param>
    /// <param name="operationNumber">操作点编号</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetOperationPointDetails(SubjectType subjectType, int operationNumber)
    {
        try
        {
            object? operationPoint = subjectType switch
            {
                SubjectType.Excel => await _excelOperationService.GetOperationPointByNumberAsync(operationNumber),
                SubjectType.Windows => await _windowsOperationService.GetOperationPointByIdAsync(operationNumber),
                _ => null
            };

            if (operationPoint == null)
            {
                return NotFound(new { success = false, message = "操作点不存在" });
            }

            return Json(new { success = true, operationPoint });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 题目管理页面 - 根据科目类型动态路由到专门的题目管理页面
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<IActionResult> QuestionManagement(int subjectId)
    {
        ExamSubject? subject = await _examSubjectService.GetSubjectByIdAsync(subjectId);
        if (subject == null)
        {
            return NotFound();
        }

        // 根据科目类型重定向到专门的题目管理页面
        return subject.SubjectType switch
        {
            SubjectType.Excel => RedirectToAction("ExcelQuestionManagement", new { subjectId }),
            SubjectType.Windows => RedirectToAction("WindowsQuestionManagement", new { subjectId }),
            SubjectType.PowerPoint => RedirectToAction("PowerPointQuestionManagement", new { subjectId }),
            SubjectType.Word => RedirectToAction("WordQuestionManagement", new { subjectId }),
            SubjectType.CSharp => RedirectToAction("CSharpQuestionManagement", new { subjectId }),
            _ => RedirectToAction("GeneralQuestionManagement", new { subjectId })
        };
    }

    /// <summary>
    /// Excel科目题目管理页面
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<IActionResult> ExcelQuestionManagement(int subjectId)
    {
        ExamSubject? subject = await _examSubjectService.GetSubjectByIdAsync(subjectId);
        if (subject == null)
        {
            return NotFound();
        }

        if (subject.SubjectType != SubjectType.Excel)
        {
            return RedirectToAction("QuestionManagement", new { subjectId });
        }

        // 获取简化题目列表
        List<SimplifiedQuestionResponse> simplifiedQuestions = await _simplifiedQuestionService.GetSimplifiedQuestionsAsync(subjectId);

        // 为了兼容性，也获取旧的题目列表
        List<ExamQuestion> oldQuestions = await _examQuestionService.GetSubjectQuestionsAsync(subjectId);

        ViewData["Title"] = $"Excel题目管理 - {subject.SubjectName}";
        ViewBag.Subject = subject;
        ViewBag.SimplifiedQuestions = simplifiedQuestions;

        return View(oldQuestions);
    }

    /// <summary>
    /// Windows科目题目管理页面
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<IActionResult> WindowsQuestionManagement(int subjectId)
    {
        ExamSubject? subject = await _examSubjectService.GetSubjectByIdAsync(subjectId);
        if (subject == null)
        {
            return NotFound();
        }

        if (subject.SubjectType != SubjectType.Windows)
        {
            return RedirectToAction("QuestionManagement", new { subjectId });
        }

        // 获取简化题目列表
        List<SimplifiedQuestionResponse> simplifiedQuestions = await _simplifiedQuestionService.GetSimplifiedQuestionsAsync(subjectId);

        // 为了兼容性，也获取旧的题目列表
        List<ExamQuestion> oldQuestions = await _examQuestionService.GetSubjectQuestionsAsync(subjectId);

        ViewData["Title"] = $"Windows题目管理 - {subject.SubjectName}";
        ViewBag.Subject = subject;
        ViewBag.SimplifiedQuestions = simplifiedQuestions;

        return View(oldQuestions);
    }

    /// <summary>
    /// 通用题目管理页面（用于其他科目类型）
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<IActionResult> GeneralQuestionManagement(int subjectId)
    {
        ExamSubject? subject = await _examSubjectService.GetSubjectByIdAsync(subjectId);
        if (subject == null)
        {
            return NotFound();
        }

        List<ExamQuestion> questions = await _examQuestionService.GetSubjectQuestionsAsync(subjectId);

        ViewData["Title"] = $"题目管理 - {subject.SubjectName}";
        ViewBag.Subject = subject;

        return View("QuestionManagement", questions);
    }

    /// <summary>
    /// PowerPoint科目题目管理页面（预留）
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<IActionResult> PowerPointQuestionManagement(int subjectId)
    {
        // 暂时重定向到通用题目管理页面
        return await GeneralQuestionManagement(subjectId);
    }

    /// <summary>
    /// Word科目题目管理页面
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<IActionResult> WordQuestionManagement(int subjectId)
    {
        ExamSubject? subject = await _examSubjectService.GetSubjectByIdAsync(subjectId);
        if (subject == null)
        {
            return NotFound();
        }

        if (subject.SubjectType != SubjectType.Word)
        {
            return RedirectToAction("QuestionManagement", new { subjectId });
        }

        // 题目通过前端API加载，这里返回空列表供视图渲染
        List<ExamQuestion> questions = new List<ExamQuestion>();

        ViewData["Title"] = $"Word题目管理 - {subject.SubjectName}";
        ViewBag.Subject = subject;

        return View(questions);
    }

    /// <summary>
    /// C#科目题目管理页面
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<IActionResult> CSharpQuestionManagement(int subjectId)
    {
        ExamSubject? subject = await _examSubjectService.GetSubjectByIdAsync(subjectId);
        if (subject == null)
        {
            return NotFound();
        }

        if (subject.SubjectType != SubjectType.CSharp)
        {
            return RedirectToAction("QuestionManagement", new { subjectId });
        }

        // 获取简化题目列表
        List<SimplifiedQuestionResponse> simplifiedQuestions = await _simplifiedQuestionService.GetSimplifiedQuestionsAsync(subjectId);

        ViewData["Title"] = $"C#题目管理 - {subject.SubjectName}";
        ViewBag.Subject = subject;
        ViewBag.SimplifiedQuestions = simplifiedQuestions;

        return View();
    }

    /// <summary>
    /// 创建Excel题目页面
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<IActionResult> CreateExcelQuestion(int subjectId)
    {
        ExamSubject? subject = await _examSubjectService.GetSubjectByIdAsync(subjectId);
        if (subject == null)
        {
            return NotFound();
        }

        // 获取Excel操作点列表
        List<Models.Excel.ExcelOperationPoint> basicOperations = await _excelOperationService.GetOperationPointsByCategoryAsync(
            Models.Excel.ExcelOperationCategory.BasicOperation);

        ViewData["Title"] = $"创建Excel题目 - {subject.SubjectName}";
        ViewBag.Subject = subject;
        ViewBag.BasicOperations = basicOperations;

        return View();
    }

    /// <summary>
    /// 试卷验证页面
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns></returns>
    public async Task<IActionResult> ValidateExam(int id)
    {
        Models.Exam.Exam? exam = await _examService.GetExamByIdAsync(id);
        if (exam == null)
        {
            return NotFound();
        }

        ExamValidationResult validationResult = await _examService.ValidateExamForPublishAsync(exam);

        ViewData["Title"] = $"验证试卷 - {exam.Name}";
        ViewBag.ValidationResult = validationResult;

        return View(exam);
    }

    /// <summary>
    /// 系统统计页面
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> Statistics()
    {
        ViewData["Title"] = "试卷管理 - 系统统计";

        ExamStatistics examStats = await _examService.GetExamStatisticsAsync();
        ExcelOperationStatistics excelStats = await _excelOperationService.GetOperationStatisticsAsync();

        ViewBag.ExamStatistics = examStats;
        ViewBag.ExcelStatistics = excelStats;

        return View();
    }

    /// <summary>
    /// 测试操作点选择器页面
    /// </summary>
    /// <returns></returns>
    public IActionResult TestOperationPointSelector()
    {
        ViewData["Title"] = "测试操作点选择器";
        return View();
    }

    /// <summary>
    /// 测试文件类型选择功能页面
    /// </summary>
    /// <returns></returns>
    public IActionResult TestFileTypeSelection()
    {
        ViewData["Title"] = "测试文件类型选择功能";
        return View();
    }

    /// <summary>
    /// 系统设置页面
    /// </summary>
    /// <returns></returns>
    public IActionResult Settings()
    {
        ViewData["Title"] = "试卷管理 - 系统设置";
        return View();
    }

    /// <summary>
    /// 试卷类型测试页面
    /// </summary>
    /// <returns></returns>
    public IActionResult TestExamTypes()
    {
        ViewData["Title"] = "试卷类型测试";
        return View();
    }

    /// <summary>
    /// JSON序列化测试页面
    /// </summary>
    /// <returns></returns>
    public IActionResult TestJsonSerialization()
    {
        ViewData["Title"] = "JSON序列化测试";
        return View();
    }
}
