using System;
using System.Threading.Tasks;
using ExamLab.Models;
using ExamLab.Services;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// 操作点编辑视图模型
/// </summary>
public class OperationPointEditViewModel : ViewModelBase
{
    /// <summary>
    /// 当前编辑的操作点
    /// </summary>
    [Reactive] public OperationPoint OperationPoint { get; set; }

    /// <summary>
    /// 模块类型
    /// </summary>
    [Reactive] public ModuleType ModuleType { get; set; }

    /// <summary>
    /// 是否为Windows模块
    /// </summary>
    public bool IsWindowsModule => ModuleType == ModuleType.Windows;

    /// <summary>
    /// 是否为C#模块
    /// </summary>
    public bool IsCSharpModule => ModuleType == ModuleType.CSharp;

    /// <summary>
    /// 是否为PowerPoint模块
    /// </summary>
    public bool IsPowerPointModule => ModuleType == ModuleType.PowerPoint;

    /// <summary>
    /// 是否为Word模块
    /// </summary>
    public bool IsWordModule => ModuleType == ModuleType.Word;

    /// <summary>
    /// 是否为Excel模块
    /// </summary>
    public bool IsExcelModule => ModuleType == ModuleType.Excel;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    /// <param name="moduleType">模块类型</param>
    public OperationPointEditViewModel(OperationPoint operationPoint, ModuleType moduleType)
    {
        OperationPoint = operationPoint ?? throw new ArgumentNullException(nameof(operationPoint));
        ModuleType = moduleType;

        Title = "编辑操作点";

        // 确保操作点的模块类型与当前模块类型一致
        OperationPoint.ModuleType = moduleType;
    }

    /// <summary>
    /// 保存操作点
    /// </summary>
    public async Task SaveOperationPointAsync()
    {
        try
        {
            // 这里可以添加保存逻辑，比如验证、数据持久化等
            // 目前操作点已经是引用类型，修改会直接反映到原对象

            await NotificationService.ShowSuccessAsync("保存成功", $"操作点"{OperationPoint.Name}"已保存");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("保存失败", $"保存操作点时发生错误：{ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    public async Task ShowErrorAsync(string title, string message)
    {
        await NotificationService.ShowErrorAsync(title, message);
    }
}
