using System.Text.Json;
using ExaminaWebApplication.Models.Exam;
using OfficeOpenXml;

namespace ExaminaWebApplication.Services.Exam;

/// <summary>
/// Excel导入导出服务 - 处理Windows题目的Excel导入导出功能
/// </summary>
public class ExcelImportExportService
{
    private readonly SimplifiedQuestionService _simplifiedQuestionService;
    private readonly ILogger<ExcelImportExportService> _logger;

    public ExcelImportExportService(
        SimplifiedQuestionService simplifiedQuestionService,
        ILogger<ExcelImportExportService> logger)
    {
        _simplifiedQuestionService = simplifiedQuestionService;
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
        public List<SimplifiedQuestionResponse> ImportedQuestions { get; set; } = [];
    }

    /// <summary>
    /// 从Excel文件导入Windows题目
    /// </summary>
    /// <param name="fileStream">Excel文件流</param>
    /// <param name="subjectId">科目ID</param>
    /// <returns>导入结果</returns>
    public async Task<ImportResult> ImportWindowsQuestionsFromExcelAsync(Stream fileStream, int subjectId)
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

                    CreateSimplifiedQuestionRequest? questionData = ParseRowToQuestionData(worksheet, row, subjectId);
                    if (questionData != null)
                    {
                        SimplifiedQuestionResponse createdQuestion = await _simplifiedQuestionService.CreateSimplifiedQuestionAsync(questionData);
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
            _logger.LogError(ex, "导入Excel文件失败");
        }

        return result;
    }

    /// <summary>
    /// 导出Windows题目到Excel文件
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <param name="enabledOnly">是否仅导出启用的题目</param>
    /// <returns>Excel文件字节数组</returns>
    public async Task<byte[]> ExportWindowsQuestionsToExcelAsync(int subjectId, bool enabledOnly = false)
    {
        try
        {
            List<SimplifiedQuestionResponse> questions = await _simplifiedQuestionService.GetSimplifiedQuestionsAsync(subjectId);

            if (enabledOnly)
            {
                questions = questions.Where(q => q.IsEnabled).ToList();
            }

            using ExcelPackage package = new();
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Windows题目");

            // 设置表头
            SetExportHeaders(worksheet);

            // 填充数据
            int row = 2;
            foreach (SimplifiedQuestionResponse question in questions)
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
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Windows题目导入模板");

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
        string[] expectedHeaders = new[]
        {
            "操作类型", "分值", "目标类型", "是否文件", "目标名称", "目标路径",
            "源路径", "源是否文件", "原名称", "新名称",
            "快捷方式位置", "属性类型", "保留原文件", "启用属性"
        };

        for (int col = 1; col <= expectedHeaders.Length; col++)
        {
            string? cellValue = worksheet.Cells[1, col].Value?.ToString();
            if (cellValue != expectedHeaders[col - 1])
            {
                result.Errors.Add($"表头格式错误：第{col}列应为'{expectedHeaders[col - 1]}'，实际为'{cellValue}'");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 解析行数据为题目数据
    /// </summary>
    private CreateSimplifiedQuestionRequest? ParseRowToQuestionData(ExcelWorksheet worksheet, int row, int subjectId)
    {
        try
        {
            string? operationType = worksheet.Cells[row, 1].Value?.ToString();
            string? scoreText = worksheet.Cells[row, 2].Value?.ToString();

            if (string.IsNullOrEmpty(operationType) || string.IsNullOrEmpty(scoreText))
            {
                return null;
            }

            if (!decimal.TryParse(scoreText, out decimal score) || score < 0.1m || score > 100.0m)
            {
                return null;
            }

            // 构建操作配置（更新后的字段结构）
            WindowsOperationConfig config = new()
            {
                OperationType = operationType,
                TargetType = worksheet.Cells[row, 3].Value?.ToString(),
                IsFile = ParseBooleanCell(worksheet.Cells[row, 4]),
                TargetName = worksheet.Cells[row, 5].Value?.ToString(),
                TargetPath = worksheet.Cells[row, 6].Value?.ToString(),
                SourcePath = worksheet.Cells[row, 7].Value?.ToString(),
                SourceIsFile = ParseBooleanCell(worksheet.Cells[row, 8]),
                OriginalName = worksheet.Cells[row, 9].Value?.ToString(),
                NewName = worksheet.Cells[row, 10].Value?.ToString(),
                ShortcutLocation = worksheet.Cells[row, 11].Value?.ToString(),
                PropertyType = NormalizePropertyType(worksheet.Cells[row, 12].Value?.ToString()),
                KeepOriginal = ParseBooleanCell(worksheet.Cells[row, 13]),
                Enable = ParseBooleanCell(worksheet.Cells[row, 14])
            };

            // 向后兼容性处理：如果有旧的快捷方式名称字段，忽略它
            // 旧格式可能在第11列有ShortcutName，第12列有ShortcutLocation
            // 新格式直接使用第11列作为ShortcutLocation

            // 验证操作配置（使用更新后的验证逻辑）
            List<string> validationErrors = ValidateOperationConfigUpdated(operationType, config, row);
            if (validationErrors.Count > 0)
            {
                // 如果有验证错误，返回null，让调用方处理错误
                return null;
            }

            return new CreateSimplifiedQuestionRequest
            {
                SubjectId = subjectId,
                OperationType = operationType,
                Score = score,
                OperationConfig = config,
                Title = GenerateTitle(operationType, config),
                Description = GenerateDescription(operationType, config)
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析布尔值单元格
    /// </summary>
    private bool? ParseBooleanCell(ExcelRange cell)
    {
        string? value = cell.Value?.ToString()?.ToLower();
        return value switch
        {
            "是" or "true" or "1" => true,
            "否" or "false" or "0" => false,
            _ => null
        };
    }

    /// <summary>
    /// 标准化属性类型值，支持新的属性类型并处理向后兼容性
    /// </summary>
    private string? NormalizePropertyType(string? propertyType)
    {
        if (string.IsNullOrEmpty(propertyType))
            return null;

        return propertyType.ToLower() switch
        {
            "readonly" or "只读" or "只读属性" => "readonly",
            "hidden" or "隐藏" or "隐藏属性" => "hidden",
            "noindex" or "无内容索引" or "无内容索引属性" => "noindex",
            // 向后兼容：旧的属性类型
            "archive" or "存档" or "存档属性" => "noindex", // 将旧的存档属性映射为无内容索引
            "system" or "系统" or "系统属性" => "readonly", // 将旧的系统属性映射为只读
            _ => propertyType.ToLower()
        };
    }

    /// <summary>
    /// 生成题目标题
    /// </summary>
    private string GenerateTitle(string operationType, WindowsOperationConfig config)
    {
        string fileType = config.IsFile == true ? "文件" : "文件夹";

        return operationType switch
        {
            "Create" => $"创建{fileType}：{config.TargetName}",
            "Delete" => $"删除{fileType}：{config.TargetName}",
            "Copy" => $"复制{fileType}到指定位置",
            "Move" => $"移动{fileType}到指定位置",
            "Rename" => $"重命名{fileType}：{config.OriginalName} → {config.NewName}",
            "CreateShortcut" => $"创建快捷方式到：{config.ShortcutLocation}",
            "ModifyProperties" => $"修改文件属性：{config.PropertyType}",
            "CopyAndRename" => $"复制并重命名{fileType}：{config.NewName}",
            _ => $"Windows文件操作题目"
        };
    }

    /// <summary>
    /// 生成题目描述
    /// </summary>
    private string GenerateDescription(string operationType, WindowsOperationConfig config)
    {
        string fileType = config.IsFile == true ? "文件" : "文件夹";

        return operationType switch
        {
            "Create" => $"请在 {config.TargetPath} 位置创建一个名为 \"{config.TargetName}\" 的{fileType}。",
            "Delete" => $"请删除位于 {config.TargetPath} 的{fileType} \"{config.TargetName}\"。",
            "Copy" => $"请将{fileType} \"{config.SourcePath}\" 复制到 \"{config.TargetPath}\" 位置。",
            "Move" => $"请将{fileType} \"{config.SourcePath}\" 移动到 \"{config.TargetPath}\" 位置。",
            "Rename" => $"请将位于 {config.TargetPath} 的{fileType} \"{config.OriginalName}\" 重命名为 \"{config.NewName}\"。",
            "CreateShortcut" => $"请为 \"{config.TargetPath}\" 在 {config.ShortcutLocation} 位置创建快捷方式。",
            "ModifyProperties" => $"请修改文件 \"{config.TargetPath}\" 的属性。",
            "CopyAndRename" => $"请将{fileType} \"{config.SourcePath}\" 复制到 \"{config.TargetPath}\" 位置，并重命名为 \"{config.NewName}\"。",
            _ => $"请完成指定的Windows文件系统操作。"
        };
    }

    /// <summary>
    /// 设置导出表头
    /// </summary>
    private void SetExportHeaders(ExcelWorksheet worksheet)
    {
        string[] headers = new[]
        {
            "题目标题", "操作类型", "分值", "题目描述", "配置详情", "状态", "创建时间"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // 设置分值列（第3列）的数值格式
        worksheet.Column(3).Style.Numberformat.Format = "0.00";
        worksheet.Column(3).Width = 12;
    }

    /// <summary>
    /// 填充导出行数据
    /// </summary>
    private void FillExportRow(ExcelWorksheet worksheet, int row, SimplifiedQuestionResponse question)
    {
        worksheet.Cells[row, 1].Value = question.Title;
        worksheet.Cells[row, 2].Value = question.OperationType;

        // 设置分值为数值格式，保留两位小数
        worksheet.Cells[row, 3].Value = question.Score;
        worksheet.Cells[row, 3].Style.Numberformat.Format = "0.00";

        worksheet.Cells[row, 4].Value = question.Description;
        worksheet.Cells[row, 5].Value = JsonSerializer.Serialize(question.OperationConfig, new JsonSerializerOptions { WriteIndented = true });
        worksheet.Cells[row, 6].Value = question.IsEnabled ? "启用" : "禁用";
        worksheet.Cells[row, 7].Value = question.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 设置模板表头
    /// </summary>
    private void SetTemplateHeaders(ExcelWorksheet worksheet)
    {
        string[] headers = new[]
        {
            "操作类型*", "分值*", "目标类型", "是否文件", "目标名称", "目标路径",
            "源路径", "源是否文件", "原名称", "新名称",
            "快捷方式位置", "属性类型", "保留原文件", "启用属性"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // 设置分值列（第2列）的数值格式
        worksheet.Column(2).Style.Numberformat.Format = "0.00";
        worksheet.Column(2).Width = 12;
    }

    /// <summary>
    /// 添加模板示例数据
    /// </summary>
    private void AddTemplateExamples(ExcelWorksheet worksheet)
    {
        // 示例1：创建文件
        worksheet.Cells[2, 1].Value = "Create";
        worksheet.Cells[2, 2].Value = 10.5m;
        worksheet.Cells[2, 2].Style.Numberformat.Format = "0.00";
        worksheet.Cells[2, 3].Value = "File";
        worksheet.Cells[2, 4].Value = "是";
        worksheet.Cells[2, 5].Value = "新建文档.txt";
        worksheet.Cells[2, 6].Value = "C:\\Users\\Desktop";

        // 示例2：删除文件夹
        worksheet.Cells[3, 1].Value = "Delete";
        worksheet.Cells[3, 2].Value = 15.25m;
        worksheet.Cells[3, 2].Style.Numberformat.Format = "0.00";
        worksheet.Cells[3, 3].Value = "Folder";
        worksheet.Cells[3, 4].Value = "否";
        worksheet.Cells[3, 5].Value = "临时文件夹";
        worksheet.Cells[3, 6].Value = "C:\\Users\\Desktop";

        // 示例3：复制文件
        worksheet.Cells[4, 1].Value = "Copy";
        worksheet.Cells[4, 2].Value = 12.75m;
        worksheet.Cells[4, 2].Style.Numberformat.Format = "0.00";
        worksheet.Cells[4, 7].Value = "C:\\Users\\Desktop\\源文件.txt";
        worksheet.Cells[4, 8].Value = "是";
        worksheet.Cells[4, 6].Value = "C:\\Users\\Documents";
        worksheet.Cells[4, 14].Value = "是";
    }

    /// <summary>
    /// 添加说明工作表
    /// </summary>
    private void AddInstructionSheet(ExcelPackage package)
    {
        ExcelWorksheet instructionSheet = package.Workbook.Worksheets.Add("导入说明");

        instructionSheet.Cells[1, 1].Value = "Windows题目导入说明";
        instructionSheet.Cells[1, 1].Style.Font.Bold = true;
        instructionSheet.Cells[1, 1].Style.Font.Size = 16;

        string[] instructions = new[]
        {
            "",
            "1. 必填字段标有*号，请确保填写完整",
            "2. 操作类型支持：Create、Delete、Copy、Move、Rename、CreateShortcut、ModifyProperties、CopyAndRename",
            "3. 是否文件字段：填写'是'表示文件，'否'表示文件夹",
            "4. 分值范围：0.1-100.0（支持小数，如：15.5、20.25）",
            "5. 路径格式：使用Windows路径格式，如 C:\\Users\\Desktop",
            "6. 布尔值字段：可填写'是/否'、'true/false'或'1/0'",
            "",
            "操作类型说明：",
            "- Create：创建文件或文件夹，需填写目标类型、是否文件、目标名称、目标路径",
            "- Delete：删除文件或文件夹，需填写目标类型、是否文件、目标名称、目标路径",
            "- Copy：复制文件或文件夹，需填写源路径、源是否文件、目标路径、保留原文件",
            "- Move：移动文件或文件夹，需填写源路径、源是否文件、目标路径",
            "- Rename：重命名文件或文件夹，需填写原名称、新名称、目标路径、是否文件",
            "- CreateShortcut：创建快捷方式，需填写目标路径、快捷方式位置",
            "- ModifyProperties：修改属性，需填写文件路径、属性类型（readonly/hidden/noindex）",
            "- CopyAndRename：复制并重命名，需填写源路径、源是否文件、目标路径、新名称",
            "",
            "属性类型说明：",
            "- readonly：只读属性",
            "- hidden：隐藏属性",
            "- noindex：无内容索引属性",
            "",
            "向后兼容性：",
            "- 旧的'archive'属性类型将自动转换为'noindex'",
            "- 旧的'system'属性类型将自动转换为'readonly'"
        };

        for (int i = 0; i < instructions.Length; i++)
        {
            instructionSheet.Cells[i + 2, 1].Value = instructions[i];
        }

        instructionSheet.Cells[instructionSheet.Dimension.Address].AutoFitColumns();
    }

    /// <summary>
    /// 验证操作配置（更新后的验证逻辑）
    /// </summary>
    private List<string> ValidateOperationConfigUpdated(string operationType, WindowsOperationConfig config, int row)
    {
        List<string> errors = new();

        switch (operationType.ToUpper())
        {
            case "CREATE":
                if (string.IsNullOrEmpty(config.TargetType))
                    errors.Add($"第{row}行：创建操作缺少目标类型");
                if (string.IsNullOrEmpty(config.TargetName))
                    errors.Add($"第{row}行：创建操作缺少目标名称");
                if (string.IsNullOrEmpty(config.TargetPath))
                    errors.Add($"第{row}行：创建操作缺少目标路径");
                break;

            case "DELETE":
                if (string.IsNullOrEmpty(config.TargetType))
                    errors.Add($"第{row}行：删除操作缺少目标类型");
                if (string.IsNullOrEmpty(config.TargetName))
                    errors.Add($"第{row}行：删除操作缺少目标名称");
                if (string.IsNullOrEmpty(config.TargetPath))
                    errors.Add($"第{row}行：删除操作缺少目标路径");
                // 注意：不再验证确认删除字段
                break;

            case "COPY":
                if (string.IsNullOrEmpty(config.SourcePath))
                    errors.Add($"第{row}行：复制操作缺少源路径");
                if (string.IsNullOrEmpty(config.TargetPath))
                    errors.Add($"第{row}行：复制操作缺少目标路径");
                break;

            case "MOVE":
                if (string.IsNullOrEmpty(config.SourcePath))
                    errors.Add($"第{row}行：移动操作缺少源路径");
                if (string.IsNullOrEmpty(config.TargetPath))
                    errors.Add($"第{row}行：移动操作缺少目标路径");
                break;

            case "RENAME":
                if (string.IsNullOrEmpty(config.OriginalName))
                    errors.Add($"第{row}行：重命名操作缺少原名称");
                if (string.IsNullOrEmpty(config.NewName))
                    errors.Add($"第{row}行：重命名操作缺少新名称");
                if (string.IsNullOrEmpty(config.TargetPath))
                    errors.Add($"第{row}行：重命名操作缺少目标路径");
                break;

            case "CREATESHORTCUT":
                if (string.IsNullOrEmpty(config.TargetPath))
                    errors.Add($"第{row}行：创建快捷方式操作缺少目标路径");
                if (string.IsNullOrEmpty(config.ShortcutLocation))
                    errors.Add($"第{row}行：创建快捷方式操作缺少快捷方式位置");
                // 注意：不再验证快捷方式名称字段
                break;

            case "MODIFYPROPERTIES":
                if (string.IsNullOrEmpty(config.TargetPath))
                    errors.Add($"第{row}行：修改属性操作缺少目标路径");
                if (string.IsNullOrEmpty(config.PropertyType))
                    errors.Add($"第{row}行：修改属性操作缺少属性类型");
                else if (!IsValidPropertyType(config.PropertyType))
                    errors.Add($"第{row}行：无效的属性类型'{config.PropertyType}'，支持的类型：readonly、hidden、noindex");
                // 注意：不再验证操作类型字段（添加/移除/切换）
                break;

            case "COPYANDRENAME":
                if (string.IsNullOrEmpty(config.SourcePath))
                    errors.Add($"第{row}行：复制重命名操作缺少源路径");
                if (string.IsNullOrEmpty(config.TargetPath))
                    errors.Add($"第{row}行：复制重命名操作缺少目标路径");
                if (string.IsNullOrEmpty(config.NewName))
                    errors.Add($"第{row}行：复制重命名操作缺少新名称");
                break;

            default:
                errors.Add($"第{row}行：不支持的操作类型'{operationType}'");
                break;
        }

        return errors;
    }

    /// <summary>
    /// 验证属性类型是否有效
    /// </summary>
    private bool IsValidPropertyType(string? propertyType)
    {
        if (string.IsNullOrEmpty(propertyType))
            return false;

        string normalizedType = propertyType.ToLower();
        return normalizedType is "readonly" or "hidden" or "noindex";
    }

    /// <summary>
    /// 验证行数据的基本格式和必填字段
    /// </summary>
    private List<string> ValidateRowData(ExcelWorksheet worksheet, int row)
    {
        List<string> errors = new();

        // 验证操作类型
        string? operationType = worksheet.Cells[row, 1].Value?.ToString();
        if (string.IsNullOrEmpty(operationType))
        {
            errors.Add($"第{row}行：操作类型不能为空");
            return errors; // 如果操作类型为空，无法进行后续验证
        }

        // 验证分值
        string? scoreText = worksheet.Cells[row, 2].Value?.ToString();
        if (string.IsNullOrEmpty(scoreText))
        {
            errors.Add($"第{row}行：分值不能为空");
        }
        else if (!decimal.TryParse(scoreText, out decimal score) || score < 0.1m || score > 100.0m)
        {
            errors.Add($"第{row}行：分值必须是0.1-100.0之间的数值，当前值：{scoreText}");
        }

        // 根据操作类型验证具体的配置字段
        WindowsOperationConfig config = new()
        {
            OperationType = operationType,
            TargetType = worksheet.Cells[row, 3].Value?.ToString(),
            IsFile = ParseBooleanCell(worksheet.Cells[row, 4]),
            TargetName = worksheet.Cells[row, 5].Value?.ToString(),
            TargetPath = worksheet.Cells[row, 6].Value?.ToString(),
            SourcePath = worksheet.Cells[row, 7].Value?.ToString(),
            SourceIsFile = ParseBooleanCell(worksheet.Cells[row, 8]),
            OriginalName = worksheet.Cells[row, 9].Value?.ToString(),
            NewName = worksheet.Cells[row, 10].Value?.ToString(),
            ShortcutLocation = worksheet.Cells[row, 11].Value?.ToString(),
            PropertyType = NormalizePropertyType(worksheet.Cells[row, 12].Value?.ToString()),
            KeepOriginal = ParseBooleanCell(worksheet.Cells[row, 13]),
            Enable = ParseBooleanCell(worksheet.Cells[row, 14])
        };

        // 使用更新后的验证逻辑
        List<string> configErrors = ValidateOperationConfigUpdated(operationType, config, row);
        errors.AddRange(configErrors);

        return errors;
    }
}
