using System;
using System.Net.Http;

namespace SimpleFFmpegGUI.Helpers;

/// <summary>
/// 远程主机 API 请求的构造：统一地址拼接与 Bearer 鉴权头。放在 Core 便于单元测试，
/// 因此只收地址与令牌字符串，不依赖 WPF 的 RemoteHost 类型。
/// </summary>
public static class RemoteApiRequest
{
    /// <summary>
    /// 按主机地址拼出完整 URL（去地址尾部斜杠、补子路径前导斜杠），
    /// 例如 ("http://host:5001/api/", "File/Dirs") → "http://host:5001/api/File/Dirs"
    /// </summary>
    public static string BuildUrl(string address, string subUrl)
    {
        return (address ?? "").TrimEnd('/') + "/" + subUrl.TrimStart('/');
    }

    /// <summary>
    /// 给请求加 Bearer 鉴权头（令牌为空则不加）。v2 WebAPI 校验 "Bearer {token}" 格式，
    /// 这里兼容 v1 已保存带 "Bearer " 前缀的令牌。
    /// </summary>
    public static void AddAuthorization(HttpRequestMessage request, string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var value = token.Trim();
        if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            value = value["Bearer ".Length..];
        }
        request.Headers.Add("Authorization", $"Bearer {value}");
    }
}
