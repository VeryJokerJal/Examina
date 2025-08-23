# 上机统考功能实现文档

## 概述

本文档描述了在Examina.Desktop项目中实现的上机统考功能。该功能基于现有的模拟考试架构，提供了与模拟考试一致的用户体验，同时针对正式考试的特殊需求进行了优化。

## 功能特点

### 1. 考试模式
- **考试类型**: 正式上机统考 (FormalExam)
- **时间控制**: 严格的倒计时模式，150分钟考试时长
- **题目配置**: 5道编程题（每道15分）+ 5道操作题（每道5分）
- **总分**: 100分，及格分数60分

### 2. 用户界面
- **规则对话框**: 专门的上机统考规则说明界面
- **工具栏窗口**: 与模拟考试一致的考试工具栏
- **权限控制**: 完整的用户权限验证机制

### 3. 考试流程
1. 用户点击"开始考试"按钮
2. 显示上机统考规则说明对话框
3. 用户确认后启动正式考试
4. 隐藏主窗口，显示考试工具栏
5. 开始倒计时，进入考试状态

## 实现架构

### 1. MVVM模式
遵循项目的MVVM架构模式：
- **Model**: 使用现有的StudentExamDto数据模型
- **ViewModel**: FormalExamRulesViewModel, ExamListViewModel, ExamToolbarViewModel
- **View**: FormalExamRulesDialog, ExamToolbarWindow

### 2. 服务层
- **IStudentExamService**: 获取考试列表和详情
- **IStudentFormalExamService**: 处理正式考试的启动和提交
- **ExamToolbarService**: 统一的考试工具栏服务
- **IAuthenticationService**: 用户认证和权限管理

### 3. 依赖注入
所有服务都通过依赖注入容器管理，确保松耦合和可测试性。

## 文件结构

### 新增文件
```
Examina/
├── ViewModels/Dialogs/
│   └── FormalExamRulesViewModel.cs          # 上机统考规则对话框ViewModel
├── Views/Dialogs/
│   ├── FormalExamRulesDialog.axaml          # 上机统考规则对话框界面
│   └── FormalExamRulesDialog.axaml.cs       # 上机统考规则对话框代码
├── Tests/
│   └── FormalExamFunctionalityTest.cs       # 功能测试类
└── Documentation/
    └── FormalExamImplementation.md          # 本文档
```

### 修改文件
```
Examina/
├── ViewModels/Pages/
│   └── ExamListViewModel.cs                 # 添加上机统考启动逻辑
└── Services/
    └── ExamToolbarService.cs                # 扩展正式考试支持
```

## 核心组件

### 1. FormalExamRulesViewModel
```csharp
public class FormalExamRulesViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, bool> ConfirmCommand { get; }
    public ReactiveCommand<Unit, bool> CancelCommand { get; }
    public FormalExamRulesInfo RulesInfo { get; }
}
```

**特点**:
- 包含上机统考特有的规则信息
- 提供确认和取消命令
- 支持响应式数据绑定

### 2. FormalExamRulesInfo
```csharp
public class FormalExamRulesInfo
{
    public int DurationMinutes { get; set; } = 150;
    public int TotalScore { get; set; } = 100;
    public int PassingScore { get; set; } = 60;
    public List<string> Rules { get; set; }
    public List<string> Notes { get; set; }
    public List<string> OperationGuide { get; set; }
    public List<string> Requirements { get; set; }
}
```

**特点**:
- 定义上机统考的基本信息
- 包含详细的规则、注意事项和操作指南
- 针对正式考试的严格要求

### 3. ExamListViewModel增强
```csharp
private async Task StartExamAsync(StudentExamDto exam)
{
    // 权限检查
    // 显示规则对话框
    // 启动正式考试
    // 创建工具栏窗口
}
```

**新增功能**:
- 集成规则对话框显示
- 完整的考试启动流程
- 工具栏窗口创建和配置
- 事件处理和错误管理

## 用户体验

### 1. 规则对话框
- **设计风格**: 与模拟考试规则对话框保持一致
- **内容区分**: 突出正式考试的严格要求
- **颜色标识**: 使用警告色标识重要注意事项
- **响应式布局**: 支持窗口缩放和滚动

### 2. 考试工具栏
- **功能一致**: 与模拟考试工具栏功能完全一致
- **时间显示**: 倒计时模式，严格控制考试时间
- **状态管理**: 准备中、进行中、即将结束、已结束等状态
- **事件处理**: 自动提交、手动提交、查看题目等

### 3. 权限控制
- **访问验证**: 检查用户是否有完整权限
- **考试权限**: 验证用户是否可以访问特定考试
- **错误提示**: 友好的错误信息和解决建议

## 技术特点

### 1. 代码复用
- 最大化复用现有的模拟考试代码
- 统一的ExamToolbarViewModel和ExamToolbarWindow
- 共享的服务层和数据模型

### 2. 类型安全
- 使用ExamType.FormalExam枚举值
- 强类型的数据模型和接口
- 编译时类型检查

### 3. 错误处理
- 完善的异常捕获和处理
- 详细的调试日志输出
- 用户友好的错误提示

### 4. 可扩展性
- 模块化的架构设计
- 易于添加新的考试类型
- 支持未来功能扩展

## 测试验证

### 1. 单元测试
- FormalExamRulesViewModel功能测试
- ExamType枚举支持测试
- 数据模型验证测试

### 2. 集成测试
- 完整的考试启动流程测试
- 服务层交互测试
- UI组件集成测试

### 3. 用户体验测试
- 规则对话框显示和交互
- 工具栏窗口功能验证
- 考试流程完整性测试

## 部署说明

### 1. 依赖项
- 所有必需的服务已在App.axaml.cs中注册
- 无需额外的NuGet包或外部依赖
- 兼容现有的项目配置

### 2. 配置要求
- 确保IStudentFormalExamService服务可用
- 验证后端API支持正式考试功能
- 检查用户权限配置

### 3. 兼容性
- 与现有模拟考试功能完全兼容
- 不影响其他考试模式的正常运行
- 支持现有的BenchSuite集成

## 未来扩展

### 1. 功能增强
- 添加考试过程中的实时监控
- 支持更多的考试配置选项
- 集成更详细的成绩分析

### 2. 用户体验优化
- 添加考试进度指示器
- 支持题目收藏和标记
- 提供更丰富的操作指南

### 3. 技术改进
- 优化网络请求和错误重试机制
- 添加离线模式支持
- 增强安全性和防作弊措施

## 总结

上机统考功能的实现成功地扩展了Examina.Desktop的考试能力，在保持与现有架构一致性的同时，提供了专门针对正式考试的优化体验。该实现遵循了项目的设计原则，具有良好的可维护性和可扩展性。
