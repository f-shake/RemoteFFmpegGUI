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
        var exportResponse = await GetAsync("/Preset/Export");
        var jsonBytes = await exportResponse.Content.ReadAsByteArrayAsync();
        jsonBytes.Length.Should().BeGreaterThan(0);
        exportResponse.Content.Headers.ContentDisposition.Should().NotBeNull();
        exportResponse.Content.Headers.ContentDisposition.FileName.Should().Contain("presetsService.json");

        // 删除现有预设，保证导入是从空状态开始
        await DeletePresetAsync(id);
        var presets = await GetPresetsAsync(null);
        presets.Count.Should().Be(0);

        // 导入预设
        var importContent = new ByteArrayContent(jsonBytes);
        importContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var form = new MultipartFormDataContent
        {
            { importContent, "file", "presetsService.json" }
        };
        await PostMultipartAsync("/Preset/Import", form);

        // 验证导入成功
        presets = await GetPresetsAsync(null);
        presets.Count.Should().Be(1);
        presets[0].Name.Should().Be("export_test");
    }

    private Task<int> AddPresetAsync(AddPresetRequest request) =>
        PostObjectFromJsonAsync<int>("/Preset", request);

    private Task UpdatePresetAsync(int id, UpdatePresetRequest request) =>
        PostAsync($"/Preset/{id}", request);

    private Task DeletePresetAsync(int id) => PostAsync($"/Preset/{id}/Delete");

    private Task<List<PresetEntity>> GetPresetsAsync(TaskType? type) =>
        GetObjectFromJsonAsync<List<PresetEntity>>(type == null ? "/Preset" : $"/Preset?type={type}");
}