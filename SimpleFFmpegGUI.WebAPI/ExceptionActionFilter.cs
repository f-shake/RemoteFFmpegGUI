using FzLib.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace SimpleFFmpegGUI.WebAPI
{
    public class ExceptionActionFilter(IConfiguration config) : IActionFilter
    {
        private readonly IConfiguration config = config;

        public void OnActionExecuting(ActionExecutingContext context)
        {
#if !DEBUG
        var token = config["Token"];
        if (context.HttpContext.Request.Method is not ("GET" or "OPTION") && !string.IsNullOrEmpty(token))
        {
            var headers = context.HttpContext.Request.Headers;
            if (headers.ContainsKey("Authorization"))
            {
                if (headers["Authorization"] != token)
                {
                    context.Result = new UnauthorizedObjectResult("登陆密钥不正确");
                }
            }
            else
            {
                context.Result = new UnauthorizedObjectResult("敏感操作，未登录");
            }
        }
#endif
        }

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
                    if (!string.IsNullOrEmpty(context.Exception.Message))
                    {
                        context.Result = new ObjectResult(context.Exception.Message) { StatusCode = 500 };
                        context.ExceptionHandled = true;
                    }
                    context.ExceptionHandled = true;
                }
            }
        }
    }
}