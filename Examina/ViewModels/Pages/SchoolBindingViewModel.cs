using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 加入学校页面视图模型
/// </summary>
public class SchoolBindingViewModel : ViewModelBase
{
    #region 属性

    /// <summary>
    /// 页面标题
    /// </summary>
    [Reactive]
    public string PageTitle { get; set; } = "加入学校";

    /// <summary>
    /// 学校代码
    /// </summary>
    [Reactive]
    public string SchoolCode { get; set; } = string.Empty;

    /// <summary>
    /// 学校名称
    /// </summary>
    [Reactive]
    public string SchoolName { get; set; } = string.Empty;

    /// <summary>
    /// 是否已绑定学校
    /// </summary>
    [Reactive]
    public bool IsSchoolBound { get; set; } = false;

    /// <summary>
    /// 当前绑定的学校
    /// </summary>
    [Reactive]
    public string CurrentSchool { get; set; } = string.Empty;

    /// <summary>
    /// 是否正在处理
    /// </summary>
    [Reactive]
    public bool IsProcessing { get; set; } = false;

    /// <summary>
    /// 状态消息
    /// </summary>
    [Reactive]
    public string StatusMessage { get; set; } = string.Empty;

    #endregion

    #region 命令

    /// <summary>
    /// 搜索学校命令
    /// </summary>
    public ICommand SearchSchoolCommand { get; }

    /// <summary>
    /// 绑定学校命令
    /// </summary>
    public ICommand BindSchoolCommand { get; }

    /// <summary>
    /// 解绑学校命令
    /// </summary>
    public ICommand UnbindSchoolCommand { get; }

    #endregion

    #region 构造函数

    public SchoolBindingViewModel()
    {
        SearchSchoolCommand = new DelegateCommand(SearchSchool, CanSearchSchool);
        BindSchoolCommand = new DelegateCommand(BindSchool, CanBindSchool);
        UnbindSchoolCommand = new DelegateCommand(UnbindSchool, CanUnbindSchool);

        LoadCurrentSchoolBinding();
    }

    #endregion

    #region 方法

    /// <summary>
    /// 加载当前学校绑定状态
    /// </summary>
    private void LoadCurrentSchoolBinding()
    {
        // TODO: 从服务加载实际绑定状态
        IsSchoolBound = false;
        CurrentSchool = string.Empty;
    }

    /// <summary>
    /// 搜索学校
    /// </summary>
    private async void SearchSchool()
    {
        if (string.IsNullOrWhiteSpace(SchoolCode))
        {
            return;
        }

        IsProcessing = true;
        StatusMessage = "正在搜索学校...";

        try
        {
            // TODO: 实现学校搜索逻辑
            await Task.Delay(1000); // 模拟网络请求

            // 模拟搜索结果
            SchoolName = $"示例学校 ({SchoolCode})";
            StatusMessage = "找到学校，请确认后点击绑定";
        }
        catch (Exception ex)
        {
            StatusMessage = $"搜索失败: {ex.Message}";
            SchoolName = string.Empty;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// 是否可以搜索学校
    /// </summary>
    private bool CanSearchSchool()
    {
        return !string.IsNullOrWhiteSpace(SchoolCode) && !IsProcessing;
    }

    /// <summary>
    /// 绑定学校
    /// </summary>
    private async void BindSchool()
    {
        IsProcessing = true;
        StatusMessage = "正在绑定学校...";

        try
        {
            // TODO: 实现学校绑定逻辑
            await Task.Delay(1000); // 模拟网络请求

            IsSchoolBound = true;
            CurrentSchool = SchoolName;
            StatusMessage = "学校绑定成功";

            // 清空输入
            SchoolCode = string.Empty;
            SchoolName = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"绑定失败: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// 是否可以绑定学校
    /// </summary>
    private bool CanBindSchool()
    {
        return !string.IsNullOrWhiteSpace(SchoolName) && !IsSchoolBound && !IsProcessing;
    }

    /// <summary>
    /// 解绑学校
    /// </summary>
    private async void UnbindSchool()
    {
        IsProcessing = true;
        StatusMessage = "正在解绑学校...";

        try
        {
            // TODO: 实现学校解绑逻辑
            await Task.Delay(1000); // 模拟网络请求

            IsSchoolBound = false;
            CurrentSchool = string.Empty;
            StatusMessage = "学校解绑成功";
        }
        catch (Exception ex)
        {
            StatusMessage = $"解绑失败: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// 是否可以解绑学校
    /// </summary>
    private bool CanUnbindSchool()
    {
        return IsSchoolBound && !IsProcessing;
    }

    #endregion
}
