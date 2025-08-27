using ExaminaWebApplication.Services;
using ExaminaWebApplication.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 设备管理API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class DeviceManagementApiController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<DeviceManagementApiController> _logger;

    public DeviceManagementApiController(
        IDeviceService deviceService,
        IUserManagementService userManagementService,
        ILogger<DeviceManagementApiController> logger)
    {
        _deviceService = deviceService;
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户设备列表
    /// </summary>
    [HttpGet("users/{userId}/devices")]
    public async Task<IActionResult> GetUserDevices(int userId)
    {
        try
        {
            var devices = await _deviceService.GetUserDevicesAsync(userId);
            return Ok(new { success = true, data = devices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户设备列表失败，用户ID: {UserId}", userId);
            return BadRequest(new { success = false, message = "获取设备列表失败" });
        }
    }

    /// <summary>
    /// 解绑设备
    /// </summary>
    [HttpDelete("devices/{deviceId}")]
    public async Task<IActionResult> UnbindDevice(int deviceId)
    {
        try
        {
            bool result = await _deviceService.UnbindDeviceAsync(deviceId);
            if (result)
            {
                _logger.LogInformation("设备解绑成功，设备ID: {DeviceId}", deviceId);
                return Ok(new { success = true, message = "设备解绑成功" });
            }
            else
            {
                return BadRequest(new { success = false, message = "设备解绑失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解绑设备失败，设备ID: {DeviceId}", deviceId);
            return StatusCode(500, new { success = false, message = "解绑设备时发生错误" });
        }
    }

    /// <summary>
    /// 设置设备信任状态
    /// </summary>
    [HttpPut("devices/{deviceId}/trust")]
    public async Task<IActionResult> SetDeviceTrust(int deviceId, [FromBody] SetDeviceTrustRequest request)
    {
        try
        {
            bool result = await _deviceService.SetDeviceTrustAsync(deviceId, request.IsTrusted);
            if (result)
            {
                string action = request.IsTrusted ? "设为信任" : "取消信任";
                _logger.LogInformation("设备{Action}成功，设备ID: {DeviceId}", action, deviceId);
                return Ok(new { success = true, message = $"设备{action}成功" });
            }
            else
            {
                return BadRequest(new { success = false, message = "操作失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置设备信任状态失败，设备ID: {DeviceId}", deviceId);
            return StatusCode(500, new { success = false, message = "操作时发生错误" });
        }
    }

    /// <summary>
    /// 延长设备有效期
    /// </summary>
    [HttpPut("devices/{deviceId}/extend")]
    public async Task<IActionResult> ExtendDeviceExpiry(int deviceId, [FromBody] ExtendDeviceExpiryRequest request)
    {
        try
        {
            bool result = await _deviceService.ExtendDeviceExpiryAsync(deviceId, request.Days);
            if (result)
            {
                _logger.LogInformation("设备有效期延长成功，设备ID: {DeviceId}, 延长天数: {Days}", deviceId, request.Days);
                return Ok(new { success = true, message = $"设备有效期已延长{request.Days}天" });
            }
            else
            {
                return BadRequest(new { success = false, message = "延长有效期失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "延长设备有效期失败，设备ID: {DeviceId}", deviceId);
            return StatusCode(500, new { success = false, message = "操作时发生错误" });
        }
    }

    /// <summary>
    /// 批量解绑设备
    /// </summary>
    [HttpPost("devices/batch-unbind")]
    public async Task<IActionResult> BatchUnbindDevices([FromBody] BatchUnbindDevicesRequest request)
    {
        try
        {
            int successCount = 0;
            int failCount = 0;

            foreach (int deviceId in request.DeviceIds)
            {
                bool result = await _deviceService.UnbindDeviceAsync(deviceId);
                if (result)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }

            _logger.LogInformation("批量解绑设备完成，成功: {SuccessCount}, 失败: {FailCount}", successCount, failCount);
            
            if (failCount == 0)
            {
                return Ok(new { success = true, message = $"成功解绑{successCount}个设备" });
            }
            else
            {
                return Ok(new { success = true, message = $"成功解绑{successCount}个设备，{failCount}个设备解绑失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量解绑设备失败");
            return StatusCode(500, new { success = false, message = "批量解绑设备时发生错误" });
        }
    }

    /// <summary>
    /// 获取设备统计信息
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetDeviceStatistics()
    {
        try
        {
            // 获取所有用户
            var users = await _userManagementService.GetUsersAsync(null, false, 1, 10000);
            
            // 统计设备信息
            int totalDevices = 0;
            int activeDevices = 0;
            int trustedDevices = 0;
            int expiredDevices = 0;
            
            foreach (var user in users)
            {
                var devices = await _deviceService.GetUserDevicesAsync(user.Id);
                totalDevices += devices.Count;
                activeDevices += devices.Count(d => d.IsActive);
                trustedDevices += devices.Count(d => d.IsTrusted);
                expiredDevices += devices.Count(d => !d.IsActive);
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalDevices,
                    activeDevices,
                    trustedDevices,
                    expiredDevices
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备统计信息失败");
            return StatusCode(500, new { success = false, message = "获取统计信息失败" });
        }
    }

    /// <summary>
    /// 清理过期设备
    /// </summary>
    [HttpPost("cleanup-expired")]
    public async Task<IActionResult> CleanupExpiredDevices()
    {
        try
        {
            int cleanedCount = await _deviceService.CleanupExpiredDevicesAsync();
            _logger.LogInformation("清理过期设备完成，清理数量: {CleanedCount}", cleanedCount);
            return Ok(new { success = true, message = $"成功清理{cleanedCount}个过期设备" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期设备失败");
            return StatusCode(500, new { success = false, message = "清理过期设备时发生错误" });
        }
    }
}

/// <summary>
/// 设置设备信任状态请求
/// </summary>
public class SetDeviceTrustRequest
{
    public bool IsTrusted { get; set; }
}

/// <summary>
/// 延长设备有效期请求
/// </summary>
public class ExtendDeviceExpiryRequest
{
    public int Days { get; set; }
}

/// <summary>
/// 批量解绑设备请求
/// </summary>
public class BatchUnbindDevicesRequest
{
    public int[] DeviceIds { get; set; } = [];
}
