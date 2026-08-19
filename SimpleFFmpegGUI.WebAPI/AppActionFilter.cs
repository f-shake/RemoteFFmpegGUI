using FzLib.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SimpleFFmpegGUI.WebAPI.Controllers;
using System.Linq;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Models;

namespace SimpleFFmpegGUI.WebAPI
{
    public class AppActionFilter(IOptionsSnapshot<AppSettings> appSettings) : IActionFilter
    {

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                if (context.Exception is HttpStatusCodeException sbe)
                {
                    if (string.IsNullOrEmpty(sbe.Message))
                    {
                        context.Result = new StatusCodeResult((int)sbe.StatusCode);
                    }
                    else
                    {
                        context.Result = new ObjectResult(sbe.Message) { StatusCode = (int)sbe.StatusCode };
                    }
                    context.ExceptionHandled = true;
                }
                else
                {
                    // 不把异常详细信息直接返回给客户端（可能泄露 SQL/路径等），统一 500（P3-3）
                    context.Result = new StatusCodeResult(500);
                    context.ExceptionHandled = true;
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<AppActionFilter>>();
                    logger?.LogError(context.Exception, "控制器执行异常");
                }
            }
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.Controller is TokenController)
            {
                return;
            }
            var token = appSettings.Value.Token;
            // IsNullOrEmpty：显式配置为 null（JSON 中写 "Token": null）与空字符串等同为不鉴权，避免鉴权逻辑反转锁死服务
            if (!string.IsNullOrEmpty(token))
            {
                if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out StringValues value)
                    || StringValues.IsNullOrEmpty(value)
                    || value.FirstOrDefault() == "undefined")
                {
                    context.Result = new UnauthorizedObjectResult("需要Token");
                    return;
                }
                if (value != $"Bearer {token}")
                {
                    context.Result = new UnauthorizedObjectResult("Token不正确");
                    return;
                }
            }
        }
    }
}