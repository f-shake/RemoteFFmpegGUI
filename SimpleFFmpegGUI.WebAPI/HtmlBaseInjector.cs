using System;

namespace SimpleFFmpegGUI.WebAPI;

/// <summary>
/// 为托管前端注入 <base href="{basePath}/">，前端在运行时从上读取部署基址（见 src/config.ts 的 getBase()）。
/// </summary>
public static class HtmlBaseInjector
{
    /// <summary>
    /// 注入 <base> 必须放在 <head> 起始处：HTML 中 <base> 只对它之后解析的标签生效。若插在 </head> 前，
    /// Vite 的 <script>/<link>（位于 base 之前）会按文档 URL 目录解析相对资源，深路由刷新时 ./assets
    /// 会落到 /ffmpeg/add/assets 等错误路径而白屏（首页 URL 目录恰等于 base 才看似正常）。
    /// </summary>
    public static string InjectBaseHref(string html, string basePath)
    {
        var clean = string.IsNullOrWhiteSpace(basePath) ? "/" : basePath.Trim();
        if (!clean.StartsWith("/")) clean = "/" + clean;
        if (!clean.EndsWith("/")) clean += "/";
        var tag = $"<base href=\"{clean}\">";
        // 定位 <head>（需排除 <header>：其后字符必须是 '>' 或空白），把 tag 插到 <head> 的 '>' 之后
        var headIdx = html.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
        while (headIdx >= 0)
        {
            var next = headIdx + "<head".Length;
            if (next < html.Length && (html[next] == '>' || char.IsWhiteSpace(html[next])))
            {
                var close = html.IndexOf('>', next);
                return close >= 0 ? html.Insert(close + 1, tag) : tag + html;
            }
            headIdx = html.IndexOf("<head", headIdx + 1, StringComparison.OrdinalIgnoreCase);
        }
        return tag + html; // 无 <head> 时兜底：置于文档最前
    }
}
