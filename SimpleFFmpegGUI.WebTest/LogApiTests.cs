using System.Net.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Models.Entities;

namespace SimpleFFmpegGUI.WebTest;

/// <summary>
/// 日志 API 用例（P4-3 缺失用例）
/// </summary>
public class LogApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestGetLogsAsync()
    {
        // 日志由 DbLoggerService 周期异步落库，这里验证接口与分页语义正确
        var response = await GetAsync("/Log?Page=1&PageSize=10");
        response.IsSuccessStatusCode.Should().BeTrue();
        var page1 = ParseLogs(await response.Content.ReadAsStringAsync());
        page1.List.Should().NotBeNull();
        // PageSize 生效：单页条数不超过请求值；TotalCount 不小于本页条数
        page1.List.Count.Should().BeLessThanOrEqualTo(10);
        page1.TotalCount.Should().BeGreaterThanOrEqualTo(page1.List.Count);

        // 分页语义：倒序分页下第 2 页与第 1 页不应有重叠记录
        response = await GetAsync("/Log?Page=2&PageSize=10");
        response.IsSuccessStatusCode.Should().BeTrue();
        var page2 = ParseLogs(await response.Content.ReadAsStringAsync());
        page2.List.Count.Should().BeLessThanOrEqualTo(10);
        var page1Ids = page1.List.Select(l => l.Id).ToHashSet();
        page2.List.Select(l => l.Id).Should().NotIntersectWith(page1Ids);
    }

    /// <summary>
    /// 无任何查询参数时使用默认分页（Page=1、PageSize=20），应正常返回
    /// </summary>
    [Fact]
    public async Task TestGetLogsDefaultPagingAsync()
    {
        var response = await GetAsync("/Log");
        response.IsSuccessStatusCode.Should().BeTrue();
        var logs = ParseLogs(await response.Content.ReadAsStringAsync());
        logs.List.Should().NotBeNull();
        logs.List.Count.Should().BeLessThanOrEqualTo(20);
    }

    private PagedListResponse<LogEntity> ParseLogs(string content)
    {
        return content.DeserializeWithWebSettings<PagedListResponse<LogEntity>>();
    }
}
