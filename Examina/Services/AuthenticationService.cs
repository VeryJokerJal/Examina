using System.IdentityModel.Tokens.Jwt;
using Examina.Converters;
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

    /// <summary>
    /// 统一的JSON序列化选项配置
    /// </summary>
    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // 添加UserRole转换器
        options.Converters.Add(new UserRoleJsonConverter());

        return options;
    }

    /// <summary>
    /// 用户信息更新事件
    /// </summary>
    public event EventHandler<UserInfo?>? UserInfoUpdated;
    private const string BaseUrl = "https://qiuzhenbd.com";
    private const string StudentAuthUrl = "student/auth";
    private const string PersistentLoginKey = "persistent_login_data";

    private UserInfo? _currentUser;
    private bool _isAutoAuthenticating = false;

    public bool IsAuthenticated { get; private set; }
    public UserInfo? CurrentUser
    {
        get => _currentUser;
        private set
        {
            if (_currentUser != value)
            {
                UserInfo? previousUser = _currentUser;
                _currentUser = value;

                // 安全地触发用户信息更新事件
                try
                {
                    System.Diagnostics.Debug.WriteLine($"AuthenticationService: CurrentUser更新 - 从 {previousUser?.Username ?? "null"} 到 {value?.Username ?? "null"}");
                    UserInfoUpdated?.Invoke(this, value);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AuthenticationService: 触发UserInfoUpdated事件时发生错误: {ex.Message}");
                }
            }
        }
    }
    public string? CurrentAccessToken { get; private set; }
    public string? CurrentRefreshToken { get; private set; }
    public DateTime? TokenExpiresAt { get; private set; }

    public bool NeedsTokenRefresh =>
        TokenExpiresAt.HasValue &&
        TokenExpiresAt.Value.Subtract(DateTime.Now).TotalMinutes <= 30;

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
        // 如果基础地址未设置，则设置它（但优先使用依赖注入配置的地址）
        if (_httpClient.BaseAddress == null)
        {
            // 只设置域名部分，路径在BuildApiUrl中构建
            _httpClient.BaseAddress = new Uri("https://qiuzhenbd.com");
        }
        else
        {
            // 验证BaseAddress是否使用HTTPS
            if (_httpClient.BaseAddress.Scheme != "https")
            {
                // 强制使用HTTPS
                UriBuilder builder = new(_httpClient.BaseAddress)
                {
                    Scheme = "https",
                    Port = _httpClient.BaseAddress.Port == 80 ? 443 : _httpClient.BaseAddress.Port
                };
                _httpClient.BaseAddress = builder.Uri;
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
                LoginResponse? loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, CreateJsonOptions());

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
            System.Diagnostics.Debug.WriteLine($"AuthenticationService: 开始短信验证码登录 - 手机号: {phoneNumber}");

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

            string apiUrl = BuildApiUrl("sms-login");
            System.Diagnostics.Debug.WriteLine($"AuthenticationService: 准备调用短信登录API - URL: {apiUrl}");

            // 使用新的短信登录端点
            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"AuthenticationService: 短信登录API响应 - 状态码: {response.StatusCode}, 内容长度: {responseContent.Length}");

            if (response.IsSuccessStatusCode)
            {
                LoginResponse? loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, CreateJsonOptions());

                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    System.Diagnostics.Debug.WriteLine($"AuthenticationService: 短信登录成功，准备设置认证状态 - 用户: {loginResponse.User?.Username}");
                    AuthenticationResult result = SetAuthenticationState(loginResponse);
                    System.Diagnostics.Debug.WriteLine($"AuthenticationService: 认证状态设置完成 - IsAuthenticated: {IsAuthenticated}");
                    return result;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AuthenticationService: 短信登录响应无效 - LoginResponse为null或AccessToken为空");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"AuthenticationService: 短信登录API调用失败 - 状态码: {response.StatusCode}, 响应: {responseContent}");
                return ParseErrorResponse(responseContent, response.StatusCode);
            }

            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "短信验证码登录失败",
                ErrorType = AuthenticationErrorType.Unknown
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AuthenticationService: 短信登录异常 - {ex.Message}");
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

            try
            {
                if (response.IsSuccessStatusCode)
                {
                    LoginResponse? loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, CreateJsonOptions());

                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                    {
                        return SetAuthenticationState(loginResponse);
                    }
                }
                else
                {
                    ApiResponse<object>? apiError = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, CreateJsonOptions());
                    string errorMessage = apiError?.Message ?? "微信登录失败";
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = errorMessage
                    };
                }
            }
            catch (Exception)
            {

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

            StringContent content = new(json, Encoding.UTF8, "application/json");

            // 使用改进的URL构建方法
            string apiUrl = BuildApiUrl("send-sms");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

            // 添加详细的响应日志
            string responseContent = await response.Content.ReadAsStringAsync();

            // 如果收到重定向响应，手动处理重定向到HTTPS
            if ((int)response.StatusCode is >= 300 and < 400)
            {
                string? location = response.Headers.Location?.ToString();

                if (!string.IsNullOrEmpty(location) && location.StartsWith("https://"))
                {
                    // 手动发送HTTPS请求
                    HttpResponseMessage redirectResponse = await _httpClient.PostAsync(location, content);
                    string redirectResponseContent = await redirectResponse.Content.ReadAsStringAsync();

                    return redirectResponse.IsSuccessStatusCode;
                }
                else
                {
                    return false;
                }
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> VerifySmsCodeAsync(string phoneNumber, string code)
    {
        try
        {
            // 只使用专用的verify-sms端点，避免使用可能创建新用户的sms-login端点
            return await TryVerifyWithDedicatedEndpoint(phoneNumber, code);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> TryVerifyWithDedicatedEndpoint(string phoneNumber, string code)
    {
        try
        {
            var request = new { PhoneNumber = phoneNumber, Code = code };
            string json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            StringContent content = new(json, Encoding.UTF8, "application/json");
            string apiUrl = BuildApiUrl("verify-sms");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    // 尝试解析为包含success字段的响应
                    Dictionary<string, JsonElement>? result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null && result.ContainsKey("success"))
                    {
                        bool success = result["success"].GetBoolean();
                        return success;
                    }

                    return false;
                }
                catch (JsonException)
                {
                    // 尝试简单的字符串检查
                    bool success = responseContent.Contains("\"success\":true") || responseContent.Contains("\"success\": true");
                    return success;
                }
            }
            else
            {
                return false;
            }
        }
        catch (Exception)
        {
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
                RefreshTokenResponse? refreshResponse = JsonSerializer.Deserialize<RefreshTokenResponse>(responseContent, CreateJsonOptions());

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
        System.Diagnostics.Debug.WriteLine("开始执行退出登录流程");

        try
        {
            // 如果有访问令牌，通知服务器退出登录
            if (!string.IsNullOrEmpty(CurrentAccessToken))
            {
                System.Diagnostics.Debug.WriteLine("向服务器发送退出登录请求");
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);
                _ = await _httpClient.PostAsync(BuildApiUrl("logout"), null);
                System.Diagnostics.Debug.WriteLine("服务器退出登录请求完成");
            }
        }
        catch (Exception ex)
        {
            // 忽略登出错误，但记录日志
            System.Diagnostics.Debug.WriteLine($"服务器退出登录请求失败: {ex.Message}");
        }
        finally
        {
            // 无论服务器请求是否成功，都要清除本地状态
            System.Diagnostics.Debug.WriteLine("开始清除本地认证状态");
            ClearAuthenticationState();
            System.Diagnostics.Debug.WriteLine("退出登录流程完成");
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
                List<DeviceInfo>? devices = JsonSerializer.Deserialize<List<DeviceInfo>>(responseContent, CreateJsonOptions());
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
        System.Diagnostics.Debug.WriteLine($"AuthenticationService: 开始设置认证状态 - 用户: {loginResponse.User?.Username}, AccessToken长度: {loginResponse.AccessToken?.Length ?? 0}");

        CurrentAccessToken = loginResponse.AccessToken;
        CurrentRefreshToken = loginResponse.RefreshToken;
        TokenExpiresAt = loginResponse.ExpiresAt;
        CurrentUser = loginResponse.User;
        IsAuthenticated = true;

        System.Diagnostics.Debug.WriteLine($"AuthenticationService: 认证状态已设置 - IsAuthenticated: {IsAuthenticated}, CurrentUser: {CurrentUser?.Username ?? "null"}");

        // 设置HTTP客户端的认证头
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

        System.Diagnostics.Debug.WriteLine($"AuthenticationService: HTTP客户端Authorization头已设置");

        // 异步保存到本地存储（不等待结果，避免阻塞UI）
        if (saveToLocal)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    bool saveResult = await SaveLoginDataAsync(loginResponse);
                    System.Diagnostics.Debug.WriteLine($"AuthenticationService: 本地存储保存结果: {saveResult}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AuthenticationService: 保存登录数据到本地失败: {ex.Message}");
                }
            });
        }

        AuthenticationResult result = new()
        {
            IsSuccess = true,
            AccessToken = CurrentAccessToken,
            RefreshToken = CurrentRefreshToken,
            ExpiresAt = TokenExpiresAt,
            User = CurrentUser,
            RequireDeviceBinding = loginResponse.RequireDeviceBinding
        };

        System.Diagnostics.Debug.WriteLine($"AuthenticationService: SetAuthenticationState完成，返回成功结果");
        return result;
    }

    /// <summary>
    /// 清除认证状态
    /// </summary>
    private void ClearAuthenticationState()
    {
        System.Diagnostics.Debug.WriteLine("开始清除认证状态");

        // 清除内存中的认证信息
        CurrentAccessToken = null;
        CurrentRefreshToken = null;
        TokenExpiresAt = null;
        CurrentUser = null;
        IsAuthenticated = false;
        _httpClient.DefaultRequestHeaders.Authorization = null;

        System.Diagnostics.Debug.WriteLine("内存中的认证信息已清除");

        // 异步清除本地存储的数据（不等待结果，避免阻塞UI）
        _ = Task.Run(async () =>
        {
            try
            {
                // 清除登录数据
                bool loginDataCleared = await ClearLoginDataAsync();
                System.Diagnostics.Debug.WriteLine($"本地登录数据清除结果: {loginDataCleared}");

                // 可以在这里添加其他缓存清除逻辑
                // 例如：清除用户偏好设置、临时文件等

                System.Diagnostics.Debug.WriteLine("所有本地数据清除完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清除本地数据失败: {ex.Message}");
            }
        });
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
    /// 测试JWT令牌验证
    /// </summary>
    public async Task<bool> TestAuthAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentAccessToken))
            {
                System.Diagnostics.Debug.WriteLine("[测试认证] CurrentAccessToken为空");
                return false;
            }

            // 设置Authorization头
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

            HttpResponseMessage response = await _httpClient.GetAsync(BuildApiUrl("test-auth"));
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"[测试认证] 响应状态: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"[测试认证] 响应内容: {responseContent}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[测试认证] 异常: {ex.Message}");
            return false;
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
                UserInfo? userInfo = JsonSerializer.Deserialize<UserInfo>(responseContent, CreateJsonOptions());

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
        // 如果没有当前用户信息，则不需要完善信息（应该重新登录）
        if (CurrentUser == null)
        {
            System.Diagnostics.Debug.WriteLine($"RequiresUserInfoCompletion: CurrentUser为null，不需要完善信息");
            return false;
        }

        bool requires = CurrentUser.IsFirstLogin == true;
        System.Diagnostics.Debug.WriteLine($"RequiresUserInfoCompletion: CurrentUser={CurrentUser.Username}, IsFirstLogin={CurrentUser.IsFirstLogin}, 需要完善信息={requires}");
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
            // 发送后端支持的字段：Username 和 RealName
            UpdateProfileRequest updateProfileRequest = new()
            {
                Username = request.Username,
                RealName = request.RealName
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
            System.Diagnostics.Debug.WriteLine($"Authorization头: Bearer {CurrentAccessToken?[..Math.Min(10, CurrentAccessToken.Length)]}...");
            System.Diagnostics.Debug.WriteLine("注意：当前支持更新用户名和真实姓名，Email和PhoneNumber暂不支持");

            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("update-profile"), content);
            string responseContent = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"UpdateUserProfileAsync: 响应状态码={response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"响应内容: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                UserInfo? updatedUser = JsonSerializer.Deserialize<UserInfo>(responseContent, CreateJsonOptions());

                if (updatedUser != null)
                {
                    System.Diagnostics.Debug.WriteLine("UpdateUserProfileAsync: 更新成功，更新本地用户信息");
                    System.Diagnostics.Debug.WriteLine($"更新后的用户信息: Username={updatedUser.Username}, RealName={updatedUser.RealName}");
                    CurrentUser = updatedUser;
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("UpdateUserProfileAsync: 反序列化用户信息失败");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"UpdateUserProfileAsync: 请求失败，状态码={response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"错误响应: {responseContent}");
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
                SavedAt = DateTime.Now
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
            if (DateTime.Now.Subtract(loginData.SavedAt).TotalDays > 30)
            {
                _ = await ClearLoginDataAsync();
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
        _isAutoAuthenticating = true;
        try
        {
            System.Diagnostics.Debug.WriteLine("AutoAuthenticateAsync: 开始自动认证");

            // 如果当前已经有认证状态，先检查是否有效
            if (IsAuthenticated && CurrentUser != null && !string.IsNullOrEmpty(CurrentAccessToken))
            {
                System.Diagnostics.Debug.WriteLine("AutoAuthenticateAsync: 当前已有认证状态，检查令牌有效性");

                // 检查令牌是否需要刷新
                if (!NeedsTokenRefresh)
                {
                    System.Diagnostics.Debug.WriteLine("AutoAuthenticateAsync: 当前认证状态有效，无需重新认证");
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

            PersistentLoginData? loginData = await LoadLoginDataAsync();
            if (loginData == null)
            {
                System.Diagnostics.Debug.WriteLine("AutoAuthenticateAsync: 没有找到本地登录信息");
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "没有找到本地登录信息"
                };
            }

            // 检查AccessToken是否过期
            if (DateTime.Now >= loginData.ExpiresAt)
            {
                // AccessToken已过期，尝试使用RefreshToken刷新
                AuthenticationResult refreshResult = await RefreshTokenAsync(loginData.RefreshToken);
                if (refreshResult.IsSuccess && refreshResult.LoginResponse != null)
                {
                    // 刷新成功，保存新的登录信息
                    _ = await SaveLoginDataAsync(refreshResult.LoginResponse);
                    return SetAuthenticationState(refreshResult.LoginResponse);
                }
                else
                {
                    // 刷新失败，清除本地数据
                    _ = await ClearLoginDataAsync();
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
                        UserInfo? latestUserInfo = JsonSerializer.Deserialize<UserInfo>(responseContent, CreateJsonOptions());

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

            // 如果当前已有有效的认证状态，不要清除它
            if (IsAuthenticated && CurrentUser != null && !string.IsNullOrEmpty(CurrentAccessToken))
            {
                System.Diagnostics.Debug.WriteLine("AutoAuthenticateAsync: 虽然自动认证失败，但当前认证状态仍然有效，保持现有状态");
                return new AuthenticationResult
                {
                    IsSuccess = true,
                    AccessToken = CurrentAccessToken,
                    RefreshToken = CurrentRefreshToken,
                    ExpiresAt = TokenExpiresAt,
                    User = CurrentUser
                };
            }

            // 只有在没有有效认证状态时才清除本地数据
            _ = await ClearLoginDataAsync();
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"自动认证过程中发生错误: {ex.Message}"
            };
        }
        finally
        {
            _isAutoAuthenticating = false;
            System.Diagnostics.Debug.WriteLine("AutoAuthenticateAsync: 自动认证流程结束");
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
                RefreshTokenResponse? refreshResponse = JsonSerializer.Deserialize<RefreshTokenResponse>(responseContent, CreateJsonOptions());

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

    /// <summary>
    /// 获取当前访问令牌
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        System.Diagnostics.Debug.WriteLine($"GetAccessTokenAsync: 开始获取访问令牌 - IsAuthenticated: {IsAuthenticated}, CurrentAccessToken长度: {CurrentAccessToken?.Length ?? 0}");

        // 如果正在进行自动认证，等待完成
        if (_isAutoAuthenticating)
        {
            System.Diagnostics.Debug.WriteLine("GetAccessTokenAsync: 等待自动认证完成...");

            // 等待自动认证完成，最多等待10秒
            int waitCount = 0;
            while (_isAutoAuthenticating && waitCount < 100)
            {
                await Task.Delay(100);
                waitCount++;
            }

            if (_isAutoAuthenticating)
            {
                System.Diagnostics.Debug.WriteLine("GetAccessTokenAsync: 等待自动认证超时");
                return null;
            }

            System.Diagnostics.Debug.WriteLine("GetAccessTokenAsync: 自动认证已完成");
        }

        // 如果需要刷新令牌，先尝试刷新
        if (NeedsTokenRefresh)
        {
            System.Diagnostics.Debug.WriteLine("GetAccessTokenAsync: 令牌需要刷新，开始刷新...");
            AuthenticationResult refreshResult = await RefreshTokenAsync();
            if (!refreshResult.IsSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"GetAccessTokenAsync: 令牌刷新失败: {refreshResult.ErrorMessage}");
                // 刷新失败，返回当前令牌（可能已过期）
                return CurrentAccessToken;
            }
            System.Diagnostics.Debug.WriteLine("GetAccessTokenAsync: 令牌刷新成功");
        }

        System.Diagnostics.Debug.WriteLine($"GetAccessTokenAsync: 返回访问令牌 - 长度: {CurrentAccessToken?.Length ?? 0}, 是否为空: {string.IsNullOrEmpty(CurrentAccessToken)}");
        return CurrentAccessToken;
    }

    /// <summary>
    /// 设置认证令牌（用于外部登录成功后设置状态）
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="user">用户信息</param>
    public void SetAuthenticationToken(string accessToken, string refreshToken, UserInfo? user = null)
    {
        CurrentAccessToken = accessToken;
        CurrentRefreshToken = refreshToken;
        TokenExpiresAt = DateTime.Now.AddHours(24); // 默认24小时过期
        CurrentUser = user;
        IsAuthenticated = true;

        // 设置HTTP客户端的认证头
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

        System.Diagnostics.Debug.WriteLine($"已设置认证令牌，用户: {user?.Username ?? "未知"}");
    }

    /// <summary>
    /// 刷新用户信息
    /// </summary>
    public async Task<bool> RefreshUserInfoAsync()
    {
        try
        {
            // 获取最新的用户信息
            string apiUrl = BuildApiUrl("profile");

            // 设置认证头
            if (!string.IsNullOrEmpty(CurrentAccessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);
            }

            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                UserInfo? userInfo = JsonSerializer.Deserialize<UserInfo>(responseContent, CreateJsonOptions());

                if (userInfo != null)
                {
                    // 检查用户信息是否真的有变化，避免不必要的事件触发
                    bool hasChanges = _currentUser == null ||
                                     _currentUser.Username != userInfo.Username ||
                                     _currentUser.HasFullAccess != userInfo.HasFullAccess ||
                                     _currentUser.RealName != userInfo.RealName ||
                                     _currentUser.PhoneNumber != userInfo.PhoneNumber ||
                                     _currentUser.Role != userInfo.Role ||
                                     _currentUser.IsFirstLogin != userInfo.IsFirstLogin;

                    if (hasChanges)
                    {
                        System.Diagnostics.Debug.WriteLine($"AuthenticationService.RefreshUserInfoAsync: 用户信息有变化，更新CurrentUser");
                        CurrentUser = userInfo;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"AuthenticationService.RefreshUserInfoAsync: 用户信息无变化，跳过CurrentUser更新");
                        // 直接更新内部字段，不触发事件
                        _currentUser = userInfo;
                    }
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 解析服务器错误响应
    /// </summary>
    private AuthenticationResult ParseErrorResponse(string responseContent, System.Net.HttpStatusCode statusCode)
    {
        try
        {
            // 尝试解析为API响应格式
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, CreateJsonOptions());

            if (apiResponse != null)
            {
                return CreateErrorResult(apiResponse.Message, statusCode, apiResponse);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ParseErrorResponse: 解析错误响应失败 - {ex.Message}");
        }

        // 如果解析失败，返回通用错误
        return CreateErrorResult("登录失败，请稍后重试", statusCode);
    }

    /// <summary>
    /// 创建错误结果
    /// </summary>
    private AuthenticationResult CreateErrorResult(string? message, System.Net.HttpStatusCode statusCode, ApiResponse<object>? apiResponse = null)
    {
        var result = new AuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = GetUserFriendlyErrorMessage(message, statusCode),
            ErrorType = DetermineErrorType(message, statusCode),
            CanRetry = IsRetryableError(statusCode)
        };

        // 检查是否是设备数量超限错误
        if (IsDeviceLimitError(message))
        {
            result.ErrorType = AuthenticationErrorType.DeviceLimitExceeded;
            result.DeviceLimitInfo = ExtractDeviceLimitInfo(apiResponse);
            result.SuggestedAction = "请在设备管理中解绑不需要的设备，或联系管理员增加设备限制";
            result.CanRetry = false;
        }

        return result;
    }

    /// <summary>
    /// 获取用户友好的错误消息
    /// </summary>
    private string GetUserFriendlyErrorMessage(string? originalMessage, System.Net.HttpStatusCode statusCode)
    {
        if (string.IsNullOrEmpty(originalMessage))
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => "用户名或密码错误",
                System.Net.HttpStatusCode.Forbidden => "账户被锁定或无权限访问",
                System.Net.HttpStatusCode.TooManyRequests => "请求过于频繁，请稍后重试",
                System.Net.HttpStatusCode.InternalServerError => "服务器内部错误，请稍后重试",
                System.Net.HttpStatusCode.BadGateway => "网络连接异常，请检查网络设置",
                System.Net.HttpStatusCode.ServiceUnavailable => "服务暂时不可用，请稍后重试",
                System.Net.HttpStatusCode.GatewayTimeout => "网络超时，请检查网络连接",
                _ => "登录失败，请稍后重试"
            };
        }

        // 处理特定的错误消息
        if (originalMessage.Contains("验证码") && originalMessage.Contains("错误"))
        {
            return "短信验证码错误，请重新输入";
        }

        if (originalMessage.Contains("验证码") && originalMessage.Contains("过期"))
        {
            return "短信验证码已过期，请重新获取";
        }

        if (originalMessage.Contains("设备") && originalMessage.Contains("超出"))
        {
            return "绑定设备数量已达上限";
        }

        if (originalMessage.Contains("账户") && originalMessage.Contains("锁定"))
        {
            return "账户已被锁定，请联系管理员";
        }

        return originalMessage;
    }

    /// <summary>
    /// 确定错误类型
    /// </summary>
    private AuthenticationErrorType DetermineErrorType(string? message, System.Net.HttpStatusCode statusCode)
    {
        if (IsDeviceLimitError(message))
        {
            return AuthenticationErrorType.DeviceLimitExceeded;
        }

        return statusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => AuthenticationErrorType.InvalidCredentials,
            System.Net.HttpStatusCode.Forbidden => AuthenticationErrorType.AccountLocked,
            System.Net.HttpStatusCode.InternalServerError => AuthenticationErrorType.ServerError,
            System.Net.HttpStatusCode.BadGateway => AuthenticationErrorType.NetworkError,
            System.Net.HttpStatusCode.ServiceUnavailable => AuthenticationErrorType.ServerError,
            System.Net.HttpStatusCode.GatewayTimeout => AuthenticationErrorType.NetworkError,
            _ => AuthenticationErrorType.Unknown
        };
    }

    /// <summary>
    /// 检查是否是设备限制错误
    /// </summary>
    private bool IsDeviceLimitError(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return false;

        return message.Contains("设备") && (message.Contains("超出") || message.Contains("限制") || message.Contains("上限"));
    }

    /// <summary>
    /// 提取设备限制信息
    /// </summary>
    private DeviceLimitInfo? ExtractDeviceLimitInfo(ApiResponse<object>? apiResponse)
    {
        // 这里可以根据实际的API响应格式来提取设备限制信息
        // 目前返回默认值，实际使用时需要根据服务器响应格式调整
        return new DeviceLimitInfo
        {
            CurrentDeviceCount = 5, // 从API响应中提取
            MaxDeviceCount = 3,     // 从API响应中提取
            DeviceManagementUrl = "https://qiuzhenbd.com/DeviceManagement"
        };
    }

    /// <summary>
    /// 检查是否是可重试的错误
    /// </summary>
    private bool IsRetryableError(System.Net.HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.InternalServerError => true,
            System.Net.HttpStatusCode.BadGateway => true,
            System.Net.HttpStatusCode.ServiceUnavailable => true,
            System.Net.HttpStatusCode.GatewayTimeout => true,
            System.Net.HttpStatusCode.TooManyRequests => true,
            _ => false
        };
    }
}
