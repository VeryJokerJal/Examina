using ExaminaWebApplication.Attributes;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 用户导入控制器
/// </summary>
[Route("api/[controller]")]
[ApiController]
[RequireLogin]
public class UserImportController : ControllerBase
{
    private readonly ExcelImportService _excelImportService;
    private readonly UserImportService _userImportService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserImportController> _logger;

    public UserImportController(
        ExcelImportService excelImportService,
        UserImportService userImportService,
        ApplicationDbContext context,
        ILogger<UserImportController> logger)
    {
        _excelImportService = excelImportService;
        _userImportService = userImportService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 导入非组织用户
    /// </summary>
    /// <param name="file">Excel文件</param>
    /// <returns>导入结果</returns>
    [HttpPost("import-non-organization-users")]
    [RequireRole(UserRole.Administrator, UserRole.Teacher)]
    public async Task<ActionResult<UserImportApiResponse>> ImportNonOrganizationUsers(IFormFile file)
    {
        try
        {
            // 验证文件
            ValidationResult? fileValidation = ValidateExcelFile(file);
            if (fileValidation != null)
            {
                return BadRequest(new UserImportApiResponse
                {
                    IsSuccess = false,
                    ErrorMessage = fileValidation.ErrorMessage
                });
            }

            int currentUserId = GetCurrentUserId();

            // 读取Excel文件
            using Stream fileStream = file.OpenReadStream();
            ExcelImportResult excelResult = await _excelImportService.ReadUserDataFromExcelAsync(fileStream, file.FileName);

            if (!excelResult.IsSuccess)
            {
                return BadRequest(new UserImportApiResponse
                {
                    IsSuccess = false,
                    ErrorMessage = excelResult.ErrorMessage,
                    ExcelErrors = excelResult.Errors
                });
            }

            // 导入用户数据
            UserImportProcessResult importResult = await _userImportService.ImportNonOrganizationUsersAsync(
                excelResult.UserDataList, currentUserId);

            UserImportApiResponse response = new UserImportApiResponse
            {
                IsSuccess = importResult.IsSuccess,
                ErrorMessage = importResult.ErrorMessage,
                TotalRows = excelResult.TotalRows,
                ValidRows = excelResult.ValidRows,
                TotalCount = importResult.TotalCount,
                SuccessCount = importResult.SuccessCount,
                FailureCount = importResult.FailureCount,
                ExcelErrors = excelResult.Errors,
                ImportResults = importResult.ImportResults
            };

            _logger.LogInformation("非组织用户导入完成: 用户{UserId}, 文件{FileName}, 成功{SuccessCount}, 失败{FailureCount}",
                currentUserId, file.FileName, importResult.SuccessCount, importResult.FailureCount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入非组织用户失败: 文件{FileName}", file.FileName);
            return StatusCode(500, new UserImportApiResponse
            {
                IsSuccess = false,
                ErrorMessage = $"导入失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// 导入组织用户
    /// </summary>
    /// <param name="request">导入请求</param>
    /// <returns>导入结果</returns>
    [HttpPost("import-organization-users")]
    [RequireRole(UserRole.Administrator, UserRole.Teacher)]
    public async Task<ActionResult<UserImportApiResponse>> ImportOrganizationUsers([FromForm] ImportOrganizationUsersRequest request)
    {
        try
        {
            // 验证文件
            ValidationResult? fileValidation = ValidateExcelFile(request.File);
            if (fileValidation != null)
            {
                return BadRequest(new UserImportApiResponse
                {
                    IsSuccess = false,
                    ErrorMessage = fileValidation.ErrorMessage
                });
            }

            // 验证组织ID
            if (request.OrganizationId <= 0)
            {
                return BadRequest(new UserImportApiResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "请选择有效的组织"
                });
            }

            // 验证组织是否存在
            bool organizationExists = await _context.Organizations
                .AnyAsync(o => o.Id == request.OrganizationId && o.IsActive);
            if (!organizationExists)
            {
                return BadRequest(new UserImportApiResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "指定的组织不存在或已禁用"
                });
            }

            int currentUserId = GetCurrentUserId();

            // 读取Excel文件
            using Stream fileStream = request.File.OpenReadStream();
            ExcelImportResult excelResult = await _excelImportService.ReadUserDataFromExcelAsync(fileStream, request.File.FileName);

            if (!excelResult.IsSuccess)
            {
                return BadRequest(new UserImportApiResponse
                {
                    IsSuccess = false,
                    ErrorMessage = excelResult.ErrorMessage,
                    ExcelErrors = excelResult.Errors
                });
            }

            // 导入用户数据
            UserImportProcessResult importResult = await _userImportService.ImportOrganizationUsersAsync(
                excelResult.UserDataList, request.OrganizationId, currentUserId);

            UserImportApiResponse response = new UserImportApiResponse
            {
                IsSuccess = importResult.IsSuccess,
                ErrorMessage = importResult.ErrorMessage,
                TotalRows = excelResult.TotalRows,
                ValidRows = excelResult.ValidRows,
                TotalCount = importResult.TotalCount,
                SuccessCount = importResult.SuccessCount,
                FailureCount = importResult.FailureCount,
                ExcelErrors = excelResult.Errors,
                ImportResults = importResult.ImportResults
            };

            _logger.LogInformation("组织用户导入完成: 用户{UserId}, 组织{OrganizationId}, 文件{FileName}, 成功{SuccessCount}, 失败{FailureCount}",
                currentUserId, request.OrganizationId, request.File.FileName, importResult.SuccessCount, importResult.FailureCount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入组织用户失败: 文件{FileName}, 组织{OrganizationId}", 
                request.File?.FileName, request.OrganizationId);
            return StatusCode(500, new UserImportApiResponse
            {
                IsSuccess = false,
                ErrorMessage = $"导入失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// 获取组织列表
    /// </summary>
    /// <returns>组织列表</returns>
    [HttpGet("organizations")]
    [RequireRole(UserRole.Administrator, UserRole.Teacher)]
    public async Task<ActionResult<List<OrganizationOption>>> GetOrganizations()
    {
        try
        {
            List<OrganizationOption> organizations = await _context.Organizations
                .Where(o => o.IsActive)
                .OrderBy(o => o.Name)
                .Select(o => new OrganizationOption
                {
                    Id = o.Id,
                    Name = o.Name,
                    Type = o.Type
                })
                .ToListAsync();

            return Ok(organizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织列表失败");
            return StatusCode(500, new { message = "获取组织列表失败" });
        }
    }

    /// <summary>
    /// 验证Excel文件
    /// </summary>
    /// <param name="file">文件</param>
    /// <returns>验证结果</returns>
    private static ValidationResult? ValidateExcelFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return new ValidationResult("请选择要导入的Excel文件");
        }

        // 检查文件扩展名
        string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (fileExtension != ".xlsx" && fileExtension != ".xls")
        {
            return new ValidationResult("只支持.xlsx和.xls格式的Excel文件");
        }

        // 检查文件大小（限制为10MB）
        const long maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return new ValidationResult("文件大小不能超过10MB");
        }

        return null;
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>用户ID</returns>
    private int GetCurrentUserId()
    {
        string? userIdClaim = User.FindFirst("UserId")?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }
}

/// <summary>
/// 导入组织用户请求
/// </summary>
public class ImportOrganizationUsersRequest
{
    /// <summary>
    /// Excel文件
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// 组织ID
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "请选择有效的组织")]
    public int OrganizationId { get; set; }
}

/// <summary>
/// 用户导入API响应
/// </summary>
public class UserImportApiResponse
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
    /// Excel总行数
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Excel有效行数
    /// </summary>
    public int ValidRows { get; set; }

    /// <summary>
    /// 导入总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数量
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Excel解析错误
    /// </summary>
    public List<string> ExcelErrors { get; set; } = new List<string>();

    /// <summary>
    /// 导入结果详情
    /// </summary>
    public List<UserImportResultItem> ImportResults { get; set; } = new List<UserImportResultItem>();
}

/// <summary>
/// 组织选项
/// </summary>
public class OrganizationOption
{
    /// <summary>
    /// 组织ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 组织名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 组织类型
    /// </summary>
    public Models.Organization.OrganizationType Type { get; set; }
}
