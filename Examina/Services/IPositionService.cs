using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Examina.Models.Position;

namespace Examina.Services;

/// <summary>
/// 位置服务接口
/// </summary>
public interface IPositionService
{
    /// <summary>
    /// 验证位置参数是否有效
    /// </summary>
    /// <param name="position">位置参数</param>
    /// <returns>验证结果</returns>
    bool ValidatePosition(PositionParameter position);
    
    /// <summary>
    /// 验证图形元素位置是否有效
    /// </summary>
    /// <param name="element">图形元素</param>
    /// <returns>验证结果</returns>
    bool ValidateGraphicElementPosition(GraphicElementPosition element);
    
    /// <summary>
    /// 转换位置单位
    /// </summary>
    /// <param name="value">原始值</param>
    /// <param name="fromUnit">原始单位</param>
    /// <param name="toUnit">目标单位</param>
    /// <returns>转换后的值</returns>
    double ConvertPositionUnit(double value, PositionUnit fromUnit, PositionUnit toUnit);
    
    /// <summary>
    /// 计算相对位置
    /// </summary>
    /// <param name="element">目标元素</param>
    /// <param name="parentElement">父元素</param>
    /// <returns>相对位置</returns>
    PositionParameter CalculateRelativePosition(GraphicElementPosition element, GraphicElementPosition parentElement);
    
    /// <summary>
    /// 计算绝对位置
    /// </summary>
    /// <param name="element">目标元素</param>
    /// <param name="parentElement">父元素（如果有的话）</param>
    /// <returns>绝对位置</returns>
    PositionParameter CalculateAbsolutePosition(GraphicElementPosition element, GraphicElementPosition? parentElement = null);
    
    /// <summary>
    /// 应用网格对齐
    /// </summary>
    /// <param name="position">位置参数</param>
    /// <param name="gridSize">网格大小</param>
    /// <returns>对齐后的位置</returns>
    PositionParameter ApplyGridSnap(PositionParameter position, double gridSize);
    
    /// <summary>
    /// 检查元素是否重叠
    /// </summary>
    /// <param name="element1">元素1</param>
    /// <param name="element2">元素2</param>
    /// <returns>是否重叠</returns>
    bool CheckElementsOverlap(GraphicElementPosition element1, GraphicElementPosition element2);
    
    /// <summary>
    /// 获取元素的边界信息
    /// </summary>
    /// <param name="element">图形元素</param>
    /// <returns>边界信息</returns>
    (double Left, double Top, double Right, double Bottom) GetElementBounds(GraphicElementPosition element);
    
    /// <summary>
    /// 创建位置参数模板
    /// </summary>
    /// <param name="type">操作点类型</param>
    /// <returns>参数模板列表</returns>
    List<ConfigurationParameter> CreatePositionParameterTemplates(OperationPointType type);
    
    /// <summary>
    /// 解析位置字符串
    /// </summary>
    /// <param name="positionString">位置字符串</param>
    /// <returns>位置参数</returns>
    PositionParameter? ParsePositionString(string positionString);
    
    /// <summary>
    /// 格式化位置为字符串
    /// </summary>
    /// <param name="position">位置参数</param>
    /// <returns>位置字符串</returns>
    string FormatPositionToString(PositionParameter position);
    
    /// <summary>
    /// 获取对齐位置
    /// </summary>
    /// <param name="element">目标元素</param>
    /// <param name="horizontalAlign">水平对齐方式</param>
    /// <param name="verticalAlign">垂直对齐方式</param>
    /// <param name="containerWidth">容器宽度</param>
    /// <param name="containerHeight">容器高度</param>
    /// <returns>对齐后的位置</returns>
    PositionParameter GetAlignedPosition(
        GraphicElementPosition element,
        HorizontalAlignment horizontalAlign,
        VerticalAlignment verticalAlign,
        double containerWidth,
        double containerHeight);
    
    /// <summary>
    /// 批量更新元素位置
    /// </summary>
    /// <param name="elements">元素列表</param>
    /// <param name="offsetX">X轴偏移</param>
    /// <param name="offsetY">Y轴偏移</param>
    /// <returns>更新任务</returns>
    Task BatchUpdateElementPositions(IEnumerable<GraphicElementPosition> elements, double offsetX, double offsetY);
    
    /// <summary>
    /// 自动排列元素
    /// </summary>
    /// <param name="elements">元素列表</param>
    /// <param name="arrangement">排列方式</param>
    /// <param name="spacing">间距</param>
    /// <returns>排列任务</returns>
    Task AutoArrangeElements(IEnumerable<GraphicElementPosition> elements, ElementArrangement arrangement, double spacing);
}

/// <summary>
/// 元素排列方式枚举
/// </summary>
public enum ElementArrangement
{
    /// <summary>
    /// 水平排列
    /// </summary>
    Horizontal,
    
    /// <summary>
    /// 垂直排列
    /// </summary>
    Vertical,
    
    /// <summary>
    /// 网格排列
    /// </summary>
    Grid,
    
    /// <summary>
    /// 圆形排列
    /// </summary>
    Circle,
    
    /// <summary>
    /// 自由排列
    /// </summary>
    Free
}
