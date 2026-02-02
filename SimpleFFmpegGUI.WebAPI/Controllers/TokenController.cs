using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SimpleFFmpegGUI.WebAPI.Controllers;

public class TokenController(IConfiguration config) : FFmpegControllerBase(config)
{
    [HttpGet]
    [Route("Check")]
    public bool CheckToken(string token)
    {
        return config.GetValue<string>(AppSettingsKeys.TokenKey) == token;
    }

    [HttpGet]
    [Route("Need")]
    public bool NeedToken()
    {
        return config.GetValue<string>(AppSettingsKeys.TokenKey) != null;
    }
}