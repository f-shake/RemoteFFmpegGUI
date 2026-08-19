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
    [HttpGet("Check/{token}")]
    public ActionResult<bool> CheckToken(string token)
    {
        var realToken = appSettings.Value.Token;
        if (string.IsNullOrEmpty(realToken))
        {
            return true;
        }

        // 仅接受明文，与 AppActionFilter 的 "Bearer {token}" 鉴权保持一致；
        // 不再接受 SHA256 哈希（旧语义会造成「校验通过但全部 API 401」的误导）；用恒时比较避免时序侧信道
        return token != null && FixedTimeEquals(token, realToken);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }

    [HttpGet("Need")]
    public ActionResult<bool> NeedToken()
    {
        return !string.IsNullOrEmpty(appSettings.Value.Token);
    }
}