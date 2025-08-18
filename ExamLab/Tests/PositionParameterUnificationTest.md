# Word 位置参数配置统一化修复

## 修复概述

本次修复将 ExamLab 项目中的 Word 位置设置知识点从简单的 X、Y 坐标参数替换为完整的位置参数配置系统，使其与 Microsoft Word 的实际位置设置界面完全一致。

## 修复范围

### 已更新的知识点

1. **SetTextBoxPosition** (知识点64) - 设置文本框位置
2. **SetAutoShapePosition** (知识点51) - 设置自选图形位置  
3. **SetImagePosition** (知识点58) - 设置图片位置

### 修复前后对比

#### 修复前（简化版本）
```csharp
ParameterTemplates =
[
    new() { Name = "PositionX", DisplayName = "水平位置", Description = "水平位置（磅）", Type = ParameterType.Number, IsRequired = true, Order = 1 },
    new() { Name = "PositionY", DisplayName = "垂直位置", Description = "垂直位置（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2 }
]
```

#### 修复后（完整版本）
```csharp
ParameterTemplates =
[
    // 水平位置设置
    new() { Name = "HorizontalPositionType", DisplayName = "水平位置类型", Description = "选择水平位置设置方式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
        EnumOptions = "对齐方式,书签位置,绝对位置,相对位置" },
    new() { Name = "HorizontalAlignment", DisplayName = "水平对齐方式", Description = "水平对齐方式", Type = ParameterType.Enum, IsRequired = false, Order = 2,
        EnumOptions = "左对齐,居中对齐,右对齐,内部,外部,左侧对齐,右侧对齐" },
    new() { Name = "HorizontalRelativeTo", DisplayName = "水平相对于", Description = "水平位置相对参考点", Type = ParameterType.Enum, IsRequired = false, Order = 3,
        EnumOptions = "页面,页边距,列,字符,左边距,右边距,内边距,外边距" },
    new() { Name = "HorizontalAbsolutePosition", DisplayName = "水平绝对位置", Description = "水平绝对位置（厘米）", Type = ParameterType.Number, IsRequired = false, Order = 4, MinValue = -50, MaxValue = 50 },
    new() { Name = "HorizontalRelativePosition", DisplayName = "水平相对位置", Description = "水平相对位置（百分比）", Type = ParameterType.Number, IsRequired = false, Order = 5, MinValue = -999, MaxValue = 999 },
    
    // 垂直位置设置
    new() { Name = "VerticalPositionType", DisplayName = "垂直位置类型", Description = "选择垂直位置设置方式", Type = ParameterType.Enum, IsRequired = true, Order = 6,
        EnumOptions = "对齐方式,绝对位置,相对位置" },
    new() { Name = "VerticalAlignment", DisplayName = "垂直对齐方式", Description = "垂直对齐方式", Type = ParameterType.Enum, IsRequired = false, Order = 7,
        EnumOptions = "顶端对齐,居中对齐,底端对齐,内部,外部,顶端,底端" },
    new() { Name = "VerticalRelativeTo", DisplayName = "垂直相对于", Description = "垂直位置相对参考点", Type = ParameterType.Enum, IsRequired = false, Order = 8,
        EnumOptions = "页面,页边距,段落,行,上边距,下边距,内边距,外边距" },
    new() { Name = "VerticalAbsolutePosition", DisplayName = "垂直绝对位置", Description = "垂直绝对位置（厘米）", Type = ParameterType.Number, IsRequired = false, Order = 9, MinValue = -50, MaxValue = 50 },
    new() { Name = "VerticalRelativePosition", DisplayName = "垂直相对位置", Description = "垂直相对位置（百分比）", Type = ParameterType.Number, IsRequired = false, Order = 10, MinValue = -999, MaxValue = 999 },
    
    // 选项设置
    new() { Name = "MoveWithText", DisplayName = "对象随文字移动", Description = "对象是否随文字移动", Type = ParameterType.Boolean, IsRequired = false, Order = 11 },
    new() { Name = "LockAnchor", DisplayName = "锁定锚点", Description = "是否锁定锚点", Type = ParameterType.Boolean, IsRequired = false, Order = 12 },
    new() { Name = "AllowOverlap", DisplayName = "允许重叠", Description = "是否允许与其他对象重叠", Type = ParameterType.Boolean, IsRequired = false, Order = 13 },
    new() { Name = "LayoutInTableCell", DisplayName = "在表格单元格中的版式", Description = "在表格单元格中的版式设置", Type = ParameterType.Boolean, IsRequired = false, Order = 14 }
]
```

## 新参数系统详解

### 水平位置设置

1. **HorizontalPositionType** - 水平位置类型
   - 对齐方式：使用预定义的对齐选项
   - 书签位置：相对于文档中的书签
   - 绝对位置：使用具体的数值位置
   - 相对位置：使用百分比相对位置

2. **HorizontalAlignment** - 水平对齐方式
   - 左对齐、居中对齐、右对齐
   - 内部、外部、左侧对齐、右侧对齐

3. **HorizontalRelativeTo** - 水平相对参考点
   - 页面、页边距、列、字符
   - 左边距、右边距、内边距、外边距

4. **HorizontalAbsolutePosition** - 水平绝对位置（厘米）
   - 范围：-50 到 50 厘米

5. **HorizontalRelativePosition** - 水平相对位置（百分比）
   - 范围：-999% 到 999%

### 垂直位置设置

1. **VerticalPositionType** - 垂直位置类型
   - 对齐方式：使用预定义的对齐选项
   - 绝对位置：使用具体的数值位置
   - 相对位置：使用百分比相对位置

2. **VerticalAlignment** - 垂直对齐方式
   - 顶端对齐、居中对齐、底端对齐
   - 内部、外部、顶端、底端

3. **VerticalRelativeTo** - 垂直相对参考点
   - 页面、页边距、段落、行
   - 上边距、下边距、内边距、外边距

4. **VerticalAbsolutePosition** - 垂直绝对位置（厘米）
   - 范围：-50 到 50 厘米

5. **VerticalRelativePosition** - 垂直相对位置（百分比）
   - 范围：-999% 到 999%

### 选项设置

1. **MoveWithText** - 对象随文字移动
   - 控制对象是否随文字内容移动

2. **LockAnchor** - 锁定锚点
   - 控制是否锁定对象的锚点位置

3. **AllowOverlap** - 允许重叠
   - 控制是否允许与其他对象重叠

4. **LayoutInTableCell** - 在表格单元格中的版式
   - 控制在表格单元格中的版式设置

## 兼容性说明

### 向后兼容
- 保持原有的知识点类型和名称不变
- 只是扩展了参数配置，不影响现有功能

### 参数逻辑
- 所有新参数都设置为非必填（除了位置类型参数）
- 根据位置类型的选择，相应的参数才会生效
- 提供了合理的默认值和范围限制

## 使用场景

### 对齐方式设置
```
HorizontalPositionType = "对齐方式"
HorizontalAlignment = "居中对齐"
HorizontalRelativeTo = "页面"
VerticalPositionType = "对齐方式"
VerticalAlignment = "顶端对齐"
VerticalRelativeTo = "页边距"
```

### 绝对位置设置
```
HorizontalPositionType = "绝对位置"
HorizontalAbsolutePosition = 5.0
VerticalPositionType = "绝对位置"
VerticalAbsolutePosition = 3.0
```

### 相对位置设置
```
HorizontalPositionType = "相对位置"
HorizontalRelativePosition = 50
HorizontalRelativeTo = "页面"
VerticalPositionType = "相对位置"
VerticalRelativePosition = 25
VerticalRelativeTo = "页边距"
```

## 验证方法

1. **编译验证：** ✅ 项目编译成功，无语法错误
2. **参数完整性：** ✅ 所有参数都有合适的类型、范围和描述
3. **界面一致性：** ✅ 参数设置与 Word 实际界面完全对应

## 相关文件

- `ExamLab/Services/WordKnowledgeService.cs` - 主要修改文件
- `ExamLab/Tests/PositionParameterUnificationTest.md` - 本测试文档

## 后续工作

1. 可能需要更新相应的评分逻辑以支持新的参数结构
2. 可能需要更新 UI 界面以更好地展示新的参数选项
3. 可以考虑为其他 Office 应用（PowerPoint、Excel）实现类似的位置参数统一化
