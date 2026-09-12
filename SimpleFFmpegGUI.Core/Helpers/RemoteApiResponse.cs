using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Helpers;

/// <summary>
/// 远程主机（WebAPI）调用的响应校验。放在 Core 而不是 WPF 客户端里：便于单元测试。
/// 只看 HTTP 状态码不足以判断"请求确实打到了 API"：地址写错时（例如漏了 v2 统一加的 "/api" 前缀），
/// 请求会落到对方站点的前端 SPA 兜底页上，拿到的是 200 + HTML —— 看起来成功，实际什么都没做。
/// 所以这里统一要求：非 2xx 一律抛错；2xx 若带了响应体，则响应体必须是 JSON。
/// </summary>
public static class RemoteApiResponse
{
    /// <summary>
    /// 读取并校验响应，返回响应体文本（204 NoContent 之类没有响应体的情况返回空串）。
    /// 调用方拿到文本后自行解析（如 Token/Need 的 true/false）；不关心响应体时直接忽略返回值即可。
    /// </summary>
    /// <exception cref="HttpRequestException">
    /// 响应非 2xx，或响应体不是 JSON —— 后者基本都意味着地址没指向 FFmpeg WebAPI
    /// </exception>
    public static async Task<string> ReadAndValidateAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        // 报错文案里带上实际请求的地址：地址写错时（如漏了 /api）一眼能看出打到哪儿去了
        string url = response.RequestMessage?.RequestUri?.ToString() ?? "（未知地址）";

        if (!response.IsSuccessStatusCode)
        {
            // 沿用原有文案：状态码，有响应体时（WebAPI 的错误说明）附在冒号后面
            throw new HttpRequestException(string.IsNullOrWhiteSpace(body)
                ? $"请求 {url} 失败：{(int)response.StatusCode} {response.StatusCode}"
                : $"请求 {url} 失败：{(int)response.StatusCode} {response.StatusCode}：{body}");
        }

        // 队列开始/暂停等接口返回 204，没有响应体，属正常情况；只要带了响应体就要求是 JSON
        if (body.Length != 0 && !IsJson(response.Content))
        {
            throw new HttpRequestException(
                $"请求 {url} 返回的不是接口响应，而是网页内容"
                + $"（Content-Type: {response.Content.Headers.ContentType?.MediaType ?? "未知"}）。"
                + "请检查地址是否正确 —— v2 的接口都挂在 /api 下，地址应形如 http://主机:5001/api/");
        }

        return body;
    }

    /// <summary>
    /// 响应体是否为 JSON。application/json、application/problem+json 等一律视为 JSON
    /// （MediaType 不含 charset 等参数，故带 charset 的写法也能匹配）。
    /// </summary>
    private static bool IsJson(HttpContent content)
    {
        return content.Headers.ContentType?.MediaType?.EndsWith("json", StringComparison.OrdinalIgnoreCase) == true;
    }
}
