namespace Examina.Services;

/// <summary>
/// 安全存储服务接口
/// </summary>
public interface ISecureStorageService
{
    /// <summary>
    /// 安全存储数据
    /// </summary>
    /// <param name="key">存储键</param>
    /// <param name="value">存储值</param>
    /// <returns>是否存储成功</returns>
    Task<bool> SetAsync(string key, string value);

    /// <summary>
    /// 获取存储的数据
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>存储的值，如果不存在则返回null</returns>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// 删除存储的数据
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>是否删除成功</returns>
    Task<bool> RemoveAsync(string key);

    /// <summary>
    /// 检查是否存在指定的键
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>是否存在</returns>
    Task<bool> ContainsKeyAsync(string key);

    /// <summary>
    /// 清除所有存储的数据
    /// </summary>
    /// <returns>是否清除成功</returns>
    Task<bool> ClearAsync();
}
