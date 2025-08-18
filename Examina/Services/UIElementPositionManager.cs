using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Examina.Models.Position;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.Services;

/// <summary>
/// UI元素位置管理器
/// </summary>
public class UIElementPositionManager : ReactiveObject
{
    private readonly IPositionService _positionService;
    private readonly ITextBoxPositionService _textBoxPositionService;
    
    #region 属性
    
    /// <summary>
    /// 所有图形元素集合
    /// </summary>
    public ObservableCollection<GraphicElementPosition> AllElements { get; } = [];
    
    /// <summary>
    /// 文本框元素集合
    /// </summary>
    public ObservableCollection<TextBoxPosition> TextBoxElements { get; } = [];
    
    /// <summary>
    /// 操作点配置集合
    /// </summary>
    public ObservableCollection<OperationPointConfiguration> OperationConfigurations { get; } = [];
    
    /// <summary>
    /// 选中的元素集合
    /// </summary>
    public ObservableCollection<GraphicElementPosition> SelectedElements { get; } = [];
    
    /// <summary>
    /// 当前活动的元素
    /// </summary>
    [Reactive] public GraphicElementPosition? ActiveElement { get; set; }
    
    /// <summary>
    /// 是否启用网格对齐
    /// </summary>
    [Reactive] public bool IsGridSnapEnabled { get; set; } = false;
    
    /// <summary>
    /// 网格大小
    /// </summary>
    [Reactive] public double GridSize { get; set; } = 10;
    
    /// <summary>
    /// 容器宽度
    /// </summary>
    [Reactive] public double ContainerWidth { get; set; } = 800;
    
    /// <summary>
    /// 容器高度
    /// </summary>
    [Reactive] public double ContainerHeight { get; set; } = 600;
    
    /// <summary>
    /// 是否显示网格
    /// </summary>
    [Reactive] public bool ShowGrid { get; set; } = false;
    
    /// <summary>
    /// 是否显示标尺
    /// </summary>
    [Reactive] public bool ShowRuler { get; set; } = false;
    
    /// <summary>
    /// 缩放比例
    /// </summary>
    [Reactive] public double ZoomLevel { get; set; } = 1.0;
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="positionService">位置服务</param>
    /// <param name="textBoxPositionService">文本框位置服务</param>
    public UIElementPositionManager(IPositionService positionService, ITextBoxPositionService textBoxPositionService)
    {
        _positionService = positionService;
        _textBoxPositionService = textBoxPositionService;
        
        // 监听元素变化
        AllElements.CollectionChanged += OnAllElementsChanged;
        TextBoxElements.CollectionChanged += OnTextBoxElementsChanged;
    }
    
    #endregion
    
    #region 元素管理
    
    /// <summary>
    /// 添加图形元素
    /// </summary>
    /// <param name="element">图形元素</param>
    public void AddElement(GraphicElementPosition element)
    {
        if (element == null) return;
        
        // 应用网格对齐
        if (IsGridSnapEnabled)
        {
            element.Position = _positionService.ApplyGridSnap(element.Position, GridSize);
        }
        
        AllElements.Add(element);
        
        // 如果是文本框，也添加到文本框集合
        if (element is TextBoxPosition textBox)
        {
            TextBoxElements.Add(textBox);
        }
    }
    
    /// <summary>
    /// 移除图形元素
    /// </summary>
    /// <param name="element">图形元素</param>
    public void RemoveElement(GraphicElementPosition element)
    {
        if (element == null) return;
        
        AllElements.Remove(element);
        SelectedElements.Remove(element);
        
        if (element is TextBoxPosition textBox)
        {
            TextBoxElements.Remove(textBox);
        }
        
        if (ActiveElement == element)
        {
            ActiveElement = null;
        }
    }
    
    /// <summary>
    /// 清空所有元素
    /// </summary>
    public void ClearAllElements()
    {
        AllElements.Clear();
        TextBoxElements.Clear();
        SelectedElements.Clear();
        ActiveElement = null;
    }
    
    #endregion
    
    #region 选择管理
    
    /// <summary>
    /// 选择元素
    /// </summary>
    /// <param name="element">要选择的元素</param>
    /// <param name="multiSelect">是否多选</param>
    public void SelectElement(GraphicElementPosition element, bool multiSelect = false)
    {
        if (element == null) return;
        
        if (!multiSelect)
        {
            SelectedElements.Clear();
        }
        
        if (!SelectedElements.Contains(element))
        {
            SelectedElements.Add(element);
        }
        
        ActiveElement = element;
    }
    
    /// <summary>
    /// 取消选择元素
    /// </summary>
    /// <param name="element">要取消选择的元素</param>
    public void DeselectElement(GraphicElementPosition element)
    {
        if (element == null) return;
        
        SelectedElements.Remove(element);
        
        if (ActiveElement == element)
        {
            ActiveElement = SelectedElements.FirstOrDefault();
        }
    }
    
    /// <summary>
    /// 清空选择
    /// </summary>
    public void ClearSelection()
    {
        SelectedElements.Clear();
        ActiveElement = null;
    }
    
    /// <summary>
    /// 选择区域内的所有元素
    /// </summary>
    /// <param name="left">区域左边界</param>
    /// <param name="top">区域上边界</param>
    /// <param name="right">区域右边界</param>
    /// <param name="bottom">区域下边界</param>
    public void SelectElementsInArea(double left, double top, double right, double bottom)
    {
        ClearSelection();
        
        foreach (var element in AllElements)
        {
            var (elementLeft, elementTop, elementRight, elementBottom) = element.GetBounds();
            
            if (elementLeft >= left && elementTop >= top && 
                elementRight <= right && elementBottom <= bottom)
            {
                SelectedElements.Add(element);
            }
        }
        
        ActiveElement = SelectedElements.FirstOrDefault();
    }
    
    #endregion
    
    #region 位置操作
    
    /// <summary>
    /// 移动选中的元素
    /// </summary>
    /// <param name="deltaX">X轴偏移</param>
    /// <param name="deltaY">Y轴偏移</param>
    public async Task MoveSelectedElements(double deltaX, double deltaY)
    {
        await _positionService.BatchUpdateElementPositions(SelectedElements, deltaX, deltaY);
        
        // 应用网格对齐
        if (IsGridSnapEnabled)
        {
            foreach (var element in SelectedElements)
            {
                element.Position = _positionService.ApplyGridSnap(element.Position, GridSize);
            }
        }
    }
    
    /// <summary>
    /// 对齐选中的元素
    /// </summary>
    /// <param name="horizontalAlign">水平对齐方式</param>
    /// <param name="verticalAlign">垂直对齐方式</param>
    public void AlignSelectedElements(HorizontalAlignment horizontalAlign, VerticalAlignment verticalAlign)
    {
        foreach (var element in SelectedElements)
        {
            var alignedPosition = _positionService.GetAlignedPosition(
                element, horizontalAlign, verticalAlign, ContainerWidth, ContainerHeight);
            
            element.Position = alignedPosition;
        }
    }
    
    /// <summary>
    /// 排列选中的元素
    /// </summary>
    /// <param name="arrangement">排列方式</param>
    /// <param name="spacing">间距</param>
    public async Task ArrangeSelectedElements(ElementArrangement arrangement, double spacing)
    {
        await _positionService.AutoArrangeElements(SelectedElements, arrangement, spacing);
    }
    
    #endregion
    
    #region 操作点配置
    
    /// <summary>
    /// 创建位置操作点配置
    /// </summary>
    /// <param name="type">操作点类型</param>
    /// <param name="targetElement">目标元素</param>
    /// <returns>操作点配置</returns>
    public OperationPointConfiguration CreatePositionOperationConfiguration(OperationPointType type, GraphicElementPosition? targetElement = null)
    {
        var config = new OperationPointConfiguration
        {
            Name = GetOperationPointName(type),
            Description = GetOperationPointDescription(type),
            Type = type,
            TargetElement = targetElement
        };
        
        // 添加参数模板
        var templates = _positionService.CreatePositionParameterTemplates(type);
        foreach (var template in templates)
        {
            config.AddParameter(template);
        }
        
        OperationConfigurations.Add(config);
        return config;
    }
    
    /// <summary>
    /// 获取操作点名称
    /// </summary>
    /// <param name="type">操作点类型</param>
    /// <returns>操作点名称</returns>
    private static string GetOperationPointName(OperationPointType type)
    {
        return type switch
        {
            OperationPointType.SetGraphicPosition => "设置图形位置",
            OperationPointType.SetTextBoxPosition => "设置文本框位置",
            OperationPointType.SetImagePosition => "设置图像位置",
            OperationPointType.SetElementPosition => "设置元素位置",
            OperationPointType.SetShapePosition => "设置形状位置",
            OperationPointType.SetChartPosition => "设置图表位置",
            _ => "设置位置"
        };
    }
    
    /// <summary>
    /// 获取操作点描述
    /// </summary>
    /// <param name="type">操作点类型</param>
    /// <returns>操作点描述</returns>
    private static string GetOperationPointDescription(OperationPointType type)
    {
        return type switch
        {
            OperationPointType.SetGraphicPosition => "设置图形元素在文档中的位置",
            OperationPointType.SetTextBoxPosition => "设置文本框在文档中的位置和对齐方式",
            OperationPointType.SetImagePosition => "设置图像在文档中的位置",
            OperationPointType.SetElementPosition => "设置UI元素在文档中的位置",
            OperationPointType.SetShapePosition => "设置形状在文档中的位置",
            OperationPointType.SetChartPosition => "设置图表在文档中的位置",
            _ => "设置元素位置"
        };
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 处理所有元素集合变化
    /// </summary>
    private void OnAllElementsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // 可以在这里添加元素变化的处理逻辑
    }
    
    /// <summary>
    /// 处理文本框元素集合变化
    /// </summary>
    private void OnTextBoxElementsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // 可以在这里添加文本框变化的处理逻辑
    }
    
    #endregion
}
