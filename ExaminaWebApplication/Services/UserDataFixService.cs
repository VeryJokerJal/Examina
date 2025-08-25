using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Services;

/// <summary>
/// 用户数据修复服务
/// 用于修复微信用户和手机号用户重复的问题
/// </summary>
public class UserDataFixService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserDataFixService> _logger;

    public UserDataFixService(ApplicationDbContext context, ILogger<UserDataFixService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 修复重复用户问题
    /// </summary>
    /// <returns>修复的用户数量</returns>
    public async Task<int> FixDuplicateUsersAsync()
    {
        _logger.LogInformation("开始修复重复用户问题");

        try
        {
            // 1. 查找重复用户对
            var duplicatePairs = await FindDuplicateUserPairsAsync();
            _logger.LogInformation("找到 {Count} 对重复用户", duplicatePairs.Count);

            int fixedCount = 0;

            foreach (var pair in duplicatePairs)
            {
                try
                {
                    await FixUserPairAsync(pair.WeChatUser, pair.PhoneUser);
                    fixedCount++;
                    _logger.LogInformation("成功修复用户对：微信用户 {WeChatUserId} 和手机号用户 {PhoneUserId}", 
                        pair.WeChatUser.Id, pair.PhoneUser.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "修复用户对失败：微信用户 {WeChatUserId} 和手机号用户 {PhoneUserId}", 
                        pair.WeChatUser.Id, pair.PhoneUser.Id);
                }
            }

            _logger.LogInformation("重复用户修复完成，共修复 {FixedCount} 对用户", fixedCount);
            return fixedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "修复重复用户过程中发生错误");
            throw;
        }
    }

    /// <summary>
    /// 查找重复用户对
    /// </summary>
    private async Task<List<(User WeChatUser, User PhoneUser)>> FindDuplicateUserPairsAsync()
    {
        var duplicatePairs = new List<(User WeChatUser, User PhoneUser)>();

        // 查找所有微信用户（有WeChatOpenId但没有PhoneNumber）
        var wechatUsers = await _context.Users
            .Where(u => u.IsActive && 
                       !string.IsNullOrEmpty(u.WeChatOpenId) && 
                       string.IsNullOrEmpty(u.PhoneNumber) &&
                       u.Role == UserRole.Student)
            .ToListAsync();

        foreach (var wechatUser in wechatUsers)
        {
            // 查找可能的对应手机号用户
            var phoneUsers = await _context.Users
                .Where(u => u.IsActive && 
                           string.IsNullOrEmpty(u.WeChatOpenId) && 
                           !string.IsNullOrEmpty(u.PhoneNumber) &&
                           u.Role == UserRole.Student &&
                           u.Username.StartsWith("考生") &&
                           u.CreatedAt > wechatUser.CreatedAt.AddMinutes(-30) && // 创建时间相近
                           u.CreatedAt < wechatUser.CreatedAt.AddMinutes(30))
                .ToListAsync();

            // 如果找到可能的重复用户，添加到列表
            foreach (var phoneUser in phoneUsers)
            {
                duplicatePairs.Add((wechatUser, phoneUser));
            }
        }

        return duplicatePairs;
    }

    /// <summary>
    /// 修复一对重复用户
    /// </summary>
    private async Task FixUserPairAsync(User wechatUser, User phoneUser)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. 更新微信用户的手机号
            wechatUser.PhoneNumber = phoneUser.PhoneNumber;
            wechatUser.IsFirstLogin = false; // 确保标记为非首次登录

            // 2. 迁移用户会话
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == phoneUser.Id)
                .ToListAsync();
            
            foreach (var session in sessions)
            {
                session.UserId = wechatUser.Id;
            }

            // 3. 迁移设备绑定
            var devices = await _context.UserDevices
                .Where(d => d.UserId == phoneUser.Id)
                .ToListAsync();
            
            foreach (var device in devices)
            {
                device.UserId = wechatUser.Id;
            }

            // 4. 软删除手机号用户
            phoneUser.IsActive = false;
            phoneUser.Username = phoneUser.Username + "_DELETED_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            // 5. 保存更改
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("成功修复用户对：将手机号 {PhoneNumber} 绑定到微信用户 {WeChatUserId}，软删除用户 {PhoneUserId}", 
                phoneUser.PhoneNumber, wechatUser.Id, phoneUser.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 检查重复用户情况
    /// </summary>
    public async Task<DuplicateUserReport> CheckDuplicateUsersAsync()
    {
        var report = new DuplicateUserReport();

        // 检查重复手机号
        var duplicatePhones = await _context.Users
            .Where(u => u.IsActive && !string.IsNullOrEmpty(u.PhoneNumber))
            .GroupBy(u => u.PhoneNumber)
            .Where(g => g.Count() > 1)
            .Select(g => new { PhoneNumber = g.Key, Count = g.Count() })
            .ToListAsync();

        report.DuplicatePhoneNumbers = duplicatePhones.ToDictionary(x => x.PhoneNumber, x => x.Count);

        // 检查可能的重复用户对
        var duplicatePairs = await FindDuplicateUserPairsAsync();
        report.PotentialDuplicatePairs = duplicatePairs.Count;

        // 检查微信用户没有手机号的情况
        var wechatUsersWithoutPhone = await _context.Users
            .CountAsync(u => u.IsActive && 
                            !string.IsNullOrEmpty(u.WeChatOpenId) && 
                            string.IsNullOrEmpty(u.PhoneNumber) &&
                            u.Role == UserRole.Student);

        report.WeChatUsersWithoutPhone = wechatUsersWithoutPhone;

        return report;
    }
}

/// <summary>
/// 重复用户检查报告
/// </summary>
public class DuplicateUserReport
{
    /// <summary>
    /// 重复的手机号及其用户数量
    /// </summary>
    public Dictionary<string, int> DuplicatePhoneNumbers { get; set; } = new();

    /// <summary>
    /// 潜在的重复用户对数量
    /// </summary>
    public int PotentialDuplicatePairs { get; set; }

    /// <summary>
    /// 没有手机号的微信用户数量
    /// </summary>
    public int WeChatUsersWithoutPhone { get; set; }
}
