using ExamLab.Models;
using ExamLab.Services;
using System;
using System.Linq;

namespace ExamLab.Examples;

/// <summary>
/// Word段落底纹功能使用示例
/// </summary>
public class WordParagraphShadingExample
{
    public static void RunExample()
    {
        Console.WriteLine("=== ExamLab Word段落底纹功能示例 ===\n");

        WordKnowledgeService service = WordKnowledgeService.Instance;

        // 示例1：创建段落底纹操作点
        Console.WriteLine("1. 创建段落底纹操作点：");
        OperationPoint shadingOperation = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);
        DisplayOperationPoint(shadingOperation);

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // 示例2：展示完整的底纹图案选项
        Console.WriteLine("2. 完整的底纹图案选项（基于WdTextureIndex枚举）：");
        DisplayShadingPatternOptions(shadingOperation);

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // 示例3：演示不同类型的底纹设置
        Console.WriteLine("3. 不同类型的底纹设置示例：");
        DemonstrateShadingTypes();

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // 示例4：参数修改示例
        Console.WriteLine("4. 参数修改示例：");
        DemonstrateParameterModification();

        Console.WriteLine("\n=== 示例完成 ===");
    }

    private static void DisplayOperationPoint(OperationPoint operation)
    {
        Console.WriteLine($"操作点名称: {operation.Name}");
        Console.WriteLine($"描述: {operation.Description}");
        Console.WriteLine($"模块类型: {operation.ModuleType}");
        Console.WriteLine($"参数数量: {operation.Parameters.Count}");
        Console.WriteLine("参数详情:");

        foreach (var param in operation.Parameters.OrderBy(p => p.Order))
        {
            Console.WriteLine($"  - {param.DisplayName} ({param.Name})");
            Console.WriteLine($"    类型: {param.Type}");
            Console.WriteLine($"    默认值: {param.DefaultValue ?? "无"}");
            Console.WriteLine($"    当前值: {param.Value}");
            Console.WriteLine($"    是否必填: {(param.IsRequired ? "是" : "否")}");
            if (param.Type == ParameterType.Number && param.MinValue.HasValue)
            {
                Console.WriteLine($"    最小值: {param.MinValue}");
            }
            if (param.Type == ParameterType.Enum && !string.IsNullOrEmpty(param.EnumOptions))
            {
                Console.WriteLine($"    选项数量: {param.EnumOptionsList.Count}");
                Console.WriteLine($"    前5个选项: {string.Join(", ", param.EnumOptionsList.Take(5))}...");
            }
            Console.WriteLine();
        }
    }

    private static void DisplayShadingPatternOptions(OperationPoint operation)
    {
        var shadingPatternParam = operation.Parameters.First(p => p.Name == "ShadingPattern");
        var options = shadingPatternParam.EnumOptionsList;

        Console.WriteLine($"底纹图案选项总数: {options.Count}");
        Console.WriteLine();

        // 分类显示选项
        Console.WriteLine("【百分比纹理选项】");
        var percentageOptions = options.Where(o => o.Contains("%") || o == "无纹理" || o == "实心填充").ToList();
        Console.WriteLine($"数量: {percentageOptions.Count}");
        Console.WriteLine($"选项: {string.Join(", ", percentageOptions.Take(10))}...");
        Console.WriteLine();

        Console.WriteLine("【深色线条纹理选项】");
        var darkLineOptions = options.Where(o => o.StartsWith("深色")).ToList();
        Console.WriteLine($"数量: {darkLineOptions.Count}");
        Console.WriteLine($"选项: {string.Join(", ", darkLineOptions)}");
        Console.WriteLine();

        Console.WriteLine("【浅色线条纹理选项】");
        var lightLineOptions = options.Where(o => !o.Contains("%") && !o.StartsWith("深色") && o != "无纹理" && o != "实心填充").ToList();
        Console.WriteLine($"数量: {lightLineOptions.Count}");
        Console.WriteLine($"选项: {string.Join(", ", lightLineOptions)}");
        Console.WriteLine();

        Console.WriteLine("【WdTextureIndex枚举对应关系】");
        Console.WriteLine("- wdTextureNone → 无纹理");
        Console.WriteLine("- wdTexture2Pt5Percent → 2.5%");
        Console.WriteLine("- wdTexture50Percent → 50%");
        Console.WriteLine("- wdTextureSolid → 实心填充");
        Console.WriteLine("- wdTextureDarkHorizontal → 深色水平线");
        Console.WriteLine("- wdTextureHorizontal → 水平线");
        Console.WriteLine("- 等等...");
    }

    private static void DemonstrateShadingTypes()
    {
        WordKnowledgeService service = WordKnowledgeService.Instance;

        Console.WriteLine("【基础底纹设置】");
        var basicShading = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);
        var colorParam = basicShading.Parameters.First(p => p.Name == "ShadingColor");
        var patternParam = basicShading.Parameters.First(p => p.Name == "ShadingPattern");
        
        colorParam.Value = "#FFFF00"; // 黄色
        patternParam.Value = "无纹理";
        Console.WriteLine($"颜色: {colorParam.Value}, 图案: {patternParam.Value}");
        Console.WriteLine("效果: 纯黄色底纹");
        Console.WriteLine();

        Console.WriteLine("【百分比纹理底纹】");
        patternParam.Value = "25%";
        Console.WriteLine($"颜色: {colorParam.Value}, 图案: {patternParam.Value}");
        Console.WriteLine("效果: 25%密度的黄色点状纹理");
        Console.WriteLine();

        Console.WriteLine("【线条纹理底纹】");
        patternParam.Value = "深色水平线";
        Console.WriteLine($"颜色: {colorParam.Value}, 图案: {patternParam.Value}");
        Console.WriteLine("效果: 黄色背景上的深色水平线条");
        Console.WriteLine();

        Console.WriteLine("【高密度纹理底纹】");
        patternParam.Value = "90%";
        Console.WriteLine($"颜色: {colorParam.Value}, 图案: {patternParam.Value}");
        Console.WriteLine("效果: 90%密度的黄色纹理，接近实心");
        Console.WriteLine();

        Console.WriteLine("【十字纹理底纹】");
        patternParam.Value = "十字";
        Console.WriteLine($"颜色: {colorParam.Value}, 图案: {patternParam.Value}");
        Console.WriteLine("效果: 黄色背景上的十字网格图案");
    }

    private static void DemonstrateParameterModification()
    {
        WordKnowledgeService service = WordKnowledgeService.Instance;
        OperationPoint operation = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);

        Console.WriteLine("修改前的参数值:");
        foreach (var param in operation.Parameters.OrderBy(p => p.Order))
        {
            Console.WriteLine($"  {param.DisplayName}: {param.Value}");
        }

        // 修改参数值
        var paragraphParam = operation.Parameters.First(p => p.Name == "ParagraphNumber");
        var colorParam = operation.Parameters.First(p => p.Name == "ShadingColor");
        var patternParam = operation.Parameters.First(p => p.Name == "ShadingPattern");

        paragraphParam.Value = "3";
        colorParam.Value = "#ADD8E6"; // 浅蓝色
        patternParam.Value = "深色对角线下";

        Console.WriteLine("\n修改后的参数值:");
        foreach (var param in operation.Parameters.OrderBy(p => p.Order))
        {
            Console.WriteLine($"  {param.DisplayName}: {param.Value}");
        }

        Console.WriteLine("\n✅ 参数修改完成！");
        Console.WriteLine("效果: 为第3个段落设置浅蓝色背景，带有深色对角线下纹理");
    }

    /// <summary>
    /// 展示与Microsoft Word COM组件的对应关系
    /// </summary>
    public static void ShowWdTextureIndexMapping()
    {
        Console.WriteLine("\n=== WdTextureIndex枚举对应关系 ===");
        
        Console.WriteLine("\n【百分比纹理】");
        Console.WriteLine("wdTextureNone (0) → 无纹理");
        Console.WriteLine("wdTexture2Pt5Percent (25) → 2.5%");
        Console.WriteLine("wdTexture5Percent (50) → 5%");
        Console.WriteLine("wdTexture10Percent (100) → 10%");
        Console.WriteLine("wdTexture25Percent (250) → 25%");
        Console.WriteLine("wdTexture50Percent (500) → 50%");
        Console.WriteLine("wdTexture75Percent (750) → 75%");
        Console.WriteLine("wdTexture95Percent (950) → 95%");
        Console.WriteLine("wdTextureSolid (1000) → 实心填充");
        
        Console.WriteLine("\n【深色线条纹理】");
        Console.WriteLine("wdTextureDarkHorizontal (-1) → 深色水平线");
        Console.WriteLine("wdTextureDarkVertical (-2) → 深色垂直线");
        Console.WriteLine("wdTextureDarkDiagonalDown (-3) → 深色对角线下");
        Console.WriteLine("wdTextureDarkDiagonalUp (-4) → 深色对角线上");
        Console.WriteLine("wdTextureDarkCross (-5) → 深色十字");
        Console.WriteLine("wdTextureDarkDiagonalCross (-6) → 深色对角十字");
        
        Console.WriteLine("\n【浅色线条纹理】");
        Console.WriteLine("wdTextureHorizontal (-7) → 水平线");
        Console.WriteLine("wdTextureVertical (-8) → 垂直线");
        Console.WriteLine("wdTextureDiagonalDown (-9) → 对角线下");
        Console.WriteLine("wdTextureDiagonalUp (-10) → 对角线上");
        Console.WriteLine("wdTextureCross (-11) → 十字");
        Console.WriteLine("wdTextureDiagonalCross (-12) → 对角十字");
        
        Console.WriteLine("\n✅ 所有选项都基于Microsoft Word COM组件的WdTextureIndex枚举");
    }

    /// <summary>
    /// 展示颜色选择器功能
    /// </summary>
    public static void DemonstrateColorPicker()
    {
        Console.WriteLine("\n=== 颜色选择器功能 ===");
        
        WordKnowledgeService service = WordKnowledgeService.Instance;
        OperationPoint operation = service.CreateOperationPoint(WordKnowledgeType.SetParagraphShading);
        var colorParam = operation.Parameters.First(p => p.Name == "ShadingColor");
        
        Console.WriteLine($"参数类型: {colorParam.Type}");
        Console.WriteLine($"默认颜色: {colorParam.DefaultValue}");
        Console.WriteLine("支持的颜色格式: #RRGGBB (十六进制)");
        Console.WriteLine();
        
        Console.WriteLine("常用底纹颜色示例:");
        Console.WriteLine("- #FFFF00 (黄色) - 高亮标记");
        Console.WriteLine("- #ADD8E6 (浅蓝色) - 信息提示");
        Console.WriteLine("- #90EE90 (浅绿色) - 成功标记");
        Console.WriteLine("- #FFB6C1 (浅粉色) - 警告标记");
        Console.WriteLine("- #D3D3D3 (浅灰色) - 中性标记");
        Console.WriteLine();
        
        Console.WriteLine("✅ 使用专业的颜色选择器控件，支持精确的颜色选择");
    }
}
