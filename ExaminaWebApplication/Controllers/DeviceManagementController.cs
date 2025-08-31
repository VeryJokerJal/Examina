using ExaminaWebApplication.Models;
using ExaminaWebApplication.Services;
using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 设备管理Web控制器
/// </summary>
[Authorize(Roles = "Administrator")]
public class DeviceManagementController : Controller
{
    private readonly IDeviceService _deviceService;
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<DeviceManagementController> _logger;

    public DeviceManagementController(
        IDeviceService deviceService,
        IUserManagementService userManagementService,
        ILogger<DeviceManagementController> logger)
    {
        _deviceService = deviceService;
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// 设备管理首页
    /// </summary>
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 50, string? searchKeyword = null, bool includeInactive = false, UserRole? userRole = null)
    {
        try
        {
            DeviceManagementViewModel viewModel = new()
            {
                CurrentPage = pageNumber,
                PageSize = pageSize,
                SearchKeyword = searchKeyword ?? string.Empty,
                IncludeInactive = includeInactive,
                SelectedUserRole = userRole
            };

            // 获取用户列表（用于筛选）
            List<Models.Organization.Dto.UserDto> users =
                await _userManagementService.GetUsersAsync(null, false, 1, 1000);
            viewModel.Users = users;

            // 使用新的GetAllDevicesAsync方法获取设备信息
            List<DeviceInfo> allDevices = await _deviceService.GetAllDevicesAsync(includeInactive, searchKeyword, userRole);

            // 分页
            List<DeviceInfo> pagedDevices = [.. allDevices
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)];

            viewModel.Devices = pagedDevices;
            viewModel.UpdateStatistics();

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载设备管理页面失败");
            TempData["ErrorMessage"] = "加载页面失败，请稍后重试";
            return View(new DeviceManagementViewModel());
        }
    }

    /// <summary>
    /// 获取用户设备列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserDevices(int userId)
    {
        try
        {
            List<DeviceInfo> devices = await _deviceService.GetUserDevicesAsync(userId);
            return Json(new { success = true, data = devices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户设备列表失败，用户ID: {UserId}", userId);
            return Json(new { success = false, message = "获取设备列表失败" });
        }
    }

    /// <summary>
    /// 解绑设备
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UnbindDevice(int deviceId)
    {
        try
        {
            bool result = await _deviceService.UnbindDeviceAsync(deviceId);
            if (result)
            {
                _logger.LogInformation("设备解绑成功，设备ID: {DeviceId}", deviceId);
                return Json(new { success = true, message = "设备解绑成功" });
            }
            else
            {
                return Json(new { success = false, message = "设备解绑失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解绑设备失败，设备ID: {DeviceId}", deviceId);
            return Json(new { success = false, message = "解绑设备时发生错误" });
        }
    }

    /// <summary>
    /// 设置设备信任状态
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SetDeviceTrust(int deviceId, bool isTrusted)
    {
        try
        {
            bool result = await _deviceService.SetDeviceTrustAsync(deviceId, isTrusted);
            if (result)
            {
                string action = isTrusted ? "设为信任" : "取消信任";
                _logger.LogInformation("设备{Action}成功，设备ID: {DeviceId}", action, deviceId);
                return Json(new { success = true, message = $"设备{action}成功" });
            }
            else
            {
                return Json(new { success = false, message = "操作失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置设备信任状态失败，设备ID: {DeviceId}", deviceId);
            return Json(new { success = false, message = "操作时发生错误" });
        }
    }

    /// <summary>
    /// 延长设备有效期
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExtendDeviceExpiry(int deviceId, int days)
    {
        try
        {
            bool result = await _deviceService.ExtendDeviceExpiryAsync(deviceId, days);
            if (result)
            {
                _logger.LogInformation("设备有效期延长成功，设备ID: {DeviceId}, 延长天数: {Days}", deviceId, days);
                return Json(new { success = true, message = $"设备有效期已延长{days}天" });
            }
            else
            {
                return Json(new { success = false, message = "延长有效期失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "延长设备有效期失败，设备ID: {DeviceId}", deviceId);
            return Json(new { success = false, message = "操作时发生错误" });
        }
    }

    /// <summary>
    /// 批量解绑设备
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> BatchUnbindDevices([FromBody] int[] deviceIds)
    {
        try
        {
            int successCount = 0;
            int failCount = 0;

            foreach (int deviceId in deviceIds)
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
                return Json(new { success = true, message = $"成功解绑{successCount}个设备" });
            }
            else
            {
                return Json(new { success = true, message = $"成功解绑{successCount}个设备，{failCount}个设备解绑失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量解绑设备失败");
            return Json(new { success = false, message = "批量解绑设备时发生错误" });
        }
    }

    /// <summary>
    /// 获取设备统计信息
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDeviceStatistics()
    {
        try
        {
            // 获取所有用户
            List<Models.Organization.Dto.UserDto> users = await _userManagementService.GetUsersAsync(null, false, 1, 10000);

            // 统计设备信息
            int totalDevices = 0;
            int activeDevices = 0;
            int trustedDevices = 0;
            int expiredDevices = 0;

            foreach (Models.Organization.Dto.UserDto user in users)
            {
                List<DeviceInfo> devices = await _deviceService.GetUserDevicesAsync(user.Id);
                totalDevices += devices.Count;
                activeDevices += devices.Count(d => d.IsActive);
                trustedDevices += devices.Count(d => d.IsTrusted);
                expiredDevices += devices.Count(d => !d.IsActive);
            }

            return Json(new
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
            return Json(new { success = false, message = "获取统计信息失败" });
        }
    }
}
