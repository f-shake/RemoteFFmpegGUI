using FzLib.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using NGettext.Plural.Ast;
using SimpleFFmpegGUI.WebAPI.Controllers;
using System.Linq;

namespace SimpleFFmpegGUI.WebAPI
{
    public class AppActionFilter : IActionFilter
    {
        private readonly string token;

        public AppActionFilter(IConfiguration config)
        {
            token ??= config.GetValue<string>(AppSettingsKeys.TokenKey) ?? "";
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

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.Controller is TokenController)
            {
                return;
            }

            if (token != "")
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