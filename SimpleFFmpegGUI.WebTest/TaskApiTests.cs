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
    public async Task TestTasksCurdAsync()
    {
        var tasks = await GetObjectFromJsonAsync<PagedListDto<TaskInfo>>("List");
        tasks.List.Count.Should().Be(0);
        int count = 15;
        var ids = await PostObjectFromJsonAsync<List<int>>("Add/Code", GetCodeTask(15));
        ids.Count.Should().Be(15);
        tasks = await GetObjectFromJsonAsync<PagedListDto<TaskInfo>>("List?page=1&pageSize=100");
        tasks.List.Count.Should().Be(15);
        tasks = await GetObjectFromJsonAsync<PagedListDto<TaskInfo>>("List?page=1&pageSize=5");
        tasks.List.Count.Should().Be(5);
        tasks = await GetObjectFromJsonAsync<PagedListDto<TaskInfo>>("List?page=2&pageSize=9");
        tasks.List.Count.Should().Be(6);
        tasks = await GetObjectFromJsonAsync<PagedListDto<TaskInfo>>("List?page=1&pageSize=100&status=1");
        tasks.List.Count.Should().Be(15);
        tasks = await GetObjectFromJsonAsync<PagedListDto<TaskInfo>>("List?page=1&pageSize=100&status=2");
        tasks.List.Count.Should().Be(0);
    }

    private TaskDto GetCodeTask(int count)
    {
        var inputs = new List<InputArguments>();
        for (int i = 0; i < count; i++)
        {
            inputs.Add(new InputArguments
            {
                FilePath = config.GetValue<string>(AppTestSettingsKeys.TestVideoKey),
            });
        }

        return new TaskDto
        {
            Inputs = inputs,
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
        };
    }

    [Fact]
    public async Task TestTasksAsync()
    {
        var ids = await PostObjectFromJsonAsync<List<int>>("Add/Code", GetCodeTask(1));
        ids.Count.Should().Be(1);
        var id = ids[0];
        var task = await GetObjectFromJsonAsync<TaskInfo>($"Detail/{id}");
        task.Id.Should().Be(id);
        task.Status.Should().Be(Model.TaskStatus.Queue);
    }
}