using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 学生端JWT认证控制器
/// </summary>
[ApiController]
[Route("api/student/auth")]
public class StudentAuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IDeviceService _deviceService;
    private readonly ISmsService _smsService;
    private readonly IWeChatService _weChatService;
    private readonly ISessionService _sessionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StudentAuthController> _logger;

    public StudentAuthController(
        ApplicationDbContext context,
        IJwtService jwtService,
        IDeviceService deviceService,
        ISmsService smsService,
        IWeChatService weChatService,
        ISessionService sessionService,
        IConfiguration configuration,
        ILogger<StudentAuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _deviceService = deviceService;
        _smsService = smsService;
        _weChatService = weChatService;
        _sessionService = sessionService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 学生登录（支持用户名密码、短信验证码、微信扫码）
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            User? user = null;

            // 根据登录类型处理不同的认证方式
            switch (request.LoginType)
            {
                case LoginType.Credentials:
                    user = await AuthenticateWithCredentials(request.Username, request.Password);
                    break;

                case LoginType.SmsCode:
                    user = await AuthenticateWithSmsCode(request.Username, request.SmsCode);
                    break;

                case LoginType.WeChat:
                    user = await AuthenticateWithWeChat(request.QrCode);
                    break;

                default:
                    return BadRequest(new { message = "不支持的登录类型" });
            }

            if (user == null)
            {
                return Unauthorized(new { message = "认证失败" });
            }

            // 检查用户角色（只允许学生登录）
            if (user.Role != UserRole.Student)
            {
                return Unauthorized(new { message = "此接口仅限学生使用" });
            }

            // 处理设备绑定
            UserDevice? device = null;
            if (request.DeviceInfo != null)
            {
                string ipAddress = GetClientIpAddress();
                string location = await GetLocationFromIp(ipAddress);

                // 验证或绑定设备
                device = await _deviceService.ValidateDeviceAsync(user.Id, request.DeviceInfo.DeviceFingerprint);
                if (device == null)
                {
                    // 检查是否可以绑定新设备
                    if (!await _deviceService.CanBindNewDeviceAsync(user.Id))
                    {
                        return BadRequest(new { message = "已达到最大设备绑定数量，请联系管理员" });
                    }

                    // 绑定新设备
                    device = await _deviceService.BindDeviceAsync(user.Id, request.DeviceInfo, ipAddress, location);
                }
                else
                {
                    // 更新设备使用时间
                    _ = await _deviceService.UpdateDeviceLastUsedAsync(device.Id, ipAddress, location);
                }
            }

            // 生成JWT令牌
            string accessToken = _jwtService.GenerateAccessToken(user, device?.Id);
            string refreshToken = _jwtService.GenerateRefreshToken(user, device?.Id);
            DateTime expiresAt = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "10080"));

            // 创建会话记录
            _ = await _sessionService.CreateSessionAsync(
                user.Id,
                accessToken,
                SessionType.JwtToken,
                device?.Id,
                refreshToken,
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString(),
                await GetLocationFromIp(GetClientIpAddress()),
                expiresAt);

            // 更新用户最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            _ = await _context.SaveChangesAsync();

            // 检查用户权限状态
            bool hasFullAccess = await CheckUserFullAccessAsync(user.Id, user.PhoneNumber);

            LoginResponse response = new()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = new UserInfo
                {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Role = user.Role,
                    IsFirstLogin = user.IsFirstLogin,
                    AllowMultipleDevices = user.AllowMultipleDevices,
                    MaxDeviceCount = user.MaxDeviceCount,
                    RealName = user.RealName,
                    HasFullAccess = hasFullAccess
                },
                RequireDeviceBinding = device == null && request.DeviceInfo == null
            };

            _logger.LogInformation("学生 {Username} 登录成功，登录类型: {LoginType}", user.Username, request.LoginType);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "学生登录失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 短信验证码登录（支持自动注册）
    /// </summary>
    [AllowAnonymous]
    [HttpPost("sms-login")]
    public async Task<ActionResult<LoginResponse>> SmsLogin([FromBody] SmsLoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.SmsCode))
            {
                return BadRequest(new { message = "手机号和验证码不能为空" });
            }

            // 验证模型状态
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "手机号格式不正确" });
            }

            // 验证短信验证码并获取或创建用户
            _logger.LogInformation("开始验证短信验证码并获取或创建用户，手机号: {PhoneNumber}", request.PhoneNumber);
            User? user = await AuthenticateWithSmsCodeAndAutoRegister(request.PhoneNumber, request.SmsCode);
            if (user == null)
            {
                _logger.LogWarning("短信验证码认证失败，手机号: {PhoneNumber}", request.PhoneNumber);
                return Unauthorized(new { message = "验证码错误或已过期" });
            }

            _logger.LogInformation("短信验证码认证成功，用户ID: {UserId}, 用户名: {Username}, 角色: {Role}, 是否激活: {IsActive}",
                user.Id, user.Username, user.Role, user.IsActive);

            // 如果是新创建的用户，再次验证用户是否真的存在于数据库中
            if (user.IsFirstLogin)
            {
                _logger.LogInformation("检测到首次登录用户，验证用户是否真正存在于数据库中，用户ID: {UserId}", user.Id);
                User? verifyUser = await _context.Users.FindAsync(user.Id);
                if (verifyUser == null)
                {
                    _logger.LogError("严重错误：新创建的用户在数据库中不存在，用户ID: {UserId}", user.Id);
                    return Unauthorized(new { message = "用户创建失败，请重试" });
                }
                _logger.LogInformation("用户验证成功，用户确实存在于数据库中，用户ID: {UserId}", user.Id);
            }

            // 检查用户角色（只允许学生登录）
            if (user.Role != UserRole.Student)
            {
                _logger.LogWarning("用户角色不正确，用户ID: {UserId}, 角色: {Role}", user.Id, user.Role);
                return Unauthorized(new { message = "此接口仅限学生使用" });
            }

            // 处理设备绑定
            UserDevice? device = null;
            if (request.DeviceInfo != null)
            {
                string ipAddress = GetClientIpAddress();
                string location = await GetLocationFromIp(ipAddress);

                // 验证或绑定设备
                device = await _deviceService.ValidateDeviceAsync(user.Id, request.DeviceInfo.DeviceFingerprint);
                if (device == null)
                {
                    // 检查是否可以绑定新设备
                    if (!await _deviceService.CanBindNewDeviceAsync(user.Id))
                    {
                        return BadRequest(new { message = "已达到最大设备绑定数量，请联系管理员" });
                    }

                    // 绑定新设备
                    device = await _deviceService.BindDeviceAsync(user.Id, request.DeviceInfo, ipAddress, location);
                }
                else
                {
                    // 更新设备使用时间
                    _ = await _deviceService.UpdateDeviceLastUsedAsync(device.Id, ipAddress, location);
                }
            }

            // 生成JWT令牌
            _logger.LogInformation("开始生成JWT令牌，用户ID: {UserId}, 用户名: {Username}, 设备ID: {DeviceId}",
                user.Id, user.Username, device?.Id);

            string accessToken = _jwtService.GenerateAccessToken(user, device?.Id);
            string refreshToken = _jwtService.GenerateRefreshToken(user, device?.Id);
            DateTime expiresAt = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "10080"));

            _logger.LogInformation("JWT令牌生成成功，用户ID: {UserId}, AccessToken前10位: {AccessTokenPrefix}, 过期时间: {ExpiresAt}",
                user.Id, accessToken[..Math.Min(10, accessToken.Length)], expiresAt);

            // 创建会话记录
            _ = await _sessionService.CreateSessionAsync(
                user.Id,
                accessToken,
                SessionType.JwtToken,
                device?.Id,
                refreshToken,
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString(),
                await GetLocationFromIp(GetClientIpAddress()),
                expiresAt);

            // 更新用户最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            _ = await _context.SaveChangesAsync();

            // 检查用户权限状态
            bool hasFullAccess = await CheckUserFullAccessAsync(user.Id, user.PhoneNumber);

            LoginResponse response = new()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = new UserInfo
                {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Role = user.Role,
                    IsFirstLogin = user.IsFirstLogin,
                    AllowMultipleDevices = user.AllowMultipleDevices,
                    MaxDeviceCount = user.MaxDeviceCount,
                    RealName = user.RealName,
                    HasFullAccess = hasFullAccess
                },
                RequireDeviceBinding = device == null && request.DeviceInfo == null
            };

            _logger.LogInformation("学生 {Username} 短信验证码登录成功，手机号: {PhoneNumber}", user.Username, request.PhoneNumber);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "短信验证码登录失败，手机号: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 发送短信验证码
    /// </summary>
    [AllowAnonymous]
    [HttpPost("send-sms")]
    public async Task<ActionResult> SendSmsCode([FromBody] SmsCodeRequest request)
    {
        try
        {
            // 记录接收到的请求信息
            _logger.LogInformation("收到发送短信验证码请求，手机号: {PhoneNumber}", request?.PhoneNumber ?? "null");

            // 验证模型绑定
            if (request == null)
            {
                _logger.LogWarning("SmsCodeRequest为null，模型绑定失败");
                return BadRequest(new { message = "请求数据无效" });
            }

            if (string.IsNullOrEmpty(request.PhoneNumber))
            {
                _logger.LogWarning("手机号为空");
                return BadRequest(new { message = "手机号不能为空" });
            }

            // 验证模型状态
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("模型验证失败: {ModelState}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(new { message = "手机号格式不正确" });
            }

            // 移除用户存在性验证，允许任何有效格式的手机号接收验证码
            _logger.LogInformation("手机号格式验证通过，准备发送验证码: {PhoneNumber}", request.PhoneNumber);

            // 检查是否可以发送验证码
            if (!await _smsService.CanSendCodeAsync(request.PhoneNumber))
            {
                return BadRequest(new { message = "发送验证码过于频繁，请稍后再试" });
            }

            // 生成并发送验证码
            string code = _smsService.GenerateVerificationCode();
            bool success = await _smsService.SendVerificationCodeAsync(request.PhoneNumber, code);

            if (success)
            {
                _logger.LogInformation("向手机号 {PhoneNumber} 发送验证码成功", request.PhoneNumber);
                return Ok(new { message = "验证码发送成功" });
            }
            else
            {
                return BadRequest(new { message = "验证码发送失败，请稍后重试" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送短信验证码失败，手机号: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取微信登录二维码
    /// </summary>
    [AllowAnonymous]
    [HttpPost("wechat-qrcode")]
    public async Task<ActionResult> GetWeChatQrCode()
    {
        try
        {
            WeChatQrCodeInfo qrCodeInfo = await _weChatService.GenerateLoginQrCodeAsync();

            _logger.LogInformation("生成微信登录二维码: {QrCodeKey}", qrCodeInfo.QrCodeKey);
            return Ok(new
            {
                qrCodeKey = qrCodeInfo.QrCodeKey,
                qrCodeUrl = qrCodeInfo.QrCodeUrl,
                expiresAt = qrCodeInfo.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成微信登录二维码失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 完善用户信息
    /// </summary>
    [HttpPost("complete-info")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> CompleteUserInfo([FromBody] CompleteUserInfoRequest request)
    {
        try
        {
            // 获取当前用户ID
            string? userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            // 查找用户
            User? user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive || user.Role != UserRole.Student)
            {
                return NotFound(new { message = "用户不存在或无权限" });
            }

            // 验证模型状态
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "输入数据格式不正确" });
            }

            bool hasChanges = false;

            // 更新用户名（需要检查唯一性）
            if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
            {
                bool usernameExists = await _context.Users
                    .AnyAsync(u => u.Username == request.Username && u.Id != userId);
                if (usernameExists)
                {
                    return BadRequest(new { message = "用户名已存在，请选择其他用户名" });
                }
                user.Username = request.Username;
                hasChanges = true;
            }

            // 更新密码
            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                hasChanges = true;
            }

            // 绑定微信
            if (!string.IsNullOrEmpty(request.WeChatOpenId) && request.WeChatOpenId != user.WeChatOpenId)
            {
                // 检查微信OpenId是否已被其他用户使用
                bool wechatExists = await _context.Users
                    .AnyAsync(u => u.WeChatOpenId == request.WeChatOpenId && u.Id != userId);
                if (wechatExists)
                {
                    return BadRequest(new { message = "该微信账号已绑定其他用户" });
                }
                user.WeChatOpenId = request.WeChatOpenId;
                hasChanges = true;
            }

            // 如果是首次登录，无论是否有其他更改，都标记为非首次登录
            if (user.IsFirstLogin)
            {
                _logger.LogInformation("用户 {UserId} 是首次登录，将IsFirstLogin设置为false", userId);
                user.IsFirstLogin = false;
                hasChanges = true;
            }
            else
            {
                _logger.LogInformation("用户 {UserId} 不是首次登录，IsFirstLogin已经是false", userId);
            }

            if (hasChanges)
            {
                int changedRows = await _context.SaveChangesAsync();
                _logger.LogInformation("用户 {UserId} 完善信息成功，数据库更新了 {ChangedRows} 行", userId, changedRows);
            }
            else
            {
                _logger.LogInformation("用户 {UserId} 没有任何更改", userId);
            }

            // 检查用户权限状态
            bool hasFullAccess = await CheckUserFullAccessAsync(user.Id, user.PhoneNumber);

            // 返回更新后的用户信息
            UserInfo userInfo = new()
            {
                Id = user.Id.ToString(),
                Username = user.Username,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = user.Role,
                IsFirstLogin = user.IsFirstLogin,
                AllowMultipleDevices = user.AllowMultipleDevices,
                MaxDeviceCount = user.MaxDeviceCount,
                RealName = user.RealName,
                HasFullAccess = hasFullAccess
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完善用户信息失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 检查微信二维码扫描状态
    /// </summary>
    [AllowAnonymous]
    [HttpGet("wechat-status/{qrCodeKey}")]
    public async Task<ActionResult> CheckWeChatStatus(string qrCodeKey)
    {
        try
        {
            WeChatScanStatus status = await _weChatService.CheckQrCodeStatusAsync(qrCodeKey);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查微信二维码状态失败: {QrCodeKey}", qrCodeKey);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 验证JWT令牌
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    public async Task<ActionResult> ValidateToken()
    {
        try
        {
            System.Security.Claims.Claim? userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "无效的令牌" });
            }

            User? user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive || user.Role != UserRole.Student)
            {
                return Unauthorized(new { message = "用户不存在、已被禁用或不是学生账号" });
            }

            return Ok(new
            {
                message = "令牌有效",
                userId = user.Id,
                username = user.Username,
                role = user.Role.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证令牌时发生错误");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> GetProfile()
    {
        try
        {
            System.Security.Claims.Claim? userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "未登录或登录已过期" });
            }

            User? user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive || user.Role != UserRole.Student)
            {
                return Unauthorized(new { message = "用户不存在、已被禁用或不是学生账号" });
            }

            // 检查用户权限状态
            bool hasFullAccess = await CheckUserFullAccessAsync(user.Id, user.PhoneNumber);

            UserInfo userInfo = new()
            {
                Id = user.Id.ToString(),
                Username = user.Username,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = user.Role,
                IsFirstLogin = user.IsFirstLogin,
                AllowMultipleDevices = user.AllowMultipleDevices,
                MaxDeviceCount = user.MaxDeviceCount,
                RealName = user.RealName,
                HasFullAccess = hasFullAccess
            };

            _logger.LogInformation("获取用户 {UserId} 的profile信息，IsFirstLogin={IsFirstLogin}", userId, user.IsFirstLogin);
            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户信息失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 更新用户资料
    /// </summary>
    /// <param name="request">更新资料请求</param>
    /// <returns>更新后的用户信息</returns>
    [HttpPost("update-profile")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            // 获取当前用户ID
            _logger.LogInformation("开始处理用户资料更新请求");
            string? userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("从JWT token获取用户ID声明: {UserIdClaim}", userIdClaim);

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("用户ID声明无效或解析失败，UserIdClaim: {UserIdClaim}", userIdClaim);
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            _logger.LogInformation("解析用户ID成功: {UserId}", userId);

            // 查找用户
            _logger.LogInformation("开始查找用户，用户ID: {UserId}", userId);
            User? user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("用户不存在，用户ID: {UserId}", userId);
                return NotFound(new { message = "用户不存在或无权限" });
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("用户未激活，用户ID: {UserId}, IsActive: {IsActive}", userId, user.IsActive);
                return NotFound(new { message = "用户不存在或无权限" });
            }

            if (user.Role != UserRole.Student)
            {
                _logger.LogWarning("用户角色不正确，用户ID: {UserId}, Role: {Role}", userId, user.Role);
                return NotFound(new { message = "用户不存在或无权限" });
            }

            _logger.LogInformation("用户验证成功，用户ID: {UserId}, 用户名: {Username}, 角色: {Role}, 是否激活: {IsActive}",
                user.Id, user.Username, user.Role, user.IsActive);

            // 验证模型状态
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("模型状态验证失败，用户ID: {UserId}", userId);
                return BadRequest(new { message = "输入数据格式不正确" });
            }

            _logger.LogInformation("开始处理用户资料更新，用户ID: {UserId}, 请求用户名: {RequestUsername}, 请求真实姓名: {RequestRealName}",
                userId, request.Username, request.RealName);

            bool hasChanges = false;

            // 更新用户名（需要检查唯一性）
            if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
            {
                _logger.LogInformation("检查用户名唯一性，新用户名: {NewUsername}, 当前用户名: {CurrentUsername}",
                    request.Username, user.Username);

                bool usernameExists = await _context.Users
                    .AnyAsync(u => u.Username == request.Username && u.Id != userId);
                if (usernameExists)
                {
                    _logger.LogWarning("用户名已存在，用户名: {Username}", request.Username);
                    return BadRequest(new { message = "用户名已存在，请选择其他用户名" });
                }

                _logger.LogInformation("更新用户名，从 {OldUsername} 到 {NewUsername}", user.Username, request.Username);
                user.Username = request.Username;
                hasChanges = true;
            }

            // 更新真实姓名
            if (request.RealName != user.RealName)
            {
                _logger.LogInformation("更新真实姓名，从 {OldRealName} 到 {NewRealName}", user.RealName, request.RealName);
                user.RealName = request.RealName;
                hasChanges = true;
            }



            // 保存更改
            if (hasChanges)
            {
                user.UpdatedAt = DateTime.UtcNow;
                int changedRows = await _context.SaveChangesAsync();
                _logger.LogInformation("用户 {UserId} 更新资料成功，数据库更新了 {ChangedRows} 行", userId, changedRows);
            }
            else
            {
                _logger.LogInformation("用户 {UserId} 没有任何更改", userId);
            }

            // 检查用户权限状态
            bool hasFullAccess = await CheckUserFullAccessAsync(user.Id, user.PhoneNumber);

            // 返回更新后的用户信息
            UserInfo userInfo = new()
            {
                Id = user.Id.ToString(),
                Username = user.Username,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = user.Role,
                IsFirstLogin = user.IsFirstLogin,
                AllowMultipleDevices = user.AllowMultipleDevices,
                MaxDeviceCount = user.MaxDeviceCount,
                RealName = user.RealName,
                HasFullAccess = hasFullAccess
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户资料失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="request">修改密码请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            // 获取当前用户ID
            string? userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            // 查找用户
            User? user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive || user.Role != UserRole.Student)
            {
                return NotFound(new { message = "用户不存在或无权限" });
            }

            // 验证模型状态
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "输入数据格式不正确" });
            }

            // 验证当前密码
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "当前密码不正确" });
            }

            // 检查新密码是否与当前密码相同
            if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "新密码不能与当前密码相同" });
            }

            // 更新密码
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            int changedRows = await _context.SaveChangesAsync();
            _logger.LogInformation("用户 {UserId} 修改密码成功，数据库更新了 {ChangedRows} 行", userId, changedRows);

            return Ok(new { message = "密码修改成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "修改密码失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 刷新JWT令牌
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            // 验证刷新令牌
            if (!_jwtService.ValidateRefreshToken(request.RefreshToken))
            {
                return Unauthorized(new { message = "无效的刷新令牌" });
            }

            // 从刷新令牌中获取用户信息
            int? userId = _jwtService.GetUserIdFromToken(request.RefreshToken);
            int? deviceId = _jwtService.GetDeviceIdFromToken(request.RefreshToken);

            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "无效的刷新令牌" });
            }

            // 验证用户是否存在且为学生
            User? user = await _context.Users.FindAsync(userId.Value);
            if (user == null || !user.IsActive || user.Role != UserRole.Student)
            {
                return Unauthorized(new { message = "用户不存在、已被禁用或不是学生账号" });
            }

            // 验证会话是否存在
            UserSession? session = await _sessionService.GetSessionByRefreshTokenAsync(request.RefreshToken);
            if (session == null)
            {
                return Unauthorized(new { message = "会话不存在或已过期" });
            }

            // 如果有设备指纹，验证设备
            if (!string.IsNullOrEmpty(request.DeviceFingerprint) && deviceId.HasValue)
            {
                UserDevice? device = await _deviceService.ValidateDeviceAsync(userId.Value, request.DeviceFingerprint);
                if (device == null || device.Id != deviceId.Value)
                {
                    return Unauthorized(new { message = "设备验证失败" });
                }
            }

            // 生成新的令牌
            string newAccessToken = _jwtService.GenerateAccessToken(user, deviceId);
            string newRefreshToken = _jwtService.GenerateRefreshToken(user, deviceId);
            DateTime newExpiresAt = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "10080"));

            // 更新会话
            _ = await _sessionService.UpdateSessionRefreshTokenAsync(session.Id, newRefreshToken, newExpiresAt);

            RefreshTokenResponse response = new()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = newExpiresAt
            };

            _logger.LogInformation("刷新令牌成功，用户ID: {UserId}", userId.Value);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新令牌失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取用户设备列表
    /// </summary>
    [HttpGet("devices")]
    [Authorize]
    public async Task<ActionResult<List<DeviceInfo>>> GetDevices()
    {
        try
        {
            System.Security.Claims.Claim? userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "无效的令牌" });
            }

            List<DeviceInfo> devices = await _deviceService.GetUserDevicesAsync(userId);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备列表失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 登出
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        try
        {
            string authHeader = Request.Headers.Authorization.ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                string token = authHeader[7..];
                _ = await _sessionService.EndSessionAsync(token);
            }

            System.Security.Claims.Claim? userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                _logger.LogInformation("学生 {UserId} 登出", userIdClaim.Value);
            }

            return Ok(new { message = "登出成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 用户名密码认证
    /// </summary>
    private async Task<User?> AuthenticateWithCredentials(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        User? user = await _context.Users
            .FirstOrDefaultAsync(u =>
                (u.Username == username || u.Email == username || u.PhoneNumber == username)
                && u.IsActive
                && u.Role == UserRole.Student);

        return user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    /// <summary>
    /// 短信验证码认证
    /// </summary>
    private async Task<User?> AuthenticateWithSmsCode(string phoneNumber, string? smsCode)
    {
        if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(smsCode))
        {
            return null;
        }

        // 验证短信验证码
        if (!await _smsService.VerifyCodeAsync(phoneNumber, smsCode))
        {
            return null;
        }

        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber
                && u.IsActive
                && u.Role == UserRole.Student);

        return user;
    }

    /// <summary>
    /// 短信验证码认证并自动注册
    /// </summary>
    private async Task<User?> AuthenticateWithSmsCodeAndAutoRegister(string phoneNumber, string smsCode)
    {
        _logger.LogInformation("开始短信验证码认证并自动注册流程，手机号: {PhoneNumber}", phoneNumber);

        if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(smsCode))
        {
            _logger.LogWarning("手机号或验证码为空，手机号: {PhoneNumber}, 验证码为空: {SmsCodeEmpty}", phoneNumber, string.IsNullOrEmpty(smsCode));
            return null;
        }

        // 验证短信验证码
        _logger.LogInformation("开始验证短信验证码，手机号: {PhoneNumber}", phoneNumber);
        if (!await _smsService.VerifyCodeAsync(phoneNumber, smsCode))
        {
            _logger.LogWarning("短信验证码验证失败，手机号: {PhoneNumber}", phoneNumber);
            return null;
        }
        _logger.LogInformation("短信验证码验证成功，手机号: {PhoneNumber}", phoneNumber);

        // 查找现有用户
        _logger.LogInformation("查找现有用户，手机号: {PhoneNumber}", phoneNumber);
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber
                && u.IsActive
                && u.Role == UserRole.Student);

        if (user != null)
        {
            _logger.LogInformation("找到现有用户，用户ID: {UserId}, 用户名: {Username}", user.Id, user.Username);
            return user;
        }

        _logger.LogInformation("未找到现有用户，开始自动注册流程，手机号: {PhoneNumber}", phoneNumber);

        // 用户不存在，自动注册
        // 使用 EF Core 的执行策略来处理重试逻辑
        Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                // 生成用户名：考生+手机号后四位
                string baseUsername = $"考生{phoneNumber[^4..]}";
                _logger.LogInformation("生成基础用户名: {BaseUsername}", baseUsername);

                string username = await GenerateUniqueUsernameAsync(baseUsername);
                _logger.LogInformation("生成唯一用户名: {Username}", username);

                // 生成临时邮箱
                string email = $"{phoneNumber}@temp.examina.com";
                _logger.LogInformation("生成临时邮箱: {Email}", email);

                // 创建新用户
                user = new User
                {
                    Username = username,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // 随机密码
                    Role = UserRole.Student,
                    IsFirstLogin = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    AllowMultipleDevices = false,
                    MaxDeviceCount = 1
                };

                _logger.LogInformation("创建新用户对象，用户名: {Username}, 邮箱: {Email}, 手机号: {PhoneNumber}, 角色: {Role}, 是否激活: {IsActive}",
                    user.Username, user.Email, user.PhoneNumber, user.Role, user.IsActive);

                _ = _context.Users.Add(user);
                _logger.LogInformation("用户对象已添加到数据库上下文，准备保存到数据库");

                int changedRows = await _context.SaveChangesAsync();
                _logger.LogInformation("数据库保存完成，影响行数: {ChangedRows}, 用户ID: {UserId}", changedRows, user.Id);

                // 确保用户ID已正确设置
                if (user.Id <= 0)
                {
                    _logger.LogError("用户ID未正确设置，用户ID: {UserId}", user.Id);
                    return null;
                }

                // 验证用户是否真正保存到数据库
                User? savedUser = await _context.Users.FindAsync(user.Id);
                if (savedUser != null)
                {
                    _logger.LogInformation("验证成功：用户已保存到数据库，用户ID: {UserId}, 用户名: {Username}, 角色: {Role}, 是否激活: {IsActive}",
                        savedUser.Id, savedUser.Username, savedUser.Role, savedUser.IsActive);

                    // 返回从数据库重新查询的用户对象，确保所有属性都是最新的
                    return savedUser;
                }
                else
                {
                    _logger.LogError("验证失败：用户未能保存到数据库，用户ID: {UserId}", user.Id);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动注册用户失败，手机号: {PhoneNumber}, 错误详情: {ErrorMessage}", phoneNumber, ex.Message);

                // 检查是否是数据库连接问题
                if (ex.InnerException != null)
                {
                    _logger.LogError("内部异常: {InnerException}", ex.InnerException.Message);
                }

                return null;
            }
        });
    }

    /// <summary>
    /// 生成唯一用户名
    /// </summary>
    private async Task<string> GenerateUniqueUsernameAsync(string baseUsername)
    {
        string username = baseUsername;
        int counter = 1;

        while (await _context.Users.AnyAsync(u => u.Username == username))
        {
            username = $"{baseUsername}{counter}";
            counter++;
        }

        return username;
    }

    /// <summary>
    /// 微信扫码认证
    /// </summary>
    private async Task<User?> AuthenticateWithWeChat(string? qrCode)
    {
        if (string.IsNullOrEmpty(qrCode))
        {
            return null;
        }

        // 检查二维码状态
        WeChatScanStatus status = await _weChatService.CheckQrCodeStatusAsync(qrCode);
        if (status.Status != 2 || string.IsNullOrEmpty(status.Code))
        {
            return null;
        }

        // 通过授权码获取微信用户信息
        WeChatUserInfo? weChatUserInfo = await _weChatService.GetUserInfoByCodeAsync(status.Code);
        if (weChatUserInfo == null)
        {
            return null;
        }

        // 查找或创建微信用户
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.WeChatOpenId == weChatUserInfo.OpenId && u.IsActive);

        if (user == null)
        {
            // 创建新的微信用户（仅限学生）
            user = new User
            {
                Username = $"微信用户_{weChatUserInfo.OpenId[..8]}",
                Email = $"wechat_{weChatUserInfo.OpenId}@examina.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                WeChatOpenId = weChatUserInfo.OpenId,
                Role = UserRole.Student,
                RealName = weChatUserInfo.Nickname,
                IsFirstLogin = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AllowMultipleDevices = false,
                MaxDeviceCount = 1
            };

            _ = _context.Users.Add(user);
            _ = await _context.SaveChangesAsync();
        }

        return user;
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string GetClientIpAddress()
    {
        string? ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        return ipAddress ?? "Unknown";
    }

    /// <summary>
    /// 根据IP地址获取地理位置（简化实现）
    /// </summary>
    private async Task<string> GetLocationFromIp(string ipAddress)
    {
        // 这里可以集成第三方IP地理位置服务
        // 简化实现，返回默认位置
        return await Task.FromResult("中国");
    }

    #endregion

    #region 权限判断方法

    /// <summary>
    /// 检查用户是否拥有完整功能权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userPhoneNumber">用户手机号</param>
    /// <returns>是否拥有完整功能权限</returns>
    private async Task<bool> CheckUserFullAccessAsync(int userId, string? userPhoneNumber)
    {
        try
        {
            // 获取用户信息
            User? user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                _logger.LogDebug("用户 {UserId} 不存在或已禁用", userId);
                return false;
            }

            string? userRealName = user.RealName;
            string? userPhone = user.PhoneNumber ?? userPhoneNumber;

            // 如果用户没有填写姓名或手机号，无法进行权限验证
            if (string.IsNullOrEmpty(userRealName) || string.IsNullOrEmpty(userPhone))
            {
                _logger.LogDebug("用户 {UserId} 缺少姓名或手机号信息，无法进行权限验证。姓名: {RealName}, 手机号: {PhoneNumber}",
                    userId, userRealName, userPhone);
                return false;
            }

            // 1. 检查用户是否属于学校组织（学生组织关系）
            // 对于组织成员，需要验证用户的姓名和手机号是否与组织中的学生信息匹配
            bool hasStudentOrganization = await _context.StudentOrganizations
                .Include(so => so.Student)
                .AnyAsync(so => so.StudentId == userId &&
                               so.IsActive &&
                               so.Student.RealName == userRealName &&
                               so.Student.PhoneNumber == userPhone);

            if (hasStudentOrganization)
            {
                _logger.LogDebug("用户 {UserId} 属于学生组织且姓名手机号匹配，拥有完整权限。姓名: {RealName}, 手机号: {PhoneNumber}",
                    userId, userRealName, userPhone);
                return true;
            }

            // 2. 检查用户是否属于学校组织（教师组织关系）
            // 对于教师，同样需要验证姓名和手机号匹配
            bool hasTeacherOrganization = await _context.TeacherOrganizations
                .Include(to => to.Teacher)
                .AnyAsync(to => to.TeacherId == userId &&
                               to.IsActive &&
                               to.Teacher.RealName == userRealName &&
                               to.Teacher.PhoneNumber == userPhone);

            if (hasTeacherOrganization)
            {
                _logger.LogDebug("用户 {UserId} 属于教师组织且姓名手机号匹配，拥有完整权限。姓名: {RealName}, 手机号: {PhoneNumber}",
                    userId, userRealName, userPhone);
                return true;
            }

            // 3. 检查用户是否在非组织学生名单中（通过UserId直接关联）
            // 需要验证关联的用户信息与非组织学生记录的姓名手机号匹配
            bool isNonOrgStudentByUserId = await _context.NonOrganizationStudents
                .AnyAsync(nos => nos.UserId == userId &&
                                nos.IsActive &&
                                nos.RealName == userRealName &&
                                nos.PhoneNumber == userPhone);

            if (isNonOrgStudentByUserId)
            {
                _logger.LogDebug("用户 {UserId} 在非组织学生名单中（通过UserId）且姓名手机号匹配，拥有完整权限。姓名: {RealName}, 手机号: {PhoneNumber}",
                    userId, userRealName, userPhone);
                return true;
            }

            // 4. 检查用户是否在非组织学生名单中（通过姓名+手机号匹配）
            // 统一使用姓名+手机号的匹配标准
            bool isNonOrgStudentByNameAndPhone = await _context.NonOrganizationStudents
                .AnyAsync(nos => nos.RealName == userRealName &&
                                nos.PhoneNumber == userPhone &&
                                nos.IsActive);

            if (isNonOrgStudentByNameAndPhone)
            {
                _logger.LogDebug("用户 {UserId} 在非组织学生名单中（通过姓名+手机号匹配），拥有完整权限。姓名: {RealName}, 手机号: {PhoneNumber}",
                    userId, userRealName, userPhone);
                return true;
            }

            // 5. 用户不满足任何权限条件，无完整权限
            _logger.LogDebug("用户 {UserId} 不满足任何权限条件，无完整权限。姓名: {RealName}, 手机号: {PhoneNumber}",
                userId, userRealName, userPhone);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用户 {UserId} 权限状态时发生错误", userId);
            // 出现异常时，为了安全起见，返回false
            return false;
        }
    }

    #endregion
}
