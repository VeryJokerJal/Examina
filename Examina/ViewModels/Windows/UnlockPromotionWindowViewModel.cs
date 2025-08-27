namespace Examina.ViewModels.Windows;

/// <summary>
/// 解锁推广窗口ViewModel
/// </summary>
public class UnlockPromotionWindowViewModel : ViewModelBase
{
    /// <summary>
    /// 推广文本
    /// </summary>
    public string PromotionText => "扫码添加微信，联系管理员解锁完整功能";

    /// <summary>
    /// 窗口标题
    /// </summary>
    public string WindowTitle => "解锁完整功能";

    public UnlockPromotionWindowViewModel()
    {

    }
}
