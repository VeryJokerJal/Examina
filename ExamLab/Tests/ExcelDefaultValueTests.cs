using ExamLab.Models;
using ExamLab.Services;
using System;
using System.Linq;
using Xunit;

namespace ExamLab.Tests;

/// <summary>
/// Excel默认值功能测试类
/// </summary>
public class ExcelDefaultValueTests
{
    [Fact]
    public void ExcelKnowledgeService_ShouldCreateOperationPointWithDefaultValues()
    {
        // Arrange
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(ExcelKnowledgeType.SetCellFont);

        // Assert
        Assert.NotNull(operationPoint);
        Assert.Equal("设置指定单元格字体", operationPoint.Name);
        Assert.NotEmpty(operationPoint.Parameters);

        // 验证目标工作簿默认值
        ConfigurationParameter? workbookParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "TargetWorkbook");
        Assert.NotNull(workbookParam);
        Assert.Equal("工作簿1.xlsx", workbookParam.DefaultValue);
        Assert.Equal(ParameterType.Text, workbookParam.Type);

        // 验证字体类型默认值
        ConfigurationParameter? fontParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "FontFamily");
        Assert.NotNull(fontParam);
        Assert.Equal("宋体", fontParam.DefaultValue);
        Assert.Equal(ParameterType.Enum, fontParam.Type);
    }

    [Fact]
    public void ExcelKnowledgeService_ShouldCreateColorParametersWithDefaultValues()
    {
        // Arrange
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(ExcelKnowledgeType.SetFontColor);

        // Assert
        ConfigurationParameter? colorParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "FontColor");
        Assert.NotNull(colorParam);
        Assert.Equal("#000000", colorParam.DefaultValue);
        Assert.Equal(ParameterType.Color, colorParam.Type);
    }

    [Fact]
    public void ExcelKnowledgeService_ShouldCreateFillColorParametersWithDefaultValues()
    {
        // Arrange
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(ExcelKnowledgeType.SetCellFillColor);

        // Assert
        ConfigurationParameter? fillColorParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "FillColor");
        Assert.NotNull(fillColorParam);
        Assert.Equal("#FFFF00", fillColorParam.DefaultValue);
        Assert.Equal(ParameterType.Color, fillColorParam.Type);
    }

    [Fact]
    public void ExcelKnowledgeService_ShouldCreateNumberParametersWithDefaultValues()
    {
        // Arrange
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(ExcelKnowledgeType.SetFontSize);

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
    public void ExcelKnowledgeService_ShouldCreateRowHeightParametersWithDefaultValues()
    {
        // Arrange
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(ExcelKnowledgeType.SetRowHeight);

        // Assert
        ConfigurationParameter? rowHeightParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "RowHeight");
        Assert.NotNull(rowHeightParam);
        Assert.Equal("20", rowHeightParam.DefaultValue);
        Assert.Equal(ParameterType.Number, rowHeightParam.Type);
        Assert.Equal(10, rowHeightParam.MinValue);
        Assert.Equal(200, rowHeightParam.MaxValue);
    }

    [Fact]
    public void ExcelKnowledgeService_ShouldCreateEnumParametersWithDefaultValues()
    {
        // Arrange
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(ExcelKnowledgeType.SetHorizontalAlignment);

        // Assert
        ConfigurationParameter? alignmentParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "HorizontalAlignment");
        Assert.NotNull(alignmentParam);
        Assert.Equal("默认", alignmentParam.DefaultValue);
        Assert.Equal(ParameterType.Enum, alignmentParam.Type);
        Assert.NotNull(alignmentParam.EnumOptions);
        Assert.Contains("默认", alignmentParam.EnumOptionsList);
    }

    [Fact]
    public void ExcelKnowledgeService_ShouldCreateBooleanParametersWithDefaultValues()
    {
        // Arrange
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(ExcelKnowledgeType.Sort);

        // Assert
        ConfigurationParameter? hasHeaderParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "HasHeader");
        Assert.NotNull(hasHeaderParam);
        Assert.Equal("true", hasHeaderParam.DefaultValue);
        Assert.Equal(ParameterType.Boolean, hasHeaderParam.Type);
    }

    [Fact]
    public void ExcelKnowledgeService_ShouldCreateChartParametersWithDefaultValues()
    {
        // Arrange
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(ExcelKnowledgeType.ChartType);

        // Assert
        ConfigurationParameter? chartTypeParam = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == "ChartType");
        Assert.NotNull(chartTypeParam);
        Assert.Equal("簇状柱形图", chartTypeParam.DefaultValue);
        Assert.Equal(ParameterType.Enum, chartTypeParam.Type);
        Assert.Contains("簇状柱形图", chartTypeParam.EnumOptionsList);
    }

    [Theory]
    [InlineData(ExcelKnowledgeType.SetCellFont, "FontFamily", "宋体")]
    [InlineData(ExcelKnowledgeType.SetFontSize, "FontSize", "12")]
    [InlineData(ExcelKnowledgeType.SetFontColor, "FontColor", "#000000")]
    [InlineData(ExcelKnowledgeType.SetCellFillColor, "FillColor", "#FFFF00")]
    [InlineData(ExcelKnowledgeType.SetRowHeight, "RowHeight", "20")]
    [InlineData(ExcelKnowledgeType.SetColumnWidth, "ColumnWidth", "15")]
    [InlineData(ExcelKnowledgeType.ChartType, "ChartType", "簇状柱形图")]
    public void ExcelKnowledgeService_ShouldHaveCorrectDefaultValues(ExcelKnowledgeType knowledgeType, string parameterName, string expectedDefaultValue)
    {
        // Arrange
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(knowledgeType);

        // Assert
        ConfigurationParameter? parameter = operationPoint.Parameters
            .FirstOrDefault(p => p.Name == parameterName);
        Assert.NotNull(parameter);
        Assert.Equal(expectedDefaultValue, parameter.DefaultValue);
    }

    [Fact]
    public void ExcelKnowledgeService_ShouldBeAccessible()
    {
        // Arrange & Act & Assert
        Assert.NotNull(ExcelKnowledgeService.Instance);
    }

    [Fact]
    public void ExcelKnowledgeService_ShouldHaveAllKnowledgeConfigs()
    {
        // Arrange
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // Act
        var configs = service.GetAllKnowledgeConfigs();

        // Assert
        Assert.NotEmpty(configs);
        Assert.True(configs.Count() > 10); // 应该有很多Excel操作点配置
    }

    [Fact]
    public void ExcelKnowledgeService_ShouldThrowExceptionForInvalidKnowledgeType()
    {
        // Arrange
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.CreateOperationPoint((ExcelKnowledgeType)999));
    }

    [Fact]
    public void ExcelOperationPoint_ShouldHaveConsistentStructureWithWord()
    {
        // Arrange
        ExcelKnowledgeService excelService = ExcelKnowledgeService.Instance;
        WordKnowledgeService wordService = WordKnowledgeService.Instance;

        // Act
        OperationPoint excelOp = excelService.CreateOperationPoint(ExcelKnowledgeType.SetCellFont);
        OperationPoint wordOp = wordService.CreateOperationPoint(WordKnowledgeType.SetParagraphFont);

        // Assert - 验证结构一致性
        Assert.NotNull(excelOp.Id);
        Assert.NotNull(wordOp.Id);
        Assert.True(excelOp.Score > 0);
        Assert.True(wordOp.Score > 0);
        Assert.True(excelOp.IsEnabled);
        Assert.True(wordOp.IsEnabled);
        Assert.NotEqual(default(DateTime), excelOp.CreatedAt);
        Assert.NotEqual(default(DateTime), wordOp.CreatedAt);
    }

    [Fact]
    public void ExcelColorParameters_ShouldUseColorType()
    {
        // Arrange
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // 测试多个包含颜色参数的操作点
        ExcelKnowledgeType[] colorOperations = [
            ExcelKnowledgeType.SetFontColor,
            ExcelKnowledgeType.SetCellFillColor,
            ExcelKnowledgeType.SetInnerBorderColor,
            ExcelKnowledgeType.SetOuterBorderColor,
            ExcelKnowledgeType.SetPatternFillColor
        ];

        foreach (ExcelKnowledgeType operation in colorOperations)
        {
            // Act
            OperationPoint operationPoint = service.CreateOperationPoint(operation);

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
    }
}
