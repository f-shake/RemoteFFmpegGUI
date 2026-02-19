using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.WebAPI;
using SimpleFFmpegGUI.WebAPI.Dto;
using TaskStatus = SimpleFFmpegGUI.Model.TaskStatus;

// 建议安装这个包，断言更丝滑

namespace SimpleFFmpegGUI.WebTest;

public class TaskApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    protected override string ControllerName => "Task";


    [Fact]
    public async Task TestTasksCurdAsync()
    {
        async Task<PagedListDto<TaskInfo>> GetTasksAsync(int page = 1, int pageSize = 1000, TaskStatus? status = null)
        {
            var statusStr = status != null ? $"&status={(int)status}" : "";
            return await GetObjectFromJsonAsync<PagedListDto<TaskInfo>>(
                $"List?page={page}&pageSize={pageSize}{statusStr}");
        }

        var tasks = await GetTasksAsync();
        tasks.List.Count.Should().Be(0);
        int count = 15;
        var inputArguments = GetCodeTask(15);
        inputArguments.Inputs[0].FilePath =
            Path.GetRelativePath(config.GetValue<string>(AppSettingsKeys.InputDirKey),
                inputArguments.Inputs[0].FilePath);
        //测试Add
        var ids = await PostObjectFromJsonAsync<List<int>>("Add/Code", inputArguments);
        ids.Count.Should().Be(15);
        tasks = await GetTasksAsync();
        tasks.List.Count.Should().Be(15);
        //检查是否可以自动识别相对和绝对路径
        tasks.List[0].Inputs[0].FilePath.Should().Be(inputArguments.Inputs[1].FilePath);
        for (int i = 1; i < count; i++)
        {
            tasks.List[i].Inputs[0].FilePath.Should().Be(tasks.List[i - 1].Inputs[0].FilePath);
        }

        tasks = await GetTasksAsync(1, 5, null);
        tasks.List.Count.Should().Be(5);
        tasks = await GetTasksAsync(2, 9, null);
        tasks.List.Count.Should().Be(6);
        tasks = await GetTasksAsync(1, 100, TaskStatus.Queue);
        tasks.List.Count.Should().Be(15);
        tasks = await GetTasksAsync(1, 100, TaskStatus.Processing);
        tasks.List.Count.Should().Be(0);

        //测试删除
        await DeleteAsync($"{ids[0]}");
        tasks = await GetTasksAsync();
        tasks.List.Count.Should().Be(14);
        await PostAsync("Delete", ids[1..4]);
        tasks = await GetTasksAsync();
        tasks.List.Count.Should().Be(11);
    }

    [Fact]
    public async Task TestTaskProcessAsync()
    {
        var ids = await PostObjectFromJsonAsync<List<int>>("Add/Code", GetCodeTask(1));
        var id = ids[0];
        var task = await GetTaskAsync(id);
        task.Status.Should().Be(TaskStatus.Queue);
        await PostAsync("/Queue/Start");
        await Task.Delay(TimeSpan.FromSeconds(1));
        task = await GetTaskAsync(id);
        task.Status.Should().Be(TaskStatus.Processing);
        await PostAsync("/Queue/Cancel");
        await Task.Delay(TimeSpan.FromSeconds(1));
        task = await GetTaskAsync(id);
        task.Status.Should().Be(TaskStatus.Cancel);
    }

    private Task<TaskInfo> GetTaskAsync(int id)
    {
        return GetObjectFromJsonAsync<TaskInfo>($"Detail/{id}");
    }

    private TaskDto GetCodeTask(int count)
    {
        var inputs = new List<InputArguments>();
        for (int i = 0; i < count; i++)
        {
            inputs.Add(new InputArguments
            {
                FilePath = config.GetValue<string>(AppTestSettingsKeys.TestVideo10sKey),
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
}