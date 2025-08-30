using System.Collections.Generic;
using System.Threading.Tasks;
using ExamLab.Models;

namespace ExamLab.Services;

/// <summary>
/// Excel文档生成服务接口
/// </summary>
public interface IExcelDocumentGenerationService
{
    /// <summary>
    /// 根据操作点列表生成Excel文档
    /// </summary>
    /// <param name="operationPoints">操作点列表</param>
    /// <returns>生成的Excel文档路径</returns>
    Task<string> GenerateExcelDocumentAsync(List<OperationPoint> operationPoints);

    /// <summary>
    /// 执行单个Excel操作点
    /// </summary>
    /// <param name="operationPoint">要执行的操作点</param>
    /// <param name="workbook">Excel工作簿对象</param>
    /// <returns>执行结果</returns>
    Task<bool> ExecuteOperationPointAsync(OperationPoint operationPoint, object workbook);

    /// <summary>
    /// 验证操作点参数
    /// </summary>
    /// <param name="operationPoint">要验证的操作点</param>
    /// <returns>验证结果</returns>
    bool ValidateOperationPoint(OperationPoint operationPoint);
}
