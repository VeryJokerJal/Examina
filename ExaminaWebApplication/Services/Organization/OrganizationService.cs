using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Organization.Requests;
using Microsoft.EntityFrameworkCore;

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
            // 验证创建者用户是否存在且有效
            User? creator = await _context.Users.FirstOrDefaultAsync(u => u.Id == creatorUserId && u.IsActive);
            if (creator == null)
            {
                throw new InvalidOperationException("创建者用户不存在或已被禁用");
            }

            // 检查组织名称是否已存在
            bool nameExists = await _context.Organizations
                .AnyAsync(o => o.Name == request.Name && o.IsActive);

            if (nameExists)
            {
                throw new InvalidOperationException($"组织名称 '{request.Name}' 已存在");
            }

            // 创建组织（显式设置 Creator 导航；CreatedBy 作为外键由模型配置映射）
            Models.Organization.Organization organization = new()
            {
                Name = request.Name,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = creatorUserId,
                Creator = creator,
                IsActive = true
            };

            _ = _context.Organizations.Add(organization);
            _ = await _context.SaveChangesAsync();

            // 如果需要自动生成邀请码
            if (request.GenerateInvitationCode)
            {
                _ = await _invitationCodeService.CreateInvitationCodeAsync(
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
            return [];
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
    public async Task<OrganizationDto?> UpdateOrganizationAsync(int organizationId, string name)
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

            _ = await _context.SaveChangesAsync();

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
            _ = await _context.SaveChangesAsync();

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
    /// 用户加入组织（支持学生和教师）
    /// </summary>
    public async Task<JoinOrganizationResult> JoinOrganizationAsync(int userId, UserRole userRole, string invitationCode)
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

            // 所有用户都可以加入组织（移除类型限制）

            // 检查用户是否已在组织中
            bool alreadyInOrganization = await IsUserInOrganizationAsync(userId, invitation.OrganizationId);
            if (alreadyInOrganization)
            {
                return JoinOrganizationResult.CreateFailure("您已经是该组织的成员");
            }

            // 验证用户信息完整性
            User? user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("用户不存在: {UserId}", userId);
                return JoinOrganizationResult.CreateFailure("用户不存在");
            }

            // 检查用户是否提供了完整的个人信息
            if (string.IsNullOrWhiteSpace(user.RealName))
            {
                _logger.LogWarning("用户 {UserId} 缺少真实姓名，无法加入组织", userId);
                return JoinOrganizationResult.CreateFailure("请先完善个人信息（真实姓名）后再加入组织");
            }

            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                _logger.LogWarning("用户 {UserId} 缺少手机号码，无法加入组织", userId);
                return JoinOrganizationResult.CreateFailure("请先完善个人信息（手机号码）后再加入组织");
            }

            _logger.LogInformation("用户 {UserId} 信息完整性验证通过，真实姓名: {RealName}, 手机号: {PhoneNumber}",
                userId, user.RealName, user.PhoneNumber);

            // 创建用户组织关系
            StudentOrganization userOrganization = new()
            {
                StudentId = userId,
                OrganizationId = invitation.OrganizationId,
                JoinedAt = DateTime.UtcNow,
                InvitationCodeId = invitation.Id,
                IsActive = true
            };

            _ = _context.StudentOrganizations.Add(userOrganization);

            // 增加邀请码使用次数
            _ = await _invitationCodeService.IncrementUsageCountAsync(invitation.Id);

            // 检查并应用预配置信息
            await ApplyPreConfiguredUserInfo(userId, invitation.OrganizationId);

            _ = await _context.SaveChangesAsync();

            string userTypeText = userRole == UserRole.Teacher ? "教师" : "学生";
            _logger.LogInformation("{UserType}加入组织成功: 用户ID: {UserId}, 组织ID: {OrganizationId}, 邀请码: {InvitationCode}",
                userTypeText, userId, invitation.OrganizationId, invitationCode);

            // 获取完整的用户组织关系信息
            StudentOrganizationDto? dto = await GetUserOrganizationDtoAsync(userOrganization.Id);
            return dto == null
                ? JoinOrganizationResult.CreateFailure("加入成功但无法获取详细信息")
                : JoinOrganizationResult.CreateSuccess(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户加入组织失败: 用户ID: {UserId}, 邀请码: {InvitationCode}", userId, invitationCode);
            return JoinOrganizationResult.CreateFailure("系统错误，请稍后重试");
        }
    }

    /// <summary>
    /// 用户退出组织
    /// </summary>
    public async Task<bool> LeaveOrganizationAsync(int userId, int organizationId)
    {
        try
        {
            StudentOrganization? userOrganization = await _context.StudentOrganizations
                .FirstOrDefaultAsync(so => so.StudentId == userId && so.OrganizationId == organizationId && so.IsActive);

            if (userOrganization == null)
            {
                return false;
            }

            userOrganization.IsActive = false;
            _ = await _context.SaveChangesAsync();

            _logger.LogInformation("用户退出组织成功: 用户ID: {UserId}, 组织ID: {OrganizationId}", userId, organizationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户退出组织失败: 用户ID: {UserId}, 组织ID: {OrganizationId}", userId, organizationId);
            return false;
        }
    }

    /// <summary>
    /// 移除组织成员（软删除）
    /// </summary>
    public async Task<bool> RemoveOrganizationMemberAsync(int membershipId, int operatorUserId)
    {
        try
        {
            StudentOrganization? membership = await _context.StudentOrganizations
                .FirstOrDefaultAsync(so => so.Id == membershipId && so.IsActive);

            if (membership == null)
            {
                _logger.LogWarning("成员关系不存在或已移除: {MembershipId}", membershipId);
                return false;
            }

            membership.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("组织成员移除成功: {MembershipId}, 操作者: {OperatorUserId}",
                membershipId, operatorUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除组织成员失败: {MembershipId}", membershipId);
            return false;
        }
    }

    /// <summary>
    /// 恢复组织成员
    /// </summary>
    public async Task<bool> RestoreOrganizationMemberAsync(int membershipId, int operatorUserId)
    {
        try
        {
            StudentOrganization? membership = await _context.StudentOrganizations
                .FirstOrDefaultAsync(so => so.Id == membershipId && !so.IsActive);

            if (membership == null)
            {
                _logger.LogWarning("成员关系不存在或已激活: {MembershipId}", membershipId);
                return false;
            }

            membership.IsActive = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("组织成员恢复成功: {MembershipId}, 操作者: {OperatorUserId}",
                membershipId, operatorUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复组织成员失败: {MembershipId}", membershipId);
            return false;
        }
    }

    /// <summary>
    /// 通过邀请码ID加入组织
    /// </summary>
    public async Task<StudentOrganizationDto?> JoinOrganizationAsync(int userId, int organizationId, int invitationCodeId)
    {
        try
        {
            // 验证用户
            User? user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
            {
                _logger.LogWarning("用户不存在或已停用: {UserId}", userId);
                return null;
            }

            // 验证用户信息完整性
            if (string.IsNullOrWhiteSpace(user.RealName))
            {
                _logger.LogWarning("用户 {UserId} 缺少真实姓名，无法加入组织", userId);
                return null;
            }

            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                _logger.LogWarning("用户 {UserId} 缺少手机号码，无法加入组织", userId);
                return null;
            }

            _logger.LogInformation("用户 {UserId} 信息完整性验证通过，真实姓名: {RealName}, 手机号: {PhoneNumber}",
                userId, user.RealName, user.PhoneNumber);

            // 验证组织
            Models.Organization.Organization? organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId && o.IsActive);
            if (organization == null)
            {
                _logger.LogWarning("组织不存在或已停用: {OrganizationId}", organizationId);
                return null;
            }

            // 验证邀请码
            InvitationCode? invitationCode = await _context.InvitationCodes
                .FirstOrDefaultAsync(ic => ic.Id == invitationCodeId && ic.IsActive);
            if (invitationCode == null)
            {
                _logger.LogWarning("邀请码不存在或已停用: {InvitationCodeId}", invitationCodeId);
                return null;
            }

            // 检查用户是否已在组织中
            bool alreadyInOrganization = await _context.StudentOrganizations
                .AnyAsync(so => so.StudentId == userId && so.OrganizationId == organizationId && so.IsActive);
            if (alreadyInOrganization)
            {
                _logger.LogWarning("用户已在组织中: {UserId}, {OrganizationId}", userId, organizationId);
                return null;
            }

            // 创建用户组织关系
            StudentOrganization userOrganization = new()
            {
                StudentId = userId,
                OrganizationId = organizationId,
                JoinedAt = DateTime.UtcNow,
                InvitationCodeId = invitationCodeId,
                IsActive = true
            };

            _context.StudentOrganizations.Add(userOrganization);

            // 增加邀请码使用次数
            await _invitationCodeService.IncrementUsageCountAsync(invitationCodeId);

            await _context.SaveChangesAsync();

            _logger.LogInformation("用户加入组织成功: {UserId} -> {OrganizationId}, 邀请码: {InvitationCodeId}",
                userId, organizationId, invitationCodeId);

            // 获取完整的用户组织关系信息
            return await GetUserOrganizationDtoAsync(userOrganization.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户加入组织失败: {UserId} -> {OrganizationId}", userId, organizationId);
            return null;
        }
    }

    /// <summary>
    /// 获取用户已加入的组织列表
    /// </summary>
    public async Task<List<StudentOrganizationDto>> GetUserOrganizationsAsync(int userId)
    {
        try
        {
            List<StudentOrganization> userOrganizations = await _context.StudentOrganizations
                .Include(so => so.Student)
                .Include(so => so.Organization)
                .Include(so => so.InvitationCode)
                .Where(so => so.StudentId == userId && so.IsActive)
                .OrderByDescending(so => so.JoinedAt)
                .ToListAsync();

            return userOrganizations.Select(MapToStudentOrganizationDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户组织列表失败: 用户ID: {UserId}", userId);
            return [];
        }
    }

    /// <summary>
    /// 获取组织的成员列表
    /// </summary>
    public async Task<List<StudentOrganizationDto>> GetOrganizationMembersAsync(int organizationId, bool includeInactive = false)
    {
        try
        {
            _logger.LogInformation("开始获取组织 {OrganizationId} 的成员列表，包含非活跃成员: {IncludeInactive}", organizationId, includeInactive);

            List<StudentOrganizationDto> result = new List<StudentOrganizationDto>();
            int nonOrgStudentCount = 0;

            // 1. 获取注册用户学生
            IQueryable<StudentOrganization> userQuery = _context.StudentOrganizations
                .Include(so => so.Student)
                .Include(so => so.Organization)
                .Include(so => so.InvitationCode)
                .Where(so => so.OrganizationId == organizationId);

            if (!includeInactive)
            {
                userQuery = userQuery.Where(so => so.IsActive);
            }

            List<StudentOrganization> userOrganizations = await userQuery
                .OrderByDescending(so => so.JoinedAt)
                .ToListAsync();

            result.AddRange(userOrganizations.Select(MapToStudentOrganizationDto));

            // 2. 获取非组织学生（如果关联表存在）
            try
            {
                IQueryable<NonOrganizationStudentOrganization> nonOrgQuery = _context.NonOrganizationStudentOrganizations
                    .Include(noso => noso.NonOrganizationStudent)
                    .Include(noso => noso.Organization)
                    .Include(noso => noso.Creator)
                    .Where(noso => noso.OrganizationId == organizationId);

                if (!includeInactive)
                {
                    nonOrgQuery = nonOrgQuery.Where(noso => noso.IsActive);
                }

                List<NonOrganizationStudentOrganization> nonOrgRelations = await nonOrgQuery
                    .OrderByDescending(noso => noso.JoinedAt)
                    .ToListAsync();

                result.AddRange(nonOrgRelations.Select(MapNonOrgStudentToDto));
                nonOrgStudentCount = nonOrgRelations.Count;

                _logger.LogInformation("从NonOrganizationStudentOrganization表获取到 {Count} 个非组织学生", nonOrgStudentCount);
            }
            catch (Exception ex)
            {
                // 如果关联表不存在，记录警告并尝试备用方案
                _logger.LogWarning(ex, "查询非组织学生关联表时出错，可能表尚未创建。组织ID: {OrganizationId}。尝试备用查询方案...", organizationId);

                // 备用方案：查询所有非组织学生（临时解决方案）
                // 注意：这不是最佳方案，但可以在迁移前临时使用
                try
                {
                    List<NonOrganizationStudent> allNonOrgStudents = await _context.NonOrganizationStudents
                        .Include(s => s.Creator)
                        .Where(s => s.IsActive)
                        .OrderByDescending(s => s.CreatedAt)
                        .ToListAsync();

                    // 将非组织学生转换为StudentOrganizationDto
                    foreach (NonOrganizationStudent student in allNonOrgStudents)
                    {
                        result.Add(new StudentOrganizationDto
                        {
                            Id = student.Id,
                            StudentId = 0,
                            StudentUsername = student.RealName,
                            StudentRealName = student.RealName,
                            StudentPhoneNumber = student.PhoneNumber,
                            OrganizationId = organizationId,
                            OrganizationName = "",
                            JoinedAt = student.CreatedAt,
                            InvitationCode = "",
                            IsActive = student.IsActive,
                            CreatedAt = student.CreatedAt,
                            CreatorUsername = student.Creator?.Username ?? "未知"
                        });
                    }

                    nonOrgStudentCount = allNonOrgStudents.Count;
                    _logger.LogInformation("使用备用方案获取到 {Count} 个非组织学生", nonOrgStudentCount);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "备用查询方案也失败了。组织ID: {OrganizationId}", organizationId);
                }
            }

            // 按加入时间排序
            result = result.OrderByDescending(dto => dto.JoinedAt).ToList();

            _logger.LogInformation("从数据库获取到 {UserCount} 个注册学生和 {NonOrgCount} 个非组织学生，总计 {TotalCount} 个成员",
                userOrganizations.Count, nonOrgStudentCount, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织成员列表失败: 组织ID: {OrganizationId}", organizationId);
            return [];
        }
    }

    /// <summary>
    /// 检查用户是否已在组织中
    /// </summary>
    public async Task<bool> IsUserInOrganizationAsync(int userId, int organizationId)
    {
        try
        {
            return await _context.StudentOrganizations
                .AnyAsync(so => so.StudentId == userId && so.OrganizationId == organizationId && so.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用户组织关系失败: 用户ID: {UserId}, 组织ID: {OrganizationId}", userId, organizationId);
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

        return invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value <= DateTime.UtcNow
            ? "邀请码已过期"
            : invitation.MaxUsage.HasValue && invitation.UsageCount >= invitation.MaxUsage.Value ? "邀请码使用次数已达上限" : "邀请码不可用";
    }

    /// <summary>
    /// 获取用户组织关系DTO
    /// </summary>
    private async Task<StudentOrganizationDto?> GetUserOrganizationDtoAsync(int userOrganizationId)
    {
        try
        {
            StudentOrganization? userOrganization = await _context.StudentOrganizations
                .Include(so => so.Student)
                .Include(so => so.Organization)
                .Include(so => so.InvitationCode)
                .FirstOrDefaultAsync(so => so.Id == userOrganizationId);

            return userOrganization == null ? null : MapToStudentOrganizationDto(userOrganization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户组织关系DTO失败: {UserOrganizationId}", userOrganizationId);
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
            ParentOrganizationId = organization.ParentOrganizationId,
            ParentOrganizationName = organization.ParentOrganization?.Name,
            CreatedAt = organization.CreatedAt,
            CreatorUsername = organization.Creator?.Username ?? "未知",
            IsActive = organization.IsActive,
            StudentCount = organization.StudentOrganizations?.Count(so => so.IsActive) ?? 0,
            InvitationCodeCount = organization.InvitationCodes?.Count(ic => ic.IsActive) ?? 0,
            ChildOrganizationCount = organization.ChildOrganizations?.Count(co => co.IsActive) ?? 0
        };
    }

    /// <summary>
    /// 创建学校
    /// </summary>
    public async Task<OrganizationDto> CreateSchoolAsync(string name, int creatorUserId)
    {
        try
        {
            User? creator = await _context.Users.FindAsync(creatorUserId);
            if (creator == null)
            {
                throw new ArgumentException("创建者用户不存在", nameof(creatorUserId));
            }

            Models.Organization.Organization school = new()
            {
                Name = name,
                Type = OrganizationType.School,
                ParentOrganizationId = null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = creatorUserId,
                Creator = creator,
                IsActive = true
            };

            _ = _context.Organizations.Add(school);
            _ = await _context.SaveChangesAsync();

            _logger.LogInformation("创建学校成功: {SchoolName}, ID: {SchoolId}, 创建者: {CreatorUserId}",
                school.Name, school.Id, creatorUserId);

            return MapToDto(school);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建学校失败: {SchoolName}, 创建者: {CreatorUserId}", name, creatorUserId);
            throw;
        }
    }

    /// <summary>
    /// 创建班级
    /// </summary>
    public async Task<OrganizationDto> CreateClassAsync(string name, int schoolId, int creatorUserId, bool generateInvitationCode = true)
    {
        try
        {
            User? creator = await _context.Users.FindAsync(creatorUserId);
            if (creator == null)
            {
                throw new ArgumentException("创建者用户不存在", nameof(creatorUserId));
            }

            Models.Organization.Organization? school = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == schoolId && o.Type == OrganizationType.School && o.IsActive);
            if (school == null)
            {
                throw new ArgumentException("学校不存在或已停用", nameof(schoolId));
            }

            Models.Organization.Organization classOrg = new()
            {
                Name = name,
                Type = OrganizationType.Class,
                ParentOrganizationId = schoolId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = creatorUserId,
                Creator = creator,
                IsActive = true
            };

            _ = _context.Organizations.Add(classOrg);
            _ = await _context.SaveChangesAsync();

            // 如果需要自动生成邀请码
            if (generateInvitationCode)
            {
                _ = await _invitationCodeService.CreateInvitationCodeAsync(classOrg.Id);
            }

            _logger.LogInformation("创建班级成功: {ClassName}, ID: {ClassId}, 学校ID: {SchoolId}, 创建者: {CreatorUserId}",
                classOrg.Name, classOrg.Id, schoolId, creatorUserId);

            return MapToDto(classOrg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建班级失败: {ClassName}, 学校ID: {SchoolId}, 创建者: {CreatorUserId}", name, schoolId, creatorUserId);
            throw;
        }
    }

    /// <summary>
    /// 获取学校列表
    /// </summary>
    public async Task<List<OrganizationDto>> GetSchoolsAsync(bool includeInactive = false)
    {
        try
        {
            IQueryable<Models.Organization.Organization> query = _context.Organizations
                .Include(o => o.Creator)
                .Include(o => o.ChildOrganizations.Where(c => c.IsActive))
                .Where(o => o.Type == OrganizationType.School);

            if (!includeInactive)
            {
                query = query.Where(o => o.IsActive);
            }

            List<Models.Organization.Organization> schools = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return schools.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学校列表失败");
            return [];
        }
    }

    /// <summary>
    /// 获取学校下的班级列表
    /// </summary>
    public async Task<List<OrganizationDto>> GetClassesBySchoolAsync(int schoolId, bool includeInactive = false)
    {
        try
        {
            IQueryable<Models.Organization.Organization> query = _context.Organizations
                .Include(o => o.Creator)
                .Include(o => o.ParentOrganization)
                .Include(o => o.InvitationCodes)
                .Include(o => o.StudentOrganizations.Where(so => so.IsActive))
                .Where(o => o.Type == OrganizationType.Class && o.ParentOrganizationId == schoolId);

            if (!includeInactive)
            {
                query = query.Where(o => o.IsActive);
            }

            List<Models.Organization.Organization> classes = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return classes.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学校班级列表失败: {SchoolId}", schoolId);
            return [];
        }
    }

    /// <summary>
    /// 获取所有班级列表
    /// </summary>
    public async Task<List<OrganizationDto>> GetClassesAsync(bool includeInactive = false)
    {
        try
        {
            IQueryable<Models.Organization.Organization> query = _context.Organizations
                .Include(o => o.Creator)
                .Include(o => o.ParentOrganization)
                .Include(o => o.InvitationCodes)
                .Include(o => o.StudentOrganizations.Where(so => so.IsActive))
                .Where(o => o.Type == OrganizationType.Class);

            if (!includeInactive)
            {
                query = query.Where(o => o.IsActive);
            }

            List<Models.Organization.Organization> classes = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return classes.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取班级列表失败");
            return [];
        }
    }

    /// <summary>
    /// 将学生组织关系实体映射为DTO
    /// </summary>
    private static StudentOrganizationDto MapToStudentOrganizationDto(StudentOrganization studentOrganization)
    {
        return studentOrganization == null
            ? throw new ArgumentNullException(nameof(studentOrganization))
            : new StudentOrganizationDto
            {
                Id = studentOrganization.Id,
                StudentId = studentOrganization.StudentId,
                StudentUsername = studentOrganization.Student?.Username ?? "未知",
                StudentRealName = studentOrganization.Student?.RealName,
                StudentPhoneNumber = studentOrganization.Student?.PhoneNumber,
                OrganizationId = studentOrganization.OrganizationId,
                OrganizationName = studentOrganization.Organization?.Name ?? "未知",
                JoinedAt = studentOrganization.JoinedAt,
                InvitationCode = studentOrganization.InvitationCode?.Code ?? "未知",
                IsActive = studentOrganization.IsActive,
                CreatedAt = studentOrganization.JoinedAt, // 对于注册学生，使用加入时间作为创建时间
                CreatorUsername = "系统" // 注册学生通过邀请码加入，创建者为系统
            };
    }

    /// <summary>
    /// 将非组织学生组织关系实体映射为DTO
    /// </summary>
    private static StudentOrganizationDto MapNonOrgStudentToDto(NonOrganizationStudentOrganization relation)
    {
        return relation == null
            ? throw new ArgumentNullException(nameof(relation))
            : new StudentOrganizationDto
            {
                Id = relation.Id,
                StudentId = 0, // 非组织学生没有用户ID
                StudentUsername = relation.NonOrganizationStudent?.RealName ?? "未知",
                StudentRealName = relation.NonOrganizationStudent?.RealName,
                StudentPhoneNumber = relation.NonOrganizationStudent?.PhoneNumber,
                OrganizationId = relation.OrganizationId,
                OrganizationName = relation.Organization?.Name ?? "未知",
                JoinedAt = relation.JoinedAt,
                InvitationCode = "", // 非组织学生不使用邀请码
                IsActive = relation.IsActive,
                CreatedAt = relation.CreatedAt,
                CreatorUsername = relation.Creator?.Username ?? "未知"
            };
    }

    /// <summary>
    /// 应用预配置用户信息
    /// </summary>
    private async Task ApplyPreConfiguredUserInfo(int userId, int organizationId)
    {
        try
        {
            // 获取用户信息
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return;
            }

            // 查找匹配的预配置记录
            PreConfiguredUser? preConfigured = await _context.PreConfiguredUsers
                .FirstOrDefaultAsync(p => p.Username == user.Username &&
                                         p.OrganizationId == organizationId &&
                                         !p.IsApplied);

            if (preConfigured != null)
            {
                // 应用预配置信息
                bool userUpdated = false;

                if (!string.IsNullOrEmpty(preConfigured.PhoneNumber) && string.IsNullOrEmpty(user.PhoneNumber))
                {
                    user.PhoneNumber = preConfigured.PhoneNumber;
                    userUpdated = true;
                }

                if (!string.IsNullOrEmpty(preConfigured.RealName) && string.IsNullOrEmpty(user.RealName))
                {
                    user.RealName = preConfigured.RealName;
                    userUpdated = true;
                }

                // 标记预配置记录为已应用
                preConfigured.IsApplied = true;
                preConfigured.AppliedAt = DateTime.UtcNow;
                preConfigured.AppliedToUserId = userId;

                if (userUpdated)
                {
                    _logger.LogInformation("应用预配置信息成功: 用户ID: {UserId}, 组织ID: {OrganizationId}, 手机号: {PhoneNumber}",
                        userId, organizationId, preConfigured.PhoneNumber);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用预配置用户信息失败: 用户ID: {UserId}, 组织ID: {OrganizationId}", userId, organizationId);
            // 不抛出异常，避免影响用户加入组织的主流程
        }
    }
}
