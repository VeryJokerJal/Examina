using ExamLab.Models;
using ExamLab.Services.DocumentGeneration;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System.Diagnostics;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ExamLab.Services.DocumentGeneration;

/// <summary>
/// PowerPoint文档生成器
/// </summary>
public class PowerPointDocumentGenerator : IDocumentGenerationService
{
    /// <summary>
    /// 获取支持的模块类型
    /// </summary>
    public ModuleType GetSupportedModuleType() => ModuleType.PowerPoint;

    /// <summary>
    /// 获取推荐的文件扩展名
    /// </summary>
    public string GetRecommendedFileExtension() => ".pptx";

    /// <summary>
    /// 获取文件类型描述
    /// </summary>
    public string GetFileTypeDescription() => "PowerPoint演示文稿";

    /// <summary>
    /// 验证模块是否可以生成文档
    /// </summary>
    public DocumentValidationResult ValidateModule(ExamModule module)
    {
        DocumentValidationResult result = new() { IsValid = true };

        // 验证模块类型
        if (module.Type != ModuleType.PowerPoint)
        {
            result.AddError($"模块类型不匹配，期望PowerPoint模块，实际为{module.Type}");
            return result;
        }

        // 验证模块是否启用
        if (!module.IsEnabled)
        {
            result.AddWarning("模块未启用");
        }

        // 验证是否有题目
        if (module.Questions.Count == 0)
        {
            result.AddError("模块中没有题目");
            return result;
        }

        // 验证题目中是否有PowerPoint操作点
        int totalPowerPointOperationPoints = 0;
        foreach (Question question in module.Questions)
        {
            int powerPointOperationPoints = question.OperationPoints.Count(op => 
                op.ModuleType == ModuleType.PowerPoint && op.IsEnabled);
            totalPowerPointOperationPoints += powerPointOperationPoints;
        }

        if (totalPowerPointOperationPoints == 0)
        {
            result.AddError("模块中没有启用的PowerPoint操作点");
            return result;
        }

        result.Details = $"验证通过：{module.Questions.Count}个题目，{totalPowerPointOperationPoints}个PowerPoint操作点";
        return result;
    }

    /// <summary>
    /// 异步生成PowerPoint文档
    /// </summary>
    public async Task<DocumentGenerationResult> GenerateDocumentAsync(ExamModule module, string filePath, IProgress<DocumentGenerationProgress>? progress = null)
    {
        DateTime startTime = DateTime.Now;
        
        try
        {
            // 验证模块
            DocumentValidationResult validation = ValidateModule(module);
            if (!validation.IsValid)
            {
                return DocumentGenerationResult.Failure($"模块验证失败：{string.Join(", ", validation.ErrorMessages)}");
            }

            // 收集所有PowerPoint操作点
            List<OperationPoint> allPowerPointOperationPoints = [];
            foreach (Question question in module.Questions)
            {
                List<OperationPoint> powerPointOps = question.OperationPoints
                    .Where(op => op.ModuleType == ModuleType.PowerPoint && op.IsEnabled)
                    .ToList();
                allPowerPointOperationPoints.AddRange(powerPointOps);
            }

            int totalOperationPoints = allPowerPointOperationPoints.Count;
            int processedCount = 0;

            // 报告初始进度
            progress?.Report(DocumentGenerationProgress.Create("开始生成PowerPoint文档", 0, totalOperationPoints));

            // 创建PowerPoint文档
            await Task.Run(() =>
            {
                using PresentationDocument document = PresentationDocument.Create(filePath, PresentationDocumentType.Presentation);
                
                // 创建演示文稿部分
                PresentationPart presentationPart = document.AddPresentationPart();
                presentationPart.Presentation = new Presentation();

                // 创建幻灯片母版部分
                SlideMasterPart slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
                slideMasterPart.SlideMaster = CreateSlideMaster();

                // 创建幻灯片布局部分
                SlideLayoutPart slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
                slideLayoutPart.SlideLayout = CreateSlideLayout();

                // 创建幻灯片列表
                SlideIdList slideIdList = new();
                presentationPart.Presentation.Append(slideIdList);

                // 添加标题幻灯片
                progress?.Report(DocumentGenerationProgress.Create("添加标题幻灯片", processedCount, totalOperationPoints));
                AddTitleSlide(presentationPart, slideLayoutPart, slideIdList, module.Name);

                uint slideId = 2;

                // 处理每个操作点
                foreach (OperationPoint operationPoint in allPowerPointOperationPoints)
                {
                    string operationName = GetOperationDisplayName(operationPoint);
                    progress?.Report(DocumentGenerationProgress.Create("处理操作点", processedCount, totalOperationPoints, operationName));

                    try
                    {
                        slideId = ApplyOperationPoint(presentationPart, slideLayoutPart, slideIdList, operationPoint, slideId);
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但继续处理其他操作点
                        Debug.WriteLine($"处理操作点 {operationName} 时出错: {ex.Message}");
                        // 添加错误幻灯片
                        AddErrorSlide(presentationPart, slideLayoutPart, slideIdList, operationName, ex.Message, slideId);
                        slideId++;
                    }

                    processedCount++;
                    progress?.Report(DocumentGenerationProgress.Create("处理操作点", processedCount, totalOperationPoints, operationName));
                }

                // 保存文档
                progress?.Report(DocumentGenerationProgress.Create("保存文档", processedCount, totalOperationPoints));
                presentationPart.Presentation.Save();
            });

            TimeSpan duration = DateTime.Now - startTime;
            string details = $"成功生成PowerPoint文档，包含{module.Questions.Count}个题目的{totalOperationPoints}个操作点";
            
            return DocumentGenerationResult.Success(filePath, processedCount, totalOperationPoints, duration, details);
        }
        catch (Exception ex)
        {
            return DocumentGenerationResult.Failure($"生成PowerPoint文档时发生错误：{ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// 创建幻灯片母版
    /// </summary>
    private static SlideMaster CreateSlideMaster()
    {
        SlideMaster slideMaster = new();
        CommonSlideData commonSlideData = new();
        ShapeTree shapeTree = new();
        
        // 添加基本的形状树结构
        NonVisualGroupShapeProperties nonVisualGroupShapeProperties = new();
        GroupShapeProperties groupShapeProperties = new();
        
        shapeTree.Append(nonVisualGroupShapeProperties);
        shapeTree.Append(groupShapeProperties);
        
        commonSlideData.Append(shapeTree);
        slideMaster.Append(commonSlideData);
        
        return slideMaster;
    }

    /// <summary>
    /// 创建幻灯片布局
    /// </summary>
    private static SlideLayout CreateSlideLayout()
    {
        SlideLayout slideLayout = new();
        CommonSlideData commonSlideData = new();
        ShapeTree shapeTree = new();
        
        // 添加基本的形状树结构
        NonVisualGroupShapeProperties nonVisualGroupShapeProperties = new();
        GroupShapeProperties groupShapeProperties = new();
        
        shapeTree.Append(nonVisualGroupShapeProperties);
        shapeTree.Append(groupShapeProperties);
        
        commonSlideData.Append(shapeTree);
        slideLayout.Append(commonSlideData);
        
        return slideLayout;
    }

    /// <summary>
    /// 添加标题幻灯片
    /// </summary>
    private static void AddTitleSlide(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, SlideIdList slideIdList, string title)
    {
        SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.AddPart(slideLayoutPart);
        
        Slide slide = new();
        CommonSlideData commonSlideData = new();
        ShapeTree shapeTree = new();
        
        // 添加标题文本框
        Shape titleShape = CreateTextShape(title, 1, 1000000, 1000000, 8000000, 2000000);
        shapeTree.Append(titleShape);
        
        commonSlideData.Append(shapeTree);
        slide.Append(commonSlideData);
        slidePart.Slide = slide;
        
        SlideId slideId = new() { Id = 1, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
        slideIdList.Append(slideId);
    }

    /// <summary>
    /// 应用操作点到演示文稿
    /// </summary>
    private static uint ApplyOperationPoint(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, SlideIdList slideIdList, OperationPoint operationPoint, uint slideId)
    {
        // 根据PowerPoint知识点类型应用不同的操作
        if (operationPoint.PowerPointKnowledgeType.HasValue)
        {
            switch (operationPoint.PowerPointKnowledgeType.Value)
            {
                case PowerPointKnowledgeType.SetSlideTitle:
                    return ApplySlideTitle(presentationPart, slideLayoutPart, slideIdList, operationPoint, slideId);
                case PowerPointKnowledgeType.InsertTextBox:
                    return ApplyTextBox(presentationPart, slideLayoutPart, slideIdList, operationPoint, slideId);
                case PowerPointKnowledgeType.SetSlideTransition:
                    return ApplySlideTransition(presentationPart, slideLayoutPart, slideIdList, operationPoint, slideId);
                default:
                    // 对于未实现的操作点，添加说明幻灯片
                    return AddOperationSlide(presentationPart, slideLayoutPart, slideIdList, operationPoint, slideId);
            }
        }
        else
        {
            // 如果没有指定PowerPoint知识点类型，添加通用说明
            return AddOperationSlide(presentationPart, slideLayoutPart, slideIdList, operationPoint, slideId);
        }
    }

    /// <summary>
    /// 应用幻灯片标题设置
    /// </summary>
    private static uint ApplySlideTitle(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, SlideIdList slideIdList, OperationPoint operationPoint, uint slideId)
    {
        string slideTitle = GetParameterValue(operationPoint, "SlideTitle", "幻灯片标题");
        
        SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.AddPart(slideLayoutPart);
        
        Slide slide = new();
        CommonSlideData commonSlideData = new();
        ShapeTree shapeTree = new();
        
        // 添加标题
        Shape titleShape = CreateTextShape(slideTitle, 1, 1000000, 1000000, 8000000, 1500000);
        shapeTree.Append(titleShape);
        
        // 添加说明文本
        Shape descShape = CreateTextShape($"操作点：设置幻灯片标题为 '{slideTitle}'", 2, 1000000, 3000000, 8000000, 1000000);
        shapeTree.Append(descShape);
        
        commonSlideData.Append(shapeTree);
        slide.Append(commonSlideData);
        slidePart.Slide = slide;
        
        SlideId newSlideId = new() { Id = slideId, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
        slideIdList.Append(newSlideId);
        
        return slideId + 1;
    }

    /// <summary>
    /// 应用文本框插入
    /// </summary>
    private static uint ApplyTextBox(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, SlideIdList slideIdList, OperationPoint operationPoint, uint slideId)
    {
        string textContent = GetParameterValue(operationPoint, "TextContent", "文本框内容");
        
        SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.AddPart(slideLayoutPart);
        
        Slide slide = new();
        CommonSlideData commonSlideData = new();
        ShapeTree shapeTree = new();
        
        // 添加标题
        Shape titleShape = CreateTextShape("插入文本框", 1, 1000000, 1000000, 8000000, 1500000);
        shapeTree.Append(titleShape);
        
        // 添加文本框
        Shape textBoxShape = CreateTextShape(textContent, 2, 2000000, 3000000, 6000000, 2000000);
        shapeTree.Append(textBoxShape);
        
        commonSlideData.Append(shapeTree);
        slide.Append(commonSlideData);
        slidePart.Slide = slide;
        
        SlideId newSlideId = new() { Id = slideId, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
        slideIdList.Append(newSlideId);
        
        return slideId + 1;
    }

    /// <summary>
    /// 应用幻灯片切换效果
    /// </summary>
    private static uint ApplySlideTransition(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, SlideIdList slideIdList, OperationPoint operationPoint, uint slideId)
    {
        string transitionType = GetParameterValue(operationPoint, "TransitionType", "淡入淡出");
        
        SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.AddPart(slideLayoutPart);
        
        Slide slide = new();
        CommonSlideData commonSlideData = new();
        ShapeTree shapeTree = new();
        
        // 添加标题
        Shape titleShape = CreateTextShape("幻灯片切换效果", 1, 1000000, 1000000, 8000000, 1500000);
        shapeTree.Append(titleShape);
        
        // 添加说明文本
        Shape descShape = CreateTextShape($"操作点：设置幻灯片切换效果为 '{transitionType}'", 2, 1000000, 3000000, 8000000, 1000000);
        shapeTree.Append(descShape);
        
        commonSlideData.Append(shapeTree);
        slide.Append(commonSlideData);
        slidePart.Slide = slide;
        
        SlideId newSlideId = new() { Id = slideId, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
        slideIdList.Append(newSlideId);
        
        return slideId + 1;
    }

    /// <summary>
    /// 添加操作点说明幻灯片
    /// </summary>
    private static uint AddOperationSlide(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, SlideIdList slideIdList, OperationPoint operationPoint, uint slideId)
    {
        SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.AddPart(slideLayoutPart);
        
        Slide slide = new();
        CommonSlideData commonSlideData = new();
        ShapeTree shapeTree = new();
        
        // 添加标题
        Shape titleShape = CreateTextShape($"操作点：{operationPoint.Name}", 1, 1000000, 1000000, 8000000, 1500000);
        shapeTree.Append(titleShape);
        
        // 添加描述
        Shape descShape = CreateTextShape(operationPoint.Description, 2, 1000000, 3000000, 8000000, 2000000);
        shapeTree.Append(descShape);
        
        commonSlideData.Append(shapeTree);
        slide.Append(commonSlideData);
        slidePart.Slide = slide;
        
        SlideId newSlideId = new() { Id = slideId, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
        slideIdList.Append(newSlideId);
        
        return slideId + 1;
    }

    /// <summary>
    /// 添加错误幻灯片
    /// </summary>
    private static void AddErrorSlide(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, SlideIdList slideIdList, string operationName, string errorMessage, uint slideId)
    {
        SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.AddPart(slideLayoutPart);
        
        Slide slide = new();
        CommonSlideData commonSlideData = new();
        ShapeTree shapeTree = new();
        
        // 添加错误标题
        Shape titleShape = CreateTextShape($"错误：{operationName}", 1, 1000000, 1000000, 8000000, 1500000);
        shapeTree.Append(titleShape);
        
        // 添加错误消息
        Shape errorShape = CreateTextShape(errorMessage, 2, 1000000, 3000000, 8000000, 2000000);
        shapeTree.Append(errorShape);
        
        commonSlideData.Append(shapeTree);
        slide.Append(commonSlideData);
        slidePart.Slide = slide;
        
        SlideId newSlideId = new() { Id = slideId, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
        slideIdList.Append(newSlideId);
    }

    /// <summary>
    /// 创建文本形状
    /// </summary>
    private static Shape CreateTextShape(string text, uint shapeId, long x, long y, long width, long height)
    {
        Shape shape = new();
        
        NonVisualShapeProperties nonVisualShapeProperties = new();
        nonVisualShapeProperties.Append(new NonVisualDrawingProperties() { Id = shapeId, Name = $"TextBox {shapeId}" });
        nonVisualShapeProperties.Append(new NonVisualShapeDrawingProperties());
        nonVisualShapeProperties.Append(new ApplicationNonVisualDrawingProperties());
        
        ShapeProperties shapeProperties = new();
        A.Transform2D transform2D = new();
        transform2D.Append(new A.Offset() { X = x, Y = y });
        transform2D.Append(new A.Extents() { Cx = width, Cy = height });
        shapeProperties.Append(transform2D);
        shapeProperties.Append(new A.PresetGeometry() { Preset = A.ShapeTypeValues.Rectangle });
        
        TextBody textBody = new();
        textBody.Append(new A.BodyProperties());
        textBody.Append(new A.ListStyle());
        
        A.Paragraph paragraph = new();
        A.Run run = new();
        run.Append(new A.Text(text));
        paragraph.Append(run);
        textBody.Append(paragraph);
        
        shape.Append(nonVisualShapeProperties);
        shape.Append(shapeProperties);
        shape.Append(textBody);
        
        return shape;
    }

    /// <summary>
    /// 获取操作点的显示名称
    /// </summary>
    private static string GetOperationDisplayName(OperationPoint operationPoint)
    {
        return !string.IsNullOrEmpty(operationPoint.Name) ? operationPoint.Name : 
               operationPoint.PowerPointKnowledgeType?.ToString() ?? "未知操作";
    }

    /// <summary>
    /// 获取参数值
    /// </summary>
    private static string GetParameterValue(OperationPoint operationPoint, string parameterName, string defaultValue)
    {
        ConfigurationParameter? parameter = operationPoint.Parameters.FirstOrDefault(p => p.Name == parameterName);
        return parameter?.Value ?? defaultValue;
    }
}
