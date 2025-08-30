using BenchSuite.Models;
using BenchSuite.Services;
using System;
using System.Linq;
using Xunit;

namespace BenchSuite.Tests;

/// <summary>
/// WordKnowledgeService测试类
/// </summary>
public class WordKnowledgeServiceTests
{
    private readonly WordKnowledgeService _service;

    public WordKnowledgeServiceTests()
    {
        _service = WordKnowledgeService.Instance;
    }

    [Fact]
    public void GetOperationConfig_ShouldReturnValidConfig_WhenOperationExists()
    {
        // Arrange
        string operationName = "SetParagraphFont";

        // Act
        WordOperationConfig? config = _service.GetOperationConfig(operationName);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("设置段落的字体", config.Name);
        Assert.Equal("段落操作", config.Category);
        Assert.NotEmpty(config.ParameterTemplates);
    }

    [Fact]
    public void GetOperationConfig_ShouldReturnNull_WhenOperationNotExists()
    {
        // Arrange
        string operationName = "NonExistentOperation";

        // Act
        WordOperationConfig? config = _service.GetOperationConfig(operationName);

        // Assert
        Assert.Null(config);
    }

    [Fact]
    public void CreateOperationPoint_ShouldCreateValidOperationPoint_WithDefaultValues()
    {
        // Arrange
        string operationName = "SetParagraphFont";

        // Act
        OperationPointModel operationPoint = _service.CreateOperationPoint(operationName);

        // Assert
        Assert.NotNull(operationPoint);
        Assert.Equal(operationName, operationPoint.Name);
        Assert.Equal(ModuleType.Word, operationPoint.ModuleType);
        Assert.NotEmpty(operationPoint.Parameters);

        // 验证段落序号参数的默认值
        ConfigurationParameterModel? paragraphParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "ParagraphNumber");
        Assert.NotNull(paragraphParam);
        Assert.Equal("-1", paragraphParam.Value);
        Assert.Equal("-1", paragraphParam.DefaultValue);

        // 验证字体类型参数的默认值
        ConfigurationParameterModel? fontParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "FontFamily");
        Assert.NotNull(fontParam);
        Assert.Equal("宋体", fontParam.Value);
        Assert.Equal("宋体", fontParam.DefaultValue);
    }

    [Fact]
    public void CreateOperationPoint_ShouldCreateColorParameters_WithCorrectType()
    {
        // Arrange
        string operationName = "SetParagraphTextColor";

        // Act
        OperationPointModel operationPoint = _service.CreateOperationPoint(operationName);

        // Assert
        ConfigurationParameterModel? colorParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "TextColor");
        Assert.NotNull(colorParam);
        Assert.Equal(ParameterType.Color, colorParam.Type);
        Assert.Equal("#000000", colorParam.Value);
        Assert.Equal("#000000", colorParam.DefaultValue);
    }

    [Fact]
    public void CreateOperationPoint_ShouldCreateNumberParameters_WithCorrectDefaults()
    {
        // Arrange
        string operationName = "SetParagraphFontSize";

        // Act
        OperationPointModel operationPoint = _service.CreateOperationPoint(operationName);

        // Assert
        ConfigurationParameterModel? fontSizeParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "FontSize");
        Assert.NotNull(fontSizeParam);
        Assert.Equal(ParameterType.Number, fontSizeParam.Type);
        Assert.Equal("12", fontSizeParam.Value);
        Assert.Equal("12", fontSizeParam.DefaultValue);
    }

    [Fact]
    public void CreateOperationPoint_ShouldCreateEnumParameters_WithCorrectDefaults()
    {
        // Arrange
        string operationName = "SetParagraphAlignment";

        // Act
        OperationPointModel operationPoint = _service.CreateOperationPoint(operationName);

        // Assert
        ConfigurationParameterModel? alignmentParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "Alignment");
        Assert.NotNull(alignmentParam);
        Assert.Equal(ParameterType.Enum, alignmentParam.Type);
        Assert.Equal("左对齐", alignmentParam.Value);
        Assert.Equal("左对齐", alignmentParam.DefaultValue);
    }

    [Fact]
    public void CreateOperationPoint_ShouldCreateTextParameters_WithCorrectDefaults()
    {
        // Arrange
        string operationName = "SetWatermarkText";

        // Act
        OperationPointModel operationPoint = _service.CreateOperationPoint(operationName);

        // Assert
        ConfigurationParameterModel? textParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "WatermarkText");
        Assert.NotNull(textParam);
        Assert.Equal(ParameterType.Text, textParam.Type);
        Assert.Equal("机密", textParam.Value);
        Assert.Equal("机密", textParam.DefaultValue);
    }

    [Fact]
    public void GetAllOperationConfigs_ShouldReturnAllConfigs()
    {
        // Act
        var configs = _service.GetAllOperationConfigs();

        // Assert
        Assert.NotEmpty(configs);
        Assert.True(configs.Count > 20); // 应该有很多操作点配置

        // 验证包含主要的操作点
        Assert.Contains("SetParagraphFont", configs.Keys);
        Assert.Contains("SetParagraphTextColor", configs.Keys);
        Assert.Contains("SetTableRowsColumns", configs.Keys);
        Assert.Contains("InsertAutoShape", configs.Keys);
        Assert.Contains("FindAndReplace", configs.Keys);
    }

    [Fact]
    public void CreateOperationPoint_ShouldThrowException_WhenOperationNotExists()
    {
        // Arrange
        string operationName = "NonExistentOperation";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CreateOperationPoint(operationName));
    }

    [Theory]
    [InlineData("SetParagraphFont", "段落操作")]
    [InlineData("SetPaperSize", "页面设置")]
    [InlineData("SetWatermarkText", "水印设置")]
    [InlineData("SetTableRowsColumns", "表格操作")]
    [InlineData("InsertAutoShape", "图形和图片设置")]
    [InlineData("SetTextBoxBorderColor", "文本框设置")]
    [InlineData("FindAndReplace", "其他操作")]
    public void GetOperationConfig_ShouldReturnCorrectCategory(string operationName, string expectedCategory)
    {
        // Act
        WordOperationConfig? config = _service.GetOperationConfig(operationName);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(expectedCategory, config.Category);
    }
}
