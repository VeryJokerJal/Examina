using ExamLab.Models;
using ExamLab.Services;
using System;
using System.Linq;
using Xunit;

namespace ExamLab.Tests;

/// <summary>
/// PowerPoint默认值功能测试类
/// </summary>
public class PowerPointDefaultValueTests
{
    [Fact]
    public void PowerPointKnowledgeService_ShouldCreateOperationPointWithDefaultValues()
    {
        // Arrange
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(PowerPointKnowledgeType.SetSlideFont);

        // Assert
        Assert.NotNull(operationPoint);
        Assert.Equal("设置幻灯片的字体", operationPoint.Name);
        Assert.NotEmpty(operationPoint.Parameters);

        // 验证幻灯片序号默认值
        ConfigurationParameter? slideParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "SlideNumber");
        Assert.NotNull(slideParam);
        Assert.Equal("1", slideParam.DefaultValue);
        Assert.Equal(ParameterType.Number, slideParam.Type);

        // 验证字体类型默认值
        ConfigurationParameter? fontParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "FontName");
        Assert.NotNull(fontParam);
        Assert.Equal("宋体", fontParam.DefaultValue);
        Assert.Equal(ParameterType.Enum, fontParam.Type);
    }

    [Fact]
    public void PowerPointKnowledgeService_ShouldCreateColorParametersWithDefaultValues()
    {
        // Arrange
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(PowerPointKnowledgeType.SetTextColor);

        // Assert
        ConfigurationParameter? colorParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "ColorValue");
        Assert.NotNull(colorParam);
        Assert.Equal("#000000", colorParam.DefaultValue);
        Assert.Equal(ParameterType.Color, colorParam.Type);
    }

    [Fact]
    public void PowerPointKnowledgeService_ShouldCreateNumberParametersWithDefaultValues()
    {
        // Arrange
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(PowerPointKnowledgeType.SetTextFontSize);

        // Assert
        ConfigurationParameter? fontSizeParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "FontSize");
        Assert.NotNull(fontSizeParam);
        Assert.Equal("18", fontSizeParam.DefaultValue);
        Assert.Equal(ParameterType.Number, fontSizeParam.Type);
        Assert.Equal(8, fontSizeParam.MinValue);
        Assert.Equal(72, fontSizeParam.MaxValue);
    }

    [Fact]
    public void PowerPointKnowledgeService_ShouldCreatePositionParametersWithDefaultValues()
    {
        // Arrange
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(PowerPointKnowledgeType.SetElementPosition);

        // Assert
        ConfigurationParameter? xParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "HorizontalPosition");
        Assert.NotNull(xParam);
        Assert.Equal("100", xParam.DefaultValue);
        Assert.Equal(ParameterType.Number, xParam.Type);

        ConfigurationParameter? yParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "VerticalPosition");
        Assert.NotNull(yParam);
        Assert.Equal("100", yParam.DefaultValue);
        Assert.Equal(ParameterType.Number, yParam.Type);
    }

    [Fact]
    public void PowerPointKnowledgeService_ShouldCreateSizeParametersWithDefaultValues()
    {
        // Arrange
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(PowerPointKnowledgeType.SetElementSize);

        // Assert
        ConfigurationParameter? widthParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "ElementWidth");
        Assert.NotNull(widthParam);
        Assert.Equal("200", widthParam.DefaultValue);
        Assert.Equal(ParameterType.Number, widthParam.Type);

        ConfigurationParameter? heightParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "ElementHeight");
        Assert.NotNull(heightParam);
        Assert.Equal("100", heightParam.DefaultValue);
        Assert.Equal(ParameterType.Number, heightParam.Type);
    }

    [Fact]
    public void PowerPointKnowledgeService_ShouldCreateEnumParametersWithDefaultValues()
    {
        // Arrange
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(PowerPointKnowledgeType.SetSlideLayout);

        // Assert
        ConfigurationParameter? layoutParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "Layout");
        Assert.NotNull(layoutParam);
        Assert.Equal("标题幻灯片", layoutParam.DefaultValue);
        Assert.Equal(ParameterType.Enum, layoutParam.Type);
        Assert.NotNull(layoutParam.EnumOptions);
        Assert.Contains("标题幻灯片", layoutParam.EnumOptionsList);
    }

    [Fact]
    public void PowerPointKnowledgeService_ShouldCreateTextParametersWithDefaultValues()
    {
        // Arrange
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(PowerPointKnowledgeType.InsertTextContent);

        // Assert
        ConfigurationParameter? textParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "TextContent");
        Assert.NotNull(textParam);
        Assert.Equal("文本内容", textParam.DefaultValue);
        Assert.Equal(ParameterType.Text, textParam.Type);
    }

    [Theory]
    [InlineData(PowerPointKnowledgeType.SetSlideFont, "FontName", "宋体")]
    [InlineData(PowerPointKnowledgeType.SetTextFontSize, "FontSize", "18")]
    [InlineData(PowerPointKnowledgeType.SetTextColor, "ColorValue", "#000000")]
    [InlineData(PowerPointKnowledgeType.SetSlideLayout, "Layout", "标题幻灯片")]
    [InlineData(PowerPointKnowledgeType.InsertTextContent, "TextContent", "文本内容")]
    [InlineData(PowerPointKnowledgeType.SetElementPosition, "HorizontalPosition", "100")]
    [InlineData(PowerPointKnowledgeType.SetElementSize, "ElementWidth", "200")]
    public void PowerPointKnowledgeService_ShouldHaveCorrectDefaultValues(PowerPointKnowledgeType knowledgeType, string parameterName, string expectedDefaultValue)
    {
        // Arrange
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(knowledgeType);

        // Assert
        ConfigurationParameter? parameter = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == parameterName);
        Assert.NotNull(parameter);
        Assert.Equal(expectedDefaultValue, parameter.DefaultValue);
    }

    [Fact]
    public void PowerPointKnowledgeService_ShouldBeAccessible()
    {
        // Arrange & Act & Assert
        Assert.NotNull(PowerPointKnowledgeService.Instance);
    }

    [Fact]
    public void PowerPointKnowledgeService_ShouldHaveAllKnowledgeConfigs()
    {
        // Arrange
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // Act
        var configs = service.GetAllKnowledgeConfigs();

        // Assert
        Assert.NotEmpty(configs);
        Assert.True(configs.Count() > 10); // 应该有很多PowerPoint操作点配置
    }

    [Fact]
    public void PowerPointKnowledgeService_ShouldThrowExceptionForInvalidKnowledgeType()
    {
        // Arrange
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.CreateOperationPoint((PowerPointKnowledgeType)999));
    }

    [Fact]
    public void PowerPointOperationPoint_ShouldHaveConsistentStructureWithWordAndExcel()
    {
        // Arrange
        PowerPointKnowledgeService pptService = PowerPointKnowledgeService.Instance;
        WordKnowledgeService wordService = WordKnowledgeService.Instance;
        ExcelKnowledgeService excelService = ExcelKnowledgeService.Instance;

        // Act
        OperationPoint pptOp = pptService.CreateOperationPoint(PowerPointKnowledgeType.SetSlideFont);
        OperationPoint wordOp = wordService.CreateOperationPoint(WordKnowledgeType.SetParagraphFont);
        OperationPoint excelOp = excelService.CreateOperationPoint(ExcelKnowledgeType.SetCellFont);

        // Assert - 验证结构一致性
        Assert.NotNull(pptOp.Id);
        Assert.NotNull(wordOp.Id);
        Assert.NotNull(excelOp.Id);
        Assert.True(pptOp.Score > 0);
        Assert.True(wordOp.Score > 0);
        Assert.True(excelOp.Score > 0);
        Assert.True(pptOp.IsEnabled);
        Assert.True(wordOp.IsEnabled);
        Assert.True(excelOp.IsEnabled);
        Assert.NotEqual(default(DateTime), pptOp.CreatedAt);
        Assert.NotEqual(default(DateTime), wordOp.CreatedAt);
        Assert.NotEqual(default(DateTime), excelOp.CreatedAt);
    }

    [Fact]
    public void PowerPointColorParameters_ShouldUseColorType()
    {
        // Arrange
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(PowerPointKnowledgeType.SetTextColor);

        // Assert
        var colorParams = operationPoint.Parameters.Where(p => p.Type == ParameterType.Color);
        Assert.NotEmpty(colorParams);

        foreach (var colorParam in colorParams)
        {
            Assert.NotNull(colorParam.DefaultValue);
            Assert.True(colorParam.DefaultValue.StartsWith("#"));
            Assert.Equal(7, colorParam.DefaultValue.Length); // #RRGGBB format
        }
    }

    [Fact]
    public void PowerPointFontConsistency_ShouldUseConsistentFontWithWordAndExcel()
    {
        // Arrange
        PowerPointKnowledgeService pptService = PowerPointKnowledgeService.Instance;
        WordKnowledgeService wordService = WordKnowledgeService.Instance;
        ExcelKnowledgeService excelService = ExcelKnowledgeService.Instance;

        // Act
        OperationPoint pptOp = pptService.CreateOperationPoint(PowerPointKnowledgeType.SetSlideFont);
        OperationPoint wordOp = wordService.CreateOperationPoint(WordKnowledgeType.SetParagraphFont);
        OperationPoint excelOp = excelService.CreateOperationPoint(ExcelKnowledgeType.SetCellFont);

        // Assert - 验证字体默认值一致性
        var pptFontParam = pptOp.Parameters.First(p => p.Name == "FontName");
        var wordFontParam = wordOp.Parameters.First(p => p.Name == "FontFamily");
        var excelFontParam = excelOp.Parameters.First(p => p.Name == "FontFamily");

        Assert.Equal("宋体", pptFontParam.DefaultValue);
        Assert.Equal("宋体", wordFontParam.DefaultValue);
        Assert.Equal("宋体", excelFontParam.DefaultValue);
    }

    [Fact]
    public void PowerPointColorConsistency_ShouldUseConsistentColorWithWordAndExcel()
    {
        // Arrange
        PowerPointKnowledgeService pptService = PowerPointKnowledgeService.Instance;
        WordKnowledgeService wordService = WordKnowledgeService.Instance;
        ExcelKnowledgeService excelService = ExcelKnowledgeService.Instance;

        // Act
        OperationPoint pptOp = pptService.CreateOperationPoint(PowerPointKnowledgeType.SetTextColor);
        OperationPoint wordOp = wordService.CreateOperationPoint(WordKnowledgeType.SetParagraphTextColor);
        OperationPoint excelOp = excelService.CreateOperationPoint(ExcelKnowledgeType.SetFontColor);

        // Assert - 验证颜色默认值一致性
        var pptColorParam = pptOp.Parameters.First(p => p.Name == "ColorValue");
        var wordColorParam = wordOp.Parameters.First(p => p.Name == "TextColor");
        var excelColorParam = excelOp.Parameters.First(p => p.Name == "FontColor");

        Assert.Equal("#000000", pptColorParam.DefaultValue);
        Assert.Equal("#000000", wordColorParam.DefaultValue);
        Assert.Equal("#000000", excelColorParam.DefaultValue);
    }
}
