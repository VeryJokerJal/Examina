# PowerPoint检测方法参数需求分析

## 已分析的检测方法参数需求

### 1. DetectSlideLayout (SetSlideLayout)
**必需参数:**
- SlideIndex (int)
- LayoutType (string)

### 2. DetectDeletedSlide (DeleteSlide) 
**必需参数:**
- ExpectedSlideCount (int) ✅ 已修复

### 3. DetectInsertedSlide (InsertSlide)
**必需参数:**
- ExpectedSlideCount (int) ✅ 已修复

### 4. DetectSlideFont (SetSlideFont)
**必需参数:**
- SlideIndex (int)
- FontName (string)

### 5. DetectSlideTransition (SlideTransitionEffect)
**必需参数:**
- SlideIndex (int)
- TransitionType (string)

### 6. DetectSlideTransitionMode (SlideTransitionMode)
**必需参数:**
- SlideIndexes (string) - 逗号分隔的索引列表
- TransitionMode (int) ✅ 已修复

### 7. DetectTextContent (InsertTextContent)
**必需参数:**
- SlideIndex (int) - 支持-1遍历所有幻灯片 ✅ 已修复
- TextContent (string)

### 8. DetectTextFontSize (SetTextFontSize)
**必需参数:**
- SlideIndex (int)
- FontSize (float)

### 9. DetectTextColor (SetTextColor)
**必需参数:**
- SlideIndex (int)
- Color (string)

### 10. DetectTextStyle (SetTextStyle)
**必需参数:**
- SlideIndex (int)
- TextBoxIndex (int)
- StyleType (int) - 1=加粗, 2=斜体, 3=下划线, 4=删除线

### 11. DetectElementPosition (SetElementPosition)
**必需参数:**
- SlideIndex (int)
- ElementIndex (int)
- Left (float)
- Top (float)

### 12. DetectElementSize (SetElementSize)
**必需参数:**
- SlideIndex (int)
- ElementIndex (int)
- Width (float)
- Height (float)

### 13. DetectTextAlignment (SetTextAlignment)
**必需参数:**
- SlideIndex (int)
- TextBoxIndex (int)
- AlignmentType (int)

### 14. DetectHyperlink (InsertHyperlink)
**必需参数:**
- SlideIndex (int)
- TextBoxIndex (int)
- HyperlinkType (int)

### 15. DetectSlideNumber (SetSlideNumber)
**必需参数:**
- SlideIndex (int)
- ShowSlideNumber (bool)

### 16. DetectFooterText (SetFooterText)
**必需参数:**
- SlideIndex (int)
- FooterText (string)

### 17. DetectInsertedImage (InsertImage)
**必需参数:**
- SlideIndex (int)
**可选参数:**
- ExpectedImageCount (int) - 默认检查至少1张图片

### 18. DetectInsertedTable (InsertTable)
**必需参数:**
- SlideIndex (int)
**可选参数:**
- ExpectedRows (int)
- ExpectedColumns (int)

### 19. DetectInsertedSmartArt (InsertSmartArt)
**必需参数:**
- SlideIndex (int)
**可选参数:**
- ExpectedSmartArtCount (int) - 默认检查至少1个SmartArt

### 20. DetectInsertedNote (InsertNote)
**必需参数:**
- SlideIndex (int)
- NoteText (string)

### 21. DetectAppliedTheme (ApplyTheme)
**必需参数:**
- ThemeName (string)

### 22. DetectSlideBackground (SetSlideBackground/SetBackgroundStyle)
**可选参数:**
- SlideIndex (int) - 如果不提供或为-1，遍历所有幻灯片
- BackgroundType (string) - 如果不提供，检查是否为非默认背景

### 23. DetectTableContent (SetTableContent)
**必需参数:**
- SlideIndex (int)
- Rows (int)
- Columns (int)
- Content (string)

### 24. DetectTableStyle (SetTableStyle)
**必需参数:**
- SlideIndex (int)
- TableIndex (int)
- StyleName (string)

### 25. DetectSlideshowMode (SlideshowMode)
**必需参数:**
- SlideshowType (int)

### 26. DetectSlideshowOptions (SlideshowOptions)
**必需参数:**
- OptionType (int)
- OptionValue (string)

### 27. DetectSlideTransitionSound (SlideTransitionSound)
**必需参数:**
- SlideIndex (int)
**可选参数:**
- SoundFile (string)

### 28. DetectWordArtStyle (SetWordArtStyle)
**必需参数:**
- SlideIndex (int)
**可选参数:**
- ExpectedWordArtCount (int) - 默认检查至少1个艺术字

### 29. DetectWordArtEffect (SetWordArtEffect)
**必需参数:**
- SlideIndex (int)

### 30. DetectSmartArtColor (SetSmartArtColor)
**必需参数:**
- SlideIndex (int)

### 31. DetectSmartArtContent (SetSmartArtContent)
**必需参数:**
- SlideIndex (int)
- Content (string)

### 32. DetectAnimationDirection (SetAnimationDirection)
**必需参数:**
- SlideIndex (int)
**可选参数:**
- ElementIndex (int) - 如果提供，检测特定元素的动画方向

### 33. DetectAnimationStyle (SetAnimationStyle)
**必需参数:**
- SlideIndex (int)

### 34. DetectAnimationDuration (SetAnimationDuration)
**必需参数:**
- SlideIndex (int)

### 35. DetectAnimationOrder (SetAnimationOrder)
**必需参数:**
- SlideIndex (int)
**可选参数:**
- ElementIndex (int) - 如果提供，检测特定元素的动画顺序

### 36. DetectAnimationTiming (SetAnimationTiming)
**必需参数:**
- SlideIndex (int)
- ElementIndex (int)

### 37. DetectParagraphSpacing (SetParagraphSpacing)
**必需参数:**
- SlideIndex (int)
- TextBoxIndex (int)

## 特殊参数需求说明

### SlideTransitionSound 特殊参数
**必需参数:**
- SlideIndex (int)
**特殊逻辑:**
- 需要 ExpectedSound (string) 或 HasSound (bool) 其中之一

### SlideshowOptions 参数需求待确认
**当前检测到的参数:**
- OptionType (int)
- OptionValue (string)

## 参数缺失问题分析

### 完全缺失配置的知识点：
1. **SlideTransitionEffect** - 幻灯片切换效果
2. **InsertTextContent** - 幻灯片插入文本内容
3. **SetTextFontSize** - 幻灯片插入文本字号
4. **SetTextColor** - 幻灯片插入文本颜色
5. **SetTextStyle** - 幻灯片插入文本字形
6. **SetElementPosition** - 元素位置
7. **SetElementSize** - 元素高度和宽度设置
8. **SetTextAlignment** - 文本对齐方式
9. **SetTableContent** - 单元格内容
10. **SetTableStyle** - 表格样式
11. **SetAnimationTiming** - 动画计时与延时设置
12. **SetParagraphSpacing** - 段落行距

### 参数不完整的知识点：
1. **SetSlideLayout** - 缺少SlideIndex参数映射
2. **SetSlideFont** - 缺少SlideIndex和TextBoxIndex参数映射
3. **InsertHyperlink** - 缺少TextBoxIndex参数
4. **SlideNumber** - 缺少ShowSlideNumber参数
5. **FooterText** - 缺少FooterText参数
6. **InsertImage** - 缺少ExpectedImageCount参数
7. **InsertTable** - 缺少ExpectedRows/ExpectedColumns参数
8. **InsertSmartArt** - 缺少ExpectedSmartArtCount参数
9. **InsertNote** - 缺少NoteText参数
10. **SlideTransitionSound** - 缺少ExpectedSound/HasSound参数

### 参数名称映射问题：
- SlideNumber → SlideIndex
- TextBoxNumber/TextBoxOrder → TextBoxIndex
- ElementOrder → ElementIndex
- Layout → LayoutType
- SlideshowMode → SlideshowType

## 总结
共分析了37个检测方法，发现12个知识点完全缺失配置，10个知识点参数不完整，还有多个参数名称映射问题需要解决。
