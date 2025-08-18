using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Examina.Models.Position;

namespace Examina.Services;

/// <summary>
/// 文本框位置服务接口
/// </summary>
public interface ITextBoxPositionService
{
    /// <summary>
    /// 创建文本框位置配置
    /// </summary>
    /// <param name="name">文本框名称</param>
    /// <param name="content">文本内容</param>
    /// <param name="position">位置参数</param>
    /// <returns>文本框位置配置</returns>
    TextBoxPosition CreateTextBoxPosition(string name, string content, PositionParameter position);
    
    /// <summary>
    /// 验证文本框位置配置
    /// </summary>
    /// <param name="textBox">文本框位置配置</param>
    /// <returns>验证结果</returns>
    bool ValidateTextBoxPosition(TextBoxPosition textBox);
    
    /// <summary>
    /// 设置文本框位置
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="unit">位置单位</param>
    void SetTextBoxPosition(TextBoxPosition textBox, double x, double y, PositionUnit unit = PositionUnit.Point);
    
    /// <summary>
    /// 设置文本框对齐方式
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="horizontalAlign">水平对齐</param>
    /// <param name="verticalAlign">垂直对齐</param>
    void SetTextBoxAlignment(TextBoxPosition textBox, TextHorizontalAlignment horizontalAlign, TextVerticalAlignment verticalAlign);
    
    /// <summary>
    /// 自动调整文本框尺寸以适应内容
    /// </summary>
    /// <param name="textBox">文本框</param>
    void AutoResizeTextBox(TextBoxPosition textBox);
    
    /// <summary>
    /// 设置文本框字体属性
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="fontFamily">字体名称</param>
    /// <param name="fontSize">字体大小</param>
    /// <param name="fontColor">字体颜色</param>
    void SetTextBoxFont(TextBoxPosition textBox, string fontFamily, double fontSize, string fontColor);
    
    /// <summary>
    /// 设置文本框样式
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="isBold">是否粗体</param>
    /// <param name="isItalic">是否斜体</param>
    /// <param name="isUnderline">是否下划线</param>
    void SetTextBoxStyle(TextBoxPosition textBox, bool isBold, bool isItalic, bool isUnderline);
    
    /// <summary>
    /// 设置文本框边框
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="isVisible">是否显示边框</param>
    /// <param name="color">边框颜色</param>
    /// <param name="width">边框宽度</param>
    /// <param name="style">边框样式</param>
    void SetTextBoxBorder(TextBoxPosition textBox, bool isVisible, string color, double width, BorderStyle style);
    
    /// <summary>
    /// 设置文本框背景
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="isVisible">是否显示背景</param>
    /// <param name="color">背景颜色</param>
    /// <param name="opacity">透明度</param>
    void SetTextBoxBackground(TextBoxPosition textBox, bool isVisible, string color, double opacity);
    
    /// <summary>
    /// 计算文本框内容所需的尺寸
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <returns>计算出的尺寸</returns>
    (double Width, double Height) CalculateTextBoxSize(TextBoxPosition textBox);
    
    /// <summary>
    /// 批量设置文本框位置
    /// </summary>
    /// <param name="textBoxes">文本框列表</param>
    /// <param name="arrangement">排列方式</param>
    /// <param name="spacing">间距</param>
    /// <returns>设置任务</returns>
    Task BatchSetTextBoxPositions(IEnumerable<TextBoxPosition> textBoxes, ElementArrangement arrangement, double spacing);
    
    /// <summary>
    /// 对齐文本框到容器
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="containerWidth">容器宽度</param>
    /// <param name="containerHeight">容器高度</param>
    /// <param name="horizontalAlign">水平对齐方式</param>
    /// <param name="verticalAlign">垂直对齐方式</param>
    void AlignTextBoxToContainer(
        TextBoxPosition textBox,
        double containerWidth,
        double containerHeight,
        HorizontalAlignment horizontalAlign,
        VerticalAlignment verticalAlign);
    
    /// <summary>
    /// 创建文本框位置参数模板
    /// </summary>
    /// <returns>参数模板列表</returns>
    List<ConfigurationParameter> CreateTextBoxPositionParameterTemplates();
    
    /// <summary>
    /// 从配置参数创建文本框位置
    /// </summary>
    /// <param name="parameters">配置参数列表</param>
    /// <returns>文本框位置配置</returns>
    TextBoxPosition CreateTextBoxFromParameters(IEnumerable<ConfigurationParameter> parameters);
    
    /// <summary>
    /// 将文本框位置转换为配置参数
    /// </summary>
    /// <param name="textBox">文本框位置</param>
    /// <returns>配置参数列表</returns>
    List<ConfigurationParameter> ConvertTextBoxToParameters(TextBoxPosition textBox);
    
    /// <summary>
    /// 检查文本框是否在指定区域内
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="areaLeft">区域左边界</param>
    /// <param name="areaTop">区域上边界</param>
    /// <param name="areaRight">区域右边界</param>
    /// <param name="areaBottom">区域下边界</param>
    /// <returns>是否在区域内</returns>
    bool IsTextBoxInArea(TextBoxPosition textBox, double areaLeft, double areaTop, double areaRight, double areaBottom);
    
    /// <summary>
    /// 移动文本框到指定位置
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="targetX">目标X坐标</param>
    /// <param name="targetY">目标Y坐标</param>
    /// <param name="animationDuration">动画持续时间（毫秒）</param>
    /// <returns>移动任务</returns>
    Task MoveTextBoxToPosition(TextBoxPosition textBox, double targetX, double targetY, int animationDuration = 0);
    
    /// <summary>
    /// 复制文本框位置配置
    /// </summary>
    /// <param name="sourceTextBox">源文本框</param>
    /// <param name="newName">新文本框名称</param>
    /// <returns>复制的文本框配置</returns>
    TextBoxPosition CloneTextBoxPosition(TextBoxPosition sourceTextBox, string newName);
    
    /// <summary>
    /// 获取文本框的锚点位置
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="anchorType">锚点类型</param>
    /// <returns>锚点位置</returns>
    (double X, double Y) GetTextBoxAnchorPosition(TextBoxPosition textBox, AnchorType anchorType);
    
    /// <summary>
    /// 设置文本框的环绕方式
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="wrapStyle">环绕方式</param>
    void SetTextBoxWrapStyle(TextBoxPosition textBox, WrapStyle wrapStyle);
    
    /// <summary>
    /// 验证文本框位置参数的有效性
    /// </summary>
    /// <param name="parameters">位置参数</param>
    /// <returns>验证结果和错误信息</returns>
    (bool IsValid, string ErrorMessage) ValidateTextBoxPositionParameters(IEnumerable<ConfigurationParameter> parameters);
}
