using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Organization.Requests;

namespace ExaminaWebApplication.Services.Organization;

/// <summary>
/// 组织管理服务实现
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly ApplicationDbContext _context;
    private readonly IInvitationCodeService _invitationCodeService;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        ApplicationDbContext context,
        IInvitationCodeService invitationCodeService,
        ILogger<OrganizationService> logger)
    {
        _context = context;
        _invitationCodeService = invitationCodeService;
        _logger = logger;
    }

    /// <summary>
    /// 创建组织
    /// </summary>
    public async Task<OrganizationDto> CreateOrganizationAsync(CreateOrganizationRequest request, int creatorUserId)
    {
        try
        {
            // 检查组织名称是否已存在
            bool nameExists = await _context.Organizations
                .AnyAsync(o => o.Name == request.Name && o.IsActive);

            if (nameExists)
            {
                throw new InvalidOperationException($"组织名称 '{request.Name}' 已存在");
            }

            // 创建组织
            Models.Organization.Organization organization = new()
            {
                Name = request.Name,
                Type = request.Type,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = creatorUserId,
                IsActive = true
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // 如果需要自动生成邀请码
            if (request.GenerateInvitationCode)
            {
                await _invitationCodeService.CreateInvitationCodeAsync(
                    organization.Id,
                    request.InvitationCodeExpiresAt,
                    request.InvitationCodeMaxUsage);
            }

            _logger.LogInformation("创建组织成功: {OrganizationName}, ID: {OrganizationId}, 创建者: {CreatorUserId}",
                organization.Name, organization.Id, creatorUserId);

            return await GetOrganizationByIdAsync(organization.Id) ?? throw new InvalidOperationException("创建组织后无法获取组织信息");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建组织失败: {OrganizationName}, 创建者: {CreatorUserId}", request.Name, creatorUserId);
            throw;
        }
    }

    /// <summary>
    /// 获取组织列表
    /// </summary>
    public async Task<List<OrganizationDto>> GetOrganizationsAsync(bool includeInactive = false)
    {
        try
        {
            IQueryable<Models.Organization.Organization> query = _context.Organizations
                .Include(o => o.Creator)
                .Include(o => o.InvitationCodes)
                .Include(o => o.StudentOrganizations.Where(so => so.IsActive));

            if (!includeInactive)
            {
                query = query.Where(o => o.IsActive);
            }

            List<Models.Organization.Organization> organizations = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return organizations.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织列表失败");
            return new List<OrganizationDto>();
        }
    }

    /// <summary>
    /// 根据ID获取组织详情
    /// </summary>
    public async Task<OrganizationDto?> GetOrganizationByIdAsync(int organizationId)
    {
        try
        {
            Models.Organization.Organization? organization = await _context.Organizations
                .Include(o => o.Creator)
                .Include(o => o.InvitationCodes)
                .Include(o => o.StudentOrganizations.Where(so => so.IsActive))
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            return organization == null ? null : MapToDto(organization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织详情失败: {OrganizationId}", organizationId);
            return null;
        }
    }

    /// <summary>
    /// 更新组织信息
    /// </summary>
    public async Task<OrganizationDto?> UpdateOrganizationAsync(int organizationId, string name, string? description = null)
    {
        try
        {
            Models.Organization.Organization? organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                return null;
            }

            // 检查新名称是否与其他组织重复
            if (organization.Name != name)
            {
                bool nameExists = await _context.Organizations
                    .AnyAsync(o => o.Name == name && o.IsActive && o.Id != organizationId);

                if (nameExists)
                {
                    throw new InvalidOperationException($"组织名称 '{name}' 已存在");
                }
            }

            organization.Name = name;
            organization.Description = description;

            await _context.SaveChangesAsync();

            _logger.LogInformation("更新组织信息成功: {OrganizationId}, 新名称: {Name}", organizationId, name);

            return await GetOrganizationByIdAsync(organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新组织信息失败: {OrganizationId}", organizationId);
            throw;
        }
    }

    /// <summary>
    /// 停用组织
    /// </summary>
    public async Task<bool> DeactivateOrganizationAsync(int organizationId)
    {
        try
        {
            Models.Organization.Organization? organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                return false;
            }

            organization.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("停用组织成功: {OrganizationId}, 名称: {Name}", organizationId, organization.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用组织失败: {OrganizationId}", organizationId);
            return false;
        }
    }

    /// <summary>
    /// 学生加入组织
    /// </summary>
    public async Task<JoinOrganizationResult> JoinOrganizationAsync(int studentUserId, string invitationCode)
    {
        try
        {
            // 验证邀请码
            InvitationCode? invitation = await _invitationCodeService.ValidateInvitationCodeAsync(invitationCode);
            if (invitation == null)
            {
                return JoinOrganizationResult.CreateFailure("邀请码不存在");
            }

            // 检查邀请码是否可用
            if (!_invitationCodeService.IsInvitationCodeAvailable(invitation))
            {
                string reason = GetInvitationCodeUnavailableReason(invitation);
                return JoinOrganizationResult.CreateFailure(reason);
            }

            // 检查学生是否已在组织中
            bool alreadyInOrganization = await IsStudentInOrganizationAsync(studentUserId, invitation.OrganizationId);
            if (alreadyInOrganization)
            {
                return JoinOrganizationResult.CreateFailure("您已经是该组织的成员");
            }

            // 创建学生组织关系
            StudentOrganization studentOrganization = new()
            {
                StudentId = studentUserId,
                OrganizationId = invitation.OrganizationId,
                JoinedAt = DateTime.UtcNow,
                InvitationCodeId = invitation.Id,
                IsActive = true
            };

            _context.StudentOrganizations.Add(studentOrganization);

            // 增加邀请码使用次数
            await _invitationCodeService.IncrementUsageCountAsync(invitation.Id);

            await _context.SaveChangesAsync();

            _logger.LogInformation("学生加入组织成功: 学生ID: {StudentId}, 组织ID: {OrganizationId}, 邀请码: {InvitationCode}",
                studentUserId, invitation.OrganizationId, invitationCode);

            // 获取完整的学生组织关系信息
            StudentOrganizationDto? dto = await GetStudentOrganizationDtoAsync(studentOrganization.Id);
            return dto == null
                ? JoinOrganizationResult.CreateFailure("加入成功但无法获取详细信息")
                : JoinOrganizationResult.CreateSuccess(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "学生加入组织失败: 学生ID: {StudentId}, 邀请码: {InvitationCode}", studentUserId, invitationCode);
            return JoinOrganizationResult.CreateFailure("系统错误，请稍后重试");
        }
    }

    /// <summary>
    /// 学生退出组织
    /// </summary>
    public async Task<bool> LeaveOrganizationAsync(int studentUserId, int organizationId)
    {
        try
        {
            StudentOrganization? studentOrganization = await _context.StudentOrganizations
                .FirstOrDefaultAsync(so => so.StudentId == studentUserId && so.OrganizationId == organizationId && so.IsActive);

            if (studentOrganization == null)
            {
                return false;
            }

            studentOrganization.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("学生退出组织成功: 学生ID: {StudentId}, 组织ID: {OrganizationId}", studentUserId, organizationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "学生退出组织失败: 学生ID: {StudentId}, 组织ID: {OrganizationId}", studentUserId, organizationId);
            return false;
        }
    }

    /// <summary>
    /// 获取学生已加入的组织列表
    /// </summary>
    public async Task<List<StudentOrganizationDto>> GetStudentOrganizationsAsync(int studentUserId)
    {
        try
        {
            List<StudentOrganization> studentOrganizations = await _context.StudentOrganizations
                .Include(so => so.Student)
                .Include(so => so.Organization)
                .Include(so => so.InvitationCode)
                .Where(so => so.StudentId == studentUserId && so.IsActive)
                .OrderByDescending(so => so.JoinedAt)
                .ToListAsync();

            return studentOrganizations.Select(MapToStudentOrganizationDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生组织列表失败: 学生ID: {StudentId}", studentUserId);
            return new List<StudentOrganizationDto>();
        }
    }

    /// <summary>
    /// 获取组织的学生列表
    /// </summary>
    public async Task<List<StudentOrganizationDto>> GetOrganizationStudentsAsync(int organizationId, bool includeInactive = false)
    {
        try
        {
            IQueryable<StudentOrganization> query = _context.StudentOrganizations
                .Include(so => so.Student)
                .Include(so => so.Organization)
                .Include(so => so.InvitationCode)
                .Where(so => so.OrganizationId == organizationId);

            if (!includeInactive)
            {
                query = query.Where(so => so.IsActive);
            }

            List<StudentOrganization> studentOrganizations = await query
                .OrderByDescending(so => so.JoinedAt)
                .ToListAsync();

            return studentOrganizations.Select(MapToStudentOrganizationDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织学生列表失败: 组织ID: {OrganizationId}", organizationId);
            return new List<StudentOrganizationDto>();
        }
    }

    /// <summary>
    /// 检查学生是否已在组织中
    /// </summary>
    public async Task<bool> IsStudentInOrganizationAsync(int studentUserId, int organizationId)
    {
        try
        {
            return await _context.StudentOrganizations
                .AnyAsync(so => so.StudentId == studentUserId && so.OrganizationId == organizationId && so.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查学生组织关系失败: 学生ID: {StudentId}, 组织ID: {OrganizationId}", studentUserId, organizationId);
            return false;
        }
    }

    /// <summary>
    /// 获取邀请码不可用的原因
    /// </summary>
    private static string GetInvitationCodeUnavailableReason(InvitationCode invitation)
    {
        if (!invitation.IsActive)
        {
            return "邀请码已被停用";
        }

        if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value <= DateTime.UtcNow)
        {
            return "邀请码已过期";
        }

        if (invitation.MaxUsage.HasValue && invitation.UsageCount >= invitation.MaxUsage.Value)
        {
            return "邀请码使用次数已达上限";
        }

        return "邀请码不可用";
    }

    /// <summary>
    /// 获取学生组织关系DTO
    /// </summary>
    private async Task<StudentOrganizationDto?> GetStudentOrganizationDtoAsync(int studentOrganizationId)
    {
        try
        {
            StudentOrganization? studentOrganization = await _context.StudentOrganizations
                .Include(so => so.Student)
                .Include(so => so.Organization)
                .Include(so => so.InvitationCode)
                .FirstOrDefaultAsync(so => so.Id == studentOrganizationId);

            return studentOrganization == null ? null : MapToStudentOrganizationDto(studentOrganization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生组织关系DTO失败: {StudentOrganizationId}", studentOrganizationId);
            return null;
        }
    }

    /// <summary>
    /// 将组织实体映射为DTO
    /// </summary>
    private static OrganizationDto MapToDto(Models.Organization.Organization organization)
    {
        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Type = organization.Type,
            Description = organization.Description,
            CreatedAt = organization.CreatedAt,
            CreatorUsername = organization.Creator?.Username ?? "未知",
            IsActive = organization.IsActive,
            StudentCount = organization.StudentOrganizations?.Count(so => so.IsActive) ?? 0,
            InvitationCodeCount = organization.InvitationCodes?.Count(ic => ic.IsActive) ?? 0
        };
    }

    /// <summary>
    /// 将学生组织关系实体映射为DTO
    /// </summary>
    private static StudentOrganizationDto MapToStudentOrganizationDto(StudentOrganization studentOrganization)
    {
        return new StudentOrganizationDto
        {
            Id = studentOrganization.Id,
            StudentId = studentOrganization.StudentId,
            StudentUsername = studentOrganization.Student?.Username ?? "未知",
            StudentRealName = studentOrganization.Student?.RealName,
            StudentId_Number = studentOrganization.Student?.StudentId,
            OrganizationId = studentOrganization.OrganizationId,
            OrganizationName = studentOrganization.Organization?.Name ?? "未知",
            OrganizationType = studentOrganization.Organization?.Type ?? OrganizationType.School,
            JoinedAt = studentOrganization.JoinedAt,
            InvitationCode = studentOrganization.InvitationCode?.Code ?? "未知",
            IsActive = studentOrganization.IsActive
        };
    }
}
