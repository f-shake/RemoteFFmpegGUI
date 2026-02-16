using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.WebAPI;
using SimpleFFmpegGUI.WebAPI.Dto;

// 建议安装这个包，断言更丝滑

namespace SimpleFFmpegGUI.WebTest;

public class TaskApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    protected override string ControllerName => "Task";

    [Fact]
    public async Task TestAddTasksAsync()
    {
        var ids = await PostObjectFromJsonAsync<List<int>>("Add/Code", new TaskDto
        {
            Inputs =
            [
                new InputArguments
                {
                    FilePath = config.GetValue<string>(AppTestSettingsKeys.TestVideo10sKey),
                }
            ],
            Output = "code_test_output.mp4",
            Argument = new OutputArguments
            {
                Video = new VideoCodeArguments
                {
                    Code = "H264",
                    AverageBitrate = 10d,
                    MaxBitrate = 20d,
                },
                Audio = new AudioCodeArguments
                {
                    Code = "AAC",
                    Bitrate = 128,
                },
            },
        });
        ids.Count.Should().Be(1);
        var id = ids[0];
        var task = await GetObjectFromJsonAsync<TaskInfo>($"Detail/{id}");
        task.Id.Should().Be(id);
        task.Status.Should().Be(Model.TaskStatus.Queue);
    }
}