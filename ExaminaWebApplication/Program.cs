using System.Text;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 配置日志记录
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 确保控制台日志在生产环境中也能工作
if (builder.Environment.IsProduction())
{
    _ = builder.Logging.AddConsole(options =>
    {
        options.IncludeScopes = true;
    });
}

// Add services to the container.
builder.Services.AddControllersWithViews();

// 添加API控制器并配置JSON序列化选项
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 配置JSON序列化以支持CamelCase命名策略
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // 允许在反序列化时忽略大小写
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // 允许读取注释
        options.JsonSerializerOptions.ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip;
        // 允许尾随逗号
        options.JsonSerializerOptions.AllowTrailingCommas = true;
    });

// 配置MySQL数据库
string? connectionString = builder.Configuration.GetConnectionString("MySqlConnection")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.Parse("8.0.0-mysql"),
        mysqlOptions =>
        {
            _ = mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        }
    ));

// 注册服务
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IWeChatService, WeChatService>();
builder.Services.AddScoped<ISessionService, SessionService>();

// 添加后台服务
builder.Services.AddHostedService<SessionCleanupService>();

// 添加内存缓存
builder.Services.AddMemoryCache();

// 添加会话支持
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "ExaminaSession";
});

// 添加HttpClient
builder.Services.AddHttpClient();

// 配置混合认证策略
IConfigurationSection jwtSettings = builder.Configuration.GetSection("Jwt");
string secretKey = jwtSettings["SecretKey"] ?? "ExaminaSecretKey2024!@#$%^&*()_+1234567890";
string issuer = jwtSettings["Issuer"] ?? "ExaminaApp";
string audience = jwtSettings["Audience"] ?? "ExaminaUsers";

builder.Services.AddAuthentication(options =>
{
    // 默认使用JWT认证
    options.DefaultAuthenticateScheme = "MultiScheme";
    options.DefaultChallengeScheme = "MultiScheme";
})
.AddJwtBearer("JwtBearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
})
.AddCookie("Cookie", options =>
{
    options.LoginPath = "/Admin/Login";
    options.LogoutPath = "/Admin/Logout";
    options.AccessDeniedPath = "/Admin/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.Name = "ExaminaAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
})
.AddPolicyScheme("MultiScheme", "Multi Scheme", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // 根据请求路径决定使用哪种认证方案
        string path = context.Request.Path.Value?.ToLower() ?? "";

        // 学生API使用JWT认证
        if (path.StartsWith("/api/student/"))
        {
            return "JwtBearer";
        }

        // 管理员API使用Cookie认证
        if (path.StartsWith("/api/admin/"))
        {
            return "Cookie";
        }

        // 其他API路径根据Authorization头决定
        if (path.StartsWith("/api/"))
        {
            string authHeader = context.Request.Headers.Authorization.ToString();
            return authHeader.StartsWith("Bearer ") ? "JwtBearer" : "Cookie";
        }

        // 管理员和教师页面使用Cookie认证
        if (path.StartsWith("/admin/") || path.StartsWith("/teacher/"))
        {
            return "Cookie";
        }

        // 默认使用JWT认证
        return "JwtBearer";
    };
});

// 配置授权策略
builder.Services.AddAuthorization(options =>
{
    // 学生策略（仅JWT认证）
    options.AddPolicy("StudentPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("JwtBearer");
        _ = policy.RequireAuthenticatedUser();
        _ = policy.RequireClaim("Role", "Student");
    });

    // 教师策略（Cookie认证）
    options.AddPolicy("TeacherPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("Cookie");
        _ = policy.RequireAuthenticatedUser();
        _ = policy.RequireClaim("Role", "Teacher");
    });

    // 管理员策略（Cookie认证）
    options.AddPolicy("AdminPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("Cookie");
        _ = policy.RequireAuthenticatedUser();
        _ = policy.RequireClaim("Role", "Administrator");
    });

    // 教师或管理员策略
    options.AddPolicy("TeacherOrAdminPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("Cookie");
        _ = policy.RequireAuthenticatedUser();
        _ = policy.RequireAssertion(context =>
        {
            string? role = context.User.FindFirst("Role")?.Value;
            return role is "Teacher" or "Administrator";
        });
    });

    // API访问策略（JWT认证）
    options.AddPolicy("ApiPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("JwtBearer");
        _ = policy.RequireAuthenticatedUser();
    });
});

// 配置HTTPS重定向
builder.Services.AddHttpsRedirection(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // 开发环境配置 - 使用launchSettings.json中的HTTPS端口
        options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
        options.HttpsPort = 7125; // 开发环境HTTPS端口
    }
    else
    {
        // 生产环境配置 - 使用307临时重定向而不是308永久重定向
        // 这样可以避免客户端缓存重定向导致的问题
        options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
        options.HttpsPort = 443; // 生产环境标准HTTPS端口
    }
});

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        _ = policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("StudentFrontend", policy =>
    {
        _ = policy.WithOrigins("http://localhost:3000", "https://qiuzhenbd.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

WebApplication app = builder.Build();

// 获取日志记录器
ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();

// 记录应用程序启动信息
logger.LogInformation("=== Examina Web Application 正在启动 ===");
logger.LogInformation("环境: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("应用程序名称: {ApplicationName}", app.Environment.ApplicationName);
logger.LogInformation("内容根路径: {ContentRoot}", app.Environment.ContentRootPath);
logger.LogInformation("启动时间: {StartTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

// 初始化MySQL数据库
using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    ILogger<Program> dbLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        dbLogger.LogInformation("开始初始化MySQL数据库...");

        // 测试数据库连接
        bool canConnect = context.Database.CanConnect();
        dbLogger.LogInformation("数据库连接测试: {CanConnect}", canConnect ? "成功" : "失败");

        if (canConnect)
        {
            // 应用MySQL数据库迁移
            context.Database.Migrate();
            dbLogger.LogInformation("MySQL数据库迁移成功");

            // 初始化测试数据
            await InitializeTestDataAsync(context, dbLogger);
        }
        else
        {
            dbLogger.LogError("无法连接到MySQL数据库");
            throw new InvalidOperationException("数据库连接失败");
        }
    }
    catch (Exception ex)
    {
        dbLogger.LogError(ex, "MySQL数据库初始化失败: {ErrorMessage}", ex.Message);
        throw; // 重新抛出异常，因为MySQL数据库是必需的
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    _ = app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

// 添加API请求调试中间件
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        app.Logger.LogInformation("API请求详情: {Method} {Scheme}://{Host}{Path} IsHttps:{IsHttps}",
            context.Request.Method,
            context.Request.Scheme,
            context.Request.Host,
            context.Request.Path,
            context.Request.IsHttps);

        // 记录请求体内容（仅用于调试）
        if (context.Request.Method == "POST" && context.Request.ContentType?.Contains("application/json") == true)
        {
            context.Request.EnableBuffering();
            using StreamReader reader = new(context.Request.Body, leaveOpen: true);
            string requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            app.Logger.LogInformation("API请求体: {RequestBody}", requestBody);
        }
    }

    await next();
});

// 对API请求完全禁用HTTPS重定向，对非API请求使用标准HTTPS重定向
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"),
    appBuilder => appBuilder.UseHttpsRedirection());

app.UseRouting();

// 启用CORS
app.UseCors("AllowAll");

// 启用会话
app.UseSession();

// 启用认证和授权
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// 映射API控制器
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// 记录应用程序即将启动的信息
logger.LogInformation("=== 应用程序配置完成，即将启动监听 ===");
logger.LogInformation("监听地址将在启动后显示");

try
{
    logger.LogInformation("=== Examina Web Application 启动成功 ===");
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "=== 应用程序启动失败 ===");
    throw;
}
finally
{
    logger.LogInformation("=== Examina Web Application 已停止 ===");
}

/// <summary>
/// 初始化测试数据
/// </summary>
/// <param name="context">数据库上下文</param>
/// <param name="logger">日志记录器</param>
static async Task InitializeTestDataAsync(ApplicationDbContext context, ILogger logger)
{
    try
    {
        // 检查是否已有测试用户
        bool hasTestUser = await context.Users.AnyAsync(u => u.PhoneNumber == "13800138000");

        if (!hasTestUser)
        {
            // 创建测试学生用户
            User testStudent = new()
            {
                Username = "test_student",
                Email = "test@example.com",
                PhoneNumber = "13800138000",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), // 简单密码用于测试
                RealName = "测试学生",
                StudentId = "TEST001",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _ = context.Users.Add(testStudent);
            _ = await context.SaveChangesAsync();

            logger.LogInformation("已创建测试学生用户: 手机号 13800138000, 密码 123456");
        }
        else
        {
            logger.LogInformation("测试用户已存在，跳过创建");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "初始化测试数据失败");
    }
}
