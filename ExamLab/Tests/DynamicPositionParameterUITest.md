# 动态位置参数UI功能测试

## 功能概述

本次实现了位置参数配置的动态UI逻辑，实现了根据位置类型选择动态显示相关参数的功能，完全模拟Microsoft Word的位置设置界面行为。

## 实现的功能

### 1. 位置类型选择控制
- **水平位置类型控制：** 用户必须首先选择 HorizontalPositionType
- **垂直位置类型控制：** 用户必须首先选择 VerticalPositionType
- **独立控制：** 水平位置和垂直位置的参数控制相互独立

### 2. 动态参数显示规则

#### 水平位置参数显示规则
- **选择"对齐方式"时：** 显示 HorizontalAlignment 和 HorizontalRelativeTo
- **选择"绝对位置"时：** 显示 HorizontalAbsolutePosition
- **选择"相对位置"时：** 显示 HorizontalRelativePosition 和 HorizontalRelativeTo
- **选择"书签位置"时：** 显示书签相关参数（待实现）

#### 垂直位置参数显示规则
- **选择"对齐方式"时：** 显示 VerticalAlignment 和 VerticalRelativeTo
- **选择"绝对位置"时：** 显示 VerticalAbsolutePosition
- **选择"相对位置"时：** 显示 VerticalRelativePosition 和 VerticalRelativeTo

### 3. UI交互特性
- **初始状态：** 未选择位置类型时，所有子参数被隐藏和禁用
- **动态显示：** 选择位置类型后，相关参数立即显示和启用
- **参数清空：** 切换位置类型时，自动清空之前类型的参数值
- **视觉反馈：** 提供清晰的视觉反馈，明确哪些参数当前可用

## 技术实现

### 1. 数据模型扩展

#### ConfigurationParameter 新增属性
```csharp
[Reactive] public string? DependsOn { get; set; }           // 依赖的参数名称
[Reactive] public string? DependsOnValue { get; set; }     // 依赖参数的值
[Reactive] public bool IsVisible { get; set; } = true;     // 是否可见
[Reactive] public string? Group { get; set; }              // 参数分组
```

#### ConfigurationParameterTemplate 新增属性
```csharp
[Reactive] public string? DependsOn { get; set; }          // 依赖的参数名称
[Reactive] public string? DependsOnValue { get; set; }     // 依赖参数的值
[Reactive] public string? Group { get; set; }              // 参数分组
```

### 2. 位置参数控制器

#### PositionParameterController 核心功能
- **InitializePositionParameters：** 初始化位置参数的依赖关系
- **UpdateParameterVisibility：** 更新参数可见性
- **ValidatePositionParameters：** 验证位置参数配置
- **GetPositionParameterGroups：** 获取位置参数分组

#### 参数依赖关系映射
```csharp
private static readonly Dictionary<string, PositionParameterRule> ParameterRules = new()
{
    ["HorizontalAlignment"] = new("HorizontalPositionType", "对齐方式"),
    ["HorizontalRelativeTo"] = new("HorizontalPositionType", new[] { "对齐方式", "相对位置" }),
    ["HorizontalAbsolutePosition"] = new("HorizontalPositionType", "绝对位置"),
    ["HorizontalRelativePosition"] = new("HorizontalPositionType", "相对位置"),
    // ... 垂直位置参数规则
};
```

### 3. UI转换器

#### ParameterVisibilityConverter
- 将参数的 IsVisible 属性转换为 Visibility 枚举
- 控制参数控件的显示和隐藏

#### ParameterEnabledConverter
- 将参数的 IsVisible 属性转换为启用状态
- 控制参数控件的启用和禁用

### 4. UI界面修改

#### OperationPointEditPage.xaml.cs 关键修改
- **InitializeControls：** 添加位置参数控制器初始化
- **CreateParameterControl：** 添加可见性和启用状态绑定
- **CreateEnumControl：** 添加位置类型参数的选择变更事件处理

#### 动态绑定实现
```csharp
// 绑定可见性
parameterGrid.SetBinding(UIElement.VisibilityProperty, new Binding
{
    Source = parameter,
    Path = new PropertyPath("IsVisible"),
    Converter = (IValueConverter)Application.Current.Resources["ParameterVisibilityConverter"]
});

// 绑定启用状态
editControl.SetBinding(Control.IsEnabledProperty, new Binding
{
    Source = parameter,
    Path = new PropertyPath("IsVisible"),
    Converter = (IValueConverter)Application.Current.Resources["ParameterEnabledConverter"]
});
```

## 应用范围

### 支持的知识点
- **SetTextBoxPosition** - 设置文本框位置
- **SetAutoShapePosition** - 设置自选图形位置
- **SetImagePosition** - 设置图片位置

### 参数分组
- **水平位置：** 5个参数（类型、对齐、相对于、绝对位置、相对位置）
- **垂直位置：** 5个参数（类型、对齐、相对于、绝对位置、相对位置）
- **选项设置：** 4个参数（随文字移动、锁定锚点、允许重叠、表格版式）

## 测试场景

### 场景1：水平位置类型选择
1. **初始状态：** 所有水平位置子参数隐藏
2. **选择"对齐方式"：** 显示 HorizontalAlignment 和 HorizontalRelativeTo
3. **选择"绝对位置"：** 隐藏对齐参数，显示 HorizontalAbsolutePosition
4. **选择"相对位置"：** 显示 HorizontalRelativePosition 和 HorizontalRelativeTo

### 场景2：垂直位置类型选择
1. **初始状态：** 所有垂直位置子参数隐藏
2. **选择"对齐方式"：** 显示 VerticalAlignment 和 VerticalRelativeTo
3. **选择"绝对位置"：** 显示 VerticalAbsolutePosition
4. **选择"相对位置"：** 显示 VerticalRelativePosition 和 VerticalRelativeTo

### 场景3：参数值清空
1. **设置对齐方式参数：** 选择具体的对齐选项
2. **切换到绝对位置：** 对齐方式参数值被自动清空
3. **设置绝对位置值：** 输入具体数值
4. **切换回对齐方式：** 绝对位置值被自动清空

### 场景4：独立控制
1. **设置水平位置为对齐方式：** 垂直位置参数不受影响
2. **设置垂直位置为绝对位置：** 水平位置参数保持不变
3. **分别配置：** 水平和垂直位置可以独立配置不同类型

## 验证方法

### 手动测试
1. 在 ExamLab 中创建包含位置参数的操作点
2. 编辑操作点参数，观察动态显示效果
3. 切换位置类型，验证参数清空和显示切换
4. 确认水平和垂直位置控制的独立性

### 自动化测试
可以通过单元测试验证 PositionParameterController 的逻辑：
- 测试参数依赖关系初始化
- 测试参数可见性更新逻辑
- 测试参数值清空机制
- 测试参数验证功能

## 技术亮点

1. **响应式设计：** 使用 ReactiveUI 实现参数状态的自动更新
2. **数据绑定：** 通过 XAML 数据绑定实现UI的动态控制
3. **转换器模式：** 使用转换器实现数据到UI状态的转换
4. **控制器模式：** 使用专门的控制器管理复杂的参数依赖关系
5. **事件驱动：** 通过事件处理实现参数变更的响应

## 扩展性

### 未来扩展
1. **书签位置支持：** 可以添加书签相关参数的处理
2. **更多位置类型：** 可以支持更多复杂的位置设置类型
3. **参数组合验证：** 可以添加参数组合的有效性验证
4. **UI优化：** 可以添加动画效果和更好的视觉反馈

### 其他应用
这套动态参数显示系统可以应用到其他需要条件显示参数的场景：
- 表格格式设置
- 字体样式配置
- 页面布局参数
- 图表配置选项

## 总结

动态位置参数UI功能的实现完全达到了设计目标：
- ✅ 实现了位置类型选择控制
- ✅ 实现了动态参数显示规则
- ✅ 提供了清晰的UI交互反馈
- ✅ 应用到了所有位置相关知识点
- ✅ 确保了水平和垂直位置的独立控制

这个功能大大提升了用户体验，使位置参数配置更加直观和易用，完全模拟了Microsoft Word的专业级位置设置界面。
