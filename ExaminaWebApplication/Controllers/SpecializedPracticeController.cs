using System.ComponentModel.DataAnnotations;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Exam;
using ExaminaWebApplication.Models.Practice;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace ExaminaWebApplication.Controllers
{
    public class SpecializedPracticeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SpecializedPracticeController> _logger;

        public SpecializedPracticeController(ApplicationDbContext context, ILogger<SpecializedPracticeController> logger)
        {
            _context = context;
            _logger = logger;

            // 设置EPPlus许可证上下文
            ExcelPackage.License.SetNonCommercialOrganization("ExaminaWebApplication");
        }

        /// <summary>
        /// 专项练习列表页面
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            try
            {
                List<SpecializedPractice> practices = await _context.SpecializedPractices
                    .Include(p => p.Creator)
                    .Include(p => p.Questions)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                ViewData["Title"] = "专项练习管理";
                return View(practices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取专项练习列表时发生错误");
                return View(new List<SpecializedPractice>());
            }
        }

        /// <summary>
        /// 专项练习详情页面
        /// </summary>
        /// <param name="id">练习ID</param>
        /// <returns></returns>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                SpecializedPractice? practice = await _context.SpecializedPractices
                    .Include(p => p.Creator)
                    .Include(p => p.Publisher)
                    .Include(p => p.Questions)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (practice == null)
                {
                    return NotFound();
                }

                ViewData["Title"] = $"专项练习详情 - {practice.Name}";
                return View(practice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取专项练习详情时发生错误，ID: {Id}", id);
                return NotFound();
            }
        }

        /// <summary>
        /// 创建专项练习页面
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewData["Title"] = "创建专项练习";
            return View();
        }

        /// <summary>
        /// 创建专项练习
        /// </summary>
        /// <param name="request">创建请求</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePracticeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                SpecializedPractice practice = new()
                {
                    Name = request.Name,
                    Description = request.Description,
                    SubjectType = request.SubjectType,
                    TotalScore = request.TotalScore,
                    DurationMinutes = request.DurationMinutes,
                    PassingScore = request.PassingScore,
                    AllowRetake = request.AllowRetake,
                    MaxRetakeCount = request.MaxRetakeCount,
                    RandomizeQuestions = request.RandomizeQuestions,
                    ShowScore = request.ShowScore,
                    ShowAnswers = request.ShowAnswers,
                    Tags = request.Tags,
                    CreatedBy = 1, // TODO: 从当前用户获取
                    CreatedAt = DateTime.UtcNow
                };

                _ = _context.SpecializedPractices.Add(practice);
                _ = await _context.SaveChangesAsync();

                return Json(new { success = true, id = practice.Id, message = "专项练习创建成功！" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建专项练习时发生错误");
                return Json(new { success = false, message = "创建失败，请重试。" });
            }
        }

        /// <summary>
        /// 编辑专项练习页面
        /// </summary>
        /// <param name="id">练习ID</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                SpecializedPractice? practice = await _context.SpecializedPractices
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (practice == null)
                {
                    return NotFound();
                }

                ViewData["Title"] = $"编辑专项练习 - {practice.Name}";
                return View(practice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取专项练习编辑页面时发生错误，ID: {Id}", id);
                return NotFound();
            }
        }

        /// <summary>
        /// 更新专项练习
        /// </summary>
        /// <param name="id">练习ID</param>
        /// <param name="request">更新请求</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePracticeRequest request)
        {
            try
            {
                SpecializedPractice? practice = await _context.SpecializedPractices
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (practice == null)
                {
                    return Json(new { success = false, message = "专项练习不存在。" });
                }

                // 只有草稿状态的练习才能编辑基本信息
                if (practice.Status == PracticeStatus.Draft)
                {
                    practice.Name = request.Name;
                    practice.Description = request.Description;
                    practice.SubjectType = request.SubjectType;
                    practice.TotalScore = request.TotalScore;
                    practice.DurationMinutes = request.DurationMinutes;
                    practice.PassingScore = request.PassingScore;
                    practice.AllowRetake = request.AllowRetake;
                    practice.MaxRetakeCount = request.MaxRetakeCount;
                    practice.RandomizeQuestions = request.RandomizeQuestions;
                    practice.ShowScore = request.ShowScore;
                    practice.ShowAnswers = request.ShowAnswers;
                    practice.Tags = request.Tags;
                }

                practice.UpdatedAt = DateTime.UtcNow;

                _ = await _context.SaveChangesAsync();

                return Json(new { success = true, message = "专项练习更新成功！" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新专项练习时发生错误，ID: {Id}", id);
                return Json(new { success = false, message = "更新失败，请重试。" });
            }
        }

        /// <summary>
        /// 删除专项练习
        /// </summary>
        /// <param name="id">练习ID</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                SpecializedPractice? practice = await _context.SpecializedPractices
                    .Include(p => p.Questions)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (practice == null)
                {
                    return Json(new { success = false, message = "专项练习不存在。" });
                }

                // 只有草稿状态的练习才能删除
                if (practice.Status != PracticeStatus.Draft)
                {
                    return Json(new { success = false, message = "只有草稿状态的练习才能删除。" });
                }

                _ = _context.SpecializedPractices.Remove(practice);
                _ = await _context.SaveChangesAsync();

                return Json(new { success = true, message = "专项练习删除成功！" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除专项练习时发生错误，ID: {Id}", id);
                return Json(new { success = false, message = "删除失败，请重试。" });
            }
        }

        /// <summary>
        /// 发布专项练习
        /// </summary>
        /// <param name="id">练习ID</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Publish(int id)
        {
            try
            {
                SpecializedPractice? practice = await _context.SpecializedPractices
                    .Include(p => p.Questions)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (practice == null)
                {
                    return Json(new { success = false, message = "专项练习不存在。" });
                }

                if (practice.Status != PracticeStatus.Draft)
                {
                    return Json(new { success = false, message = "只有草稿状态的练习才能发布。" });
                }

                if (!practice.Questions.Any())
                {
                    return Json(new { success = false, message = "练习中没有题目，无法发布。" });
                }

                practice.Status = PracticeStatus.Published;
                practice.PublishedAt = DateTime.UtcNow;
                practice.PublishedBy = 1; // TODO: 从当前用户获取
                practice.UpdatedAt = DateTime.UtcNow;

                _ = await _context.SaveChangesAsync();

                return Json(new { success = true, message = "专项练习发布成功！" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布专项练习时发生错误，ID: {Id}", id);
                return Json(new { success = false, message = "发布失败，请重试。" });
            }
        }

        /// <summary>
        /// 导出专项练习到Excel文件
        /// </summary>
        /// <param name="id">练习ID</param>
        /// <returns>Excel文件</returns>
        [HttpGet("{id}/export")]
        public async Task<ActionResult> ExportPractice(int id)
        {
            try
            {
                SpecializedPractice? practice = await _context.SpecializedPractices
                    .Include(p => p.Questions)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (practice == null)
                {
                    return NotFound($"专项练习 ID {id} 不存在");
                }

                byte[] excelData = await ExportPracticeToExcelAsync(practice);
                string fileName = $"专项练习_{practice.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出专项练习失败，ID: {Id}", id);
                return StatusCode(500, new { message = "导出失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 导出专项练习到Excel
        /// </summary>
        /// <param name="practice">专项练习</param>
        /// <returns>Excel文件字节数组</returns>
        private async Task<byte[]> ExportPracticeToExcelAsync(SpecializedPractice practice)
        {
            using ExcelPackage package = new();

            await Task.Run(() =>
            {
                // 创建练习信息工作表
                CreatePracticeInfoSheet(package, practice);

                // 创建题目信息工作表
                CreatePracticeQuestionsSheet(package, practice);
            });
            return package.GetAsByteArray();
        }

        /// <summary>
        /// 创建练习信息工作表
        /// </summary>
        private void CreatePracticeInfoSheet(ExcelPackage package, SpecializedPractice practice)
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("练习信息");

            // 设置表头
            worksheet.Cells[1, 1].Value = "专项练习基本信息";
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 14;

            // 填充练习信息
            int row = 3;
            worksheet.Cells[row, 1].Value = "练习名称";
            worksheet.Cells[row, 2].Value = practice.Name;
            row++;

            worksheet.Cells[row, 1].Value = "练习描述";
            worksheet.Cells[row, 2].Value = practice.Description ?? "";
            row++;

            worksheet.Cells[row, 1].Value = "科目类型";
            worksheet.Cells[row, 2].Value = practice.SubjectType.ToString();
            row++;

            worksheet.Cells[row, 1].Value = "总分";
            worksheet.Cells[row, 2].Value = practice.TotalScore;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "0.00";
            row++;

            worksheet.Cells[row, 1].Value = "练习时长(分钟)";
            worksheet.Cells[row, 2].Value = practice.DurationMinutes;
            row++;

            worksheet.Cells[row, 1].Value = "及格分数";
            worksheet.Cells[row, 2].Value = practice.PassingScore;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "0.00";
            row++;

            worksheet.Cells[row, 1].Value = "允许重做";
            worksheet.Cells[row, 2].Value = practice.AllowRetake ? "是" : "否";
            row++;

            worksheet.Cells[row, 1].Value = "最大重做次数";
            worksheet.Cells[row, 2].Value = practice.MaxRetakeCount;
            row++;

            worksheet.Cells[row, 1].Value = "随机题目";
            worksheet.Cells[row, 2].Value = practice.RandomizeQuestions ? "是" : "否";
            row++;

            worksheet.Cells[row, 1].Value = "显示分数";
            worksheet.Cells[row, 2].Value = practice.ShowScore ? "是" : "否";
            row++;

            worksheet.Cells[row, 1].Value = "显示答案";
            worksheet.Cells[row, 2].Value = practice.ShowAnswers ? "是" : "否";
            row++;

            worksheet.Cells[row, 1].Value = "状态";
            worksheet.Cells[row, 2].Value = practice.Status.ToString();
            row++;

            worksheet.Cells[row, 1].Value = "标签";
            worksheet.Cells[row, 2].Value = practice.Tags ?? "";
            row++;

            worksheet.Cells[row, 1].Value = "创建时间";
            worksheet.Cells[row, 2].Value = practice.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

            // 设置列宽
            worksheet.Column(1).Width = 20;
            worksheet.Column(2).Width = 40;
        }

        /// <summary>
        /// 创建题目信息工作表
        /// </summary>
        private void CreatePracticeQuestionsSheet(ExcelPackage package, SpecializedPractice practice)
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("题目信息");

            // 设置表头
            string[] headers = ["题目标题", "分值", "难度等级", "预计时间(分钟)", "操作类型", "题目要求", "状态", "创建时间"];
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // 填充题目数据
            int row = 2;
            foreach (PracticeQuestion practiceQuestion in practice.Questions.OrderBy(q => q.SortOrder))
            {
                worksheet.Cells[row, 1].Value = practiceQuestion.Title;
                worksheet.Cells[row, 2].Value = practiceQuestion.Score;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "0.00";
                worksheet.Cells[row, 3].Value = practiceQuestion.DifficultyLevel;
                worksheet.Cells[row, 4].Value = practiceQuestion.EstimatedMinutes;
                worksheet.Cells[row, 5].Value = practiceQuestion.OperationType;
                worksheet.Cells[row, 6].Value = practiceQuestion.Requirements ?? "";
                worksheet.Cells[row, 7].Value = practiceQuestion.IsEnabled ? "启用" : "禁用";
                worksheet.Cells[row, 8].Value = practiceQuestion.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                row++;
            }

            // 设置列宽
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }
    }

    // 请求模型
    public class CreatePracticeRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public SubjectType SubjectType { get; set; }

        [Range(0.1, 9999.99)]
        public decimal TotalScore { get; set; } = 100.0m;

        public int DurationMinutes { get; set; } = 60;

        [Range(0.1, 9999.99)]
        public decimal PassingScore { get; set; } = 60.0m;

        public bool AllowRetake { get; set; } = true;
        public int MaxRetakeCount { get; set; } = 0;
        public bool RandomizeQuestions { get; set; } = false;
        public bool ShowScore { get; set; } = true;
        public bool ShowAnswers { get; set; } = false;
        public string? Tags { get; set; }
    }

    public class UpdatePracticeRequest : CreatePracticeRequest
    {
    }
}
