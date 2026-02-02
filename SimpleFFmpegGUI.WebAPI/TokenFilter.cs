using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using SimpleFFmpegGUI.WebAPI.Controllers;
using System.Linq;

namespace SimpleFFmpegGUI.WebAPI
{
    public class TokenFilter(IConfiguration config) : ActionFilterAttribute
    {
        private readonly IConfiguration config = config;
        private string token;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.Controller is TokenController)
            {
                return;
            }
            var http = context.HttpContext;

            token ??= config.GetValue<string>(AppSettingsKeys.TokenKey) ?? "";
            if (token != "")
            {
                if (!http.Request.Headers.TryGetValue("Authorization", out StringValues value)
                    || StringValues.IsNullOrEmpty(value)
                    || value.FirstOrDefault() == "undefined")
                {
                    context.Result = new UnauthorizedObjectResult("需要Token");
                    return;
                }
                if (value != token)
                {
                    context.Result = new UnauthorizedObjectResult("Token不正确");
                    return;
                }
            }
        }
    }
}