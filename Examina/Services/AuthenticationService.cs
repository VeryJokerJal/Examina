using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Examina.Models;

namespace Examina.Services;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IDeviceService _deviceService;
    private readonly ISecureStorageService _secureStorage;
    private const string BaseUrl = "https://qiuzhenbd.com/api";
    private const string StudentAuthUrl = "student/auth";
    private const string PersistentLoginKey = "persistent_login_data";

    public bool IsAuthenticated { get; private set; }
    public UserInfo? CurrentUser { get; private set; }
    public string? CurrentAccessToken { get; private set; }
    public string? CurrentRefreshToken { get; private set; }
    public DateTime? TokenExpiresAt { get; private set; }

    public bool NeedsTokenRefresh =>
        TokenExpiresAt.HasValue &&
        TokenExpiresAt.Value.Subtract(DateTime.UtcNow).TotalMinutes <= 30;

    public AuthenticationService(HttpClient httpClient, IDeviceService deviceService, ISecureStorageService secureStorage)
    {
        _httpClient = httpClient;
        _deviceService = deviceService;
        _secureStorage = secureStorage;

        // HttpClient现在通过依赖注入配置，不需要在这里重复配置
        // 但我们可以确保基础设置正确
        EnsureHttpClientConfiguration();
    }

    /// <summary>
    /// 确保HttpClient配置正确
    /// </summary>
    private void EnsureHttpClientConfiguration()
    {
        // 调试信息：记录当前HttpClient配置
        System.Diagnostics.Debug.WriteLine($"=== HttpClient配置检查 ===");
        System.Diagnostics.Debug.WriteLine($"当前BaseAddress: {_httpClient.BaseAddress}");
        System.Diagnostics.Debug.WriteLine($"AuthenticationService BaseUrl常量: {BaseUrl}");

        // 如果基础地址未设置，则设置它（但优先使用依赖注入配置的地址）
        if (_httpClient.BaseAddress == null)
        {
            // 只设置域名部分，路径在BuildApiUrl中构建
            _httpClient.BaseAddress = new Uri("https://qiuzhenbd.com");
            System.Diagnostics.Debug.WriteLine($"设置BaseAddress为: {_httpClient.BaseAddress}");
        }
        else
        {
            // 验证BaseAddress是否使用HTTPS
            if (_httpClient.BaseAddress.Scheme != "https")
            {
                System.Diagnostics.Debug.WriteLine($"警告：BaseAddress使用的不是HTTPS协议: {_httpClient.BaseAddress.Scheme}");
                // 强制使用HTTPS
                UriBuilder builder = new(_httpClient.BaseAddress)
                {
                    Scheme = "https",
                    Port = _httpClient.BaseAddress.Port == 80 ? 443 : _httpClient.BaseAddress.Port
                };
                _httpClient.BaseAddress = builder.Uri;
                System.Diagnostics.Debug.WriteLine($"已强制修改为HTTPS: {_httpClient.BaseAddress}");
            }
        }

        // 确保必要的请求头存在
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Examina-Desktop-Client/1.0");
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("Accept"))
        {
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        System.Diagnostics.Debug.WriteLine($"最终BaseAddress: {_httpClient.BaseAddress}");
    }

    /// <summary>
    /// 构建API端点URL（完整路径，因为BaseAddress现在只包含域名）
    /// </summary>
    /// <param name="endpoint">端点路径</param>
    /// <returns>完整的API路径</returns>
    private string BuildApiUrl(string endpoint)
    {
        // 构建完整的API路径：/api/student/auth/{endpoint}
        string apiPath = "api";
        string studentAuth = StudentAuthUrl.Trim('/');
        string endpointPath = endpoint.TrimStart('/');

        return $"/{apiPath}/{studentAuth}/{endpointPath}";
    }

    public async Task<AuthenticationResult> LoginWithCredentialsAsync(string username, string password)
    {
        try
        {
            DeviceBindRequest deviceInfo = _deviceService.GetDeviceInfo();

            LoginRequest loginRequest = new()
            {
                Username = username,
                Password = password,
                LoginType = LoginType.Credentials,
                DeviceInfo = deviceInfo
            };

            string json = JsonSerializer.Serialize(loginRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("login"), content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                LoginResponse? loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    return SetAuthenticationState(loginResponse);
                }
            }

            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "登录失败，请检查用户名和密码"
            };
        }
        catch (Exception ex)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"登录过程中发生错误: {ex.Message}"
            };
        }
    }

    public async Task<AuthenticationResult> LoginWithSmsAsync(string phoneNumber, string smsCode)
    {
        try
        {
            DeviceBindRequest deviceInfo = _deviceService.GetDeviceInfo();

            SmsLoginRequest smsLoginRequest = new()
            {
                PhoneNumber = phoneNumber,
                SmsCode = smsCode,
                DeviceInfo = deviceInfo
            };

            string json = JsonSerializer.Serialize(smsLoginRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            StringContent content = new(json, Encoding.UTF8, "application/json");

            // 使用新的短信登录端点
            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("sms-login"), content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                LoginResponse? loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    return SetAuthenticationState(loginResponse);
                }
            }

            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "短信验证码登录失败"
            };
        }
        catch (Exception ex)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"短信登录过程中发生错误: {ex.Message}"
            };
        }
    }

    public async Task<AuthenticationResult> LoginWithWeChatAsync(string qrCode)
    {
        try
        {
            DeviceBindRequest deviceInfo = _deviceService.GetDeviceInfo();

            LoginRequest loginRequest = new()
            {
                LoginType = LoginType.WeChat,
                QrCode = qrCode,
                DeviceInfo = deviceInfo
            };

            string json = JsonSerializer.Serialize(loginRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("login"), content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                LoginResponse? loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    return SetAuthenticationState(loginResponse);
                }
            }

            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "微信登录失败"
            };
        }
        catch (Exception ex)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"微信登录过程中发生错误: {ex.Message}"
            };
        }
    }

    public async Task<bool> SendSmsCodeAsync(string phoneNumber)
    {
        try
        {
            var request = new { PhoneNumber = phoneNumber };
            string json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // 调试信息：记录发送的JSON内容
            System.Diagnostics.Debug.WriteLine($"发送的JSON: {json}");
            StringContent content = new(json, Encoding.UTF8, "application/json");

            // 使用改进的URL构建方法
            string apiUrl = BuildApiUrl("send-sms");

            // 验证URL构建是否正确
            System.Diagnostics.Debug.WriteLine($"BuildApiUrl结果: {apiUrl}");

            // 调试信息：记录HttpClient配置和实际请求URL
            System.Diagnostics.Debug.WriteLine($"=== SMS API调用调试信息 ===");
            System.Diagnostics.Debug.WriteLine($"HttpClient BaseAddress: {_httpClient.BaseAddress}");
            System.Diagnostics.Debug.WriteLine($"相对API URL: {apiUrl}");

            // 构建完整URL用于调试
            Uri? fullUrl = _httpClient.BaseAddress != null ? new Uri(_httpClient.BaseAddress, apiUrl) : new Uri(apiUrl);
            System.Diagnostics.Debug.WriteLine($"完整请求URL: {fullUrl}");
            System.Diagnostics.Debug.WriteLine($"协议: {fullUrl.Scheme}");
            System.Diagnostics.Debug.WriteLine($"主机: {fullUrl.Host}");
            System.Diagnostics.Debug.WriteLine($"端口: {fullUrl.Port}");
            System.Diagnostics.Debug.WriteLine($"路径: {fullUrl.AbsolutePath}");
            System.Diagnostics.Debug.WriteLine($"查询字符串: {fullUrl.Query}");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

            // 添加详细的响应日志
            string responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"响应状态码: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"响应内容: {responseContent}");

            // 如果收到重定向响应，手动处理重定向到HTTPS
            if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
            {
                string? location = response.Headers.Location?.ToString();
                System.Diagnostics.Debug.WriteLine($"收到重定向: {response.StatusCode} -> {location}");

                if (!string.IsNullOrEmpty(location) && location.StartsWith("https://"))
                {
                    System.Diagnostics.Debug.WriteLine($"手动重定向到HTTPS: {location}");

                    // 手动发送HTTPS请求
                    HttpResponseMessage redirectResponse = await _httpClient.PostAsync(location, content);
                    string redirectResponseContent = await redirectResponse.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"重定向后响应状态码: {redirectResponse.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"重定向后响应内容: {redirectResponseContent}");

                    return redirectResponse.IsSuccessStatusCode;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"无效的重定向位置: {location}");
                    return false;
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"SMS API调用失败: {response.StatusCode}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SMS API调用成功");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"发送短信验证码异常: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<WeChatQrCodeInfo?> GetWeChatQrCodeAsync()
    {
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("wechat-qrcode"), null);
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<WeChatQrCodeInfo>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch
        {
            // 忽略错误
        }
        return null;
    }

    public async Task<WeChatScanStatus?> CheckWeChatStatusAsync(string qrCodeKey)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(BuildApiUrl($"wechat-status/{qrCodeKey}"));
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<WeChatScanStatus>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch
        {
            // 忽略错误
        }
        return null;
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await _httpClient.GetAsync(BuildApiUrl("validate"));
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AuthenticationResult> RefreshTokenAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentRefreshToken))
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "没有可用的刷新令牌"
                };
            }

            string deviceFingerprint = _deviceService.GenerateDeviceFingerprint();
            RefreshTokenRequest refreshRequest = new()
            {
                RefreshToken = CurrentRefreshToken,
                DeviceFingerprint = deviceFingerprint
            };

            string json = JsonSerializer.Serialize(refreshRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("refresh"), content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                RefreshTokenResponse? refreshResponse = JsonSerializer.Deserialize<RefreshTokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (refreshResponse != null && !string.IsNullOrEmpty(refreshResponse.AccessToken))
                {
                    CurrentAccessToken = refreshResponse.AccessToken;
                    CurrentRefreshToken = refreshResponse.RefreshToken;
                    TokenExpiresAt = refreshResponse.ExpiresAt;

                    // 更新HTTP客户端的认证头
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

                    return new AuthenticationResult
                    {
                        IsSuccess = true,
                        AccessToken = CurrentAccessToken,
                        RefreshToken = CurrentRefreshToken,
                        ExpiresAt = TokenExpiresAt,
                        User = CurrentUser
                    };
                }
            }

            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "刷新令牌失败"
            };
        }
        catch (Exception ex)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"刷新令牌过程中发生错误: {ex.Message}"
            };
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(CurrentAccessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);
                _ = await _httpClient.PostAsync(BuildApiUrl("logout"), null);
            }
        }
        catch
        {
            // 忽略登出错误
        }
        finally
        {
            ClearAuthenticationState();
        }
    }

    public async Task<List<DeviceInfo>> GetUserDevicesAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentAccessToken))
            {
                return [];
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

            HttpResponseMessage response = await _httpClient.GetAsync(BuildApiUrl("devices"));
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                List<DeviceInfo>? devices = JsonSerializer.Deserialize<List<DeviceInfo>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return devices ?? [];
            }
        }
        catch
        {
            // 忽略错误
        }
        return [];
    }

    /// <summary>
    /// 设置认证状态
    /// </summary>
    /// <param name="loginResponse">登录响应</param>
    /// <param name="saveToLocal">是否保存到本地存储</param>
    /// <returns>认证结果</returns>
    private AuthenticationResult SetAuthenticationState(LoginResponse loginResponse, bool saveToLocal = true)
    {
        CurrentAccessToken = loginResponse.AccessToken;
        CurrentRefreshToken = loginResponse.RefreshToken;
        TokenExpiresAt = loginResponse.ExpiresAt;
        CurrentUser = loginResponse.User;
        IsAuthenticated = true;

        // 设置HTTP客户端的认证头
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

        // 异步保存到本地存储（不等待结果，避免阻塞UI）
        if (saveToLocal)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await SaveLoginDataAsync(loginResponse);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存登录数据到本地失败: {ex.Message}");
                }
            });
        }

        return new AuthenticationResult
        {
            IsSuccess = true,
            AccessToken = CurrentAccessToken,
            RefreshToken = CurrentRefreshToken,
            ExpiresAt = TokenExpiresAt,
            User = CurrentUser,
            RequireDeviceBinding = loginResponse.RequireDeviceBinding
        };
    }

    /// <summary>
    /// 清除认证状态
    /// </summary>
    private void ClearAuthenticationState()
    {
        CurrentAccessToken = null;
        CurrentRefreshToken = null;
        TokenExpiresAt = null;
        CurrentUser = null;
        IsAuthenticated = false;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// 解析JWT令牌获取过期时间
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>过期时间</returns>
    private DateTime? GetTokenExpirationTime(string token)
    {
        try
        {
            JwtSecurityTokenHandler handler = new();
            JwtSecurityToken jwtToken = handler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 完善用户信息
    /// </summary>
    /// <param name="request">用户信息完善请求</param>
    /// <returns>更新后的用户信息</returns>
    public async Task<UserInfo?> CompleteUserInfoAsync(CompleteUserInfoRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentAccessToken))
            {
                return null;
            }

            string json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            StringContent content = new(json, Encoding.UTF8, "application/json");

            // 设置Authorization头
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("complete-info"), content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                UserInfo? userInfo = JsonSerializer.Deserialize<UserInfo>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (userInfo != null)
                {
                    System.Diagnostics.Debug.WriteLine($"CompleteUserInfoAsync: 服务端返回成功，IsFirstLogin={userInfo.IsFirstLogin}");
                    // 更新当前用户信息
                    CurrentUser = userInfo;
                    System.Diagnostics.Debug.WriteLine($"CompleteUserInfoAsync: 更新CurrentUser后，IsFirstLogin={CurrentUser?.IsFirstLogin}");
                    return userInfo;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CompleteUserInfoAsync: 反序列化失败");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"CompleteUserInfoAsync: 服务端返回错误，状态码={response.StatusCode}，内容={responseContent}");
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"完善用户信息异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 检查用户是否需要完善信息
    /// </summary>
    /// <returns>是否需要完善信息</returns>
    public bool RequiresUserInfoCompletion()
    {
        bool requires = CurrentUser?.IsFirstLogin == true;
        System.Diagnostics.Debug.WriteLine($"RequiresUserInfoCompletion: CurrentUser={CurrentUser?.Username}, IsFirstLogin={CurrentUser?.IsFirstLogin}, 需要完善信息={requires}");
        return requires;
    }

    /// <summary>
    /// 更新用户资料
    /// </summary>
    /// <param name="request">更新用户资料请求</param>
    /// <returns>是否更新成功</returns>
    public async Task<bool> UpdateUserProfileAsync(UpdateUserProfileRequest request)
    {
        try
        {
            if (CurrentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdateUserProfileAsync: 用户未登录");
                return false;
            }

            if (string.IsNullOrEmpty(CurrentAccessToken))
            {
                System.Diagnostics.Debug.WriteLine("UpdateUserProfileAsync: 访问令牌为空");
                return false;
            }

            // 使用专门的update-profile端点
            // 只发送后端支持的字段：Username和AvatarUrl
            UpdateProfileRequest updateProfileRequest = new()
            {
                Username = request.Username,
                AvatarUrl = request.AvatarUrl
                // 注意：Email和PhoneNumber暂不支持更新
            };

            string requestJson = JsonSerializer.Serialize(updateProfileRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            StringContent content = new(requestJson, Encoding.UTF8, "application/json");

            // 设置Authorization头
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

            System.Diagnostics.Debug.WriteLine($"UpdateUserProfileAsync: 发送更新用户资料请求到 {BuildApiUrl("update-profile")}");
            System.Diagnostics.Debug.WriteLine($"请求内容: {requestJson}");
            System.Diagnostics.Debug.WriteLine("注意：当前支持更新用户名和头像，Email和PhoneNumber暂不支持");

            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("update-profile"), content);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"UpdateUserProfileAsync: 响应状态码={response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"响应内容: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                UserInfo? updatedUser = JsonSerializer.Deserialize<UserInfo>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (updatedUser != null)
                {
                    System.Diagnostics.Debug.WriteLine("UpdateUserProfileAsync: 更新成功，更新本地用户信息");
                    CurrentUser = updatedUser;
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"更新用户资料异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="request">修改密码请求</param>
    /// <returns>是否修改成功</returns>
    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            if (CurrentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("ChangePasswordAsync: 用户未登录");
                return false;
            }

            if (string.IsNullOrEmpty(CurrentAccessToken))
            {
                System.Diagnostics.Debug.WriteLine("ChangePasswordAsync: 访问令牌为空");
                return false;
            }

            // 使用专门的change-password端点
            // 现在可以验证当前密码了
            string requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            StringContent content = new(requestJson, Encoding.UTF8, "application/json");

            // 设置Authorization头
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

            System.Diagnostics.Debug.WriteLine($"ChangePasswordAsync: 发送修改密码请求到 {BuildApiUrl("change-password")}");
            System.Diagnostics.Debug.WriteLine($"请求内容: {requestJson}");
            System.Diagnostics.Debug.WriteLine("注意：现在支持验证当前密码");

            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("change-password"), content);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"ChangePasswordAsync: 响应状态码={response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"响应内容: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("ChangePasswordAsync: 密码修改成功");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"修改密码异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 保存登录信息到本地存储
    /// </summary>
    /// <param name="loginResponse">登录响应</param>
    /// <returns>是否保存成功</returns>
    public async Task<bool> SaveLoginDataAsync(LoginResponse loginResponse)
    {
        try
        {
            PersistentLoginData loginData = new()
            {
                AccessToken = loginResponse.AccessToken,
                RefreshToken = loginResponse.RefreshToken,
                ExpiresAt = loginResponse.ExpiresAt,
                User = loginResponse.User,
                RequireDeviceBinding = loginResponse.RequireDeviceBinding,
                SavedAt = DateTime.UtcNow
            };

            string json = JsonSerializer.Serialize(loginData);
            return await _secureStorage.SetAsync(PersistentLoginKey, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存登录数据失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从本地存储加载登录信息
    /// </summary>
    /// <returns>持久化登录数据，如果不存在或无效则返回null</returns>
    public async Task<PersistentLoginData?> LoadLoginDataAsync()
    {
        try
        {
            string? json = await _secureStorage.GetAsync(PersistentLoginKey);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            PersistentLoginData? loginData = JsonSerializer.Deserialize<PersistentLoginData>(json);
            if (loginData == null)
            {
                return null;
            }

            // 检查数据是否过期（保存时间超过30天则认为无效）
            if (DateTime.UtcNow.Subtract(loginData.SavedAt).TotalDays > 30)
            {
                await ClearLoginDataAsync();
                return null;
            }

            return loginData;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载登录数据失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 清除本地存储的登录信息
    /// </summary>
    /// <returns>是否清除成功</returns>
    public async Task<bool> ClearLoginDataAsync()
    {
        try
        {
            return await _secureStorage.RemoveAsync(PersistentLoginKey);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"清除登录数据失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 自动验证本地存储的登录信息
    /// </summary>
    /// <returns>验证结果</returns>
    public async Task<AuthenticationResult> AutoAuthenticateAsync()
    {
        try
        {
            PersistentLoginData? loginData = await LoadLoginDataAsync();
            if (loginData == null)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "没有找到本地登录信息"
                };
            }

            // 检查AccessToken是否过期
            if (DateTime.UtcNow >= loginData.ExpiresAt)
            {
                // AccessToken已过期，尝试使用RefreshToken刷新
                AuthenticationResult refreshResult = await RefreshTokenAsync(loginData.RefreshToken);
                if (refreshResult.IsSuccess && refreshResult.LoginResponse != null)
                {
                    // 刷新成功，保存新的登录信息
                    await SaveLoginDataAsync(refreshResult.LoginResponse);
                    return SetAuthenticationState(refreshResult.LoginResponse);
                }
                else
                {
                    // 刷新失败，清除本地数据
                    await ClearLoginDataAsync();
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "令牌已过期且刷新失败，请重新登录"
                    };
                }
            }
            else
            {
                // AccessToken仍然有效，但需要从服务端获取最新的用户信息
                System.Diagnostics.Debug.WriteLine("AutoAuthenticateAsync: AccessToken有效，从服务端获取最新用户信息");

                // 设置Authorization头
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.AccessToken);

                try
                {
                    // 从服务端获取最新用户信息
                    HttpResponseMessage response = await _httpClient.GetAsync(BuildApiUrl("profile"));
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        UserInfo? latestUserInfo = JsonSerializer.Deserialize<UserInfo>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (latestUserInfo != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"AutoAuthenticateAsync: 获取到最新用户信息，IsFirstLogin={latestUserInfo.IsFirstLogin}");

                            // 使用最新的用户信息
                            LoginResponse loginResponse = new()
                            {
                                AccessToken = loginData.AccessToken,
                                RefreshToken = loginData.RefreshToken,
                                ExpiresAt = loginData.ExpiresAt,
                                User = latestUserInfo, // 使用从服务端获取的最新用户信息
                                RequireDeviceBinding = loginData.RequireDeviceBinding
                            };

                            return SetAuthenticationState(loginResponse, true); // 保存更新后的用户信息
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("AutoAuthenticateAsync: 无法获取最新用户信息，使用本地缓存");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AutoAuthenticateAsync: 获取用户信息失败: {ex.Message}，使用本地缓存");
                }

                // 如果无法获取最新信息，回退到使用本地信息
                LoginResponse fallbackResponse = new()
                {
                    AccessToken = loginData.AccessToken,
                    RefreshToken = loginData.RefreshToken,
                    ExpiresAt = loginData.ExpiresAt,
                    User = loginData.User,
                    RequireDeviceBinding = loginData.RequireDeviceBinding
                };

                return SetAuthenticationState(fallbackResponse, false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"自动认证失败: {ex.Message}");
            await ClearLoginDataAsync();
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"自动认证过程中发生错误: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>刷新结果</returns>
    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "刷新令牌为空"
                };
            }

            RefreshTokenRequest request = new() { RefreshToken = refreshToken };
            string json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("refresh-token"), content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                RefreshTokenResponse? refreshResponse = JsonSerializer.Deserialize<RefreshTokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (refreshResponse != null && !string.IsNullOrEmpty(refreshResponse.AccessToken))
                {
                    LoginResponse loginResponse = new()
                    {
                        AccessToken = refreshResponse.AccessToken,
                        RefreshToken = refreshResponse.RefreshToken,
                        ExpiresAt = refreshResponse.ExpiresAt,
                        User = CurrentUser ?? new UserInfo(),
                        RequireDeviceBinding = false
                    };

                    return new AuthenticationResult
                    {
                        IsSuccess = true,
                        LoginResponse = loginResponse
                    };
                }
            }

            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "令牌刷新失败"
            };
        }
        catch (Exception ex)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"令牌刷新过程中发生错误: {ex.Message}"
            };
        }
    }
}
