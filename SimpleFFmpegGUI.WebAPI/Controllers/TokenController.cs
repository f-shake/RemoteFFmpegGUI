using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Models;

namespace SimpleFFmpegGUI.WebAPI.Controllers;

public class TokenController(IOptionsSnapshot<AppSettings> appSettings) : FFmpegControllerBase
{
    [HttpGet("Check/{tokenWithSha256}")]
    public ActionResult<bool> CheckToken(string token)
    {
        var realToken = appSettings.Value.Token;
        if (string.IsNullOrEmpty(realToken))
        {
            return true;
        }

        if (token == realToken)
        {
            return true;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(realToken));
        var hashString = BitConverter.ToString(hash).Replace("-", "");
        return hashString == token;
    }

    [HttpGet("Need")]
    public ActionResult<bool> NeedToken()
    {
        return !string.IsNullOrEmpty(appSettings.Value.Token);
    }
}