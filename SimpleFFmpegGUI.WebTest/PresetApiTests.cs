using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.WebAPI;

// 建议安装这个包，断言更丝滑

namespace SimpleFFmpegGUI.WebTest;

public class PresetApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestPresetsAsync()
    {
        //测试新增
        var id = await AddPresetAsync(new AddPresetRequest("test", new OutputArguments(), TaskType.Code));
        id.Should().BeGreaterThan(0);
        var presets = await GetPresetsAsync(TaskType.Code);
        presets.Count.Should().Be(1);
        presets.Should().Contain(p => p.Id == id);

        //测试更新
        await UpdatePresetAsync(id,
            new UpdatePresetRequest("test2", new OutputArguments() { DisableVideo = true }, TaskType.Code));
        presets = await GetPresetsAsync(TaskType.Code);
        presets.Should().Contain(p => p.Id == id && p.Name == "test2");
        presets.First(p => p.Id == id).Arguments.DisableVideo.Should().BeTrue();
        
        //测试删除
        await DeletePresetAsync(id);
        presets = await GetPresetsAsync(TaskType.Code);
        presets.Count.Should().Be(0);
    }

    private Task<int> AddPresetAsync(AddPresetRequest request) =>
        PostObjectFromJsonAsync<int>("/Preset/Add", request);

    private Task UpdatePresetAsync(int id, UpdatePresetRequest request) =>
        PutAsync($"/Preset/{id}", request);

    private Task DeletePresetAsync(int id) => DeleteAsync($"/Preset/{id}");

    private Task<List<CodePreset>> GetPresetsAsync(TaskType? type) =>
        GetObjectFromJsonAsync<List<CodePreset>>(type == null ? "/Preset/List" : $"/Preset/List?type={type}");
}