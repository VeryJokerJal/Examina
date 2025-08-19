using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Services.Organization;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Services;

/// <summary>
/// 用户导入服务
/// </summary>
public class UserImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserImportService> _logger;
    private readonly UserManagementService _userManagementService;
    private readonly NonOrganizationStudentService _nonOrganizationStudentService;

    public UserImportService(
        ApplicationDbContext context,
        ILogger<UserImportService> logger,
        UserManagementService userManagementService,
        NonOrganizationStudentService nonOrganizationStudentService)
    {
        _context = context;
        _logger = logger;
        _userManagementService = userManagementService;
        _nonOrganizationStudentService = nonOrganizationStudentService;
    }

    /// <summary>
    /// 导入非组织用户
    /// </summary>
    /// <param name="userDataList">用户数据列表</param>
    /// <param name="creatorUserId">创建者用户ID</param>
    /// <returns>导入结果</returns>
    public async Task<UserImportProcessResult> ImportNonOrganizationUsersAsync(
        List<UserImportData> userDataList,
        int creatorUserId)
    {
        UserImportProcessResult result = new();

        try
        {
            // 验证创建者
            User? creator = await _context.Users.FindAsync(creatorUserId);
            if (creator == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "创建者用户不存在";
                return result;
            }

            List<UserImportResultItem> importResults = [];

            foreach (UserImportData userData in userDataList)
            {
                UserImportResultItem importResult = new()
                {
                    RealName = userData.RealName,
                    PhoneNumber = userData.PhoneNumber,
                    RowNumber = userData.RowNumber
                };

                try
                {
                    // 检查手机号是否已存在于用户表
                    bool userExists = await _context.Users
                        .AnyAsync(u => u.PhoneNumber == userData.PhoneNumber);

                    if (userExists)
                    {
                        importResult.IsSuccess = false;
                        importResult.ErrorMessage = "手机号已存在于用户表中";
                        importResults.Add(importResult);
                        continue;
                    }

                    // 检查手机号是否已存在于非组织学生表
                    bool nonOrgStudentExists = await _context.NonOrganizationStudents
                        .AnyAsync(s => s.PhoneNumber == userData.PhoneNumber && s.IsActive);

                    if (nonOrgStudentExists)
                    {
                        importResult.IsSuccess = false;
                        importResult.ErrorMessage = "手机号已存在于非组织学生表中";
                        importResults.Add(importResult);
                        continue;
                    }

                    // 创建非组织学生
                    Models.Organization.Dto.NonOrganizationStudentDto? studentDto =
                        await _nonOrganizationStudentService.CreateStudentAsync(
                            userData.RealName,
                            userData.PhoneNumber,
                            creatorUserId,
                            "通过Excel导入创建");

                    if (studentDto != null)
                    {
                        importResult.IsSuccess = true;
                        importResult.CreatedId = studentDto.Id;
                        result.SuccessCount++;
                    }
                    else
                    {
                        importResult.IsSuccess = false;
                        importResult.ErrorMessage = "创建非组织学生失败";
                        result.FailureCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "导入非组织学生失败: {RealName}, {PhoneNumber}",
                        userData.RealName, userData.PhoneNumber);
                    importResult.IsSuccess = false;
                    importResult.ErrorMessage = $"导入失败: {ex.Message}";
                    result.FailureCount++;
                }

                importResults.Add(importResult);
            }

            result.ImportResults = importResults;
            result.TotalCount = userDataList.Count;
            result.IsSuccess = result.FailureCount == 0;
            result.ErrorMessage = result.FailureCount > 0 ?
                $"导入完成，成功{result.SuccessCount}条，失败{result.FailureCount}条" : null;

            _logger.LogInformation("非组织用户导入完成: 总数{TotalCount}, 成功{SuccessCount}, 失败{FailureCount}",
                result.TotalCount, result.SuccessCount, result.FailureCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入非组织用户失败");
            result.IsSuccess = false;
            result.ErrorMessage = $"导入失败: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 导入组织用户（预配置用户）
    /// </summary>
    /// <param name="userDataList">用户数据列表</param>
    /// <param name="organizationId">组织ID</param>
    /// <param name="creatorUserId">创建者用户ID</param>
    /// <returns>导入结果</returns>
    public async Task<UserImportProcessResult> ImportOrganizationUsersAsync(
        List<UserImportData> userDataList,
        int organizationId,
        int creatorUserId)
    {
        UserImportProcessResult result = new();

        try
        {
            // 验证组织是否存在
            Models.Organization.Organization? organization = await _context.Organizations
                .FindAsync(organizationId);
            if (organization == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "指定的组织不存在";
                return result;
            }

            // 验证创建者
            User? creator = await _context.Users.FindAsync(creatorUserId);
            if (creator == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "创建者用户不存在";
                return result;
            }

            List<UserImportResultItem> importResults = [];

            foreach (UserImportData userData in userDataList)
            {
                UserImportResultItem importResult = new()
                {
                    RealName = userData.RealName,
                    PhoneNumber = userData.PhoneNumber,
                    RowNumber = userData.RowNumber
                };

                try
                {
                    // 生成用户名（使用手机号作为用户名）
                    string username = userData.PhoneNumber;

                    // 检查用户名是否已存在于预配置用户表中
                    bool preConfiguredUserExists = await _context.PreConfiguredUsers
                        .AnyAsync(p => p.Username == username && p.OrganizationId == organizationId);

                    if (preConfiguredUserExists)
                    {
                        importResult.IsSuccess = false;
                        importResult.ErrorMessage = "用户名已存在于预配置用户表中";
                        importResults.Add(importResult);
                        continue;
                    }

                    // 创建预配置用户
                    PreConfiguredUser preConfiguredUser = new()
                    {
                        Username = username,
                        PhoneNumber = userData.PhoneNumber,
                        RealName = userData.RealName,
                        OrganizationId = organizationId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = creatorUserId,
                        IsApplied = false,
                        Notes = "通过Excel导入创建"
                    };

                    _ = _context.PreConfiguredUsers.Add(preConfiguredUser);
                    _ = await _context.SaveChangesAsync();

                    importResult.IsSuccess = true;
                    importResult.CreatedId = preConfiguredUser.Id;
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "导入预配置用户失败: {RealName}, {PhoneNumber}",
                        userData.RealName, userData.PhoneNumber);
                    importResult.IsSuccess = false;
                    importResult.ErrorMessage = $"导入失败: {ex.Message}";
                    result.FailureCount++;
                }

                importResults.Add(importResult);
            }

            result.ImportResults = importResults;
            result.TotalCount = userDataList.Count;
            result.IsSuccess = result.FailureCount == 0;
            result.ErrorMessage = result.FailureCount > 0 ?
                $"导入完成，成功{result.SuccessCount}条，失败{result.FailureCount}条" : null;

            _logger.LogInformation("组织用户导入完成: 总数{TotalCount}, 成功{SuccessCount}, 失败{FailureCount}",
                result.TotalCount, result.SuccessCount, result.FailureCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入组织用户失败");
            result.IsSuccess = false;
            result.ErrorMessage = $"导入失败: {ex.Message}";
            return result;
        }
    }
}

/// <summary>
/// 用户导入处理结果
/// </summary>
public class UserImportProcessResult
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
    /// 总数量
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
    /// 导入结果详情
    /// </summary>
    public List<UserImportResultItem> ImportResults { get; set; } = [];
}

/// <summary>
/// 用户导入结果项
/// </summary>
public class UserImportResultItem
{
    /// <summary>
    /// 真实姓名
    /// </summary>
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 行号
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建的记录ID
    /// </summary>
    public int? CreatedId { get; set; }
}
