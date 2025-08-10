# ExamLab - 试卷制作系统

基于WinUI3和ReactiveUI的现代化试卷制作系统，支持Windows、C#、PowerPoint、Excel、Word等多种模块的题目配置和操作点管理。

## 项目特点

### 🎯 核心功能
- **多模块支持**：Windows操作、C#编程、PowerPoint、Excel、Word
- **灵活配置**：支持题目描述和评分题目描述两种类型
- **操作点管理**：详细的操作点配置和参数设置
- **PPT专业支持**：39个详细的PowerPoint知识点配置

### 🏗️ 技术架构
- **框架**：WinUI3 + .NET 8
- **MVVM**：ReactiveUI + ReactiveUI.Fody
- **响应式编程**：Reactive Extensions
- **数据绑定**：双向绑定和命令模式

### 📋 模块详情

#### Windows模块（9种操作类型）
1. 快捷创建
2. 创建操作
3. 删除操作
4. 复制操作
5. 移动操作
6. 重命名操作
7. 快捷方式操作
8. 文件属性修改操作
9. 复制重命名操作

#### C#模块
- 程序参数输入配置
- 程序控制台输出配置
- 支持代码执行结果验证

#### PowerPoint模块（39个知识点）
**幻灯片操作类（16个）**
- 设置幻灯片版式
- 删除幻灯片
- 插入幻灯片
- 设置幻灯片字体
- 幻灯片切换效果
- 幻灯片切换方式
- 等等...

**文字与字体设置类（14个）**
- 插入文本内容
- 设置文本字号
- 设置文本颜色
- 设置文本字形
- 元素位置设置
- 元素尺寸设置
- 等等...

**背景样式与设计类（1个）**
- 应用主题

**母版与主题设置类（1个）**
- 设置幻灯片背景

**其他设置类（7个）**
- 表格内容和样式
- SmartArt配置
- 动画设置
- 段落行距
- 背景样式
- 等等...

#### Excel模块
- 创建工作簿
- 格式化单元格
- 插入图表
- 创建公式
- 数据筛选

#### Word模块
- 创建文档
- 格式化文本
- 插入表格
- 插入图片
- 创建页眉页脚

## 项目结构

```
ExamLab/
├── Models/                     # 数据模型
│   ├── Exam.cs                # 试卷模型
│   ├── ExamModule.cs          # 模块模型
│   ├── Question.cs            # 题目模型
│   ├── OperationPoint.cs      # 操作点模型
│   ├── ConfigurationParameter.cs # 配置参数模型
│   └── PowerPointKnowledgeConfig.cs # PPT知识点配置
├── ViewModels/                # 视图模型
│   ├── ViewModelBase.cs       # ViewModel基类
│   ├── MainWindowViewModel.cs # 主窗口ViewModel
│   ├── ModuleViewModelBase.cs # 模块ViewModel基类
│   ├── PowerPointModuleViewModel.cs # PPT模块ViewModel
│   ├── WindowsModuleViewModel.cs # Windows模块ViewModel
│   ├── CSharpModuleViewModel.cs # C#模块ViewModel
│   ├── ExcelModuleViewModel.cs # Excel模块ViewModel
│   └── WordModuleViewModel.cs # Word模块ViewModel
├── Views/                     # 视图
│   ├── PowerPointModuleView.xaml # PPT模块视图
│   ├── WindowsModuleView.xaml # Windows模块视图
│   ├── CSharpModuleView.xaml  # C#模块视图
│   ├── ExcelModuleView.xaml   # Excel模块视图
│   ├── WordModuleView.xaml    # Word模块视图
│   └── OperationPointConfigView.xaml # 操作点配置视图
├── Services/                  # 服务层
│   ├── PowerPointKnowledgeService.cs # PPT知识点服务
│   ├── ValidationService.cs  # 验证服务
│   ├── NotificationService.cs # 通知服务
│   └── ExportService.cs       # 导出服务
├── Converters/               # 转换器
│   ├── NullToVisibilityConverter.cs
│   ├── ParameterTypeToVisibilityConverter.cs
│   └── BoolToVisibilityConverter.cs
├── Styles/                   # 样式资源
│   └── AppStyles.xaml        # 应用程序样式
└── MainWindow.xaml           # 主窗口
```

## 核心特性

### 🎨 现代化UI设计
- 使用WinUI3的现代控件
- 响应式布局设计
- 一致的视觉风格
- 流畅的用户交互

### 🔧 灵活的配置系统
- 动态参数配置
- 类型安全的参数验证
- 支持多种参数类型（文本、数字、枚举、颜色等）
- 可扩展的配置模板

### 📊 数据管理
- 完整的数据验证
- JSON格式导入导出
- CSV格式数据导出
- 统计报告生成

### 🎯 专业的PPT支持
- 39个详细的知识点配置
- 分类管理（幻灯片操作、文字设置、背景设计等）
- 参数化配置支持
- 与PowerPoint Interop兼容

## 使用方法

1. **创建试卷**：点击"新建试卷"按钮
2. **选择模块**：从左侧模块列表中选择要配置的模块
3. **添加题目**：在模块中添加题目描述或评分题目
4. **配置操作点**：为评分题目添加具体的操作点配置
5. **参数设置**：为每个操作点配置详细参数
6. **验证和导出**：验证配置完整性并导出试卷

## 技术亮点

- **MVVM架构**：清晰的代码分离和可维护性
- **响应式编程**：使用ReactiveUI实现响应式数据绑定
- **类型安全**：强类型的数据模型和参数验证
- **可扩展性**：模块化设计，易于添加新的模块类型
- **用户体验**：直观的界面设计和流畅的操作流程

## 开发状态

✅ 项目基础架构完成
✅ 核心数据模型实现
✅ 主界面和导航功能
✅ PPT模块39个知识点完整实现
✅ Windows模块9种操作类型
✅ C#、Excel、Word模块基础框架
✅ 验证和导出功能
✅ UI样式和用户体验优化

## 后续扩展

- 数据持久化（数据库支持）
- 更多Office操作类型
- 试卷模板系统
- 批量导入功能
- 在线协作功能

---

**ExamLab** - 让试卷制作更简单、更专业！
