using ExaminaWebApplication.Models.FileUpload;

namespace ExaminaWebApplication.Services.FileUpload;

/// <summary>
/// 文件上传服务接口
/// </summary>
public interface IFileUploadService
{
    /// <summary>
    /// 上传单个文件
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <param name="uploadedBy">上传者ID</param>
    /// <param name="description">文件描述</param>
    /// <param name="tags">文件标签</param>
    /// <returns>上传结果</returns>
    Task<FileUploadResult> UploadFileAsync(IFormFile file, int uploadedBy, string? description = null, string? tags = null);

    /// <summary>
    /// 上传多个文件
    /// </summary>
    /// <param name="files">上传的文件列表</param>
    /// <param name="uploadedBy">上传者ID</param>
    /// <param name="description">文件描述</param>
    /// <param name="tags">文件标签</param>
    /// <returns>上传结果列表</returns>
    Task<List<FileUploadResult>> UploadFilesAsync(IFormFileCollection files, int uploadedBy, string? description = null, string? tags = null);

    /// <summary>
    /// 验证文件
    /// </summary>
    /// <param name="file">要验证的文件</param>
    /// <returns>验证结果</returns>
    FileValidationResult ValidateFile(IFormFile file);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <param name="deletedBy">删除者ID</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteFileAsync(int fileId, int deletedBy);

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>文件信息</returns>
    Task<UploadedFile?> GetFileAsync(int fileId);

    /// <summary>
    /// 获取用户上传的文件列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>文件列表</returns>
    Task<(List<UploadedFile> Files, int TotalCount)> GetUserFilesAsync(int userId, int pageIndex = 0, int pageSize = 20);

    /// <summary>
    /// 关联文件到考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="fileId">文件ID</param>
    /// <param name="fileType">文件类型</param>
    /// <param name="createdBy">创建者ID</param>
    /// <param name="purpose">文件用途</param>
    /// <returns>是否关联成功</returns>
    Task<bool> AssociateFileToExamAsync(int examId, int fileId, string fileType, int createdBy, string? purpose = null);

    /// <summary>
    /// 关联文件到综合训练
    /// </summary>
    /// <param name="comprehensiveTrainingId">综合训练ID</param>
    /// <param name="fileId">文件ID</param>
    /// <param name="fileType">文件类型</param>
    /// <param name="createdBy">创建者ID</param>
    /// <param name="purpose">文件用途</param>
    /// <returns>是否关联成功</returns>
    Task<bool> AssociateFileToComprehensiveTrainingAsync(int comprehensiveTrainingId, int fileId, string fileType, int createdBy, string? purpose = null);

    /// <summary>
    /// 关联文件到专项训练
    /// </summary>
    /// <param name="specializedTrainingId">专项训练ID</param>
    /// <param name="fileId">文件ID</param>
    /// <param name="fileType">文件类型</param>
    /// <param name="createdBy">创建者ID</param>
    /// <param name="purpose">文件用途</param>
    /// <returns>是否关联成功</returns>
    Task<bool> AssociateFileToSpecializedTrainingAsync(int specializedTrainingId, int fileId, string fileType, int createdBy, string? purpose = null);

    /// <summary>
    /// 获取考试关联的文件列表
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>文件列表</returns>
    Task<List<UploadedFile>> GetExamFilesAsync(int examId);

    /// <summary>
    /// 获取综合训练关联的文件列表
    /// </summary>
    /// <param name="comprehensiveTrainingId">综合训练ID</param>
    /// <returns>文件列表</returns>
    Task<List<UploadedFile>> GetComprehensiveTrainingFilesAsync(int comprehensiveTrainingId);

    /// <summary>
    /// 获取专项训练关联的文件列表
    /// </summary>
    /// <param name="specializedTrainingId">专项训练ID</param>
    /// <returns>文件列表</returns>
    Task<List<UploadedFile>> GetSpecializedTrainingFilesAsync(int specializedTrainingId);

    /// <summary>
    /// 计算文件哈希值
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <returns>哈希值</returns>
    Task<string> ComputeFileHashAsync(Stream stream);

    /// <summary>
    /// 生成唯一文件名
    /// </summary>
    /// <param name="originalFileName">原始文件名</param>
    /// <returns>唯一文件名</returns>
    string GenerateUniqueFileName(string originalFileName);

    /// <summary>
    /// 获取文件下载URL
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>下载URL</returns>
    string GetFileDownloadUrl(int fileId);

    /// <summary>
    /// 更新文件下载统计
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateDownloadStatsAsync(int fileId);
}
