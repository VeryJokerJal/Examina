using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Organization.Requests;
using ExaminaWebApplication.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 管理员网页端组织管理控制器
/// </summary>
[Authorize(Policy = "AdminPolicy")]
public class AdminOrganizationWebController : Controller
{
    private readonly IOrganizationService _organizationService;
    private readonly IInvitationCodeService _invitationCodeService;
    private readonly ILogger<AdminOrganizationWebController> _logger;
    private readonly ApplicationDbContext _context;

    public AdminOrganizationWebController(
        IOrganizationService organizationService,
        IInvitationCodeService invitationCodeService,
        ILogger<AdminOrganizationWebController> logger,
        ApplicationDbContext context)
    {
        _organizationService = organizationService;
        _invitationCodeService = invitationCodeService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 组织管理首页
    /// </summary>
    [HttpGet]
    [Route("Admin/Organization")]
    public async Task<IActionResult> Index()
    {
        try
        {
            List<OrganizationDto> organizations = await _organizationService.GetOrganizationsAsync(includeInactive: false);
            return View(organizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织列表时发生错误");
            TempData["ErrorMessage"] = "获取组织列表失败，请稍后重试";
            return View(new List<OrganizationDto>());
        }
    }

    /// <summary>
    /// 创建组织页面
    /// </summary>
    [HttpGet]
    [Route("Admin/Organization/Create")]
    public IActionResult Create()
    {
        return View(new CreateOrganizationRequest());
    }

    /// <summary>
    /// 创建组织
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrganizationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            // 获取当前管理员用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int adminUserId))
            {
                TempData["ErrorMessage"] = "无法获取用户信息";
                return View(request);
            }

            OrganizationDto organization = await _organizationService.CreateOrganizationAsync(request, adminUserId);

            _logger.LogInformation("管理员 {AdminUserId} 创建组织成功: {OrganizationName}", adminUserId, organization.Name);
            TempData["SuccessMessage"] = $"组织 '{organization.Name}' 创建成功";

            return RedirectToAction(nameof(Details), new { id = organization.Id });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("创建组织失败: {Message}", ex.Message);
            ModelState.AddModelError("", ex.Message);
            return View(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建组织时发生错误");
            ModelState.AddModelError("", "创建组织失败，请稍后重试");
            return View(request);
        }
    }

    /// <summary>
    /// 组织详情页面
    /// </summary>
    [HttpGet]
    [Route("Admin/Organization/Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            OrganizationDto? organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "组织不存在";
                return RedirectToAction(nameof(Index));
            }

            // 获取邀请码列表
            List<InvitationCode> invitationCodes = await _invitationCodeService.GetOrganizationInvitationCodesAsync(id, includeInactive: true);

            // 获取成员列表（从 OrganizationMember 表）
            List<OrganizationMemberDto> members = await GetOrganizationMembersFromTableAsync(id, includeInactive: false);

            // 添加调试日志
            _logger.LogInformation("组织 {OrganizationId} 的成员数量: {MemberCount}", id, members.Count);
            foreach (OrganizationMemberDto member in members)
            {
                _logger.LogInformation("成员: {Username}, 手机号: {Phone}", member.Username, member.PhoneNumber ?? "未设置");
            }

            OrganizationDetailsViewModel viewModel = new()
            {
                Organization = organization,
                InvitationCodes = invitationCodes.Select(ic => new InvitationCodeDto
                {
                    Id = ic.Id,
                    Code = ic.Code,
                    OrganizationId = ic.OrganizationId,
                    OrganizationName = organization.Name,
                    CreatedAt = ic.CreatedAt,
                    ExpiresAt = ic.ExpiresAt,
                    IsActive = ic.IsActive,
                    UsageCount = ic.UsageCount,
                    MaxUsage = ic.MaxUsage
                }).ToList(),
                Members = members ?? []
            };

            // 验证 ViewModel
            _logger.LogInformation("ViewModel 创建完成 - 成员数量: {MemberCount}, 邀请码数量: {InvitationCodeCount}",
                viewModel.Members.Count, viewModel.InvitationCodes.Count);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织详情时发生错误: {OrganizationId}", id);
            TempData["ErrorMessage"] = "获取组织详情失败，请稍后重试";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// 编辑组织页面
    /// </summary>
    [HttpGet]
    [Route("Admin/Organization/Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            OrganizationDto? organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "组织不存在";
                return RedirectToAction(nameof(Index));
            }

            UpdateOrganizationRequest request = new()
            {
                Name = organization.Name
            };

            ViewBag.OrganizationId = id;
            return View(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织编辑页面时发生错误: {OrganizationId}", id);
            TempData["ErrorMessage"] = "获取组织信息失败，请稍后重试";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// 更新组织信息
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateOrganizationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                ViewBag.OrganizationId = id;
                return View(request);
            }

            OrganizationDto? organization = await _organizationService.UpdateOrganizationAsync(id, request.Name);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "组织不存在";
                return RedirectToAction(nameof(Index));
            }

            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("管理员 {AdminUserId} 更新组织 {OrganizationId} 成功", userIdClaim, id);

            TempData["SuccessMessage"] = "组织信息更新成功";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("更新组织失败: {Message}", ex.Message);
            ModelState.AddModelError("", ex.Message);
            ViewBag.OrganizationId = id;
            return View(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新组织时发生错误: {OrganizationId}", id);
            ModelState.AddModelError("", "更新组织失败，请稍后重试");
            ViewBag.OrganizationId = id;
            return View(request);
        }
    }

    /// <summary>
    /// 生成邀请码
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/{id}/CreateInvitationCode")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInvitationCode(int id, CreateInvitationCodeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "请求参数无效";
                return RedirectToAction(nameof(Details), new { id });
            }

            // 检查组织是否存在
            OrganizationDto? organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "组织不存在";
                return RedirectToAction(nameof(Index));
            }

            // 创建邀请码
            InvitationCode invitationCode = await _invitationCodeService.CreateInvitationCodeAsync(
                id, request.ExpiresAt, request.MaxUsage);

            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("管理员 {AdminUserId} 为组织 {OrganizationId} 生成邀请码 {InvitationCode} 成功",
                userIdClaim, id, invitationCode.Code);

            TempData["SuccessMessage"] = $"邀请码 '{invitationCode.Code}' 生成成功";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成邀请码时发生错误: {OrganizationId}", id);
            TempData["ErrorMessage"] = "生成邀请码失败，请稍后重试";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    /// <summary>
    /// 停用邀请码
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/DeactivateInvitationCode/{invitationCodeId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateInvitationCode(int invitationCodeId, int organizationId)
    {
        try
        {
            bool success = await _invitationCodeService.DeactivateInvitationCodeAsync(invitationCodeId);
            if (!success)
            {
                TempData["ErrorMessage"] = "邀请码不存在";
            }
            else
            {
                string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("管理员 {AdminUserId} 停用邀请码 {InvitationCodeId} 成功", userIdClaim, invitationCodeId);
                TempData["SuccessMessage"] = "邀请码已停用";
            }

            return RedirectToAction(nameof(Details), new { id = organizationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用邀请码时发生错误: {InvitationCodeId}", invitationCodeId);
            TempData["ErrorMessage"] = "停用邀请码失败，请稍后重试";
            return RedirectToAction(nameof(Details), new { id = organizationId });
        }
    }

    /// <summary>
    /// 停用组织
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/Deactivate/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            bool success = await _organizationService.DeactivateOrganizationAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = "组织不存在";
            }
            else
            {
                string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("管理员 {AdminUserId} 停用组织 {OrganizationId} 成功", userIdClaim, id);
                TempData["SuccessMessage"] = "组织已停用";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用组织时发生错误: {OrganizationId}", id);
            TempData["ErrorMessage"] = "停用组织失败，请稍后重试";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    /// <summary>
    /// 更新成员手机号
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/UpdateMemberPhone")]
    public async Task<IActionResult> UpdateMemberPhone([FromBody] UpdateMemberPhoneRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "请求参数无效" });
            }

            // 查找组织成员（使用成员ID而不是用户ID）
            OrganizationMember? member = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.Id == request.StudentId);
            if (member == null)
            {
                return NotFound(new { message = "成员不存在" });
            }

            // 检查手机号是否已被同组织其他成员使用（如果不为空）
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                bool phoneExists = await _context.OrganizationMembers
                    .AnyAsync(m => m.PhoneNumber == request.PhoneNumber &&
                                  m.Id != request.StudentId &&
                                  m.OrganizationId == member.OrganizationId);
                if (phoneExists)
                {
                    return BadRequest(new { message = "该手机号已被同组织其他成员使用" });
                }
            }

            // 获取当前用户ID
            string? userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            // 更新手机号
            member.PhoneNumber = string.IsNullOrEmpty(request.PhoneNumber) ? null : request.PhoneNumber;
            member.UpdatedAt = DateTime.UtcNow;
            member.UpdatedBy = currentUserId;
            _ = await _context.SaveChangesAsync();

            _logger.LogInformation("管理员更新用户手机号成功: 用户ID: {UserId}, 新手机号: {PhoneNumber}",
                request.StudentId, request.PhoneNumber ?? "空");

            return Ok(new { message = "手机号更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户手机号失败: 用户ID: {UserId}", request.StudentId);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 更新成员信息（真实姓名和手机号）
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/UpdateMemberInfo")]
    public async Task<IActionResult> UpdateMemberInfo([FromBody] UpdateMemberInfoRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "请求参数无效" });
            }

            // 查找组织成员
            OrganizationMember? member = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.Id == request.MemberId);
            if (member == null)
            {
                return NotFound(new { message = "成员不存在" });
            }

            // 检查手机号是否已被同组织其他成员使用（如果不为空）
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                bool phoneExists = await _context.OrganizationMembers
                    .AnyAsync(m => m.PhoneNumber == request.PhoneNumber &&
                                  m.Id != request.MemberId &&
                                  m.OrganizationId == member.OrganizationId);
                if (phoneExists)
                {
                    return BadRequest(new { message = "该手机号已被同组织其他成员使用" });
                }
            }

            // 获取当前用户ID
            string? userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            // 更新成员信息
            member.RealName = request.RealName?.Trim();
            member.PhoneNumber = string.IsNullOrEmpty(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
            member.UpdatedAt = DateTime.UtcNow;
            member.UpdatedBy = currentUserId;
            await _context.SaveChangesAsync();

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
    /// 批量添加/更新组织成员信息
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/BatchUpdateMemberPhone")]
    public async Task<IActionResult> BatchUpdateMemberPhone([FromBody] BatchUpdateMemberPhoneRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "请求参数无效" });
            }

            // 获取当前用户ID
            string? userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            int addedCount = 0;
            int updatedCount = 0;
            int failureCount = 0;
            List<string> errors = [];
            List<string> addedMembers = [];
            List<string> updatedMembers = [];

            // 验证组织ID
            int organizationId = request.OrganizationId;
            if (organizationId <= 0)
            {
                return BadRequest(new { message = "组织ID无效" });
            }

            // 验证组织是否存在
            bool organizationExists = await _context.Organizations.AnyAsync(o => o.Id == organizationId);
            if (!organizationExists)
            {
                return BadRequest(new { message = "指定的组织不存在" });
            }

            foreach (PhoneEntry entry in request.PhoneEntries)
            {
                try
                {
                    // 检查该用户名在当前组织中是否已存在
                    OrganizationMember? existingMember = await _context.OrganizationMembers
                        .FirstOrDefaultAsync(m => m.Username == entry.Username && m.OrganizationId == organizationId);

                    if (existingMember != null)
                    {
                        // 更新现有成员信息
                        if (!request.OverwriteExisting && !string.IsNullOrEmpty(existingMember.PhoneNumber))
                        {
                            errors.Add($"成员 {entry.Username} 已有手机号，跳过");
                            failureCount++;
                            continue;
                        }

                        existingMember.PhoneNumber = entry.Phone;
                        existingMember.UpdatedAt = DateTime.UtcNow;
                        existingMember.UpdatedBy = currentUserId;

                        updatedCount++;
                        updatedMembers.Add(entry.Username);
                    }
                    else
                    {
                        // 检查手机号是否已被同组织其他成员使用
                        bool phoneExists = await _context.OrganizationMembers
                            .AnyAsync(m => m.PhoneNumber == entry.Phone && m.OrganizationId == organizationId);
                        if (phoneExists)
                        {
                            errors.Add($"手机号 {entry.Phone} 已被同组织其他成员使用");
                            failureCount++;
                            continue;
                        }

                        // 创建新成员记录
                        OrganizationMember newMember = new()
                        {
                            Username = entry.Username,
                            PhoneNumber = entry.Phone,
                            OrganizationId = organizationId,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUserId,
                            UpdatedAt = DateTime.UtcNow,
                            UpdatedBy = currentUserId,
                            IsActive = true
                        };

                        _ = _context.OrganizationMembers.Add(newMember);
                        addedCount++;
                        addedMembers.Add(entry.Username);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量处理成员信息时处理用户 {Username} 失败", entry.Username);
                    errors.Add($"处理成员 {entry.Username} 时发生错误");
                    failureCount++;
                }
            }

            // 保存所有更改
            if (addedCount > 0 || updatedCount > 0)
            {
                _ = await _context.SaveChangesAsync();
            }

            _logger.LogInformation("批量处理组织成员完成: 新增 {AddedCount}, 更新 {UpdatedCount}, 失败 {FailureCount}",
                addedCount, updatedCount, failureCount);

            string message = $"批量处理完成";
            if (addedCount > 0)
            {
                message += $"，新增成员 {addedCount} 个";
            }

            if (updatedCount > 0)
            {
                message += $"，更新成员 {updatedCount} 个";
            }

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
                addedMembers = addedMembers.Take(10).ToList(),
                updatedMembers = updatedMembers.Take(10).ToList(),
                errors = errors.Take(10).ToList() // 只返回前10个错误
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新用户手机号失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 调试方法：检查组织成员数据
    /// </summary>
    [HttpGet]
    [Route("Admin/Organization/TestMembers/{id}")]
    public async Task<IActionResult> TestMembers(int id)
    {
        try
        {
            // 直接查询数据库
            List<StudentOrganization> rawMembers = await _context.StudentOrganizations
                .Include(so => so.Student)
                .Include(so => so.Organization)
                .Include(so => so.InvitationCode)
                .Where(so => so.OrganizationId == id)
                .ToListAsync();

            var debugInfo = new
            {
                OrganizationId = id,
                TotalMembersInDb = rawMembers.Count,
                ActiveMembers = rawMembers.Count(m => m.IsActive),
                Members = rawMembers.Select(m => new
                {
                    m.Id,
                    m.StudentId,
                    StudentUsername = m.Student?.Username ?? "NULL",
                    StudentRealName = m.Student?.RealName ?? "NULL",
                    StudentPhoneNumber = m.Student?.PhoneNumber ?? "NULL",
                    m.IsActive,
                    m.JoinedAt
                }).ToList()
            };

            return Json(debugInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调试组织成员数据失败: {OrganizationId}", id);
            return Json(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 测试PreConfiguredUsers表访问
    /// </summary>
    [HttpGet]
    [Route("Admin/Organization/TestPreConfiguredTable")]
    public async Task<IActionResult> TestPreConfiguredTable()
    {
        try
        {
            int count = await _context.PreConfiguredUsers.CountAsync();
            return Json(new
            {
                success = true,
                message = "PreConfiguredUsers表访问正常",
                recordCount = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试PreConfiguredUsers表访问失败");
            return Json(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 从 OrganizationMember 表获取组织成员列表
    /// </summary>
    private async Task<List<OrganizationMemberDto>> GetOrganizationMembersFromTableAsync(int organizationId, bool includeInactive = false)
    {
        try
        {
            _logger.LogInformation("开始从 OrganizationMember 表获取组织 {OrganizationId} 的成员列表，包含非活跃成员: {IncludeInactive}", organizationId, includeInactive);

            IQueryable<OrganizationMember> query = _context.OrganizationMembers
                .Include(m => m.Organization)
                .Include(m => m.Creator)
                .Where(m => m.OrganizationId == organizationId);

            if (!includeInactive)
            {
                query = query.Where(m => m.IsActive);
            }

            List<OrganizationMember> organizationMembers = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("从数据库获取到 {Count} 个成员记录", organizationMembers.Count);

            List<OrganizationMemberDto> result = organizationMembers.Select(MapToOrganizationMemberDto).ToList();

            _logger.LogInformation("成功映射 {Count} 个成员DTO", result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织成员列表失败: 组织ID: {OrganizationId}", organizationId);
            return [];
        }
    }

    /// <summary>
    /// 将 OrganizationMember 实体映射为 DTO
    /// </summary>
    private static OrganizationMemberDto MapToOrganizationMemberDto(OrganizationMember member)
    {
        return member == null
            ? throw new ArgumentNullException(nameof(member))
            : new OrganizationMemberDto
            {
                Id = member.Id,
                Username = member.Username,
                PhoneNumber = member.PhoneNumber,
                OrganizationId = member.OrganizationId,
                OrganizationName = member.Organization?.Name ?? "未知",
                JoinedAt = member.CreatedAt, // 使用创建时间作为加入时间
                IsActive = member.IsActive,
                UserId = member.UserId,
                Notes = member.Notes,
                CreatedByUsername = member.Creator?.Username,
                UpdatedAt = member.UpdatedAt
            };
    }
}

/// <summary>
/// 组织详情视图模型
/// </summary>
public class OrganizationDetailsViewModel
{
    public OrganizationDto Organization { get; set; } = new();
    public List<InvitationCodeDto> InvitationCodes { get; set; } = [];
    public List<OrganizationMemberDto> Members { get; set; } = [];
}

/// <summary>
/// 更新组织请求模型
/// </summary>
public class UpdateOrganizationRequest
{
    /// <summary>
    /// 组织名称
    /// </summary>
    [Required(ErrorMessage = "组织名称不能为空")]
    [StringLength(100, ErrorMessage = "组织名称长度不能超过100个字符")]
    [Display(Name = "组织名称")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 更新成员手机号请求模型
/// </summary>
public class UpdateMemberPhoneRequest
{
    /// <summary>
    /// 学生用户ID
    /// </summary>
    [Required(ErrorMessage = "学生ID不能为空")]
    public int StudentId { get; set; }

    /// <summary>
    /// 手机号（可为空表示清除）
    /// </summary>
    [Phone(ErrorMessage = "手机号格式不正确")]
    [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// 批量添加/更新组织成员信息请求模型
/// </summary>
public class BatchUpdateMemberPhoneRequest
{
    /// <summary>
    /// 组织ID
    /// </summary>
    [Required(ErrorMessage = "组织ID不能为空")]
    public int OrganizationId { get; set; }

    /// <summary>
    /// 成员信息条目列表
    /// </summary>
    [Required(ErrorMessage = "成员信息条目不能为空")]
    public List<PhoneEntry> PhoneEntries { get; set; } = [];

    /// <summary>
    /// 是否覆盖现有信息
    /// </summary>
    public bool OverwriteExisting { get; set; }
}

/// <summary>
/// 成员信息条目
/// </summary>
public class PhoneEntry
{
    /// <summary>
    /// 成员用户名
    /// </summary>
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 成员手机号
    /// </summary>
    [Required(ErrorMessage = "手机号不能为空")]
    [Phone(ErrorMessage = "手机号格式不正确")]
    public string Phone { get; set; } = string.Empty;
}


