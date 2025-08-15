using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ExamLab.Models;
using ExamLab.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.ViewModels;

/// <summary>
/// 参数编辑视图模型
/// </summary>
public class ParameterEditViewModel : ViewModelBase
{
    /// <summary>
    /// 当前编辑的参数
    /// </summary>
    [Reactive] public ConfigurationParameter Parameter { get; set; }

    /// <summary>
    /// 是否为数字类型
    /// </summary>
    public bool IsNumberType => Parameter?.Type == ParameterType.Number;

    /// <summary>
    /// 是否为枚举类型（包括枚举和多选）
    /// </summary>
    public bool IsEnumType => Parameter?.Type == ParameterType.Enum || Parameter?.Type == ParameterType.MultipleChoice;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="parameter">参数</param>
    public ParameterEditViewModel(ConfigurationParameter parameter)
    {
        Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));

        Title = "编辑参数";

        // 监听参数类型变化，更新相关属性
        this.WhenAnyValue(x => x.Parameter.Type)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(IsNumberType));
                this.RaisePropertyChanged(nameof(IsEnumType));
            });
    }

    /// <summary>
    /// 保存参数
    /// </summary>
    public async Task SaveParameterAsync()
    {
        try
        {
            // 这里可以添加保存逻辑，比如验证、数据持久化等
            // 目前参数已经是引用类型，修改会直接反映到原对象

            await NotificationService.ShowSuccessAsync("保存成功", $"参数"{Parameter.DisplayName}"已保存");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("保存失败", $"保存参数时发生错误：{ex.Message}");
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
