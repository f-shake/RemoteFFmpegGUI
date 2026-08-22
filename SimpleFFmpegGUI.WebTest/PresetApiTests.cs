using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.WebAPI;

// 建议安装这个包，断言更丝滑

namespace SimpleFFmpegGUI.WebTest;

public class PresetApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestPresetsAsync()
    {
        //测试新增
        var id = await AddPresetAsync(new AddPresetRequest("test", new OutputParameters(), TaskType.Transcode));
        id.Should().BeGreaterThan(0);
        var presets = await GetPresetsAsync(TaskType.Transcode);
        presets.Count.Should().Be(1);
        presets.Should().Contain(p => p.Id == id);

        //测试更新
        var op = new OutputParameters();
        op.Video.Strategy = StreamStrategy.Disable;
        await UpdatePresetAsync(id,
            new UpdatePresetRequest("test2", op, TaskType.Transcode));
        presets = await GetPresetsAsync(TaskType.Transcode);
        presets.Should().Contain(p => p.Id == id && p.Name == "test2");
        presets.First(p => p.Id == id).Parameters.Video.Strategy.Should().Be(StreamStrategy.Disable);

        //测试删除
        await DeletePresetAsync(id);
        presets = await GetPresetsAsync(TaskType.Transcode);
        presets.Count.Should().Be(0);
    }

    [Fact]
    public async Task TestExportImportAsync()
    {
        // 先创建一个预设
        var id = await AddPresetAsync(new AddPresetRequest("export_test", new OutputParameters(), TaskType.Transcode));
        id.Should().BeGreaterThan(0);

        // 导出预设
        var exportResponse = await GetAsync("/api/Preset/Export");
        var jsonBytes = await exportResponse.Content.ReadAsByteArrayAsync();
        jsonBytes.Length.Should().BeGreaterThan(0);
        exportResponse.Content.Headers.ContentDisposition.Should().NotBeNull();
        exportResponse.Content.Headers.ContentDisposition.FileName.Should().Contain("presets.json");

        // 删除现有预设，保证导入是从空状态开始
        await DeletePresetAsync(id);
        var presets = await GetPresetsAsync(null);
        presets.Count.Should().Be(0);

        // 导入预设
        var importContent = new ByteArrayContent(jsonBytes);
        importContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var form = new MultipartFormDataContent
        {
            { importContent, "file", "presets.json" }
        };
        await PostMultipartAsync("/api/Preset/Import", form);

        // 验证导入成功
        presets = await GetPresetsAsync(null);
        presets.Count.Should().Be(1);
        presets[0].Name.Should().Be("export_test");
    }

    /// <summary>
    /// 清空预设接口
    /// </summary>
    [Fact]
    public async Task TestClearPresetsAsync()
    {
        await AddPresetAsync(new AddPresetRequest("clear_test", new OutputParameters(), TaskType.Transcode));
        var presets = await GetPresetsAsync(null);
        presets.Count.Should().BeGreaterThan(0);

        await PostAsync("/api/Preset/Clear");
        presets = await GetPresetsAsync(null);
        presets.Count.Should().Be(0);
    }

    /// <summary>
    /// v1 导出的 Custom 预设（Type=3）导入后应转换为 v2 的 Custom(99)（P1-9 回归）
    /// </summary>
    [Fact]
    public async Task TestImportV1CustomPresetAsync()
    {
        const string v1Json = """[{"Name":"v1_custom","Type":3,"Default":false,"Arguments":{}}]""";
        var form = new MultipartFormDataContent
        {
            { new StringContent(v1Json, Encoding.UTF8, "application/json"), "file", "presets.json" }
        };
        await PostMultipartAsync("/api/Preset/Import", form);

        var presets = await GetPresetsAsync(null);
        presets.Should().Contain(p => p.Name == "v1_custom" && p.Type == TaskType.Custom);
    }

    /// <summary>
    /// 新增预设：空 body / 空参数 / 空名称 应被拒绝
    /// </summary>
    [Fact]
    public async Task TestAddValidationAsync()
    {
        var act = async () => await PostAsync("/api/Preset");
        await act.Should().ThrowAsync<Exception>();

        act = async () => await PostAsync("/api/Preset", new AddPresetRequest("t", null, TaskType.Transcode));
        await act.Should().ThrowAsync<Exception>();

        act = async () => await PostAsync("/api/Preset", new AddPresetRequest(null, new OutputParameters(), TaskType.Transcode));
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 更新不存在的预设应返回 404；删除不存在的预设应幂等成功（控制器忽略 service 的 NotFound，恒 204）
    /// </summary>
    [Fact]
    public async Task TestUpdateDeleteNonExistentAsync()
    {
        var act = async () =>
            await UpdatePresetAsync(999999, new UpdatePresetRequest("t", new OutputParameters(), TaskType.Transcode));
        await act.Should().ThrowAsync<Exception>();

        // Delete 恒返回 NoContent（204），对不存在的 id 也不抛异常
        var response = await PostAsync("/api/Preset/999999/Delete");
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    /// <summary>
    /// 导入非法 JSON 应报错
    /// </summary>
    [Fact]
    public async Task TestImportInvalidJsonAsync()
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent("not-a-json", Encoding.UTF8, "application/json"), "file", "presets.json" }
        };
        var act = async () => await PostMultipartAsync("/api/Preset/Import", form);
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 按 TaskType 过滤预设
    /// </summary>
    [Fact]
    public async Task TestGetByTypeFilterAsync()
    {
        await AddPresetAsync(new AddPresetRequest("t1", new OutputParameters(), TaskType.Transcode));
        await AddPresetAsync(new AddPresetRequest("m1", new OutputParameters(), TaskType.Mux));

        var transcode = await GetPresetsAsync(TaskType.Transcode);
        transcode.Should().Contain(p => p.Name == "t1");
        transcode.Should().NotContain(p => p.Name == "m1");
    }

    private Task<int> AddPresetAsync(AddPresetRequest request) =>
        PostObjectFromJsonAsync<int>("/api/Preset", request);

    private Task UpdatePresetAsync(int id, UpdatePresetRequest request) =>
        PostAsync($"/api/Preset/{id}", request);

    private Task DeletePresetAsync(int id) => PostAsync($"/api/Preset/{id}/Delete");

    private Task<List<PresetEntity>> GetPresetsAsync(TaskType? type) =>
        GetObjectFromJsonAsync<List<PresetEntity>>(type == null ? "/api/Preset" : $"/api/Preset?type={type}");
}