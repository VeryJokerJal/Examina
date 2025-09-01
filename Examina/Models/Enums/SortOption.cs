namespace Examina.Models.Enums;

/// <summary>
/// 排序选项枚举
/// </summary>
public enum SortOption
{
    /// <summary>
    /// 按名称A-Z排序（升序）
    /// </summary>
    NameAscending,

    /// <summary>
    /// 按名称Z-A排序（降序）
    /// </summary>
    NameDescending,

    /// <summary>
    /// 按时间最早排序（创建时间或导入时间升序）
    /// </summary>
    TimeEarliest,

    /// <summary>
    /// 按时间最晚排序（创建时间或导入时间降序）
    /// </summary>
    TimeLatest,

    /// <summary>
    /// 按分数从高到低排序
    /// </summary>
    ScoreDescending,

    /// <summary>
    /// 按分数从低到高排序
    /// </summary>
    ScoreAscending,

    /// <summary>
    /// 按排名排序（默认排序方式）
    /// </summary>
    RankAscending
}

/// <summary>
/// 排序选项扩展方法
/// </summary>
public static class SortOptionExtensions
{
    /// <summary>
    /// 获取排序选项的显示文本
    /// </summary>
    /// <param name="sortOption">排序选项</param>
    /// <returns>显示文本</returns>
    public static string GetDisplayText(this SortOption sortOption)
    {
        return sortOption switch
        {
            SortOption.NameAscending => "按名称A-Z排序",
            SortOption.NameDescending => "按名称Z-A排序",
            SortOption.TimeEarliest => "按时间最早排序",
            SortOption.TimeLatest => "按时间最晚排序",
            SortOption.ScoreDescending => "按分数从高到低排序",
            SortOption.ScoreAscending => "按分数从低到高排序",
            SortOption.RankAscending => "按排名排序",
            _ => "未知排序"
        };
    }

    /// <summary>
    /// 获取排序选项的简短描述
    /// </summary>
    /// <param name="sortOption">排序选项</param>
    /// <returns>简短描述</returns>
    public static string GetShortDescription(this SortOption sortOption)
    {
        return sortOption switch
        {
            SortOption.NameAscending => "名称升序",
            SortOption.NameDescending => "名称降序",
            SortOption.TimeEarliest => "时间升序",
            SortOption.TimeLatest => "时间降序",
            SortOption.ScoreDescending => "分数降序",
            SortOption.ScoreAscending => "分数升序",
            SortOption.RankAscending => "排名升序",
            _ => "未知"
        };
    }
}
