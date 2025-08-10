using Microsoft.UI.Xaml.Controls;
using ExamLab.Models;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace ExamLab.Views;

/// <summary>
/// 操作点编辑页面
/// </summary>
public sealed partial class OperationPointEditPage : Page
{
    /// <summary>
    /// 参数编辑控件字典
    /// </summary>
    private readonly Dictionary<string, FrameworkElement> _parameterControls = new();

    /// <summary>
    /// 当前编辑的操作点
    /// </summary>
    public OperationPoint? OperationPoint { get; private set; }

    public OperationPointEditPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 初始化页面内容
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    public void Initialize(OperationPoint operationPoint)
    {
        OperationPoint = operationPoint;
        InitializeControls(operationPoint);
    }

    /// <summary>
    /// 初始化控件
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    private void InitializeControls(OperationPoint operationPoint)
    {
        // 设置基本信息
        NameTextBox.Text = operationPoint.Name;
        DescriptionTextBox.Text = operationPoint.Description;

        // 创建所有参数的编辑控件
        foreach (ConfigurationParameter parameter in operationPoint.Parameters)
        {
            CreateParameterControl(parameter);
        }
    }

    /// <summary>
    /// 创建参数编辑控件
    /// </summary>
    /// <param name="parameter">参数</param>
    private void CreateParameterControl(ConfigurationParameter parameter)
    {
        // 创建参数容器
        Grid parameterGrid = new() 
        { 
            Margin = new Thickness(0, 0, 0, 16)
        };
        
        parameterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        parameterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // 创建参数标签
        StackPanel labelPanel = new() { Orientation = Orientation.Horizontal };
        
        TextBlock labelText = new() 
        { 
            Text = parameter.DisplayName, 
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        labelPanel.Children.Add(labelText);

        if (parameter.IsRequired)
        {
            TextBlock requiredMark = new() 
            { 
                Text = " *", 
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                VerticalAlignment = VerticalAlignment.Center
            };
            labelPanel.Children.Add(requiredMark);
        }

        Grid.SetColumn(labelPanel, 0);
        parameterGrid.Children.Add(labelPanel);

        // 创建右侧内容面板
        StackPanel contentPanel = new() { Spacing = 4 };

        // 根据参数类型创建编辑控件
        FrameworkElement editControl = CreateEditControlByType(parameter);
        _parameterControls[parameter.Name] = editControl;
        contentPanel.Children.Add(editControl);

        // 添加参数描述
        if (!string.IsNullOrWhiteSpace(parameter.Description))
        {
            TextBlock descriptionText = new() 
            { 
                Text = parameter.Description, 
                FontSize = 11,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                TextWrapping = TextWrapping.Wrap
            };
            contentPanel.Children.Add(descriptionText);
        }

        // 添加类型和约束信息
        string constraintInfo = GetConstraintInfo(parameter);
        if (!string.IsNullOrEmpty(constraintInfo))
        {
            TextBlock constraintText = new() 
            { 
                Text = constraintInfo, 
                FontSize = 10,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkGray),
                TextWrapping = TextWrapping.Wrap
            };
            contentPanel.Children.Add(constraintText);
        }

        Grid.SetColumn(contentPanel, 1);
        parameterGrid.Children.Add(contentPanel);

        // 添加到参数面板
        ParametersPanel.Children.Add(parameterGrid);
    }

    /// <summary>
    /// 获取参数约束信息
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <returns>约束信息</returns>
    private static string GetConstraintInfo(ConfigurationParameter parameter)
    {
        List<string> constraints = new();

        // 添加类型信息
        string typeInfo = parameter.Type switch
        {
            ParameterType.Number => "数字",
            ParameterType.Enum => "选择项",
            ParameterType.Boolean => "布尔值",
            _ => "文本"
        };
        constraints.Add($"类型：{typeInfo}");

        // 添加范围信息
        if (parameter.Type == ParameterType.Number)
        {
            if (parameter.MinValue.HasValue && parameter.MaxValue.HasValue)
            {
                constraints.Add($"范围：{parameter.MinValue.Value} - {parameter.MaxValue.Value}");
            }
            else if (parameter.MinValue.HasValue)
            {
                constraints.Add($"最小值：{parameter.MinValue.Value}");
            }
            else if (parameter.MaxValue.HasValue)
            {
                constraints.Add($"最大值：{parameter.MaxValue.Value}");
            }
        }

        // 添加枚举选项
        if (parameter.Type == ParameterType.Enum && parameter.EnumOptionsList.Count > 0)
        {
            constraints.Add($"可选值：{string.Join(", ", parameter.EnumOptionsList)}");
        }

        // 添加默认值
        if (!string.IsNullOrWhiteSpace(parameter.DefaultValue))
        {
            constraints.Add($"默认值：{parameter.DefaultValue}");
        }

        return string.Join(" | ", constraints);
    }

    /// <summary>
    /// 根据参数类型创建编辑控件
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <returns>编辑控件</returns>
    private FrameworkElement CreateEditControlByType(ConfigurationParameter parameter)
    {
        return parameter.Type switch
        {
            ParameterType.Number => CreateNumberControl(parameter),
            ParameterType.Enum => CreateEnumControl(parameter),
            ParameterType.Boolean => CreateBooleanControl(parameter),
            _ => CreateTextControl(parameter)
        };
    }

    /// <summary>
    /// 创建数字编辑控件
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <returns>NumberBox控件</returns>
    private NumberBox CreateNumberControl(ConfigurationParameter parameter)
    {
        NumberBox numberBox = new()
        {
            PlaceholderText = parameter.DefaultValue,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (int.TryParse(parameter.Value, out int currentValue))
        {
            numberBox.Value = currentValue;
        }
        else if (int.TryParse(parameter.DefaultValue, out int defaultValue))
        {
            numberBox.Value = defaultValue;
        }

        if (parameter.MinValue.HasValue)
        {
            numberBox.Minimum = parameter.MinValue.Value;
        }

        if (parameter.MaxValue.HasValue)
        {
            numberBox.Maximum = parameter.MaxValue.Value;
        }

        return numberBox;
    }

    /// <summary>
    /// 创建枚举编辑控件
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <returns>ComboBox控件</returns>
    private ComboBox CreateEnumControl(ConfigurationParameter parameter)
    {
        ComboBox comboBox = new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // 添加枚举选项
        foreach (string option in parameter.EnumOptionsList)
        {
            comboBox.Items.Add(option);
        }

        // 设置当前值
        if (!string.IsNullOrWhiteSpace(parameter.Value))
        {
            comboBox.SelectedItem = parameter.Value;
        }
        else if (!string.IsNullOrWhiteSpace(parameter.DefaultValue))
        {
            comboBox.SelectedItem = parameter.DefaultValue;
        }

        return comboBox;
    }

    /// <summary>
    /// 创建布尔编辑控件
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <returns>CheckBox控件</returns>
    private CheckBox CreateBooleanControl(ConfigurationParameter parameter)
    {
        CheckBox checkBox = new()
        {
            Content = "启用"
        };

        // 设置当前值
        if (bool.TryParse(parameter.Value, out bool currentValue))
        {
            checkBox.IsChecked = currentValue;
        }
        else if (bool.TryParse(parameter.DefaultValue, out bool defaultValue))
        {
            checkBox.IsChecked = defaultValue;
        }

        return checkBox;
    }

    /// <summary>
    /// 创建文本编辑控件
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <returns>TextBox控件</returns>
    private TextBox CreateTextControl(ConfigurationParameter parameter)
    {
        TextBox textBox = new()
        {
            Text = parameter.Value ?? parameter.DefaultValue ?? "",
            PlaceholderText = parameter.DefaultValue,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        return textBox;
    }

    /// <summary>
    /// 获取参数值
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <returns>参数值</returns>
    public string GetParameterValue(ConfigurationParameter parameter)
    {
        if (!_parameterControls.TryGetValue(parameter.Name, out FrameworkElement? control))
        {
            return parameter.Value ?? "";
        }

        return control switch
        {
            NumberBox numberBox => numberBox.Value.ToString(),
            ComboBox comboBox => comboBox.SelectedItem?.ToString() ?? "",
            CheckBox checkBox => checkBox.IsChecked?.ToString() ?? "false",
            TextBox textBox => textBox.Text,
            _ => parameter.Value ?? ""
        };
    }

    /// <summary>
    /// 显示错误信息
    /// </summary>
    /// <param name="message">错误信息</param>
    public void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 隐藏错误信息
    /// </summary>
    public void HideError()
    {
        ErrorTextBlock.Visibility = Visibility.Collapsed;
    }
}
