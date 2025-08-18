using System;
using System.Windows;
using System.Windows.Controls;
using ExamLab.Models;

namespace ExamLab.Views.Controls;

/// <summary>
/// PositionParameterEditor.xaml 的交互逻辑
/// </summary>
public partial class PositionParameterEditor : UserControl
{
    #region 依赖属性

    /// <summary>
    /// 位置参数依赖属性
    /// </summary>
    public static readonly DependencyProperty PositionParameterProperty =
        DependencyProperty.Register(nameof(PositionParameter), typeof(PositionParameter), typeof(PositionParameterEditor),
            new PropertyMetadata(null, OnPositionParameterChanged));

    /// <summary>
    /// 位置参数
    /// </summary>
    public PositionParameter? PositionParameter
    {
        get => (PositionParameter?)GetValue(PositionParameterProperty);
        set => SetValue(PositionParameterProperty, value);
    }

    #endregion

    #region 事件

    /// <summary>
    /// 位置参数变化事件
    /// </summary>
    public event EventHandler<PositionParameter?>? PositionParameterChanged;

    #endregion

    #region 构造函数

    public PositionParameterEditor()
    {
        InitializeComponent();
        InitializeControls();
    }

    #endregion

    #region 方法

    /// <summary>
    /// 初始化控件
    /// </summary>
    private void InitializeControls()
    {
        // 设置默认选择
        PositionTypeComboBox.SelectedIndex = 0;
        
        // 绑定事件
        XCoordinateTextBox.TextChanged += OnParameterChanged;
        YCoordinateTextBox.TextChanged += OnParameterChanged;
        RelativeXCoordinateTextBox.TextChanged += OnParameterChanged;
        RelativeYCoordinateTextBox.TextChanged += OnParameterChanged;
        CoordinateSystemComboBox.SelectionChanged += OnParameterChanged;
        RelativeCoordinateSystemComboBox.SelectionChanged += OnParameterChanged;
        RelativeReferenceComboBox.SelectionChanged += OnParameterChanged;
        HorizontalAlignmentComboBox.SelectionChanged += OnParameterChanged;
        VerticalAlignmentComboBox.SelectionChanged += OnParameterChanged;
        LockAspectRatioCheckBox.Checked += OnParameterChanged;
        LockAspectRatioCheckBox.Unchecked += OnParameterChanged;
        RelativeLockAspectRatioCheckBox.Checked += OnParameterChanged;
        RelativeLockAspectRatioCheckBox.Unchecked += OnParameterChanged;
    }

    /// <summary>
    /// 位置参数变化时的处理
    /// </summary>
    private static void OnPositionParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PositionParameterEditor editor)
        {
            editor.LoadPositionParameter();
        }
    }

    /// <summary>
    /// 加载位置参数到UI
    /// </summary>
    private void LoadPositionParameter()
    {
        if (PositionParameter == null)
        {
            // 重置UI到默认状态
            PositionTypeComboBox.SelectedIndex = 0;
            UpdatePositionTypeVisibility();
            return;
        }

        // 设置位置类型
        PositionTypeComboBox.SelectedValue = PositionParameter.Type.ToString();
        
        // 根据类型加载相应参数
        switch (PositionParameter.Type)
        {
            case PositionType.Absolute:
                LoadAbsolutePosition();
                break;
            case PositionType.Relative:
                LoadRelativePosition();
                break;
            case PositionType.Alignment:
                LoadAlignmentPosition();
                break;
        }
        
        UpdatePositionTypeVisibility();
        UpdatePreview();
    }

    /// <summary>
    /// 加载绝对位置参数
    /// </summary>
    private void LoadAbsolutePosition()
    {
        if (PositionParameter == null) return;
        
        CoordinateSystemComboBox.SelectedValue = PositionParameter.CoordinateSystem.ToString();
        XCoordinateTextBox.Text = PositionParameter.X?.ToString() ?? "";
        YCoordinateTextBox.Text = PositionParameter.Y?.ToString() ?? "";
        LockAspectRatioCheckBox.IsChecked = PositionParameter.LockAspectRatio;
    }

    /// <summary>
    /// 加载相对位置参数
    /// </summary>
    private void LoadRelativePosition()
    {
        if (PositionParameter == null) return;
        
        RelativeReferenceComboBox.SelectedValue = PositionParameter.RelativeRef?.ToString() ?? "Page";
        RelativeCoordinateSystemComboBox.SelectedValue = PositionParameter.CoordinateSystem.ToString();
        RelativeXCoordinateTextBox.Text = PositionParameter.X?.ToString() ?? "";
        RelativeYCoordinateTextBox.Text = PositionParameter.Y?.ToString() ?? "";
        RelativeLockAspectRatioCheckBox.IsChecked = PositionParameter.LockAspectRatio;
    }

    /// <summary>
    /// 加载对齐位置参数
    /// </summary>
    private void LoadAlignmentPosition()
    {
        if (PositionParameter == null) return;
        
        HorizontalAlignmentComboBox.SelectedValue = PositionParameter.HorizontalAlign?.ToString() ?? "Left";
        VerticalAlignmentComboBox.SelectedValue = PositionParameter.VerticalAlign?.ToString() ?? "Top";
    }

    /// <summary>
    /// 位置类型选择变化处理
    /// </summary>
    private void PositionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePositionTypeVisibility();
        CreateNewPositionParameter();
        UpdatePreview();
    }

    /// <summary>
    /// 更新位置类型的可见性
    /// </summary>
    private void UpdatePositionTypeVisibility()
    {
        if (PositionTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            string? tag = selectedItem.Tag?.ToString();
            
            AbsolutePositionGroup.Visibility = tag == "Absolute" ? Visibility.Visible : Visibility.Collapsed;
            RelativePositionGroup.Visibility = tag == "Relative" ? Visibility.Visible : Visibility.Collapsed;
            AlignmentPositionGroup.Visibility = tag == "Alignment" ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// 创建新的位置参数
    /// </summary>
    private void CreateNewPositionParameter()
    {
        if (PositionTypeComboBox.SelectedItem is not ComboBoxItem selectedItem)
            return;
            
        string? tag = selectedItem.Tag?.ToString();
        
        PositionParameter = tag switch
        {
            "Absolute" => new PositionParameter { Type = PositionType.Absolute },
            "Relative" => new PositionParameter { Type = PositionType.Relative, RelativeRef = RelativeReference.Page },
            "Alignment" => new PositionParameter { Type = PositionType.Alignment, HorizontalAlign = HorizontalAlignment.Left, VerticalAlign = VerticalAlignment.Top },
            _ => new PositionParameter()
        };
    }

    /// <summary>
    /// 参数变化处理
    /// </summary>
    private void OnParameterChanged(object sender, EventArgs e)
    {
        UpdatePositionParameterFromUI();
        UpdatePreview();
        PositionParameterChanged?.Invoke(this, PositionParameter);
    }

    /// <summary>
    /// 从UI更新位置参数
    /// </summary>
    private void UpdatePositionParameterFromUI()
    {
        if (PositionParameter == null) return;

        switch (PositionParameter.Type)
        {
            case PositionType.Absolute:
                UpdateAbsolutePositionFromUI();
                break;
            case PositionType.Relative:
                UpdateRelativePositionFromUI();
                break;
            case PositionType.Alignment:
                UpdateAlignmentPositionFromUI();
                break;
        }
    }

    /// <summary>
    /// 从UI更新绝对位置参数
    /// </summary>
    private void UpdateAbsolutePositionFromUI()
    {
        if (PositionParameter == null) return;
        
        if (CoordinateSystemComboBox.SelectedItem is ComboBoxItem coordItem)
        {
            Enum.TryParse<CoordinateSystem>(coordItem.Tag?.ToString(), out CoordinateSystem coordSystem);
            PositionParameter.CoordinateSystem = coordSystem;
        }
        
        if (double.TryParse(XCoordinateTextBox.Text, out double x))
            PositionParameter.X = x;
            
        if (double.TryParse(YCoordinateTextBox.Text, out double y))
            PositionParameter.Y = y;
            
        PositionParameter.LockAspectRatio = LockAspectRatioCheckBox.IsChecked ?? false;
    }

    /// <summary>
    /// 从UI更新相对位置参数
    /// </summary>
    private void UpdateRelativePositionFromUI()
    {
        if (PositionParameter == null) return;
        
        if (RelativeReferenceComboBox.SelectedItem is ComboBoxItem refItem)
        {
            Enum.TryParse<RelativeReference>(refItem.Tag?.ToString(), out RelativeReference relRef);
            PositionParameter.RelativeRef = relRef;
        }
        
        if (RelativeCoordinateSystemComboBox.SelectedItem is ComboBoxItem coordItem)
        {
            Enum.TryParse<CoordinateSystem>(coordItem.Tag?.ToString(), out CoordinateSystem coordSystem);
            PositionParameter.CoordinateSystem = coordSystem;
        }
        
        if (double.TryParse(RelativeXCoordinateTextBox.Text, out double x))
            PositionParameter.X = x;
            
        if (double.TryParse(RelativeYCoordinateTextBox.Text, out double y))
            PositionParameter.Y = y;
            
        PositionParameter.LockAspectRatio = RelativeLockAspectRatioCheckBox.IsChecked ?? false;
    }

    /// <summary>
    /// 从UI更新对齐位置参数
    /// </summary>
    private void UpdateAlignmentPositionFromUI()
    {
        if (PositionParameter == null) return;
        
        if (HorizontalAlignmentComboBox.SelectedItem is ComboBoxItem hAlignItem)
        {
            Enum.TryParse<HorizontalAlignment>(hAlignItem.Tag?.ToString(), out HorizontalAlignment hAlign);
            PositionParameter.HorizontalAlign = hAlign;
        }
        
        if (VerticalAlignmentComboBox.SelectedItem is ComboBoxItem vAlignItem)
        {
            Enum.TryParse<VerticalAlignment>(vAlignItem.Tag?.ToString(), out VerticalAlignment vAlign);
            PositionParameter.VerticalAlign = vAlign;
        }
    }

    /// <summary>
    /// 更新预览
    /// </summary>
    private void UpdatePreview()
    {
        if (PositionParameter == null)
        {
            PositionPreviewTextBlock.Text = "请选择位置类型并设置参数";
            return;
        }
        
        PositionPreviewTextBlock.Text = PositionParameter.GetDescription();
    }

    #endregion
}
