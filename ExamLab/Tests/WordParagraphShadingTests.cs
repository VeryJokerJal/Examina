using ExamLab.Models;
using ExamLab.Services;
using System;
using System.Linq;
using Xunit;

namespace ExamLab.Tests;

/// <summary>
/// Word段落底纹功能测试类
/// </summary>
public class WordParagraphShadingTests
{
    [Fact]
    public void WordKnowledgeService_ShouldCreateParagraphShadingOperationPointWithCompleteOptions()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);

        // Assert
        Assert.NotNull(operationPoint);
        Assert.Equal("设置段落底纹", operationPoint.Name);
        Assert.Equal("设置指定段落的底纹填充", operationPoint.Description);
        Assert.Equal(3, operationPoint.Parameters.Count);
    }

    [Fact]
    public void ParagraphShadingOperationPoint_ShouldHaveCorrectParameterTypes()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);

        // Assert
        var paragraphNumberParam = operationPoint.Parameters.First(p => p.Name == "ParagraphNumber");
        Assert.Equal(ParameterType.Number, paragraphNumberParam.Type);
        Assert.True(paragraphNumberParam.IsRequired);
        Assert.Equal("-1", paragraphNumberParam.DefaultValue);

        var shadingColorParam = operationPoint.Parameters.First(p => p.Name == "ShadingColor");
        Assert.Equal(ParameterType.Color, shadingColorParam.Type);
        Assert.True(shadingColorParam.IsRequired);
        Assert.Equal("#FFFF00", shadingColorParam.DefaultValue);

        var shadingPatternParam = operationPoint.Parameters.First(p => p.Name == "ShadingPattern");
        Assert.Equal(ParameterType.Enum, shadingPatternParam.Type);
        Assert.False(shadingPatternParam.IsRequired);
        Assert.Equal("无纹理", shadingPatternParam.DefaultValue);
    }

    [Fact]
    public void ParagraphShadingPattern_ShouldContainAllWdTextureIndexOptions()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);
        var shadingPatternParam = operationPoint.Parameters.First(p => p.Name == "ShadingPattern");

        // Assert
        Assert.NotNull(shadingPatternParam.EnumOptions);
        var options = shadingPatternParam.EnumOptionsList;

        // 验证百分比纹理选项
        Assert.Contains("无纹理", options);
        Assert.Contains("2.5%", options);
        Assert.Contains("5%", options);
        Assert.Contains("10%", options);
        Assert.Contains("25%", options);
        Assert.Contains("50%", options);
        Assert.Contains("75%", options);
        Assert.Contains("95%", options);
        Assert.Contains("97.5%", options);
        Assert.Contains("实心填充", options);

        // 验证深色线条纹理选项
        Assert.Contains("深色水平线", options);
        Assert.Contains("深色垂直线", options);
        Assert.Contains("深色对角线下", options);
        Assert.Contains("深色对角线上", options);
        Assert.Contains("深色十字", options);
        Assert.Contains("深色对角十字", options);

        // 验证浅色线条纹理选项
        Assert.Contains("水平线", options);
        Assert.Contains("垂直线", options);
        Assert.Contains("对角线下", options);
        Assert.Contains("对角线上", options);
        Assert.Contains("十字", options);
        Assert.Contains("对角十字", options);
    }

    [Fact]
    public void ParagraphShadingPattern_ShouldHaveCorrectOptionCount()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);
        var shadingPatternParam = operationPoint.Parameters.First(p => p.Name == "ShadingPattern");

        // Assert
        var options = shadingPatternParam.EnumOptionsList;
        
        // 应该包含：
        // - 1个无纹理选项
        // - 39个百分比选项（2.5%到97.5%，每2.5%一个）
        // - 1个实心填充选项
        // - 6个深色线条选项
        // - 6个浅色线条选项
        // 总计：53个选项
        Assert.Equal(53, options.Count);
    }

    [Theory]
    [InlineData("无纹理")]
    [InlineData("2.5%")]
    [InlineData("50%")]
    [InlineData("实心填充")]
    [InlineData("深色水平线")]
    [InlineData("水平线")]
    public void ParagraphShadingPattern_ShouldContainSpecificOptions(string expectedOption)
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);
        var shadingPatternParam = operationPoint.Parameters.First(p => p.Name == "ShadingPattern");

        // Assert
        var options = shadingPatternParam.EnumOptionsList;
        Assert.Contains(expectedOption, options);
    }

    [Fact]
    public void ParagraphShadingColor_ShouldUseColorType()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);
        var shadingColorParam = operationPoint.Parameters.First(p => p.Name == "ShadingColor");

        // Assert
        Assert.Equal(ParameterType.Color, shadingColorParam.Type);
        Assert.NotNull(shadingColorParam.DefaultValue);
        Assert.True(shadingColorParam.DefaultValue.StartsWith("#"));
        Assert.Equal(7, shadingColorParam.DefaultValue.Length); // #RRGGBB format
    }

    [Fact]
    public void ParagraphShadingOperationPoint_ShouldHaveCorrectParameterOrder()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);

        // Assert
        var orderedParams = operationPoint.Parameters.OrderBy(p => p.Order).ToList();
        
        Assert.Equal("ParagraphNumber", orderedParams[0].Name);
        Assert.Equal(1, orderedParams[0].Order);
        
        Assert.Equal("ShadingColor", orderedParams[1].Name);
        Assert.Equal(2, orderedParams[1].Order);
        
        Assert.Equal("ShadingPattern", orderedParams[2].Name);
        Assert.Equal(3, orderedParams[2].Order);
    }

    [Fact]
    public void ParagraphShadingOperationPoint_ShouldHaveCorrectValidationRules()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);

        // Assert
        var paragraphNumberParam = operationPoint.Parameters.First(p => p.Name == "ParagraphNumber");
        Assert.Equal(1, paragraphNumberParam.MinValue);
        Assert.True(paragraphNumberParam.IsRequired);

        var shadingColorParam = operationPoint.Parameters.First(p => p.Name == "ShadingColor");
        Assert.True(shadingColorParam.IsRequired);

        var shadingPatternParam = operationPoint.Parameters.First(p => p.Name == "ShadingPattern");
        Assert.False(shadingPatternParam.IsRequired); // 图案是可选的
    }

    [Fact]
    public void WordKnowledgeService_ShouldMaintainBackwardCompatibility()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);

        // Assert
        // 确保操作点仍然可以正常创建
        Assert.NotNull(operationPoint);
        Assert.NotNull(operationPoint.Id);
        Assert.True(operationPoint.Score > 0);
        Assert.True(operationPoint.IsEnabled);
        Assert.NotEqual(default(DateTime), operationPoint.CreatedAt);
        
        // 确保所有参数都有有效的默认值
        foreach (var param in operationPoint.Parameters)
        {
            if (param.IsRequired)
            {
                Assert.NotNull(param.DefaultValue);
                Assert.NotEmpty(param.DefaultValue);
            }
        }
    }

    [Fact]
    public void ParagraphShadingPattern_ShouldHaveLogicalOptionOrder()
    {
        // Arrange
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // Act
        OperationPoint operationPoint = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);
        var shadingPatternParam = operationPoint.Parameters.First(p => p.Name == "ShadingPattern");

        // Assert
        var options = shadingPatternParam.EnumOptionsList;
        
        // 验证选项顺序是否合理
        Assert.Equal("无纹理", options[0]); // 第一个应该是无纹理
        Assert.Equal("2.5%", options[1]); // 然后是最小的百分比
        
        // 验证百分比选项是否按顺序排列
        var percentageOptions = options.Where(o => o.Contains("%") && !o.Contains("实心")).ToList();
        Assert.True(percentageOptions.Count > 30); // 应该有很多百分比选项
        
        // 验证实心填充在百分比选项之后
        int solidIndex = options.IndexOf("实心填充");
        int lastPercentageIndex = options.IndexOf("97.5%");
        Assert.True(solidIndex > lastPercentageIndex);
    }
}
