using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Organization;
using System.Security.Cryptography;
using System.Text;

namespace ExaminaWebApplication.Services.Organization;

/// <summary>
/// 邀请码服务实现
/// </summary>
public class InvitationCodeService : IInvitationCodeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvitationCodeService> _logger;

    // 邀请码字符集（避免容易混淆的字符：0/O, 1/l/I）
    private static readonly char[] CodeCharacters = "ABCDEFGHJKMNPQRSTUVWXYZ23456789abcdefghjkmnpqrstuvwxyz".ToCharArray();
    private const int CodeLength = 7;
    private const int MaxRetryAttempts = 10;

    public InvitationCodeService(ApplicationDbContext context, ILogger<InvitationCodeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 生成唯一的7位邀请码
    /// </summary>
    public async Task<string> GenerateUniqueCodeAsync()
    {
        for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            string code = GenerateRandomCode();
            
            // 检查是否已存在
            bool exists = await _context.InvitationCodes
                .AnyAsync(ic => ic.Code == code);

            if (!exists)
            {
                _logger.LogInformation("生成唯一邀请码成功: {Code}, 尝试次数: {Attempt}", code, attempt + 1);
                return code;
            }

            _logger.LogWarning("邀请码 {Code} 已存在，重新生成 (尝试 {Attempt}/{MaxAttempts})", code, attempt + 1, MaxRetryAttempts);
        }

        throw new InvalidOperationException($"在 {MaxRetryAttempts} 次尝试后仍无法生成唯一邀请码");
    }

    /// <summary>
    /// 验证邀请码是否有效
    /// </summary>
    public async Task<InvitationCode?> ValidateInvitationCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != CodeLength)
        {
            return null;
        }

        try
        {
            InvitationCode? invitationCode = await _context.InvitationCodes
                .Include(ic => ic.Organization)
                .FirstOrDefaultAsync(ic => ic.Code.Equals(code, StringComparison.CurrentCultureIgnoreCase));

            return invitationCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证邀请码时发生错误: {Code}", code);
            return null;
        }
    }

    /// <summary>
    /// 检查邀请码是否可用
    /// </summary>
    public bool IsInvitationCodeAvailable(InvitationCode invitationCode)
    {
        // 检查是否激活
        if (!invitationCode.IsActive)
        {
            return false;
        }

        // 检查是否过期
        if (invitationCode.ExpiresAt.HasValue && invitationCode.ExpiresAt.Value <= DateTime.UtcNow)
        {
            return false;
        }

        // 检查是否达到使用上限
        if (invitationCode.MaxUsage.HasValue && invitationCode.UsageCount >= invitationCode.MaxUsage.Value)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 增加邀请码使用次数
    /// </summary>
    public async Task<bool> IncrementUsageCountAsync(int invitationCodeId)
    {
        try
        {
            InvitationCode? invitationCode = await _context.InvitationCodes
                .FirstOrDefaultAsync(ic => ic.Id == invitationCodeId);

            if (invitationCode == null)
            {
                return false;
            }

            invitationCode.UsageCount++;
            await _context.SaveChangesAsync();

            _logger.LogInformation("邀请码 {Code} 使用次数增加到 {UsageCount}", invitationCode.Code, invitationCode.UsageCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "增加邀请码使用次数失败: {InvitationCodeId}", invitationCodeId);
            return false;
        }
    }

    /// <summary>
    /// 创建邀请码
    /// </summary>
    public async Task<InvitationCode> CreateInvitationCodeAsync(int organizationId, DateTime? expiresAt = null, int? maxUsage = null)
    {
        try
        {
            string code = await GenerateUniqueCodeAsync();

            InvitationCode invitationCode = new()
            {
                Code = code,
                OrganizationId = organizationId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IsActive = true,
                UsageCount = 0,
                MaxUsage = maxUsage
            };

            _context.InvitationCodes.Add(invitationCode);
            await _context.SaveChangesAsync();

            _logger.LogInformation("创建邀请码成功: {Code}, 组织ID: {OrganizationId}", code, organizationId);
            return invitationCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建邀请码失败, 组织ID: {OrganizationId}", organizationId);
            throw;
        }
    }

    /// <summary>
    /// 停用邀请码
    /// </summary>
    public async Task<bool> DeactivateInvitationCodeAsync(int invitationCodeId)
    {
        try
        {
            InvitationCode? invitationCode = await _context.InvitationCodes
                .FirstOrDefaultAsync(ic => ic.Id == invitationCodeId);

            if (invitationCode == null)
            {
                return false;
            }

            invitationCode.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("停用邀请码成功: {Code}", invitationCode.Code);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用邀请码失败: {InvitationCodeId}", invitationCodeId);
            return false;
        }
    }

    /// <summary>
    /// 更新邀请码信息
    /// </summary>
    public async Task<InvitationCode?> UpdateInvitationCodeAsync(int invitationCodeId, int? maxUsage = null, DateTime? expiresAt = null, bool? isActive = null)
    {
        try
        {
            InvitationCode? invitationCode = await _context.InvitationCodes
                .FirstOrDefaultAsync(ic => ic.Id == invitationCodeId);

            if (invitationCode == null)
            {
                return null;
            }

            // 更新字段（只更新提供的字段）
            if (maxUsage.HasValue)
            {
                invitationCode.MaxUsage = maxUsage.Value == 0 ? null : maxUsage.Value;
            }

            if (expiresAt.HasValue)
            {
                invitationCode.ExpiresAt = expiresAt.Value;
            }

            if (isActive.HasValue)
            {
                invitationCode.IsActive = isActive.Value;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("更新邀请码成功: {Code}, ID: {InvitationCodeId}", invitationCode.Code, invitationCodeId);
            return invitationCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新邀请码失败: {InvitationCodeId}", invitationCodeId);
            throw;
        }
    }

    /// <summary>
    /// 删除邀请码
    /// </summary>
    public async Task<bool> DeleteInvitationCodeAsync(int invitationCodeId)
    {
        try
        {
            InvitationCode? invitationCode = await _context.InvitationCodes
                .FirstOrDefaultAsync(ic => ic.Id == invitationCodeId);

            if (invitationCode == null)
            {
                return false;
            }

            _context.InvitationCodes.Remove(invitationCode);
            await _context.SaveChangesAsync();

            _logger.LogInformation("删除邀请码成功: {Code}, ID: {InvitationCodeId}", invitationCode.Code, invitationCodeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除邀请码失败: {InvitationCodeId}", invitationCodeId);
            return false;
        }
    }

    /// <summary>
    /// 设置邀请码激活状态
    /// </summary>
    public async Task<bool> SetInvitationCodeStatusAsync(int invitationCodeId, bool isActive)
    {
        try
        {
            InvitationCode? invitationCode = await _context.InvitationCodes
                .FirstOrDefaultAsync(ic => ic.Id == invitationCodeId);

            if (invitationCode == null)
            {
                return false;
            }

            invitationCode.IsActive = isActive;
            await _context.SaveChangesAsync();

            string status = isActive ? "激活" : "停用";
            _logger.LogInformation("{Status}邀请码成功: {Code}, ID: {InvitationCodeId}", status, invitationCode.Code, invitationCodeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置邀请码状态失败: {InvitationCodeId}", invitationCodeId);
            return false;
        }
    }

    /// <summary>
    /// 获取组织的邀请码列表
    /// </summary>
    public async Task<List<InvitationCode>> GetOrganizationInvitationCodesAsync(int organizationId, bool includeInactive = false)
    {
        try
        {
            IQueryable<InvitationCode> query = _context.InvitationCodes
                .Where(ic => ic.OrganizationId == organizationId);

            if (!includeInactive)
            {
                query = query.Where(ic => ic.IsActive);
            }

            List<InvitationCode> invitationCodes = await query
                .OrderByDescending(ic => ic.CreatedAt)
                .ToListAsync();

            return invitationCodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织邀请码列表失败: {OrganizationId}", organizationId);
            return [];
        }
    }

    /// <summary>
    /// 生成随机邀请码
    /// </summary>
    private static string GenerateRandomCode()
    {
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        StringBuilder codeBuilder = new(CodeLength);

        for (int i = 0; i < CodeLength; i++)
        {
            byte[] randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            int randomIndex = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % CodeCharacters.Length;
            codeBuilder.Append(CodeCharacters[randomIndex]);
        }

        return codeBuilder.ToString().ToUpper();
    }
}
