namespace ExamLab.Services.DocumentGeneration;

/// <summary>
/// 文档验证结果
/// </summary>
public class DocumentValidationResult
{
    /// <summary>
    /// 是否验证通过
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误消息列表
    /// </summary>
    public List<string> ErrorMessages { get; set; } = [];

    /// <summary>
    /// 警告消息列表
    /// </summary>
    public List<string> WarningMessages { get; set; } = [];

    /// <summary>
    /// 验证的详细信息
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// 创建成功的验证结果
    /// </summary>
    /// <param name="details">详细信息</param>
    /// <returns>验证结果</returns>
    public static DocumentValidationResult Success(string? details = null)
    {
        return new DocumentValidationResult
        {
            IsValid = true,
            Details = details
        };
    }

    /// <summary>
    /// 创建失败的验证结果
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <returns>验证结果</returns>
    public static DocumentValidationResult Failure(string errorMessage)
    {
        return new DocumentValidationResult
        {
            IsValid = false,
            ErrorMessages = [errorMessage]
        };
    }

    /// <summary>
    /// 创建失败的验证结果
    /// </summary>
    /// <param name="errorMessages">错误消息列表</param>
    /// <returns>验证结果</returns>
    public static DocumentValidationResult Failure(IEnumerable<string> errorMessages)
    {
        return new DocumentValidationResult
        {
            IsValid = false,
            ErrorMessages = errorMessages.ToList()
        };
    }

    /// <summary>
    /// 添加错误消息
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    public void AddError(string errorMessage)
    {
        ErrorMessages.Add(errorMessage);
        IsValid = false;
    }

    /// <summary>
    /// 添加警告消息
    /// </summary>
    /// <param name="warningMessage">警告消息</param>
    public void AddWarning(string warningMessage)
    {
        WarningMessages.Add(warningMessage);
    }
}
