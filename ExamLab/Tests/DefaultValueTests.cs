using ExamLab.Models;
using ExamLab.Services;
using System;
using System.Linq;
using Xunit;

namespace ExamLab.Tests;

/// <summary>
/// 默认值功能测试类
/// </summary>
public class DefaultValueTests
{
    [Fact]
    public void WordKnowledgeService_ShouldCreateOperationPointWithDefaultValues()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphFont);

        // Assert
        Assert.NotNull(operationPoint);
        Assert.Equal("设置段落的字体", operationPoint.Name);
        Assert.NotEmpty(operationPoint.Parameters);

        // 验证段落序号默认值
        ConfigurationParameter? paragraphParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "ParagraphNumber");
        Assert.NotNull(paragraphParam);
        Assert.Equal("-1", paragraphParam.DefaultValue);
        Assert.Equal(ParameterType.Number, paragraphParam.Type);

        // 验证字体类型默认值
        ConfigurationParameter? fontParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "FontFamily");
        Assert.NotNull(fontParam);
        Assert.Equal("宋体", fontParam.DefaultValue);
        Assert.Equal(ParameterType.Enum, fontParam.Type);
    }

    [Fact]
    public void WordKnowledgeService_ShouldCreateColorParametersWithDefaultValues()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphTextColor);

        // Assert
        ConfigurationParameter? colorParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "TextColor");
        Assert.NotNull(colorParam);
        Assert.Equal("#000000", colorParam.DefaultValue);
        Assert.Equal(ParameterType.Color, colorParam.Type);
    }

    [Fact]
    public void WordKnowledgeService_ShouldCreateNumberParametersWithDefaultValues()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphFontSize);

        // Assert
        ConfigurationParameter? fontSizeParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "FontSize");
        Assert.NotNull(fontSizeParam);
        Assert.Equal("12", fontSizeParam.DefaultValue);
        Assert.Equal(ParameterType.Number, fontSizeParam.Type);
        Assert.Equal(8, fontSizeParam.MinValue);
        Assert.Equal(72, fontSizeParam.MaxValue);
    }

    [Fact]
    public void WordKnowledgeService_ShouldCreateEnumParametersWithDefaultValues()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphAlignment);

        // Assert
        ConfigurationParameter? alignmentParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "Alignment");
        Assert.NotNull(alignmentParam);
        Assert.Equal("左对齐", alignmentParam.DefaultValue);
        Assert.Equal(ParameterType.Enum, alignmentParam.Type);
        Assert.NotNull(alignmentParam.EnumOptions);
        Assert.Contains("左对齐", alignmentParam.EnumOptionsList);
    }

    [Fact]
    public void WordKnowledgeService_ShouldCreateTextParametersWithDefaultValues()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetWatermarkText);

        // Assert
        ConfigurationParameter? textParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "WatermarkText");
        Assert.NotNull(textParam);
        Assert.Equal("机密", textParam.DefaultValue);
        Assert.Equal(ParameterType.Text, textParam.Type);
    }

    [Fact]
    public void PowerPointKnowledgeService_ShouldCreateOperationPointWithDefaultValues()
    {
        // Arrange
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(PowerPointKnowledgeType.SetSlideTitle);

        // Assert
        Assert.NotNull(operationPoint);
        Assert.NotEmpty(operationPoint.Parameters);

        // 验证幻灯片序号默认值
        ConfigurationParameter? slideParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "SlideNumber");
        Assert.NotNull(slideParam);
        Assert.Equal("-1", slideParam.DefaultValue);
        Assert.Equal(ParameterType.Number, slideParam.Type);

        // 验证标题文字默认值
        ConfigurationParameter? titleParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "TitleText");
        Assert.NotNull(titleParam);
        Assert.Equal("标题内容", titleParam.DefaultValue);
        Assert.Equal(ParameterType.Text, titleParam.Type);
    }

    [Fact]
    public void WindowsKnowledgeService_ShouldCreateOperationPointWithDefaultValues()
    {
        // Arrange
        WindowsKnowledgeService service = WindowsKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WindowsKnowledgeType.CreateFolder);

        // Assert
        Assert.NotNull(operationPoint);
        Assert.NotEmpty(operationPoint.Parameters);

        // 验证文件夹名称默认值
        ConfigurationParameter? nameParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "FolderName");
        Assert.NotNull(nameParam);
        Assert.Equal("新建文件夹", nameParam.DefaultValue);
        Assert.Equal(ParameterType.Text, nameParam.Type);
    }

    [Theory]
    [InlineData(WordKnowledgeType.SetParagraphFont, "ParagraphNumber", "-1")]
    [InlineData(WordKnowledgeType.SetParagraphFontSize, "FontSize", "12")]
    [InlineData(WordKnowledgeType.SetParagraphTextColor, "TextColor", "#000000")]
    [InlineData(WordKnowledgeType.SetParagraphAlignment, "Alignment", "左对齐")]
    [InlineData(WordKnowledgeType.SetWatermarkText, "WatermarkText", "机密")]
    public void WordKnowledgeService_ShouldHaveCorrectDefaultValues(WordKnowledgeType knowledgeType, string parameterName, string expectedDefaultValue)
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(knowledgeType);

        // Assert
        ConfigurationParameter? parameter = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == parameterName);
        Assert.NotNull(parameter);
        Assert.Equal(expectedDefaultValue, parameter.DefaultValue);
    }

    [Fact]
    public void AllKnowledgeServices_ShouldBeAccessible()
    {
        // Arrange & Act & Assert
        Assert.NotNull(WordKnowledgeService.Instance);
        Assert.NotNull(PowerPointKnowledgeService.Instance);
        Assert.NotNull(WindowsKnowledgeService.Instance);
    }

    [Fact]
    public void ConfigurationParameter_ShouldSupportAllParameterTypes()
    {
        // Arrange
        var parameterTypes = Enum.GetValues<ParameterType>();

        // Act & Assert
        foreach (ParameterType type in parameterTypes)
        {
            ConfigurationParameter parameter = new()
            {
                Type = type,
                Name = $"Test{type}Parameter",
                DisplayName = $"测试{type}参数"
            };

            Assert.Equal(type, parameter.Type);
            Assert.NotNull(parameter.Name);
            Assert.NotNull(parameter.DisplayName);
        }
    }

    [Fact]
    public void ValidationService_ShouldValidateColorParameters()
    {
        // Arrange
        ConfigurationParameter colorParameter = new()
        {
            Name = "TestColor",
            Type = ParameterType.Color,
            Value = "#FF0000",
            IsRequired = true
        };

        // Act
        var result = ValidationService.ValidateParameter(colorParameter);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidationService_ShouldRejectInvalidColorFormat()
    {
        // Arrange
        ConfigurationParameter colorParameter = new()
        {
            Name = "TestColor",
            Type = ParameterType.Color,
            Value = "invalid-color",
            IsRequired = true
        };

        // Act
        var result = ValidationService.ValidateParameter(colorParameter);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("颜色格式不正确", result.Errors.First());
    }
}
