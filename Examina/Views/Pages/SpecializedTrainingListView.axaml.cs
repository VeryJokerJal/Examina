using Avalonia.Controls;
using Examina.ViewModels.Pages;

namespace Examina.Views.Pages;

/// <summary>
/// 专项训练列表视图
/// </summary>
public partial class SpecializedTrainingListView : UserControl
{
    public SpecializedTrainingListView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 设置ViewModel并初始化数据
    /// </summary>
    /// <param name="viewModel">ViewModel实例</param>
    public async Task SetViewModelAsync(SpecializedTrainingListViewModel viewModel)
    {
        DataContext = viewModel;
        await viewModel.InitializeAsync();
    }
}
