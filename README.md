# 河北对口计算机新版上机考试系统

## 项目概述

这是一个基于FluentAvaloniaUI和ASP.NET Core的考试系统，支持多种登录方式，包括账号密码登录和微信扫码登录。

## 技术栈

### 前端 (Examina)
- **框架**: Avalonia UI (.NET 8)
- **UI库**: FluentAvaloniaUI 2.4.0
- **MVVM框架**: ReactiveUI + ReactiveUI.Fody
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **HTTP客户端**: Microsoft.Extensions.Http
- **架构模式**: MVVM with Reactive Programming

### 后端 (ExaminaWebApplication)
- **框架**: ASP.NET Core (.NET 9)
- **认证**: JWT Bearer Token
- **数据库**: Entity Framework Core (In-Memory)
- **密码加密**: BCrypt.Net-Next
- **API**: RESTful Web API

## 项目结构

```
Examina/
├── Examina/                    # 前端核心项目
│   ├── Services/              # 服务层
│   │   ├── IAuthenticationService.cs
│   │   └── AuthenticationService.cs
│   ├── ViewModels/            # 视图模型
│   │   ├── ViewModelBase.cs
│   │   ├── LoginViewModel.cs
│   │   └── MainViewModel.cs
│   ├── Views/                 # 视图
│   │   ├── LoginWindow.axaml
│   │   ├── MainWindow.axaml
│   │   └── MainView.axaml
│   └── App.axaml             # 应用程序入口
├── Examina.Desktop/           # 桌面应用程序启动项目
└── ExaminaWebApplication/     # 后端API项目
    ├── Controllers/           # API控制器
    │   └── AuthController.cs
    ├── Data/                  # 数据访问层
    │   └── ApplicationDbContext.cs
    ├── Models/                # 数据模型
    │   └── User.cs
    ├── Services/              # 服务层
    │   ├── IJwtService.cs
    │   └── JwtService.cs
    └── Program.cs            # 应用程序配置
```

## 功能特性

### 登录系统
1. **账号密码登录**
   - 支持用户名、邮箱、手机号登录
   - 密码使用BCrypt加密存储
   - JWT令牌认证

2. **微信扫码登录**
   - 二维码生成和刷新
   - 自动创建微信用户账号
   - 统一的JWT令牌管理

3. **自适应主题**
   - 自动跟随Windows系统明暗主题
   - 使用FluentAvalonia动态资源
   - 与Windows 11原生应用视觉一致

4. **现代MVVM架构**
   - 使用ReactiveUI.Fody自动属性更改通知
   - 编译时代码生成，提升性能
   - 响应式编程模式，代码更简洁

### 用户管理
- 用户信息存储和管理
- 首次登录标识
- 用户状态管理（激活/禁用）

### 安全特性
- JWT令牌认证
- 密码BCrypt加密
- CORS跨域支持
- 令牌验证和刷新

## 预设测试账号

系统预设了以下测试账号：

| 用户名 | 密码 | 角色 | 邮箱 |
|--------|------|------|------|
| admin | admin123 | 管理员 | admin@examina.com |
| student001 | 123456 | 学生 | student001@examina.com |
| teacher001 | 123456 | 教师 | teacher001@examina.com |

## 运行说明

### 启动后端服务
```bash
cd ExaminaWebApplication
dotnet run
```
后端服务将在 http://localhost:5117 启动

### 启动前端应用
```bash
cd Examina.Desktop
dotnet run
```

### 测试主题切换功能
使用提供的PowerShell脚本测试主题切换：
```powershell
# 以管理员身份运行PowerShell，然后执行：
.\测试主题切换.ps1
```

或者手动切换Windows主题：
1. 打开Windows设置（Win + I）
2. 导航到 **个性化** > **颜色**
3. 在"选择模式"中切换浅色/深色主题
4. 观察应用程序界面的实时变化

### API测试
可以使用以下PowerShell脚本测试登录API：
```powershell
$body = @{
    Username = "admin"
    Password = "admin123"
} | ConvertTo-Json

Invoke-WebRequest -Uri "http://localhost:5117/api/auth/login" -Method POST -ContentType "application/json" -Body $body
```

## API端点

### 认证相关
- `POST /api/auth/login` - 账号密码登录
- `POST /api/auth/wechat-login` - 微信扫码登录
- `GET /api/auth/validate` - 验证令牌
- `POST /api/auth/logout` - 用户登出
- `GET /api/auth/qrcode` - 获取登录二维码

## 配置说明

### JWT配置 (appsettings.json)
```json
{
  "Jwt": {
    "SecretKey": "ExaminaSecretKey2024!@#$%^&*()_+1234567890",
    "Issuer": "ExaminaApp",
    "Audience": "ExaminaUsers",
    "ExpirationMinutes": "480"
  }
}
```

## 开发说明

### 前端开发
- 使用ViewModelBase作为所有ViewModel的基类
- 实现INotifyPropertyChanged进行属性更改通知
- 使用依赖注入管理服务和ViewModels

### 后端开发
- 使用Entity Framework Core In-Memory数据库
- JWT令牌有效期为8小时
- 支持CORS跨域请求
- 统一的错误处理和日志记录

## 功能模块

### 已实现功能

#### 用户认证系统
- 账号密码登录
- 微信扫码登录（模拟）
- JWT令牌认证
- 用户权限管理

#### 试卷管理系统
- 试卷创建和编辑
- 科目配置管理
- 题目类型支持

#### Excel题目管理
- Excel操作点配置
- 题目模板管理
- 参数化题目生成

#### Windows题目管理
- Windows文件系统操作
- 操作点参数配置
- 题目自动生成

#### Word题目管理 ✨ 新增
- Word段落操作（14项知识点）
- 参数化配置界面
- 操作点分类管理
- 支持的操作类型：
  - 段落文字样式（字体、字号、字形、字间距、颜色）
  - 段落格式设置（对齐方式、缩进、行间距、首字下沉）
  - 段落间距与边框（间距、边框颜色/线型/线宽）
  - 段落背景设置（底纹）

### Word功能设置

#### 快速开始
1. 运行数据库迁移：
   ```bash
   cd ExaminaWebApplication
   dotnet ef migrations add AddWordOperationSystem
   dotnet ef database update
   ```

2. 初始化Word操作点数据：
   ```bash
   # 启动应用后访问
   POST /api/word/operation/initialize
   ```

3. 创建包含Word科目的试卷，进入Word题目管理页面

#### 详细设置指南
请参考 [WORD_SETUP_GUIDE.md](WORD_SETUP_GUIDE.md) 获取完整的设置和故障排除指南。

## 下一步开发计划

1. Word功能扩展（页面设置、水印、表格、图片等）
2. PowerPoint题目管理
3. C#编程题目管理
4. 考试执行引擎
5. 成绩统计分析
6. 数据库持久化优化

## 技术支持

如有问题，请联系开发团队或查看项目文档。
