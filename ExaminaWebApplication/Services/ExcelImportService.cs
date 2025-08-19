using OfficeOpenXml;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ExaminaWebApplication.Services;

/// <summary>
/// Excel导入服务
/// </summary>
public class ExcelImportService
{
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(ILogger<ExcelImportService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 从Excel文件中读取用户数据
    /// </summary>
    /// <param name="fileStream">Excel文件流</param>
    /// <param name="fileName">文件名</param>
    /// <returns>用户数据列表和导入结果</returns>
    public async Task<ExcelImportResult> ReadUserDataFromExcelAsync(Stream fileStream, string fileName)
    {
        ExcelImportResult result = new ExcelImportResult();
        
        try
        {
            // 设置EPPlus许可证上下文
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using ExcelPackage package = new ExcelPackage(fileStream);
            
            if (package.Workbook.Worksheets.Count == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Excel文件中没有工作表";
                return result;
            }

            ExcelWorksheet worksheet = package.Workbook.Worksheets[0]; // 使用第一个工作表
            int rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount <= 1)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Excel文件中没有数据行（除标题行外）";
                return result;
            }

            List<UserImportData> userDataList = new List<UserImportData>();
            List<string> errors = new List<string>();

            // 从第2行开始读取数据（第1行通常是标题）
            for (int row = 2; row <= rowCount; row++)
            {
                string? realName = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                string? phoneNumber = worksheet.Cells[row, 2].Value?.ToString()?.Trim();

                // 跳过空行
                if (string.IsNullOrWhiteSpace(realName) && string.IsNullOrWhiteSpace(phoneNumber))
                {
                    continue;
                }

                UserImportData userData = new UserImportData
                {
                    RealName = realName ?? string.Empty,
                    PhoneNumber = phoneNumber ?? string.Empty,
                    RowNumber = row
                };

                // 验证数据
                List<ValidationResult> validationResults = new List<ValidationResult>();
                ValidationContext validationContext = new ValidationContext(userData);
                
                if (!Validator.TryValidateObject(userData, validationContext, validationResults, true))
                {
                    foreach (ValidationResult validationResult in validationResults)
                    {
                        errors.Add($"第{row}行: {validationResult.ErrorMessage}");
                    }
                    continue;
                }

                // 额外的手机号格式验证
                if (!IsValidPhoneNumber(userData.PhoneNumber))
                {
                    errors.Add($"第{row}行: 手机号格式不正确 ({userData.PhoneNumber})");
                    continue;
                }

                userDataList.Add(userData);
            }

            // 检查重复的手机号
            Dictionary<string, List<int>> phoneNumberGroups = userDataList
                .GroupBy(u => u.PhoneNumber)
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.Select(u => u.RowNumber).ToList());

            foreach (KeyValuePair<string, List<int>> group in phoneNumberGroups)
            {
                string rowNumbers = string.Join(", ", group.Value);
                errors.Add($"手机号重复: {group.Key} (出现在第{rowNumbers}行)");
            }

            result.UserDataList = userDataList;
            result.TotalRows = rowCount - 1; // 减去标题行
            result.ValidRows = userDataList.Count;
            result.Errors = errors;
            result.IsSuccess = errors.Count == 0;
            result.ErrorMessage = errors.Count > 0 ? $"发现{errors.Count}个错误" : null;

            _logger.LogInformation("Excel文件读取完成: {FileName}, 总行数: {TotalRows}, 有效行数: {ValidRows}, 错误数: {ErrorCount}",
                fileName, result.TotalRows, result.ValidRows, errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取Excel文件失败: {FileName}", fileName);
            result.IsSuccess = false;
            result.ErrorMessage = $"读取Excel文件失败: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 验证手机号格式
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <returns>是否有效</returns>
    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        // 中国大陆手机号正则表达式
        string pattern = @"^1[3-9]\d{9}$";
        return Regex.IsMatch(phoneNumber, pattern);
    }
}

/// <summary>
/// 用户导入数据模型
/// </summary>
public class UserImportData
{
    /// <summary>
    /// 真实姓名
    /// </summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    [Required(ErrorMessage = "手机号码不能为空")]
    [StringLength(20, ErrorMessage = "手机号码长度不能超过20个字符")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 在Excel中的行号
    /// </summary>
    public int RowNumber { get; set; }
}

/// <summary>
/// Excel导入结果
/// </summary>
public class ExcelImportResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 用户数据列表
    /// </summary>
    public List<UserImportData> UserDataList { get; set; } = new List<UserImportData>();

    /// <summary>
    /// 总行数（不包括标题行）
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// 有效行数
    /// </summary>
    public int ValidRows { get; set; }

    /// <summary>
    /// 错误列表
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();
}
