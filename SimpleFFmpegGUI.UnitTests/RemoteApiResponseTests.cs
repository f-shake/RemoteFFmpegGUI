using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using SimpleFFmpegGUI.Helpers;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// 远程主机 API 响应校验（RemoteApiResponse）：区分"真接口响应"与"前端 SPA 兜底页"
/// </summary>
public class RemoteApiResponseTests
{
    private static HttpResponseMessage Response(HttpStatusCode status, string body, string mediaType)
    {
        var response = new HttpResponseMessage(status)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://host:5001/api/Token/Need")
        };
        if (body != null)
        {
            response.Content = new StringContent(body, Encoding.UTF8, mediaType);
        }
        return response;
    }

    [Fact]
    public async Task Json200_Passes()
    {
        // StringContent 会附加 charset=utf-8，MediaType 仍是 application/json
        string body = await RemoteApiResponse.ReadAndValidateAsync(Response(HttpStatusCode.OK, "false", "application/json"));
        body.Should().Be("false");
    }

    [Fact]
    public async Task NoContent204_Passes()
    {
        // Queue/Start、Queue/Pause、Queue/Schedule 都是 204 无响应体，必须放行
        (await RemoteApiResponse.ReadAndValidateAsync(Response(HttpStatusCode.NoContent, null, null)))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Html200_Throws()
    {
        // 地址漏了 /api 时打到前端 SPA 兜底页：200 + HTML，原来只看状态码会误判成成功
        var act = async () => await RemoteApiResponse.ReadAndValidateAsync(
            Response(HttpStatusCode.OK, "<!doctype html><html><body>app</body></html>", "text/html"));

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Which.Message.Should().Contain("不是接口响应").And.Contain("text/html");
    }

    [Fact]
    public async Task ErrorStatus_Throws_WithStatusAndBody()
    {
        var act = async () => await RemoteApiResponse.ReadAndValidateAsync(
            Response(HttpStatusCode.NotFound, "输入文件不存在: x.mp4", "application/json"));

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Which.Message.Should().Contain("404").And.Contain("输入文件不存在");
    }

    [Fact]
    public async Task ErrorStatus_EmptyBody_Throws_WithStatusOnly()
    {
        var act = async () => await RemoteApiResponse.ReadAndValidateAsync(
            Response(HttpStatusCode.Unauthorized, "", "application/json"));

        (await act.Should().ThrowAsync<HttpRequestException>()).Which.Message.Should().Contain("401");
    }

    [Fact]
    public async Task EmptyBodyWithHtmlContentType_Passes()
    {
        // 补齐 body.Length != 0 这个分支的另一半：没有响应体时无从判断内容类型，一律放行
        // （与 204 同理，不能因为 Content-Type 是 text/html 就误伤）
        (await RemoteApiResponse.ReadAndValidateAsync(Response(HttpStatusCode.OK, "", "text/html")))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task ProblemJson_Passes()
    {
        // IsJson 按 "以 json 结尾" 判断，application/problem+json 之类的变体也应放行
        (await RemoteApiResponse.ReadAndValidateAsync(
            Response(HttpStatusCode.OK, "{\"title\":\"x\"}", "application/problem+json")))
            .Should().Contain("title");
    }

    [Fact]
    public async Task ErrorMessage_ContainsRequestUrl()
    {
        // 文案里带上实际请求的地址，地址写错时能一眼看出打到哪儿去了
        var act = async () => await RemoteApiResponse.ReadAndValidateAsync(
            Response(HttpStatusCode.NotFound, null, null));

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Which.Message.Should().Contain("http://host:5001/api/Token/Need");
    }
}
