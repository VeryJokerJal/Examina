using System;
using System.Collections.Generic;
using ExamLab.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    private readonly Dictionary<string, FrameworkElement> _parameterControls = [];

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
        ScoreNumberBox.Value = operationPoint.Score;

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
        StackPanel labelPanel = new()
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Top
        };

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
        List<string> constraints = [];

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
            ParameterType.Color => CreateColorControl(parameter),
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
            PlaceholderText = string.IsNullOrEmpty(parameter.DefaultValue) ? "输入数值（-1表示匹配任意值）" : $"{parameter.DefaultValue}（-1表示匹配任意值）",
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
            // 允许-1作为通配符值，所以最小值设为-1
            numberBox.Minimum = Math.Min(-1, parameter.MinValue.Value);
        }
        else
        {
            // 如果没有设置最小值，默认允许-1
            numberBox.Minimum = -1;
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
    /// 创建颜色编辑控件
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <returns>颜色控件组合</returns>
    private StackPanel CreateColorControl(ConfigurationParameter parameter)
    {
        StackPanel colorPanel = new()
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        // 创建ColorPicker
        ColorPicker colorPicker = new()
        {
            ColorSpectrumShape = ColorSpectrumShape.Box,
            IsMoreButtonVisible = false,
            IsColorSliderVisible = true,
            IsColorChannelTextInputVisible = true,
            IsHexInputVisible = true,
            IsAlphaEnabled = false,
            IsAlphaSliderVisible = false,
            IsAlphaTextInputVisible = false
        };

        // 创建十六进制文本输入框
        TextBox hexTextBox = new()
        {
            PlaceholderText = "#B4F4FF",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // 设置初始值
        string initialValue = parameter.Value ?? parameter.DefaultValue ?? "#000000";
        if (TryParseHexColor(initialValue, out Windows.UI.Color color))
        {
            colorPicker.Color = color;
            hexTextBox.Text = initialValue;
        }
        else
        {
            colorPicker.Color = Windows.UI.Color.FromArgb(255, 0, 0, 0); // 黑色
            hexTextBox.Text = "#000000";
        }

        // ColorPicker变化时更新文本框
        colorPicker.ColorChanged += (sender, args) =>
        {
            Windows.UI.Color selectedColor = args.NewColor;
            string hexValue = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";
            hexTextBox.Text = hexValue;
        };

        // 文本框变化时更新ColorPicker
        hexTextBox.TextChanged += (sender, args) =>
        {
            if (TryParseHexColor(hexTextBox.Text, out Windows.UI.Color parsedColor))
            {
                colorPicker.Color = parsedColor;
            }
        };

        colorPanel.Children.Add(colorPicker);
        colorPanel.Children.Add(hexTextBox);

        // 将文本框注册为主控件，用于获取值
        _parameterControls[parameter.Name] = hexTextBox;

        return colorPanel;
    }

    /// <summary>
    /// 尝试解析十六进制颜色值
    /// </summary>
    /// <param name="hexColor">十六进制颜色字符串</param>
    /// <param name="color">解析出的颜色</param>
    /// <returns>是否解析成功</returns>
    private static bool TryParseHexColor(string hexColor, out Windows.UI.Color color)
    {
        color = Windows.UI.Color.FromArgb(255, 0, 0, 0); // 黑色

        if (string.IsNullOrWhiteSpace(hexColor))
        {
            return false;
        }

        string hex = hexColor.TrimStart('#');

        if (hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int hexValue))
        {
            color = Windows.UI.Color.FromArgb(255,
                (byte)((hexValue >> 16) & 0xFF),
                (byte)((hexValue >> 8) & 0xFF),
                (byte)(hexValue & 0xFF));
            return true;
        }
        else if (hex.Length == 3)
        {
            // 支持 #RGB 格式，转换为 #RRGGBB
            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int shortHexValue))
            {
                byte r = (byte)(((shortHexValue >> 8) & 0xF) * 17);
                byte g = (byte)(((shortHexValue >> 4) & 0xF) * 17);
                byte b = (byte)((shortHexValue & 0xF) * 17);
                color = Windows.UI.Color.FromArgb(255, r, g, b);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取参数值
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <returns>参数值</returns>
    public string GetParameterValue(ConfigurationParameter parameter)
    {
        return !_parameterControls.TryGetValue(parameter.Name, out FrameworkElement? control)
            ? parameter.Value ?? ""
            : control switch
            {
                NumberBox numberBox => numberBox.Value.ToString(),
                ComboBox comboBox => comboBox.SelectedItem?.ToString() ?? "",
                CheckBox checkBox => checkBox.IsChecked?.ToString() ?? "false",
                TextBox textBox => textBox.Text,
                ColorPicker colorPicker => $"#{colorPicker.Color.R:X2}{colorPicker.Color.G:X2}{colorPicker.Color.B:X2}",
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

    /// <summary>
    /// 获取操作点分数
    /// </summary>
    /// <returns>操作点分数</returns>
    public decimal GetScore()
    {
        return (decimal)ScoreNumberBox.Value;
    }
}
