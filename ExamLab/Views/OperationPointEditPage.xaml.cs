using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ExamLab.Models;
using ExamLab.Services;
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
        ScoreNumberBox.Value = (double)operationPoint.Score;

        // 初始化位置参数控制器
        if (IsPositionKnowledgePoint(operationPoint))
        {
            PositionParameterController.InitializePositionParameters(operationPoint.Parameters);
        }

        // 初始化背景填充参数可见性
        if (IsBackgroundFillKnowledgePoint(operationPoint))
        {
            InitializeBackgroundFillParameters(operationPoint.Parameters);
        }

        // 创建所有参数的编辑控件
        foreach (ConfigurationParameter parameter in operationPoint.Parameters)
        {
            CreateParameterControl(parameter);
        }
    }

    /// <summary>
    /// 判断是否为位置相关的知识点
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    /// <returns>是否为位置知识点</returns>
    private static bool IsPositionKnowledgePoint(OperationPoint operationPoint)
    {
        string[] positionKnowledgePoints =
        [
            "设置文本框位置",
            "设置自选图形位置",
            "设置图片位置"
        ];

        return positionKnowledgePoints.Contains(operationPoint.Name);
    }

    /// <summary>
    /// 判断是否为背景填充相关的知识点
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    /// <returns>是否为背景填充知识点</returns>
    private static bool IsBackgroundFillKnowledgePoint(OperationPoint operationPoint)
    {
        return operationPoint.Name == "设置幻灯片背景";
    }

    /// <summary>
    /// 初始化背景填充参数
    /// </summary>
    /// <param name="parameters">参数列表</param>
    private static void InitializeBackgroundFillParameters(ObservableCollection<ConfigurationParameter> parameters)
    {
        // 获取填充类型参数
        ConfigurationParameter? fillTypeParam = parameters.FirstOrDefault(p => p.Name == "FillType");

        // 初始化依赖参数的可见性
        List<ConfigurationParameter> dependentParameters = [.. parameters.Where(p => p.DependsOn == "FillType")];

        foreach (ConfigurationParameter parameter in dependentParameters)
        {
            // 初始状态根据当前填充类型值设置可见性
            bool shouldBeVisible = !string.IsNullOrEmpty(fillTypeParam?.Value) &&
                                   !string.IsNullOrEmpty(parameter.DependsOnValue) &&
                                   parameter.DependsOnValue == fillTypeParam.Value;
            parameter.IsVisible = shouldBeVisible;
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

        // 绑定可见性到参数的 IsVisible 属性
        parameterGrid.SetBinding(UIElement.VisibilityProperty, new Microsoft.UI.Xaml.Data.Binding
        {
            Source = parameter,
            Path = new PropertyPath("IsVisible"),
            Converter = (Microsoft.UI.Xaml.Data.IValueConverter)Application.Current.Resources["ParameterVisibilityConverter"]
        });

        parameterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        parameterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        parameterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // 创建参数标签
        TextBlock labelText = new()
        {
            Text = parameter.DisplayName,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Top,
            TextWrapping = TextWrapping.Wrap
        };

        if (parameter.IsRequired)
        {
            TextBlock requiredMark = new()
            {
                Text = "*",
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(requiredMark, 1);
            parameterGrid.Children.Add(requiredMark);
        }

        Grid.SetColumn(labelText, 0);
        parameterGrid.Children.Add(labelText);

        // 创建右侧内容面板
        StackPanel contentPanel = new() { Spacing = 4 };

        // 根据参数类型创建编辑控件
        FrameworkElement editControl = CreateEditControlByType(parameter);

        // 绑定启用状态到参数的 IsVisible 属性
        editControl.SetBinding(Control.IsEnabledProperty, new Microsoft.UI.Xaml.Data.Binding
        {
            Source = parameter,
            Path = new PropertyPath("IsVisible"),
            Converter = (Microsoft.UI.Xaml.Data.IValueConverter)Application.Current.Resources["ParameterEnabledConverter"]
        });

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

        Grid.SetColumn(contentPanel, 2);
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
            ParameterType.File => "文件路径",
            ParameterType.Folder => "文件夹路径",
            ParameterType.Color => "颜色",
            ParameterType.MultipleChoice => "多选",
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
            ParameterType.File => CreateFileControl(parameter),
            ParameterType.Folder => CreateFolderControl(parameter),
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

        // 为位置类型参数添加选择变更事件处理
        if (parameter.Name is "HorizontalPositionType" or "VerticalPositionType")
        {
            comboBox.SelectionChanged += (sender, e) =>
            {
                if (sender is ComboBox cb && cb.SelectedItem is string selectedValue)
                {
                    parameter.Value = selectedValue;
                    // 触发位置参数可见性更新
                    if (OperationPoint != null)
                    {
                        PositionParameterController.UpdateParameterVisibility(OperationPoint.Parameters, parameter.Name, selectedValue);
                    }
                }
            };
        }

        // 为填充类型参数添加选择变更事件处理
        if (parameter.Name == "FillType")
        {
            comboBox.SelectionChanged += (sender, e) =>
            {
                if (sender is ComboBox cb && cb.SelectedItem is string selectedValue)
                {
                    parameter.Value = selectedValue;
                    // 触发背景填充参数可见性更新
                    if (OperationPoint != null)
                    {
                        UpdateBackgroundFillParameterVisibility(OperationPoint.Parameters, selectedValue);
                    }
                }
            };
        }

        return comboBox;
    }

    /// <summary>
    /// 更新背景填充参数可见性
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <param name="fillTypeValue">填充类型值</param>
    private static void UpdateBackgroundFillParameterVisibility(ObservableCollection<ConfigurationParameter> parameters, string? fillTypeValue)
    {
        // 获取依赖于FillType的所有参数
        List<ConfigurationParameter> dependentParameters = [.. parameters.Where(p => p.DependsOn == "FillType")];

        foreach (ConfigurationParameter parameter in dependentParameters)
        {
            bool shouldBeVisible = !string.IsNullOrEmpty(fillTypeValue) &&
                                   !string.IsNullOrEmpty(parameter.DependsOnValue) &&
                                   parameter.DependsOnValue == fillTypeValue;

            if (parameter.IsVisible != shouldBeVisible)
            {
                parameter.IsVisible = shouldBeVisible;

                // 如果参数变为不可见，清空其值
                if (!shouldBeVisible)
                {
                    parameter.Value = null;
                }
            }
        }
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
    /// 创建文件选择控件
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <returns>文件选择控件组合</returns>
    private StackPanel CreateFileControl(ConfigurationParameter parameter)
    {
        StackPanel filePanel = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        TextBox pathTextBox = new()
        {
            Text = parameter.Value ?? parameter.DefaultValue ?? "",
            PlaceholderText = "选择文件路径",
            Width = 300
        };

        Button browseButton = new()
        {
            Content = "浏览文件...",
            MinWidth = 100
        };

        browseButton.Click += async (sender, e) =>
        {
            try
            {
                List<string> fileTypes = [".txt", ".xml", ".json", ".exe", ".bat", ".cmd", ".ps1", "*"];
                Windows.Storage.StorageFile? selectedFile = await FilePickerService.PickSingleFileAsync(fileTypes);

                if (selectedFile != null)
                {
                    pathTextBox.Text = selectedFile.Path;
                    parameter.Value = selectedFile.Path;
                }
            }
            catch (Exception ex)
            {
                await NotificationService.ShowErrorAsync("文件选择失败", $"无法选择文件：{ex.Message}");
            }
        };

        filePanel.Children.Add(pathTextBox);
        filePanel.Children.Add(browseButton);

        return filePanel;
    }

    /// <summary>
    /// 创建文件夹选择控件
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <returns>文件夹选择控件组合</returns>
    private StackPanel CreateFolderControl(ConfigurationParameter parameter)
    {
        StackPanel folderPanel = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        TextBox pathTextBox = new()
        {
            Text = parameter.Value ?? parameter.DefaultValue ?? "",
            PlaceholderText = "选择文件夹路径",
            Width = 300
        };

        Button browseButton = new()
        {
            Content = "浏览文件夹...",
            MinWidth = 100
        };

        browseButton.Click += async (sender, e) =>
        {
            try
            {
                Windows.Storage.StorageFolder? selectedFolder = await FolderPickerService.PickSingleFolderAsync();

                if (selectedFolder != null)
                {
                    pathTextBox.Text = selectedFolder.Path;
                    parameter.Value = selectedFolder.Path;
                }
            }
            catch (Exception ex)
            {
                await NotificationService.ShowErrorAsync("文件夹选择失败", $"无法选择文件夹：{ex.Message}");
            }
        };

        folderPanel.Children.Add(pathTextBox);
        folderPanel.Children.Add(browseButton);

        return folderPanel;
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
                StackPanel stackPanel when parameter.Type == ParameterType.File || parameter.Type == ParameterType.Folder =>
                    GetPathFromStackPanel(stackPanel),
                _ => parameter.Value ?? ""
            };
    }

    /// <summary>
    /// 从StackPanel中获取路径值
    /// </summary>
    /// <param name="stackPanel">包含TextBox的StackPanel</param>
    /// <returns>路径值</returns>
    private static string GetPathFromStackPanel(StackPanel stackPanel)
    {
        foreach (UIElement child in stackPanel.Children)
        {
            if (child is TextBox textBox)
            {
                return textBox.Text;
            }
        }
        return string.Empty;
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
    public double GetScore()
    {
        return ScoreNumberBox.Value;
    }
}
