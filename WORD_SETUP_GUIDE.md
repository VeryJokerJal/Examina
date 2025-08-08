# Word题目管理功能设置指南

本指南介绍如何设置和初始化Word题目管理功能。

## 1. 数据库迁移

### 生成迁移文件

在项目根目录下运行以下命令生成新的迁移文件：

```bash
# 进入项目目录
cd ExaminaWebApplication

# 生成迁移文件
dotnet ef migrations add AddWordOperationSystem --context ApplicationDbContext

# 查看迁移文件（可选）
dotnet ef migrations list
```

### 应用迁移

将迁移应用到数据库：

```bash
# 更新数据库
dotnet ef database update --context ApplicationDbContext

# 验证迁移是否成功（可选）
dotnet ef migrations list
```

## 2. 初始化Word操作点数据

### 方法一：通过API初始化（推荐）

启动应用程序后，访问以下API端点来初始化Word操作点数据：

```http
POST /api/word/operation/initialize
```

或者在浏览器中访问：
```
http://localhost:5000/api/word/operation/initialize
```

### 方法二：通过代码初始化

在应用程序启动时自动初始化，可以在 `Program.cs` 中添加以下代码：

```csharp
// 在 app.Run() 之前添加
using (var scope = app.Services.CreateScope())
{
    var wordOperationService = scope.ServiceProvider.GetRequiredService<WordOperationService>();
    await wordOperationService.InitializeWordOperationDataAsync();
}
```

### 方法三：通过管理界面初始化

1. 登录管理员账户
2. 进入试卷管理
3. 创建包含Word科目的试卷
4. 进入Word题目管理页面
5. 系统会自动检查并初始化操作点数据

## 3. 验证安装

### 检查数据库表

确认以下表已创建：

- `WordOperationPoints` - Word操作点主表
- `WordOperationParameters` - Word操作参数表
- `WordEnumTypes` - Word枚举类型表
- `WordEnumValues` - Word枚举值表
- `WordQuestionTemplates` - Word题目模板表
- `WordQuestionInstances` - Word题目实例表
- `WordQuestions` - Word题目表
- `WordQuestionOperationPoints` - Word题目操作点表

### 检查操作点数据

访问以下API端点检查操作点数据是否正确加载：

```http
GET /api/word/operation/points
GET /api/word/operation/enum-types
GET /api/word/operation/statistics
```

预期结果：
- 应该有14个操作点（段落操作第一类）
- 应该有7个枚举类型（字体、字形、字号、对齐方式、行距类型、边框线型、底纹图案）
- 应该有58个枚举值

### 检查页面访问

1. 创建包含Word科目的试卷
2. 进入试卷详情页面
3. 点击Word科目的"题目管理"按钮
4. 应该能正常访问Word题目管理页面
5. 左侧应该显示14个操作点，按类别分组
6. 右侧应该显示题目列表（初始为空）

## 4. 功能测试

### 创建Word题目

1. 在Word题目管理页面点击"创建Word题目"
2. 填写题目标题、描述和要求
3. 点击"创建题目"
4. 题目应该出现在题目列表中

### 添加操作点

1. 选择一个题目进行编辑
2. 点击"添加操作点"
3. 选择操作类型（如"设置段落的字体"）
4. 配置参数（如段落序号、字体类型）
5. 设置分值
6. 点击"添加操作点"
7. 操作点应该出现在题目详情中

### 参数配置测试

测试不同类型的参数配置：

- **枚举参数**：字体、字形、对齐方式等下拉选择
- **颜色参数**：文字颜色、边框颜色等颜色选择器
- **数值参数**：字号、缩进、间距等数字输入
- **布尔参数**：首字下沉启用等复选框

## 5. 故障排除

### 常见问题

**问题1：迁移失败**
```
解决方案：
1. 检查数据库连接字符串
2. 确保数据库服务正在运行
3. 检查是否有权限创建表
4. 查看详细错误信息并根据提示解决
```

**问题2：操作点数据初始化失败**
```
解决方案：
1. 检查WordOperationService是否正确注册
2. 确保数据库迁移已成功应用
3. 查看应用程序日志获取详细错误信息
4. 手动调用初始化API进行测试
```

**问题3：页面无法访问**
```
解决方案：
1. 确保Word科目已启用（IsEnabled = true）
2. 检查路由配置是否正确
3. 确认用户有访问权限
4. 查看浏览器控制台是否有JavaScript错误
```

**问题4：API调用失败**
```
解决方案：
1. 检查API控制器是否正确注册
2. 确认服务依赖注入配置正确
3. 查看服务器日志获取详细错误信息
4. 使用Postman等工具测试API端点
```

### 日志查看

应用程序日志位置：
- 开发环境：控制台输出
- 生产环境：根据配置的日志提供程序

关键日志关键词：
- `WordOperationService`
- `WordQuestionService`
- `Word题目管理`
- `InitializeWordOperationDataAsync`

## 6. 后续扩展

当前实现仅包含"第一类：段落操作（14项）"，后续可扩展：

- 第二类：页面设置（15项）
- 第三类：水印设置（4项）
- 第四类：项目符号与编号（1项）
- 第五类：表格操作（10项）
- 第六类：图形和图片设置（16项）
- 第七类：文本框设置（5项）
- 第八类：其他操作（2项）

扩展时需要：
1. 在WordOperationData中添加新的操作点定义
2. 在WordEnumData中添加相应的枚举类型和值
3. 更新前端界面支持新的参数类型
4. 生成新的数据库迁移
5. 更新初始化脚本

## 7. 技术支持

如遇到问题，请：
1. 查看本指南的故障排除部分
2. 检查应用程序日志
3. 查看数据库状态
4. 联系开发团队获取支持
