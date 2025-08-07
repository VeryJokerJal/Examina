using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Exam;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text.Json;

namespace ExaminaWebApplication.Services.Exam;

/// <summary>
/// 试卷管理服务类
/// </summary>
public class ExamService
{
    private readonly ApplicationDbContext _context;

    public ExamService(ApplicationDbContext context)
    {
        _context = context;

        // 设置EPPlus许可证上下文
        ExcelPackage.License.SetNonCommercialOrganization("ExaminaWebApplication");
    }

    /// <summary>
    /// 获取所有试卷
    /// </summary>
    /// <param name="includeDetails">是否包含详细信息</param>
    /// <returns></returns>
    public async Task<List<Models.Exam.Exam>> GetAllExamsAsync(bool includeDetails = false)
    {
        IQueryable<Models.Exam.Exam> query = _context.Exams
            .Include(e => e.Creator);

        if (includeDetails)
        {
            query = query
                .Include(e => e.Subjects)
                .Include(e => e.Questions)
                .Include(e => e.Publisher);
        }

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 根据状态获取试卷
    /// </summary>
    /// <param name="status">试卷状态</param>
    /// <param name="includeDetails">是否包含详细信息</param>
    /// <returns></returns>
    public async Task<List<Models.Exam.Exam>> GetExamsByStatusAsync(ExamStatus status, bool includeDetails = false)
    {
        IQueryable<Models.Exam.Exam> query = _context.Exams
            .Include(e => e.Creator)
            .Where(e => e.Status == status);

        if (includeDetails)
        {
            query = query
                .Include(e => e.Subjects)
                .Include(e => e.Questions)
                .Include(e => e.Publisher);
        }

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 根据创建者获取试卷
    /// </summary>
    /// <param name="createdBy">创建者ID</param>
    /// <param name="includeDetails">是否包含详细信息</param>
    /// <returns></returns>
    public async Task<List<Models.Exam.Exam>> GetExamsByCreatorAsync(int createdBy, bool includeDetails = false)
    {
        IQueryable<Models.Exam.Exam> query = _context.Exams
            .Include(e => e.Creator)
            .Where(e => e.CreatedBy == createdBy);

        if (includeDetails)
        {
            query = query
                .Include(e => e.Subjects)
                .Include(e => e.Questions)
                .Include(e => e.Publisher);
        }

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 根据ID获取试卷详情
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <returns></returns>
    public async Task<Models.Exam.Exam?> GetExamByIdAsync(int examId)
    {
        return await _context.Exams
            .Include(e => e.Creator)
            .Include(e => e.Publisher)
            .Include(e => e.Subjects.OrderBy(s => s.SortOrder))
            .ThenInclude(s => s.Questions.OrderBy(q => q.SortOrder))
            .Include(e => e.Questions.OrderBy(q => q.SortOrder))
            .ThenInclude(q => q.ExcelOperationPoint)
            .FirstOrDefaultAsync(e => e.Id == examId);
    }

    /// <summary>
    /// 创建新试卷
    /// </summary>
    /// <param name="exam">试卷信息</param>
    /// <returns></returns>
    public async Task<Models.Exam.Exam> CreateExamAsync(Models.Exam.Exam exam)
    {
        // 验证创建者是否存在
        bool creatorExists = await _context.Users.AnyAsync(u => u.Id == exam.CreatedBy);
        if (!creatorExists)
        {
            throw new ArgumentException($"创建者 ID {exam.CreatedBy} 不存在");
        }

        exam.CreatedAt = DateTime.UtcNow;
        exam.Status = ExamStatus.Draft;

        _ = _context.Exams.Add(exam);
        _ = await _context.SaveChangesAsync();

        return exam;
    }

    /// <summary>
    /// 更新试卷信息
    /// </summary>
    /// <param name="exam">试卷信息</param>
    /// <returns></returns>
    public async Task<Models.Exam.Exam> UpdateExamAsync(Models.Exam.Exam exam)
    {
        Models.Exam.Exam? existingExam = await _context.Exams.FindAsync(exam.Id);
        if (existingExam == null)
        {
            throw new ArgumentException($"试卷 ID {exam.Id} 不存在");
        }

        // 根据试卷状态决定可编辑的字段
        bool isDraft = existingExam.Status == ExamStatus.Draft;
        bool isPublished = existingExam.Status == ExamStatus.Published;
        bool isInProgress = existingExam.Status == ExamStatus.InProgress;

        // 进行中的试卷不能编辑任何信息
        if (isInProgress)
        {
            throw new InvalidOperationException("进行中的试卷不能编辑");
        }

        // 草稿状态：可以编辑所有字段
        if (isDraft)
        {
            existingExam.Name = exam.Name;
            existingExam.Description = exam.Description;
            existingExam.ExamType = exam.ExamType;
            existingExam.TotalScore = exam.TotalScore;
            existingExam.DurationMinutes = exam.DurationMinutes;
            existingExam.StartTime = exam.StartTime;
            existingExam.EndTime = exam.EndTime;
            existingExam.AllowRetake = exam.AllowRetake;
            existingExam.MaxRetakeCount = exam.MaxRetakeCount;
            existingExam.PassingScore = exam.PassingScore;
            existingExam.RandomizeQuestions = exam.RandomizeQuestions;
            existingExam.ShowScore = exam.ShowScore;
            existingExam.ShowAnswers = exam.ShowAnswers;
            existingExam.Tags = exam.Tags;
            existingExam.ExtendedConfig = exam.ExtendedConfig;
        }
        // 已发布状态：只能编辑部分字段（描述、标签、扩展配置）
        else if (isPublished)
        {
            existingExam.Description = exam.Description;
            existingExam.Tags = exam.Tags;
            existingExam.ExtendedConfig = exam.ExtendedConfig;
            // 可以修改考试时间窗口（如果还未开始）
            if (existingExam.StartTime == null || existingExam.StartTime > DateTime.UtcNow)
            {
                existingExam.StartTime = exam.StartTime;
                existingExam.EndTime = exam.EndTime;
            }
        }
        // 其他状态：不允许编辑
        else
        {
            throw new InvalidOperationException($"状态为'{existingExam.Status}'的试卷不能编辑");
        }

        existingExam.UpdatedAt = DateTime.UtcNow;
        _ = await _context.SaveChangesAsync();
        return existingExam;
    }

    /// <summary>
    /// 删除试卷
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <returns></returns>
    public async Task<bool> DeleteExamAsync(int examId)
    {
        Models.Exam.Exam? exam = await _context.Exams.FindAsync(examId);
        if (exam == null)
        {
            return false;
        }

        // 检查是否可以删除
        if (exam.Status is ExamStatus.Published or ExamStatus.InProgress)
        {
            throw new InvalidOperationException("已发布或进行中的试卷不能删除");
        }

        _ = _context.Exams.Remove(exam);
        _ = await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 发布试卷
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <param name="publishedBy">发布者ID</param>
    /// <returns></returns>
    public async Task<Models.Exam.Exam> PublishExamAsync(int examId, int publishedBy)
    {
        Models.Exam.Exam? exam = await GetExamByIdAsync(examId);
        if (exam == null)
        {
            throw new ArgumentException($"试卷 ID {examId} 不存在");
        }

        // 验证发布者是否存在
        bool publisherExists = await _context.Users.AnyAsync(u => u.Id == publishedBy);
        if (!publisherExists)
        {
            throw new ArgumentException($"发布者 ID {publishedBy} 不存在");
        }

        // 验证试卷是否可以发布
        ExamValidationResult validationResult = await ValidateExamForPublishAsync(exam);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"试卷验证失败: {string.Join(", ", validationResult.Errors)}");
        }

        exam.Status = ExamStatus.Published;
        exam.PublishedBy = publishedBy;
        exam.PublishedAt = DateTime.UtcNow;
        exam.UpdatedAt = DateTime.UtcNow;

        _ = await _context.SaveChangesAsync();
        return exam;
    }

    /// <summary>
    /// 验证试卷是否可以发布
    /// </summary>
    /// <param name="exam">试卷</param>
    /// <returns></returns>
    public Task<ExamValidationResult> ValidateExamForPublishAsync(Models.Exam.Exam exam)
    {
        ExamValidationResult result = new();

        // 基本信息验证
        if (string.IsNullOrWhiteSpace(exam.Name))
        {
            result.Errors.Add("试卷名称不能为空");
        }

        if (exam.TotalScore <= 0)
        {
            result.Errors.Add("试卷总分必须大于0");
        }

        if (exam.DurationMinutes <= 0)
        {
            result.Errors.Add("考试时长必须大于0");
        }

        // 科目验证
        if (exam.Subjects.Count == 0)
        {
            result.Errors.Add("试卷必须包含至少一个科目");
        }

        decimal totalSubjectScore = exam.Subjects.Sum(s => s.Score);
        if (totalSubjectScore != exam.TotalScore)
        {
            result.Errors.Add($"科目总分({totalSubjectScore})与试卷总分({exam.TotalScore})不匹配");
        }

        // 题目验证
        if (exam.Questions.Count == 0)
        {
            result.Errors.Add("试卷必须包含至少一道题目");
        }

        decimal totalQuestionScore = exam.Questions.Sum(q => q.Score);
        if (totalQuestionScore != exam.TotalScore)
        {
            result.Errors.Add($"题目总分({totalQuestionScore})与试卷总分({exam.TotalScore})不匹配");
        }

        // 时间验证
        if (exam.StartTime.HasValue && exam.EndTime.HasValue)
        {
            if (exam.EndTime <= exam.StartTime)
            {
                result.Errors.Add("考试结束时间必须晚于开始时间");
            }
        }

        result.IsValid = result.Errors.Count == 0;
        return Task.FromResult(result);
    }

    /// <summary>
    /// 获取试卷统计信息
    /// </summary>
    /// <returns></returns>
    public async Task<ExamStatistics> GetExamStatisticsAsync()
    {
        int totalExams = await _context.Exams.CountAsync();
        int draftExams = await _context.Exams.CountAsync(e => e.Status == ExamStatus.Draft);
        int publishedExams = await _context.Exams.CountAsync(e => e.Status == ExamStatus.Published);
        int inProgressExams = await _context.Exams.CountAsync(e => e.Status == ExamStatus.InProgress);
        int completedExams = await _context.Exams.CountAsync(e => e.Status == ExamStatus.Completed);

        return new ExamStatistics
        {
            TotalExams = totalExams,
            DraftExams = draftExams,
            PublishedExams = publishedExams,
            InProgressExams = inProgressExams,
            CompletedExams = completedExams
        };
    }

    /// <summary>
    /// 导出试卷到Excel文件
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <returns>Excel文件字节数组</returns>
    public async Task<byte[]> ExportExamToExcelAsync(int examId)
    {
        Models.Exam.Exam? exam = await GetExamByIdAsync(examId);
        if (exam == null)
        {
            throw new ArgumentException($"试卷 ID {examId} 不存在");
        }

        using ExcelPackage package = new();

        // 创建试卷信息工作表
        CreateExamInfoSheet(package, exam);

        // 创建科目信息工作表
        CreateSubjectsSheet(package, exam);

        // 创建题目信息工作表
        CreateQuestionsSheet(package, exam);

        return package.GetAsByteArray();
    }

    /// <summary>
    /// 创建试卷信息工作表
    /// </summary>
    private void CreateExamInfoSheet(ExcelPackage package, Models.Exam.Exam exam)
    {
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("试卷信息");

        // 设置表头
        worksheet.Cells[1, 1].Value = "试卷基本信息";
        worksheet.Cells[1, 1].Style.Font.Bold = true;
        worksheet.Cells[1, 1].Style.Font.Size = 14;

        // 填充试卷信息
        int row = 3;
        worksheet.Cells[row, 1].Value = "试卷名称";
        worksheet.Cells[row, 2].Value = exam.Name;
        row++;

        worksheet.Cells[row, 1].Value = "试卷描述";
        worksheet.Cells[row, 2].Value = exam.Description ?? "";
        row++;

        worksheet.Cells[row, 1].Value = "试卷类型";
        worksheet.Cells[row, 2].Value = exam.ExamType.ToString();
        row++;

        worksheet.Cells[row, 1].Value = "总分";
        worksheet.Cells[row, 2].Value = exam.TotalScore;
        worksheet.Cells[row, 2].Style.Numberformat.Format = "0.00";
        row++;

        worksheet.Cells[row, 1].Value = "考试时长(分钟)";
        worksheet.Cells[row, 2].Value = exam.DurationMinutes;
        row++;

        worksheet.Cells[row, 1].Value = "及格分数";
        worksheet.Cells[row, 2].Value = exam.PassingScore;
        worksheet.Cells[row, 2].Style.Numberformat.Format = "0.00";
        row++;

        worksheet.Cells[row, 1].Value = "状态";
        worksheet.Cells[row, 2].Value = exam.Status.ToString();
        row++;

        worksheet.Cells[row, 1].Value = "创建时间";
        worksheet.Cells[row, 2].Value = exam.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        row++;

        // 设置列宽
        worksheet.Column(1).Width = 20;
        worksheet.Column(2).Width = 40;
    }

    /// <summary>
    /// 创建科目信息工作表
    /// </summary>
    private void CreateSubjectsSheet(ExcelPackage package, Models.Exam.Exam exam)
    {
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("科目信息");

        // 设置表头
        string[] headers = { "科目名称", "科目类型", "分值", "考试时长(分钟)", "排序", "是否必考", "是否启用", "权重" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // 填充科目数据
        int row = 2;
        foreach (ExamSubject subject in exam.Subjects.OrderBy(s => s.SortOrder))
        {
            worksheet.Cells[row, 1].Value = subject.SubjectName;
            worksheet.Cells[row, 2].Value = subject.SubjectType.ToString();
            worksheet.Cells[row, 3].Value = subject.Score;
            worksheet.Cells[row, 3].Style.Numberformat.Format = "0.00";
            worksheet.Cells[row, 4].Value = subject.DurationMinutes;
            worksheet.Cells[row, 5].Value = subject.SortOrder;
            worksheet.Cells[row, 6].Value = subject.IsRequired ? "是" : "否";
            worksheet.Cells[row, 7].Value = subject.IsEnabled ? "是" : "否";
            worksheet.Cells[row, 8].Value = subject.Weight;
            worksheet.Cells[row, 8].Style.Numberformat.Format = "0.00";
            row++;
        }

        // 设置列宽
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
    }

    /// <summary>
    /// 创建题目信息工作表
    /// </summary>
    private void CreateQuestionsSheet(ExcelPackage package, Models.Exam.Exam exam)
    {
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("题目信息");

        // 设置表头
        string[] headers = { "题目标题", "科目", "分值", "难度等级", "预计时间(分钟)", "是否必答", "标签", "备注", "状态", "创建时间" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // 填充题目数据
        int row = 2;
        foreach (ExamQuestion question in exam.Questions.OrderBy(q => q.ExamSubject.SortOrder).ThenBy(q => q.CreatedAt))
        {
            worksheet.Cells[row, 1].Value = question.Title;
            worksheet.Cells[row, 2].Value = question.ExamSubject.SubjectName;
            worksheet.Cells[row, 3].Value = question.Score;
            worksheet.Cells[row, 3].Style.Numberformat.Format = "0.00";
            worksheet.Cells[row, 4].Value = question.DifficultyLevel;
            worksheet.Cells[row, 5].Value = question.EstimatedMinutes;
            worksheet.Cells[row, 6].Value = question.IsRequired ? "是" : "否";
            worksheet.Cells[row, 7].Value = question.Tags ?? "";
            worksheet.Cells[row, 8].Value = question.Remarks ?? "";
            worksheet.Cells[row, 9].Value = question.IsEnabled ? "启用" : "禁用";
            worksheet.Cells[row, 10].Value = question.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            row++;
        }

        // 设置列宽
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
    }
}

/// <summary>
/// 试卷验证结果
/// </summary>
public class ExamValidationResult
{
    /// <summary>
    /// 是否验证通过
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// 警告信息列表
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// 试卷统计信息
/// </summary>
public class ExamStatistics
{
    /// <summary>
    /// 试卷总数
    /// </summary>
    public int TotalExams { get; set; }

    /// <summary>
    /// 草稿试卷数
    /// </summary>
    public int DraftExams { get; set; }

    /// <summary>
    /// 已发布试卷数
    /// </summary>
    public int PublishedExams { get; set; }

    /// <summary>
    /// 进行中试卷数
    /// </summary>
    public int InProgressExams { get; set; }

    /// <summary>
    /// 已完成试卷数
    /// </summary>
    public int CompletedExams { get; set; }
}
