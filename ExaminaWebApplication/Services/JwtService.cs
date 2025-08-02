using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExaminaWebApplication.Models;

namespace ExaminaWebApplication.Services;

/// <summary>
/// JWT服务实现
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _secretKey = _configuration["Jwt:SecretKey"] ?? "ExaminaSecretKey2024!@#$%^&*()_+1234567890";
        _issuer = _configuration["Jwt:Issuer"] ?? "ExaminaApp";
        _audience = _configuration["Jwt:Audience"] ?? "ExaminaUsers";
        _accessTokenExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "10080"); // 7天
        _refreshTokenExpirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "30"); // 30天
    }

    public string GenerateAccessToken(User user, int? deviceId = null)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("IsFirstLogin", user.IsFirstLogin.ToString()),
                new("TokenType", "AccessToken")
            };

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
            }

            if (!string.IsNullOrEmpty(user.RealName))
            {
                claims.Add(new Claim("RealName", user.RealName));
            }

            if (!string.IsNullOrEmpty(user.StudentId))
            {
                claims.Add(new Claim("StudentId", user.StudentId));
            }

            if (deviceId.HasValue)
            {
                claims.Add(new Claim("DeviceId", deviceId.Value.ToString()));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation("生成访问令牌成功，用户ID: {UserId}, 设备ID: {DeviceId}", user.Id, deviceId);
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成访问令牌失败，用户ID: {UserId}", user.Id);
            throw;
        }
    }

    public string GenerateRefreshToken(User user, int? deviceId = null)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new("TokenType", "RefreshToken"),
                new("Jti", Guid.NewGuid().ToString()) // JWT ID for uniqueness
            };

            if (deviceId.HasValue)
            {
                claims.Add(new Claim("DeviceId", deviceId.Value.ToString()));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation("生成刷新令牌成功，用户ID: {UserId}, 设备ID: {DeviceId}", user.Id, deviceId);
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成刷新令牌失败，用户ID: {UserId}", user.Id);
            throw;
        }
    }

    public bool ValidateAccessToken(string token)
    {
        return ValidateTokenInternal(token, "AccessToken");
    }

    public bool ValidateRefreshToken(string token)
    {
        return ValidateTokenInternal(token, "RefreshToken");
    }

    private bool ValidateTokenInternal(string token, string expectedTokenType)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            // 验证令牌类型
            var tokenTypeClaim = principal.FindFirst("TokenType");
            if (tokenTypeClaim?.Value != expectedTokenType)
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("令牌验证失败: {Error}", ex.Message);
            return false;
        }
    }

    public int? GetUserIdFromToken(string token)
    {
        try
        {
            var claims = GetClaimsFromToken(token);
            var userIdClaim = claims?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("从令牌获取用户ID失败: {Error}", ex.Message);
            return null;
        }
    }

    public int? GetDeviceIdFromToken(string token)
    {
        try
        {
            var claims = GetClaimsFromToken(token);
            var deviceIdClaim = claims?.FindFirst("DeviceId");
            if (deviceIdClaim != null && int.TryParse(deviceIdClaim.Value, out int deviceId))
            {
                return deviceId;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("从令牌获取设备ID失败: {Error}", ex.Message);
            return null;
        }
    }

    public UserRole? GetUserRoleFromToken(string token)
    {
        try
        {
            var claims = GetClaimsFromToken(token);
            var roleClaim = claims?.FindFirst(ClaimTypes.Role);
            if (roleClaim != null && Enum.TryParse<UserRole>(roleClaim.Value, out var role))
            {
                return role;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("从令牌获取用户角色失败: {Error}", ex.Message);
            return null;
        }
    }

    public ClaimsPrincipal? GetClaimsFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("从令牌获取声明失败: {Error}", ex.Message);
            return null;
        }
    }

    public DateTime? GetTokenExpirationTime(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("获取令牌过期时间失败: {Error}", ex.Message);
            return null;
        }
    }

    public bool IsTokenNearExpiry(string token, int minutesBeforeExpiry = 30)
    {
        try
        {
            var expirationTime = GetTokenExpirationTime(token);
            if (expirationTime.HasValue)
            {
                var timeUntilExpiry = expirationTime.Value - DateTime.UtcNow;
                return timeUntilExpiry.TotalMinutes <= minutesBeforeExpiry;
            }
            return true; // 如果无法获取过期时间，认为即将过期
        }
        catch (Exception ex)
        {
            _logger.LogWarning("检查令牌是否即将过期失败: {Error}", ex.Message);
            return true;
        }
    }

    // 兼容旧版本的方法
    [Obsolete("请使用 GenerateAccessToken 方法")]
    public string GenerateToken(User user)
    {
        return GenerateAccessToken(user);
    }

    [Obsolete("请使用 ValidateAccessToken 方法")]
    public bool ValidateToken(string token)
    {
        return ValidateAccessToken(token);
    }
}
