using System;
using FluentAssertions;
using SimpleFFmpegGUI.WebAPI;
using Xunit;

namespace SimpleFFmpegGUI.WebTest;

/// <summary>
/// 回归测试：<base> 必须注入在 <head> 起始处（排在 <script>/<link> 之前），否则深路由刷新时
/// 相对资源 ./assets 会按文档 URL 目录解析（/ffmpeg/add/assets/...）而白屏。参见 HtmlBaseInjector。
/// </summary>
public class HtmlBaseInjectorTests
{
    // 模拟 Vite 产物形态：<head> 内已含相对引用的 <script>/<link>（它们排在 base 之前是正常产物顺序）
    private const string ViteLike = "<!doctype html><html><head><title>t</title><script src=\"./assets/a.js\"></script><link rel=\"stylesheet\" href=\"./assets/a.css\"></head><body>x</body></html>";

    [Fact]
    public void InjectBaseHref_把base插在script和link之前()
    {
        var result = HtmlBaseInjector.InjectBaseHref(ViteLike, "/ffmpeg");

        var baseIdx = result.IndexOf("<base href=\"/ffmpeg/\">", StringComparison.OrdinalIgnoreCase);
        var scriptIdx = result.IndexOf("<script", StringComparison.OrdinalIgnoreCase);
        var linkIdx = result.IndexOf("<link", StringComparison.OrdinalIgnoreCase);

        baseIdx.Should().BeGreaterThanOrEqualTo(0, "应注入 <base>");
        baseIdx.Should().BeLessThan(scriptIdx, "<base> 应排在 <script> 之前");
        baseIdx.Should().BeLessThan(linkIdx, "<base> 应排在 <link> 之前");
    }

    [Fact]
    public void InjectBaseHref_空或没有head时兜底为文档开头()
    {
        HtmlBaseInjector.InjectBaseHref("<html><body>x</body></html>", null)
            .Should().StartWith("<base href=\"/\">");
        HtmlBaseInjector.InjectBaseHref("plain", "/ffmpeg")
            .Should().StartWith("<base href=\"/ffmpeg/\">");
    }
}
