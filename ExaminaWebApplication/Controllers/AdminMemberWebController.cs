using System.Security.Claims;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Requests;
using ExaminaWebApplication.Models.Organization.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 管理员网页端成员管理控制器（非组织成员）
/// </summary>
[Authorize(Policy = "AdminPolicy")]
public class AdminMemberWebController : Controller
{
    private readonly ILogger<AdminMemberWebController> _logger;
    private readonly ApplicationDbContext _context;

    public AdminMemberWebController(
        ILogger<AdminMemberWebController> logger,
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 成员管理主页
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            _logger.LogInformation("访问成员管理页面");

            // 获取所有成员列表
            List<MemberDto>? members = await GetAllMembersAsync(includeInactive: false);

            MemberManagementViewModel viewModel = new()
            {
                Members = members ?? []
            };

            _logger.LogInformation("成员管理页面加载完成，成员数量: {MemberCount}", members.Count);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载成员管理页面失败");
            TempData["ErrorMessage"] = "加载页面失败，请稍后重试";
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// 更新成员信息
    /// </summary>
    [HttpPost]
    [Route("Admin/Member/UpdateMemberInfo")]
    public async Task<IActionResult> UpdateMemberInfo([FromBody] UpdateMemberInfoRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "请求参数无效" });
            }

            // 查找成员
            OrganizationMember? member = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.Id == request.MemberId);
            if (member == null)
            {
                return NotFound(new { message = "成员不存在" });
            }

            // 检查手机号是否已被其他成员使用（如果不为空）
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                bool phoneExists = await _context.OrganizationMembers
                    .AnyAsync(m => m.PhoneNumber == request.PhoneNumber && m.Id != request.MemberId);
                if (phoneExists)
                {
                    return BadRequest(new { message = "该手机号已被其他成员使用" });
                }
            }

            // 获取当前用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            // 更新成员信息
            member.RealName = request.RealName?.Trim();
            member.PhoneNumber = string.IsNullOrEmpty(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
            member.UpdatedAt = DateTime.UtcNow;
            member.UpdatedBy = currentUserId;
            _ = await _context.SaveChangesAsync();

            _logger.LogInformation("管理员更新成员信息成功: 成员ID: {MemberId}, 真实姓名: {RealName}, 手机号: {PhoneNumber}",
                request.MemberId, request.RealName, request.PhoneNumber ?? "空");

            return Ok(new { message = "成员信息更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新成员信息失败: 成员ID: {MemberId}", request.MemberId);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 批量添加成员
    /// </summary>
    [HttpPost]
    [Route("Admin/Member/BatchAddMembers")]
    public async Task<IActionResult> BatchAddMembers([FromBody] BatchAddMembersRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "请求参数无效" });
            }

            // 获取当前用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            int addedCount = 0;
            int updatedCount = 0;
            int failureCount = 0;
            List<string> addedMembers = [];
            List<string> updatedMembers = [];
            List<string> errors = [];

            foreach (MemberEntry entry in request.MemberEntries)
            {
                try
                {
                    // 检查是否已存在相同真实姓名的成员
                    OrganizationMember? existingMember = await _context.OrganizationMembers
                        .FirstOrDefaultAsync(m => m.RealName == entry.RealName && m.OrganizationId == -1);

                    if (existingMember != null)
                    {
                        // 更新现有成员
                        if (request.OverwriteExisting)
                        {
                            existingMember.PhoneNumber = entry.PhoneNumber;
                            existingMember.UpdatedAt = DateTime.UtcNow;
                            existingMember.UpdatedBy = currentUserId;
                            updatedCount++;
                            updatedMembers.Add(entry.RealName);
                        }
                        else
                        {
                            errors.Add($"成员 {entry.RealName} 已存在");
                            failureCount++;
                        }
                    }
                    else
                    {
                        // 创建新成员
                        OrganizationMember newMember = new()
                        {
                            Username = entry.RealName, // 使用真实姓名作为用户名
                            RealName = entry.RealName,
                            PhoneNumber = entry.PhoneNumber,
                            OrganizationId = -1, // 非组织成员
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUserId,
                            UpdatedAt = DateTime.UtcNow,
                            UpdatedBy = currentUserId,
                            IsActive = true
                        };

                        _ = _context.OrganizationMembers.Add(newMember);
                        addedCount++;
                        addedMembers.Add(entry.RealName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理成员 {RealName} 时发生错误", entry.RealName);
                    errors.Add($"处理成员 {entry.RealName} 时发生错误: {ex.Message}");
                    failureCount++;
                }
            }

            _ = await _context.SaveChangesAsync();

            string message = $"批量处理完成，新增成员 {addedCount} 个，更新成员 {updatedCount} 个";
            if (failureCount > 0)
            {
                message += $"，失败 {failureCount} 个";
            }

            return Ok(new
            {
                message,
                addedCount,
                updatedCount,
                failureCount,
                addedMembers,
                updatedMembers,
                errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量添加成员失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 删除成员
    /// </summary>
    [HttpDelete]
    [Route("Admin/Member/DeleteMember/{memberId}")]
    public async Task<IActionResult> DeleteMember(int memberId)
    {
        try
        {
            // 查找成员
            OrganizationMember? member = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.Id == memberId && m.OrganizationId == null);

            if (member == null)
            {
                return NotFound(new { message = "成员不存在" });
            }

            // 获取当前用户ID用于日志记录
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            // 硬删除成员记录
            _context.OrganizationMembers.Remove(member);
            await _context.SaveChangesAsync();

            _logger.LogInformation("管理员删除成员成功: 成员ID: {MemberId}, 成员姓名: {RealName}, 操作者: {UserId}",
                memberId, member.RealName, currentUserId);

            return Ok(new { message = $"成员"{member.RealName}"删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除成员失败: 成员ID: {MemberId}", memberId);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取所有成员列表
    /// </summary>
    private async Task<List<MemberDto>> GetAllMembersAsync(bool includeInactive = false)
    {
        try
        {
            _logger.LogInformation("开始获取所有成员列表，包含非活跃成员: {IncludeInactive}", includeInactive);

            IQueryable<OrganizationMember> query = _context.OrganizationMembers
                .Include(m => m.Creator)
                .Where(m => m.OrganizationId == null); // 只获取非组织成员

            if (!includeInactive)
            {
                query = query.Where(m => m.IsActive);
            }

            List<OrganizationMember> members = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("从数据库获取到 {Count} 个成员记录", members.Count);

            List<MemberDto> result = members.Select(MapToMemberDto).ToList();

            _logger.LogInformation("成功映射 {Count} 个成员DTO", result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取成员列表失败");
            return [];
        }
    }

    /// <summary>
    /// 将 OrganizationMember 实体映射为 MemberDto
    /// </summary>
    private static MemberDto MapToMemberDto(OrganizationMember member)
    {
        return member == null
            ? throw new ArgumentNullException(nameof(member))
            : new MemberDto
            {
                Id = member.Id,
                RealName = member.RealName,
                PhoneNumber = member.PhoneNumber,
                JoinedAt = member.CreatedAt,
                IsActive = member.IsActive,
                UserId = member.UserId,
                Notes = member.Notes,
                CreatedByUsername = member.Creator?.Username,
                UpdatedAt = member.UpdatedAt
            };
    }
}


