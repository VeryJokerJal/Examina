using Examina.Models.Enums;
using ReactiveUI;

namespace Examina.Models;

/// <summary>
/// 排序选项项目模型
/// </summary>
public class SortOptionItem : ReactiveObject
{
    private SortOption _value;
    private string _displayText = string.Empty;
    private string _shortDescription = string.Empty;
    private bool _isSelected;

    /// <summary>
    /// 排序选项值
    /// </summary>
    public SortOption Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    /// <summary>
    /// 显示文本
    /// </summary>
    public string DisplayText
    {
        get => _displayText;
        set => this.RaiseAndSetIfChanged(ref _displayText, value);
    }

    /// <summary>
    /// 简短描述
    /// </summary>
    public string ShortDescription
    {
        get => _shortDescription;
        set => this.RaiseAndSetIfChanged(ref _shortDescription, value);
    }

    /// <summary>
    /// 是否被选中
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public SortOptionItem()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="sortOption">排序选项</param>
    public SortOptionItem(SortOption sortOption)
    {
        Value = sortOption;
        DisplayText = sortOption.GetDisplayText();
        ShortDescription = sortOption.GetShortDescription();
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="sortOption">排序选项</param>
    /// <param name="isSelected">是否被选中</param>
    public SortOptionItem(SortOption sortOption, bool isSelected) : this(sortOption)
    {
        IsSelected = isSelected;
    }

    /// <summary>
    /// 重写ToString方法
    /// </summary>
    /// <returns>显示文本</returns>
    public override string ToString()
    {
        return DisplayText;
    }
}
