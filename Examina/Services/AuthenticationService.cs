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
                _currentUser = value;
                try
                {
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

        EnsureHttpClientConfiguration();
    }

    /// <summary>
    /// 确保HttpClient配置正确
    /// </summary>
    private void EnsureHttpClientConfiguration()
    {
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("https://qiuzhenbd.com");
        }
        else
        {
            if (_httpClient.BaseAddress.Scheme != "https")
            {
                UriBuilder builder = new(_httpClient.BaseAddress)
                {
                    Scheme = "https",
                    Port = _httpClient.BaseAddress.Port == 80 ? 443 : _httpClient.BaseAddress.Port
                };
                _httpClient.BaseAddress = builder.Uri;
            }
        }

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
    private string BuildApiUrl(string endpoint)
    {
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
            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                LoginResponse? loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, CreateJsonOptions());

                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    AuthenticationResult result = SetAuthenticationState(loginResponse);
                    return result;
                }
            }
            else
            {
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
            catch
            {
                // ignore
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
            string apiUrl = BuildApiUrl("send-sms");

            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if ((int)response.StatusCode is >= 300 and < 400)
            {
                string? location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location) && location.StartsWith("https://"))
                {
                    HttpResponseMessage redirectResponse = await _httpClient.PostAsync(location, content);
                    _ = await redirectResponse.Content.ReadAsStringAsync();
                    return redirectResponse.IsSuccessStatusCode;
                }
                else
                {
                    return false;
                }
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> VerifySmsCodeAsync(string phoneNumber, string code)
    {
        try
        {
            return await TryVerifyWithDedicatedEndpoint(phoneNumber, code);
        }
        catch
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
                    bool success = responseContent.Contains("\"success\":true") || responseContent.Contains("\"success\": true");
                    return success;
                }
            }
            else
            {
                return false;
            }
        }
        catch
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
            // ignore
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
            // ignore
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"服务器退出登录请求失败: {ex.Message}");
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
                List<DeviceInfo>? devices = JsonSerializer.Deserialize<List<DeviceInfo>>(responseContent, CreateJsonOptions());
                return devices ?? [];
            }
        }
        catch
        {
            // ignore
        }
        return [];
    }

    /// <summary>
    /// 设置认证状态
    /// </summary>
    private AuthenticationResult SetAuthenticationState(LoginResponse loginResponse, bool saveToLocal = true)
    {
        CurrentAccessToken = loginResponse.AccessToken;
        CurrentRefreshToken = loginResponse.RefreshToken;
        TokenExpiresAt = loginResponse.ExpiresAt;
        CurrentUser = loginResponse.User;
        IsAuthenticated = true;

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

        if (saveToLocal)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _ = await SaveLoginDataAsync(loginResponse);
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

        return result;
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

        _ = Task.Run(async () =>
        {
            try
            {
                _ = await ClearLoginDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清除本地数据失败: {ex.Message}");
            }
        });
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
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

            HttpResponseMessage response = await _httpClient.GetAsync(BuildApiUrl("test-auth"));
            _ = await response.Content.ReadAsStringAsync();
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

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("complete-info"), content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                UserInfo? userInfo = JsonSerializer.Deserialize<UserInfo>(responseContent, CreateJsonOptions());
                if (userInfo != null)
                {
                    CurrentUser = userInfo;
                    return userInfo;
                }
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
    public bool RequiresUserInfoCompletion()
    {
        return CurrentUser != null && CurrentUser.IsFirstLogin == true;
    }

    /// <summary>
    /// 更新用户资料
    /// </summary>
    public async Task<bool> UpdateUserProfileAsync(UpdateUserProfileRequest request)
    {
        try
        {
            if (CurrentUser == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(CurrentAccessToken))
            {
                return false;
            }

            UpdateProfileRequest updateProfileRequest = new()
            {
                Username = request.Username,
                RealName = request.RealName
            };

            string requestJson = JsonSerializer.Serialize(updateProfileRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            StringContent content = new(requestJson, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("update-profile"), content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                UserInfo? updatedUser = JsonSerializer.Deserialize<UserInfo>(responseContent, CreateJsonOptions());
                if (updatedUser != null)
                {
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
    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            if (CurrentUser == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(CurrentAccessToken))
            {
                return false;
            }

            string requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            StringContent content = new(requestJson, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);

            HttpResponseMessage response = await _httpClient.PostAsync(BuildApiUrl("change-password"), content);
            _ = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode;
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
    public async Task<AuthenticationResult> AutoAuthenticateAsync()
    {
        _isAutoAuthenticating = true;
        try
        {
            if (IsAuthenticated && CurrentUser != null && !string.IsNullOrEmpty(CurrentAccessToken))
            {
                if (!NeedsTokenRefresh)
                {
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
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "没有找到本地登录信息"
                };
            }

            if (DateTime.Now >= loginData.ExpiresAt)
            {
                AuthenticationResult refreshResult = await RefreshTokenAsync(loginData.RefreshToken);
                if (refreshResult.IsSuccess && refreshResult.LoginResponse != null)
                {
                    _ = await SaveLoginDataAsync(refreshResult.LoginResponse);
                    return SetAuthenticationState(refreshResult.LoginResponse);
                }
                else
                {
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
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.AccessToken);

                try
                {
                    HttpResponseMessage response = await _httpClient.GetAsync(BuildApiUrl("profile"));
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        UserInfo? latestUserInfo = JsonSerializer.Deserialize<UserInfo>(responseContent, CreateJsonOptions());

                        if (latestUserInfo != null)
                        {
                            LoginResponse loginResponse = new()
                            {
                                AccessToken = loginData.AccessToken,
                                RefreshToken = loginData.RefreshToken,
                                ExpiresAt = loginData.ExpiresAt,
                                User = latestUserInfo,
                                RequireDeviceBinding = loginData.RequireDeviceBinding
                            };

                            return SetAuthenticationState(loginResponse, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AutoAuthenticateAsync: 获取用户信息失败: {ex.Message}");
                }

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

            if (IsAuthenticated && CurrentUser != null && !string.IsNullOrEmpty(CurrentAccessToken))
            {
                return new AuthenticationResult
                {
                    IsSuccess = true,
                    AccessToken = CurrentAccessToken,
                    RefreshToken = CurrentRefreshToken,
                    ExpiresAt = TokenExpiresAt,
                    User = CurrentUser
                };
            }

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
        }
    }

    /// <summary>
    /// 刷新访问令牌（带参重载）
    /// </summary>
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
        if (_isAutoAuthenticating)
        {
            int waitCount = 0;
            while (_isAutoAuthenticating && waitCount < 100)
            {
                await Task.Delay(100);
                waitCount++;
            }
            if (_isAutoAuthenticating)
            {
                return null;
            }
        }

        if (NeedsTokenRefresh)
        {
            AuthenticationResult refreshResult = await RefreshTokenAsync();
            if (!refreshResult.IsSuccess)
            {
                return CurrentAccessToken;
            }
        }

        return CurrentAccessToken;
    }

    /// <summary>
    /// 设置认证令牌（用于外部登录成功后设置状态）
    /// </summary>
    public void SetAuthenticationToken(string accessToken, string refreshToken, UserInfo? user = null)
    {
        CurrentAccessToken = accessToken;
        CurrentRefreshToken = refreshToken;
        TokenExpiresAt = DateTime.Now.AddHours(24);
        CurrentUser = user;
        IsAuthenticated = true;

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentAccessToken);
    }

    /// <summary>
    /// 刷新用户信息
    /// </summary>
    public async Task<bool> RefreshUserInfoAsync()
    {
        try
        {
            string apiUrl = BuildApiUrl("profile");

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
                    bool hasChanges = _currentUser == null ||
                                     _currentUser.Username != userInfo.Username ||
                                     _currentUser.HasFullAccess != userInfo.HasFullAccess ||
                                     _currentUser.RealName != userInfo.RealName ||
                                     _currentUser.PhoneNumber != userInfo.PhoneNumber ||
                                     _currentUser.Role != userInfo.Role ||
                                     _currentUser.IsFirstLogin != userInfo.IsFirstLogin;

                    if (hasChanges)
                    {
                        CurrentUser = userInfo;
                    }
                    else
                    {
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
            ApiResponse<object>? apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, CreateJsonOptions());
            if (apiResponse != null)
            {
                return CreateErrorResult(apiResponse.Message, statusCode, apiResponse);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ParseErrorResponse: 解析错误响应失败 - {ex.Message}");
        }

        return CreateErrorResult("登录失败，请稍后重试", statusCode);
    }

    /// <summary>
    /// 创建错误结果
    /// </summary>
    private AuthenticationResult CreateErrorResult(string? message, System.Net.HttpStatusCode statusCode, ApiResponse<object>? apiResponse = null)
    {
        AuthenticationResult result = new()
        {
            IsSuccess = false,
            ErrorMessage = GetUserFriendlyErrorMessage(message, statusCode),
            ErrorType = DetermineErrorType(message, statusCode),
            CanRetry = IsRetryableError(statusCode)
        };

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

        return originalMessage.Contains("账户") && originalMessage.Contains("锁定") ? "账户已被锁定，请联系管理员" : originalMessage;
    }

    /// <summary>
    /// 确定错误类型
    /// </summary>
    private AuthenticationErrorType DetermineErrorType(string? message, System.Net.HttpStatusCode statusCode)
    {
        return IsDeviceLimitError(message)
            ? AuthenticationErrorType.DeviceLimitExceeded
            : statusCode switch
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
        return !string.IsNullOrEmpty(message) && message.Contains("设备") && (message.Contains("超出") || message.Contains("限制") || message.Contains("上限"));
    }

    /// <summary>
    /// 提取设备限制信息（示例）
    /// </summary>
    private DeviceLimitInfo? ExtractDeviceLimitInfo(ApiResponse<object>? apiResponse)
    {
        return new DeviceLimitInfo
        {
            CurrentDeviceCount = 5,
            MaxDeviceCount = 3,
            DeviceManagementUrl = "https://qiuzhenbd.com/DeviceManagement"
        };
    }

    /// <summary>
    /// 是否可重试
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
