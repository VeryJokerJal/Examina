using System.Reactive;
using Examina.Models;
using ReactiveUI;

namespace Examina.ViewModels.Dialogs;

/// <summary>
/// 综合实训规则说明对话框视图模型
/// </summary>
public class ComprehensiveTrainingRulesViewModel : ViewModelBase
{
    /// <summary>
    /// 确认开始命令
    /// </summary>
    public ReactiveCommand<Unit, bool> ConfirmCommand { get; }

    /// <summary>
    /// 取消命令
    /// </summary>
    public ReactiveCommand<Unit, bool> CancelCommand { get; }

    /// <summary>
    /// 训练规则信息
    /// </summary>
    public ComprehensiveTrainingRulesInfo RulesInfo { get; }

    public ComprehensiveTrainingRulesViewModel()
    {
        // 初始化规则信息
        RulesInfo = new ComprehensiveTrainingRulesInfo();

        // 初始化命令
        ConfirmCommand = ReactiveCommand.Create(() =>
        {
            System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingRulesViewModel: 确认命令被执行");
            return true;
        });

        CancelCommand = ReactiveCommand.Create(() =>
        {
            System.Diagnostics.Debug.WriteLine("ComprehensiveTrainingRulesViewModel: 取消命令被执行");
            return false;
        });
    }
}
