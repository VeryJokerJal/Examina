using Avalonia.Controls;
using Examina.ViewModels.Pages;

namespace Examina.Views.Pages;

/// <summary>
/// 专项训练详情视图
/// </summary>
public partial class SpecializedTrainingDetailView : UserControl
{
    public SpecializedTrainingDetailView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 设置ViewModel并初始化数据
    /// </summary>
    /// <param name="viewModel">ViewModel实例</param>
    /// <param name="trainingId">训练ID</param>
    public async Task SetViewModelAsync(SpecializedTrainingDetailViewModel viewModel, int trainingId)
    {
        DataContext = viewModel;
        await viewModel.InitializeAsync(trainingId);
    }
}
