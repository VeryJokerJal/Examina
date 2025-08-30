using System;
using System.Threading.Tasks;
using ExamLab.Models;

namespace ExamLab.Services.DocumentGeneration;

/// <summary>
/// 文档生成服务接口
/// </summary>
public interface IDocumentGenerationService
{
    /// <summary>
    /// 验证模块是否可以生成文档
    /// </summary>
    /// <param name="module">要验证的模块</param>
    /// <returns>验证结果</returns>
    DocumentValidationResult ValidateModule(ExamModule module);

    /// <summary>
    /// 异步生成文档
    /// </summary>
    /// <param name="module">要生成文档的模块</param>
    /// <param name="filePath">输出文件路径</param>
    /// <param name="progress">进度报告器</param>
    /// <returns>生成结果</returns>
    Task<DocumentGenerationResult> GenerateDocumentAsync(ExamModule module, string filePath, IProgress<DocumentGenerationProgress>? progress = null);

    /// <summary>
    /// 获取推荐的文件扩展名
    /// </summary>
    /// <returns>文件扩展名（包含点号）</returns>
    string GetRecommendedFileExtension();

    /// <summary>
    /// 获取文件类型描述
    /// </summary>
    /// <returns>文件类型描述</returns>
    string GetFileTypeDescription();

    /// <summary>
    /// 获取支持的模块类型
    /// </summary>
    /// <returns>支持的模块类型</returns>
    ModuleType GetSupportedModuleType();
}
