using System.ComponentModel.DataAnnotations;

namespace Examina.Models;

/// <summary>
/// 考试类型枚举
/// </summary>
public enum ExamCategory
{
    /// <summary>
    /// 学校统考
    /// </summary>
    [Display(Name = "学校统考")]
    School = 0,

    /// <summary>
    /// 全省统考
    /// </summary>
    [Display(Name = "全省统考")]
    Provincial = 1
}
