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
        
    }

    private Task<int> AddPresetAsync(CodePresetDto request) =>
        PostObjectFromJsonAsync<int>("/Preset/Add", request);

    private Task DeletePresetAsync(int id) => DeleteAsync($"/Preset/{id}");

    private Task<List<CodePreset>> GetPresetsAsync(TaskType? type) =>
        GetObjectFromJsonAsync<List<CodePreset>>(type == null ? "/Preset/List" : $"/Preset/List?type={type}");
}