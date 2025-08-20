using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// 考试错误处理服务
/// </summary>
public class ExamErrorHandler
{
    private readonly ILogger<ExamErrorHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamErrorHandler(ILogger<ExamErrorHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理网络相关错误
    /// </summary>
    public async Task<bool> HandleNetworkErrorAsync(Exception exception, string operation)
    {
        _logger.LogError(exception, "网络错误发生在操作: {Operation}", operation);

        // 检查网络连接状态
        bool isNetworkAvailable = await CheckNetworkConnectivityAsync();
        
        if (!isNetworkAvailable)
        {
            _logger.LogWarning("网络连接不可用，操作: {Operation}", operation);
            return false;
        }

        // 检查是否是超时错误
        if (IsTimeoutException(exception))
        {
            _logger.LogWarning("网络超时错误，操作: {Operation}", operation);
            return false;
        }

        // 检查是否是服务器错误
        if (IsServerException(exception))
        {
            _logger.LogWarning("服务器错误，操作: {Operation}", operation);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 处理考试提交错误
    /// </summary>
    public async Task<ExamSubmitErrorResult> HandleExamSubmitErrorAsync(Exception exception, string examType, int examId)
    {
        _logger.LogError(exception, "考试提交错误，类型: {ExamType}, ID: {ExamId}", examType, examId);

        ExamSubmitErrorResult result = new()
        {
            IsRetryable = false,
            ErrorMessage = "提交失败",
            SuggestedAction = "请联系技术支持"
        };

        // 网络相关错误
        if (IsNetworkException(exception))
        {
            bool networkAvailable = await CheckNetworkConnectivityAsync();
            if (!networkAvailable)
            {
                result.ErrorMessage = "网络连接不可用";
                result.SuggestedAction = "请检查网络连接后重试";
                result.IsRetryable = true;
            }
            else
            {
                result.ErrorMessage = "网络请求失败";
                result.SuggestedAction = "请稍后重试";
                result.IsRetryable = true;
            }
        }
        // 超时错误
        else if (IsTimeoutException(exception))
        {
            result.ErrorMessage = "请求超时";
            result.SuggestedAction = "网络较慢，请稍后重试";
            result.IsRetryable = true;
        }
        // 服务器错误
        else if (IsServerException(exception))
        {
            result.ErrorMessage = "服务器暂时不可用";
            result.SuggestedAction = "服务器繁忙，请稍后重试";
            result.IsRetryable = true;
        }
        // 认证错误
        else if (IsAuthenticationException(exception))
        {
            result.ErrorMessage = "身份验证失败";
            result.SuggestedAction = "请重新登录";
            result.IsRetryable = false;
        }
        // 权限错误
        else if (IsAuthorizationException(exception))
        {
            result.ErrorMessage = "没有权限执行此操作";
            result.SuggestedAction = "请联系管理员";
            result.IsRetryable = false;
        }

        return result;
    }

    /// <summary>
    /// 处理考试数据同步错误
    /// </summary>
    public async Task<bool> HandleDataSyncErrorAsync(Exception exception, string dataType)
    {
        _logger.LogError(exception, "数据同步错误，数据类型: {DataType}", dataType);

        // 检查网络连接
        bool networkAvailable = await CheckNetworkConnectivityAsync();
        if (!networkAvailable)
        {
            _logger.LogWarning("数据同步失败：网络不可用，数据类型: {DataType}", dataType);
            return false;
        }

        // 检查是否是临时错误
        if (IsTemporaryException(exception))
        {
            _logger.LogInformation("检测到临时错误，建议重试，数据类型: {DataType}", dataType);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查网络连接状态
    /// </summary>
    public async Task<bool> CheckNetworkConnectivityAsync()
    {
        try
        {
            // 检查网络接口状态
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                _logger.LogWarning("网络接口不可用");
                return false;
            }

            // Ping测试
            using Ping ping = new();
            PingReply reply = await ping.SendPingAsync("8.8.8.8", 5000);
            
            bool isConnected = reply.Status == IPStatus.Success;
            _logger.LogInformation("网络连接检查结果: {IsConnected}", isConnected);
            
            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "网络连接检查失败");
            return false;
        }
    }

    /// <summary>
    /// 记录考试异常事件
    /// </summary>
    public void LogExamException(Exception exception, string examType, int examId, string operation)
    {
        _logger.LogError(exception, 
            "考试异常 - 类型: {ExamType}, ID: {ExamId}, 操作: {Operation}, 异常类型: {ExceptionType}, 消息: {Message}",
            examType, examId, operation, exception.GetType().Name, exception.Message);
    }

    /// <summary>
    /// 记录考试警告事件
    /// </summary>
    public void LogExamWarning(string message, string examType, int examId, string operation)
    {
        _logger.LogWarning("考试警告 - 类型: {ExamType}, ID: {ExamId}, 操作: {Operation}, 消息: {Message}",
            examType, examId, operation, message);
    }

    /// <summary>
    /// 记录考试信息事件
    /// </summary>
    public void LogExamInfo(string message, string examType, int examId, string operation)
    {
        _logger.LogInformation("考试信息 - 类型: {ExamType}, ID: {ExamId}, 操作: {Operation}, 消息: {Message}",
            examType, examId, operation, message);
    }

    /// <summary>
    /// 检查是否是网络异常
    /// </summary>
    private static bool IsNetworkException(Exception exception)
    {
        return exception is HttpRequestException ||
               exception is WebException ||
               exception is System.Net.Sockets.SocketException;
    }

    /// <summary>
    /// 检查是否是超时异常
    /// </summary>
    private static bool IsTimeoutException(Exception exception)
    {
        return exception is TimeoutException ||
               exception is TaskCanceledException ||
               (exception is WebException webEx && webEx.Status == WebExceptionStatus.Timeout);
    }

    /// <summary>
    /// 检查是否是服务器异常
    /// </summary>
    private static bool IsServerException(Exception exception)
    {
        if (exception is HttpRequestException httpEx)
        {
            string message = httpEx.Message.ToLower();
            return message.Contains("500") || message.Contains("502") || message.Contains("503") || message.Contains("504");
        }
        
        if (exception is WebException webEx)
        {
            return webEx.Status == WebExceptionStatus.ServerProtocolViolation ||
                   webEx.Status == WebExceptionStatus.ReceiveFailure;
        }

        return false;
    }

    /// <summary>
    /// 检查是否是认证异常
    /// </summary>
    private static bool IsAuthenticationException(Exception exception)
    {
        if (exception is HttpRequestException httpEx)
        {
            return httpEx.Message.Contains("401");
        }

        return false;
    }

    /// <summary>
    /// 检查是否是授权异常
    /// </summary>
    private static bool IsAuthorizationException(Exception exception)
    {
        if (exception is HttpRequestException httpEx)
        {
            return httpEx.Message.Contains("403");
        }

        return false;
    }

    /// <summary>
    /// 检查是否是临时异常
    /// </summary>
    private static bool IsTemporaryException(Exception exception)
    {
        return IsTimeoutException(exception) || IsServerException(exception);
    }
}

/// <summary>
/// 考试提交错误结果
/// </summary>
public class ExamSubmitErrorResult
{
    /// <summary>
    /// 是否可以重试
    /// </summary>
    public bool IsRetryable { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 建议的操作
    /// </summary>
    public string SuggestedAction { get; set; } = string.Empty;
}
