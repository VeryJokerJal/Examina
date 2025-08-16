using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ExaminaWebApplication.Filters
{
    /// <summary>
    /// 自定义授权特性：未登录时
    /// - API 请求返回 401 Unauthorized
    /// - 页面请求重定向至 /Admin/Login?returnUrl=...
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RequireLoginAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // 允许显式标注 [AllowAnonymous]
            Endpoint? endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                return Task.CompletedTask;
            }

            if (context.HttpContext.User?.Identity?.IsAuthenticated == true)
            {
                return Task.CompletedTask;
            }

            string path = context.HttpContext.Request.Path.Value ?? "/";
            if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new JsonResult(new { message = "未授权" })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return Task.CompletedTask;
            }

            string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            string target = $"/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";
            context.Result = new RedirectResult(target);
            return Task.CompletedTask;
        }
    }
}

