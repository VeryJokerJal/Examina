using System.Text.Json;
using ExaminaWebApplication.Models.Exam;
using OfficeOpenXml;

namespace ExaminaWebApplication.Services.Word;

/// <summary>
/// Word题目Excel导入导出服务 - 处理Word题目的Excel导入导出功能
/// </summary>
public class WordQuestionExcelService
{
    private readonly WordQuestionService _wordQuestionService;
    private readonly ILogger<WordQuestionExcelService> _logger;

    public WordQuestionExcelService(
        WordQuestionService wordQuestionService,
        ILogger<WordQuestionExcelService> logger)
    {
        _wordQuestionService = wordQuestionService;
        _logger = logger;

        // 设置EPPlus许可证上下文（EPPlus 8+）
        ExcelPackage.License.SetNonCommercialOrganization("ExaminaWebApplication");
    }

    /// <summary>
    /// 导入结果模型
    /// </summary>
    public class ImportResult
    {
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public List<string> Errors { get; set; } = [];
        public List<WordQuestion> ImportedQuestions { get; set; } = [];
    }

    /// <summary>
    /// 从Excel文件导入Word题目
    /// </summary>
    /// <param name="fileStream">Excel文件流</param>
    /// <param name="subjectId">科目ID</param>
    /// <returns>导入结果</returns>
    public async Task<ImportResult> ImportWordQuestionsFromExcelAsync(Stream fileStream, int subjectId)
    {
        ImportResult result = new();

        try
        {
            using ExcelPackage package = new(fileStream);
            ExcelWorksheet? worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                result.Errors.Add("Excel文件中没有找到工作表");
                return result;
            }

            // 验证表头
            if (!ValidateHeaders(worksheet, result))
            {
                return result;
            }

            // 从第2行开始读取数据（第1行是表头）
            int rowCount = worksheet.Dimension?.Rows ?? 0;

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    // 先进行详细的行验证
                    List<string> rowErrors = ValidateRowData(worksheet, row);
                    if (rowErrors.Count > 0)
                    {
                        result.FailCount++;
                        result.Errors.AddRange(rowErrors);
                        continue;
                    }

                    WordQuestion? questionData = ParseRowToQuestionData(worksheet, row, subjectId);
                    if (questionData != null)
                    {
                        WordQuestion createdQuestion = await _wordQuestionService.CreateQuestionAsync(questionData);
                        
                        // 如果有操作点数据，添加操作点
                        if (questionData.OperationPoints.Any())
                        {
                            foreach (WordQuestionOperationPoint operationPoint in questionData.OperationPoints)
                            {
                                await _wordQuestionService.AddOperationPointToQuestionAsync(createdQuestion.Id, operationPoint);
                            }
                        }

                        result.ImportedQuestions.Add(createdQuestion);
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailCount++;
                        result.Errors.Add($"第{row}行：数据格式错误或缺少必填字段（分值必须在0.1-100.0范围内）");
                    }
                }
                catch (Exception ex)
                {
                    result.FailCount++;
                    result.Errors.Add($"第{row}行：{ex.Message}");
                    _logger.LogError(ex, $"导入第{row}行数据失败");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"读取Excel文件失败：{ex.Message}");
            _logger.LogError(ex, "读取Excel文件失败");
        }

        return result;
    }

    /// <summary>
    /// 导出Word题目到Excel文件
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <param name="enabledOnly">是否仅导出启用的题目</param>
    /// <returns>Excel文件字节数组</returns>
    public async Task<byte[]> ExportWordQuestionsToExcelAsync(int subjectId, bool enabledOnly = false)
    {
        try
        {
            List<WordQuestion> questions = await _wordQuestionService.GetQuestionsBySubjectIdAsync(subjectId);

            if (enabledOnly)
            {
                questions = questions.Where(q => q.IsEnabled).ToList();
            }

            using ExcelPackage package = new();
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Word题目");

            // 设置表头
            SetExportHeaders(worksheet);

            // 填充数据
            int row = 2;
            foreach (WordQuestion question in questions)
            {
                FillExportRow(worksheet, row, question);
                row++;
            }

            // 设置列宽
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出Excel文件失败");
            throw;
        }
    }

    /// <summary>
    /// 生成导入模板
    /// </summary>
    /// <returns>模板Excel文件字节数组</returns>
    public byte[] GenerateImportTemplate()
    {
        try
        {
            using ExcelPackage package = new();
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Word题目导入模板");

            // 设置表头
            SetTemplateHeaders(worksheet);

            // 添加示例数据
            AddTemplateExamples(worksheet);

            // 设置列宽
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // 添加说明工作表
            AddInstructionSheet(package);

            return package.GetAsByteArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成导入模板失败");
            throw;
        }
    }

    /// <summary>
    /// 验证Excel表头
    /// </summary>
    private bool ValidateHeaders(ExcelWorksheet worksheet, ImportResult result)
    {
        string[] expectedHeaders = [
            "题目标题", "题目描述", "题目要求", "总分值", "是否启用",
            "操作类型1", "操作分值1", "操作配置1",
            "操作类型2", "操作分值2", "操作配置2",
            "操作类型3", "操作分值3", "操作配置3"
        ];

        for (int col = 1; col <= expectedHeaders.Length; col++)
        {
            string? actualHeader = worksheet.Cells[1, col].Value?.ToString();
            if (actualHeader != expectedHeaders[col - 1])
            {
                result.Errors.Add($"表头格式错误：第{col}列应为'{expectedHeaders[col - 1]}'，实际为'{actualHeader}'");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 验证行数据
    /// </summary>
    private List<string> ValidateRowData(ExcelWorksheet worksheet, int row)
    {
        List<string> errors = [];

        // 验证必填字段
        string? title = worksheet.Cells[row, 1].Value?.ToString();
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add($"第{row}行：题目标题不能为空");
        }

        // 验证分值
        string? scoreText = worksheet.Cells[row, 4].Value?.ToString();
        if (!decimal.TryParse(scoreText, out decimal score) || score < 0.1m || score > 100.0m)
        {
            errors.Add($"第{row}行：总分值必须是0.1-100.0之间的数字");
        }

        return errors;
    }

    /// <summary>
    /// 解析行数据为题目对象
    /// </summary>
    private WordQuestion? ParseRowToQuestionData(ExcelWorksheet worksheet, int row, int subjectId)
    {
        try
        {
            string title = worksheet.Cells[row, 1].Value?.ToString() ?? "";
            string? description = worksheet.Cells[row, 2].Value?.ToString();
            string? requirements = worksheet.Cells[row, 3].Value?.ToString();
            
            if (!decimal.TryParse(worksheet.Cells[row, 4].Value?.ToString(), out decimal totalScore))
            {
                return null;
            }

            bool isEnabled = worksheet.Cells[row, 5].Value?.ToString()?.Trim() == "是";

            WordQuestion question = new()
            {
                SubjectId = subjectId,
                Title = title,
                Description = description,
                Requirements = requirements,
                TotalScore = totalScore,
                IsEnabled = isEnabled,
                CreatedAt = DateTime.UtcNow
            };

            // 解析操作点数据（最多3个操作点）
            List<WordQuestionOperationPoint> operationPoints = [];
            for (int i = 0; i < 3; i++)
            {
                int baseCol = 6 + (i * 3); // 操作类型列：6, 9, 12
                string? operationType = worksheet.Cells[row, baseCol].Value?.ToString();
                
                if (!string.IsNullOrWhiteSpace(operationType))
                {
                    if (decimal.TryParse(worksheet.Cells[row, baseCol + 1].Value?.ToString(), out decimal opScore))
                    {
                        string operationConfig = worksheet.Cells[row, baseCol + 2].Value?.ToString() ?? "{}";
                        
                        operationPoints.Add(new WordQuestionOperationPoint
                        {
                            OperationType = operationType,
                            Score = opScore,
                            OperationConfig = operationConfig,
                            OrderIndex = i + 1,
                            IsEnabled = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            question.OperationPoints = operationPoints;
            return question;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"解析第{row}行数据失败");
            return null;
        }
    }

    /// <summary>
    /// 设置导出表头
    /// </summary>
    private void SetExportHeaders(ExcelWorksheet worksheet)
    {
        string[] headers = [
            "题目ID", "题目标题", "题目描述", "题目要求", "总分值", "是否启用", "创建时间", "更新时间",
            "操作点数量", "操作点详情"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }
    }

    /// <summary>
    /// 填充导出行数据
    /// </summary>
    private void FillExportRow(ExcelWorksheet worksheet, int row, WordQuestion question)
    {
        worksheet.Cells[row, 1].Value = question.Id;
        worksheet.Cells[row, 2].Value = question.Title;
        worksheet.Cells[row, 3].Value = question.Description;
        worksheet.Cells[row, 4].Value = question.Requirements;
        worksheet.Cells[row, 5].Value = question.TotalScore;
        worksheet.Cells[row, 6].Value = question.IsEnabled ? "是" : "否";
        worksheet.Cells[row, 7].Value = question.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        worksheet.Cells[row, 8].Value = question.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss");
        worksheet.Cells[row, 9].Value = question.OperationPoints.Count;
        
        // 操作点详情（JSON格式）
        if (question.OperationPoints.Any())
        {
            string operationPointsJson = JsonSerializer.Serialize(question.OperationPoints.Select(op => new
            {
                op.OperationType,
                op.Score,
                op.OperationConfig,
                op.OrderIndex,
                op.IsEnabled
            }), new JsonSerializerOptions { WriteIndented = true });
            worksheet.Cells[row, 10].Value = operationPointsJson;
        }
    }

    /// <summary>
    /// 设置模板表头
    /// </summary>
    private void SetTemplateHeaders(ExcelWorksheet worksheet)
    {
        string[] headers = [
            "题目标题", "题目描述", "题目要求", "总分值", "是否启用",
            "操作类型1", "操作分值1", "操作配置1",
            "操作类型2", "操作分值2", "操作配置2",
            "操作类型3", "操作分值3", "操作配置3"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
        }
    }

    /// <summary>
    /// 添加模板示例数据
    /// </summary>
    private void AddTemplateExamples(ExcelWorksheet worksheet)
    {
        // 示例1：基础Word题目
        worksheet.Cells[2, 1].Value = "Word文档格式设置";
        worksheet.Cells[2, 2].Value = "设置Word文档的基本格式";
        worksheet.Cells[2, 3].Value = "请按照要求设置文档格式，包括字体、段落等";
        worksheet.Cells[2, 4].Value = 15.0m;
        worksheet.Cells[2, 5].Value = "是";
        worksheet.Cells[2, 6].Value = "字体设置";
        worksheet.Cells[2, 7].Value = 5.0m;
        worksheet.Cells[2, 8].Value = "{\"fontName\":\"宋体\",\"fontSize\":12}";
        worksheet.Cells[2, 9].Value = "段落设置";
        worksheet.Cells[2, 10].Value = 5.0m;
        worksheet.Cells[2, 11].Value = "{\"alignment\":\"left\",\"lineSpacing\":1.5}";
        worksheet.Cells[2, 12].Value = "页面设置";
        worksheet.Cells[2, 13].Value = 5.0m;
        worksheet.Cells[2, 14].Value = "{\"pageSize\":\"A4\",\"margin\":\"normal\"}";

        // 示例2：表格操作题目
        worksheet.Cells[3, 1].Value = "Word表格制作";
        worksheet.Cells[3, 2].Value = "在Word中创建和编辑表格";
        worksheet.Cells[3, 3].Value = "创建一个3行4列的表格，并设置表格样式";
        worksheet.Cells[3, 4].Value = 20.0m;
        worksheet.Cells[3, 5].Value = "是";
        worksheet.Cells[3, 6].Value = "插入表格";
        worksheet.Cells[3, 7].Value = 10.0m;
        worksheet.Cells[3, 8].Value = "{\"rows\":3,\"columns\":4}";
        worksheet.Cells[3, 9].Value = "表格样式";
        worksheet.Cells[3, 10].Value = 10.0m;
        worksheet.Cells[3, 11].Value = "{\"style\":\"表格样式1\",\"borderStyle\":\"单线\"}";
    }

    /// <summary>
    /// 添加说明工作表
    /// </summary>
    private void AddInstructionSheet(ExcelPackage package)
    {
        ExcelWorksheet instructionSheet = package.Workbook.Worksheets.Add("导入说明");

        instructionSheet.Cells[1, 1].Value = "Word题目导入说明";
        instructionSheet.Cells[1, 1].Style.Font.Bold = true;
        instructionSheet.Cells[1, 1].Style.Font.Size = 16;

        instructionSheet.Cells[3, 1].Value = "字段说明：";
        instructionSheet.Cells[3, 1].Style.Font.Bold = true;

        string[] instructions = [
            "1. 题目标题：必填，最多200个字符",
            "2. 题目描述：可选，最多1000个字符",
            "3. 题目要求：可选，最多2000个字符，支持Markdown格式",
            "4. 总分值：必填，范围0.1-100.0",
            "5. 是否启用：填写'是'或'否'",
            "6. 操作类型：操作点的类型名称",
            "7. 操作分值：该操作点的分值",
            "8. 操作配置：JSON格式的操作配置参数",
            "",
            "注意事项：",
            "• 每个题目最多可以包含3个操作点",
            "• 操作配置必须是有效的JSON格式",
            "• 总分值应等于所有操作点分值之和",
            "• 导入前请确保数据格式正确"
        ];

        for (int i = 0; i < instructions.Length; i++)
        {
            instructionSheet.Cells[4 + i, 1].Value = instructions[i];
        }

        instructionSheet.Cells[instructionSheet.Dimension.Address].AutoFitColumns();
    }
}
