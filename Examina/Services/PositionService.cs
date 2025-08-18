using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Examina.Models.Position;

namespace Examina.Services;

/// <summary>
/// 位置服务实现
/// </summary>
public class PositionService : IPositionService
{
    #region 常量定义
    
    /// <summary>
    /// 磅到像素的转换比例（96 DPI）
    /// </summary>
    private const double PointToPixelRatio = 96.0 / 72.0;
    
    /// <summary>
    /// 厘米到磅的转换比例
    /// </summary>
    private const double CentimeterToPointRatio = 72.0 / 2.54;
    
    /// <summary>
    /// 英寸到磅的转换比例
    /// </summary>
    private const double InchToPointRatio = 72.0;
    
    #endregion
    
    #region 位置验证
    
    /// <summary>
    /// 验证位置参数是否有效
    /// </summary>
    /// <param name="position">位置参数</param>
    /// <returns>验证结果</returns>
    public bool ValidatePosition(PositionParameter position)
    {
        if (position == null) return false;
        
        return position.IsValid();
    }
    
    /// <summary>
    /// 验证图形元素位置是否有效
    /// </summary>
    /// <param name="element">图形元素</param>
    /// <returns>验证结果</returns>
    public bool ValidateGraphicElementPosition(GraphicElementPosition element)
    {
        if (element == null) return false;
        
        return element.IsValid();
    }
    
    #endregion
    
    #region 单位转换
    
    /// <summary>
    /// 转换位置单位
    /// </summary>
    /// <param name="value">原始值</param>
    /// <param name="fromUnit">原始单位</param>
    /// <param name="toUnit">目标单位</param>
    /// <returns>转换后的值</returns>
    public double ConvertPositionUnit(double value, PositionUnit fromUnit, PositionUnit toUnit)
    {
        if (fromUnit == toUnit) return value;
        
        // 先转换为磅（作为中间单位）
        double pointValue = fromUnit switch
        {
            PositionUnit.Pixel => value / PointToPixelRatio,
            PositionUnit.Point => value,
            PositionUnit.Centimeter => value * CentimeterToPointRatio,
            PositionUnit.Inch => value * InchToPointRatio,
            PositionUnit.Percentage => value, // 百分比需要上下文信息，这里直接返回
            _ => value
        };
        
        // 再从磅转换为目标单位
        return toUnit switch
        {
            PositionUnit.Pixel => pointValue * PointToPixelRatio,
            PositionUnit.Point => pointValue,
            PositionUnit.Centimeter => pointValue / CentimeterToPointRatio,
            PositionUnit.Inch => pointValue / InchToPointRatio,
            PositionUnit.Percentage => pointValue, // 百分比需要上下文信息，这里直接返回
            _ => pointValue
        };
    }
    
    #endregion
    
    #region 位置计算
    
    /// <summary>
    /// 计算相对位置
    /// </summary>
    /// <param name="element">目标元素</param>
    /// <param name="parentElement">父元素</param>
    /// <returns>相对位置</returns>
    public PositionParameter CalculateRelativePosition(GraphicElementPosition element, GraphicElementPosition parentElement)
    {
        var relativePosition = element.Position.Clone();
        relativePosition.Type = PositionType.Relative;
        relativePosition.RelativeToElementId = parentElement.ElementId;
        
        // 计算相对坐标
        relativePosition.X = element.Position.X - parentElement.Position.X;
        relativePosition.Y = element.Position.Y - parentElement.Position.Y;
        
        return relativePosition;
    }
    
    /// <summary>
    /// 计算绝对位置
    /// </summary>
    /// <param name="element">目标元素</param>
    /// <param name="parentElement">父元素（如果有的话）</param>
    /// <returns>绝对位置</returns>
    public PositionParameter CalculateAbsolutePosition(GraphicElementPosition element, GraphicElementPosition? parentElement = null)
    {
        var absolutePosition = element.Position.Clone();
        absolutePosition.Type = PositionType.Absolute;
        absolutePosition.RelativeToElementId = null;
        
        if (element.Position.Type == PositionType.Relative && parentElement != null)
        {
            // 从相对位置转换为绝对位置
            absolutePosition.X = element.Position.X + parentElement.Position.X;
            absolutePosition.Y = element.Position.Y + parentElement.Position.Y;
        }
        
        return absolutePosition;
    }
    
    /// <summary>
    /// 应用网格对齐
    /// </summary>
    /// <param name="position">位置参数</param>
    /// <param name="gridSize">网格大小</param>
    /// <returns>对齐后的位置</returns>
    public PositionParameter ApplyGridSnap(PositionParameter position, double gridSize)
    {
        var snappedPosition = position.Clone();
        
        if (gridSize > 0)
        {
            snappedPosition.X = Math.Round(position.X / gridSize) * gridSize;
            snappedPosition.Y = Math.Round(position.Y / gridSize) * gridSize;
        }
        
        return snappedPosition;
    }
    
    #endregion
    
    #region 碰撞检测
    
    /// <summary>
    /// 检查元素是否重叠
    /// </summary>
    /// <param name="element1">元素1</param>
    /// <param name="element2">元素2</param>
    /// <returns>是否重叠</returns>
    public bool CheckElementsOverlap(GraphicElementPosition element1, GraphicElementPosition element2)
    {
        return element1.IsOverlapping(element2);
    }
    
    /// <summary>
    /// 获取元素的边界信息
    /// </summary>
    /// <param name="element">图形元素</param>
    /// <returns>边界信息</returns>
    public (double Left, double Top, double Right, double Bottom) GetElementBounds(GraphicElementPosition element)
    {
        return element.GetBounds();
    }
    
    #endregion
    
    #region 参数模板
    
    /// <summary>
    /// 创建位置参数模板
    /// </summary>
    /// <param name="type">操作点类型</param>
    /// <returns>参数模板列表</returns>
    public List<ConfigurationParameter> CreatePositionParameterTemplates(OperationPointType type)
    {
        var templates = new List<ConfigurationParameter>();
        
        switch (type)
        {
            case OperationPointType.SetGraphicPosition:
            case OperationPointType.SetImagePosition:
            case OperationPointType.SetShapePosition:
            case OperationPointType.SetChartPosition:
                templates.AddRange([
                    new ConfigurationParameter
                    {
                        Name = "PositionX",
                        DisplayName = "水平位置",
                        Description = "水平位置（磅）",
                        Type = ParameterType.Number,
                        IsRequired = true,
                        Order = 1,
                        DefaultValue = "0",
                        MinValue = 0
                    },
                    new ConfigurationParameter
                    {
                        Name = "PositionY",
                        DisplayName = "垂直位置",
                        Description = "垂直位置（磅）",
                        Type = ParameterType.Number,
                        IsRequired = true,
                        Order = 2,
                        DefaultValue = "0",
                        MinValue = 0
                    }
                ]);
                break;
                
            case OperationPointType.SetTextBoxPosition:
                templates.AddRange([
                    new ConfigurationParameter
                    {
                        Name = "PositionX",
                        DisplayName = "水平位置",
                        Description = "水平位置（磅）",
                        Type = ParameterType.Number,
                        IsRequired = true,
                        Order = 1,
                        DefaultValue = "0",
                        MinValue = 0
                    },
                    new ConfigurationParameter
                    {
                        Name = "PositionY",
                        DisplayName = "垂直位置",
                        Description = "垂直位置（磅）",
                        Type = ParameterType.Number,
                        IsRequired = true,
                        Order = 2,
                        DefaultValue = "0",
                        MinValue = 0
                    },
                    new ConfigurationParameter
                    {
                        Name = "HorizontalAlignment",
                        DisplayName = "水平对齐",
                        Description = "文本框水平对齐方式",
                        Type = ParameterType.Enum,
                        IsRequired = false,
                        Order = 3,
                        EnumOptions = "左对齐,居中对齐,右对齐,两端对齐,均匀分布",
                        DefaultValue = "左对齐"
                    },
                    new ConfigurationParameter
                    {
                        Name = "VerticalAlignment",
                        DisplayName = "垂直对齐",
                        Description = "文本框垂直对齐方式",
                        Type = ParameterType.Enum,
                        IsRequired = false,
                        Order = 4,
                        EnumOptions = "顶部对齐,居中对齐,底部对齐",
                        DefaultValue = "顶部对齐"
                    }
                ]);
                break;
        }
        
        return templates;
    }
    
    #endregion
    
    #region 字符串解析和格式化
    
    /// <summary>
    /// 解析位置字符串
    /// </summary>
    /// <param name="positionString">位置字符串</param>
    /// <returns>位置参数</returns>
    public PositionParameter? ParsePositionString(string positionString)
    {
        if (string.IsNullOrEmpty(positionString)) return null;
        
        try
        {
            // 简单的解析逻辑，格式：X,Y,Unit
            var parts = positionString.Split(',');
            if (parts.Length >= 2)
            {
                var position = new PositionParameter
                {
                    X = double.Parse(parts[0]),
                    Y = double.Parse(parts[1])
                };
                
                if (parts.Length >= 3 && Enum.TryParse<PositionUnit>(parts[2], out var unit))
                {
                    position.Unit = unit;
                }
                
                return position;
            }
        }
        catch
        {
            // 解析失败，返回null
        }
        
        return null;
    }
    
    /// <summary>
    /// 格式化位置为字符串
    /// </summary>
    /// <param name="position">位置参数</param>
    /// <returns>位置字符串</returns>
    public string FormatPositionToString(PositionParameter position)
    {
        return $"{position.X},{position.Y},{position.Unit}";
    }
    
    #endregion

    #region 对齐和排列

    /// <summary>
    /// 获取对齐位置
    /// </summary>
    /// <param name="element">目标元素</param>
    /// <param name="horizontalAlign">水平对齐方式</param>
    /// <param name="verticalAlign">垂直对齐方式</param>
    /// <param name="containerWidth">容器宽度</param>
    /// <param name="containerHeight">容器高度</param>
    /// <returns>对齐后的位置</returns>
    public PositionParameter GetAlignedPosition(
        GraphicElementPosition element,
        HorizontalAlignment horizontalAlign,
        VerticalAlignment verticalAlign,
        double containerWidth,
        double containerHeight)
    {
        var alignedPosition = element.Position.Clone();
        alignedPosition.Type = PositionType.Alignment;
        alignedPosition.HorizontalAlign = horizontalAlign;
        alignedPosition.VerticalAlign = verticalAlign;

        // 计算对齐后的坐标
        alignedPosition.X = horizontalAlign switch
        {
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Center => (containerWidth - element.Width) / 2,
            HorizontalAlignment.Right => containerWidth - element.Width,
            HorizontalAlignment.Justify => 0, // 两端对齐时左边界为0
            HorizontalAlignment.Distribute => 0, // 均匀分布需要多个元素的上下文
            _ => element.Position.X
        };

        alignedPosition.Y = verticalAlign switch
        {
            VerticalAlignment.Top => 0,
            VerticalAlignment.Middle => (containerHeight - element.Height) / 2,
            VerticalAlignment.Bottom => containerHeight - element.Height,
            _ => element.Position.Y
        };

        return alignedPosition;
    }

    /// <summary>
    /// 批量更新元素位置
    /// </summary>
    /// <param name="elements">元素列表</param>
    /// <param name="offsetX">X轴偏移</param>
    /// <param name="offsetY">Y轴偏移</param>
    /// <returns>更新任务</returns>
    public async Task BatchUpdateElementPositions(IEnumerable<GraphicElementPosition> elements, double offsetX, double offsetY)
    {
        await Task.Run(() =>
        {
            foreach (var element in elements)
            {
                if (!element.IsPositionLocked)
                {
                    element.Position.X += offsetX;
                    element.Position.Y += offsetY;
                    element.LastModified = DateTime.Now;
                }
            }
        });
    }

    /// <summary>
    /// 自动排列元素
    /// </summary>
    /// <param name="elements">元素列表</param>
    /// <param name="arrangement">排列方式</param>
    /// <param name="spacing">间距</param>
    /// <returns>排列任务</returns>
    public async Task AutoArrangeElements(IEnumerable<GraphicElementPosition> elements, ElementArrangement arrangement, double spacing)
    {
        await Task.Run(() =>
        {
            var elementList = elements.Where(e => !e.IsPositionLocked).ToList();
            if (elementList.Count == 0) return;

            switch (arrangement)
            {
                case ElementArrangement.Horizontal:
                    ArrangeHorizontally(elementList, spacing);
                    break;

                case ElementArrangement.Vertical:
                    ArrangeVertically(elementList, spacing);
                    break;

                case ElementArrangement.Grid:
                    ArrangeInGrid(elementList, spacing);
                    break;

                case ElementArrangement.Circle:
                    ArrangeInCircle(elementList, spacing);
                    break;

                case ElementArrangement.Free:
                    // 自由排列不做任何操作
                    break;
            }
        });
    }

    /// <summary>
    /// 水平排列元素
    /// </summary>
    /// <param name="elements">元素列表</param>
    /// <param name="spacing">间距</param>
    private static void ArrangeHorizontally(List<GraphicElementPosition> elements, double spacing)
    {
        double currentX = 0;

        foreach (var element in elements)
        {
            element.Position.X = currentX;
            currentX += element.Width + spacing;
            element.LastModified = DateTime.Now;
        }
    }

    /// <summary>
    /// 垂直排列元素
    /// </summary>
    /// <param name="elements">元素列表</param>
    /// <param name="spacing">间距</param>
    private static void ArrangeVertically(List<GraphicElementPosition> elements, double spacing)
    {
        double currentY = 0;

        foreach (var element in elements)
        {
            element.Position.Y = currentY;
            currentY += element.Height + spacing;
            element.LastModified = DateTime.Now;
        }
    }

    /// <summary>
    /// 网格排列元素
    /// </summary>
    /// <param name="elements">元素列表</param>
    /// <param name="spacing">间距</param>
    private static void ArrangeInGrid(List<GraphicElementPosition> elements, double spacing)
    {
        int columns = (int)Math.Ceiling(Math.Sqrt(elements.Count));
        int rows = (int)Math.Ceiling((double)elements.Count / columns);

        for (int i = 0; i < elements.Count; i++)
        {
            int row = i / columns;
            int col = i % columns;

            var element = elements[i];
            element.Position.X = col * (element.Width + spacing);
            element.Position.Y = row * (element.Height + spacing);
            element.LastModified = DateTime.Now;
        }
    }

    /// <summary>
    /// 圆形排列元素
    /// </summary>
    /// <param name="elements">元素列表</param>
    /// <param name="spacing">间距（作为半径）</param>
    private static void ArrangeInCircle(List<GraphicElementPosition> elements, double spacing)
    {
        double radius = spacing;
        double angleStep = 2 * Math.PI / elements.Count;

        for (int i = 0; i < elements.Count; i++)
        {
            double angle = i * angleStep;
            var element = elements[i];

            element.Position.X = radius * Math.Cos(angle);
            element.Position.Y = radius * Math.Sin(angle);
            element.LastModified = DateTime.Now;
        }
    }

    #endregion
}
