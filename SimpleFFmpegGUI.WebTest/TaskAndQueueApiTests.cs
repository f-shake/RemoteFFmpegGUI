using System.Diagnostics;
using FluentAssertions;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Model;
using TaskStatus = SimpleFFmpegGUI.Model.TaskStatus;


namespace SimpleFFmpegGUI.WebTest;

public class TaskAndQueueApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestQueueScheduleAsync()
    {
        //新增一个任务
        var ids = await AddCodeTaskAsync(1);
        var id = ids[0];
        var task = await GetTaskAsync(id);
        task.Status.Should().Be(TaskStatus.Queue);

        //测试计划和取消计划
        var time = DateTime.Now.AddSeconds(100);
        await ScheduleAsync(time);
        var scheduleTime = await GetScheduleTimeAsync();
        scheduleTime.Should().Be(time);
        await CancelScheduleAsync();
        scheduleTime = await GetScheduleTimeAsync();
        scheduleTime.Should().BeNull();

        //测试计划执行
        time = DateTime.Now.AddSeconds(5);
        await ScheduleAsync(time);
        await Task.Delay(TimeSpan.FromSeconds(8));
        task = await GetTaskAsync(id);
        task.Status.Should().Be(TaskStatus.Processing);
        var status = await GetStatusAsync();
        status.IsProcessing.Should().BeTrue();

        //取消任务
        await CancelQueueAsync();
        await Task.Delay(TimeSpan.FromSeconds(2));
        task = await GetTaskAsync(id);
        task.Status.Should().Be(TaskStatus.Cancel);
        status = await GetStatusAsync();
        status.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public async Task TestQueueStartAndCancelAsync()
    {
        //创建任务
        var ids = await AddCodeTaskAsync(1);
        var id = ids[0];
        var task = await GetTaskAsync(id);
        task.Status.Should().Be(TaskStatus.Queue);

        //开始队列
        await StartQueueAsync();
        await Task.Delay(TimeSpan.FromSeconds(1));
        task = await GetTaskAsync(id);
        task.Status.Should().Be(TaskStatus.Processing);
        var ffmpegCount = GetProcessCount();
        ffmpegCount.Should().BeGreaterThan(0);

        //取消队列
        await CancelQueueAsync();
        await Task.Delay(TimeSpan.FromSeconds(1));
        task = await GetTaskAsync(id);
        task.Status.Should().Be(TaskStatus.Cancel);
        GetProcessCount().Should().Be(ffmpegCount - 1);
    }

    [Fact]
    public async Task TestTasksCurdAsync()
    {
        var tasks = await GetTasksAsync();
        tasks.List.Count.Should().Be(0);
        int count = 15;
        var inputArguments = GetCodeTask(15);
        inputArguments.Inputs[0].FilePath =
            Path.GetRelativePath(appSettings.InputDir, inputArguments.Inputs[0].FilePath);
        //测试Add
        var ids = await AddCodeTaskAsync(inputArguments);
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
        await DeleteTaskAsync(ids[0]);
        tasks = await GetTasksAsync();
        tasks.List.Count.Should().Be(14);
        await DeleteTaskAsync(ids[1..4]);
        tasks = await GetTasksAsync();
        tasks.List.Count.Should().Be(11);
    }

    private Task<List<int>> AddCodeTaskAsync(TaskDto task) =>
        PostObjectFromJsonAsync<List<int>>("/Task/Add/Code", task);

    private Task<List<int>> AddCodeTaskAsync(int count) => AddCodeTaskAsync(GetCodeTask(count));

    private Task CancelQueueAsync() => PostAsync("/Queue/Cancel");

    private Task CancelScheduleAsync() => PostAsync("/Queue/CancelSchedule");

    private Task DeleteTaskAsync(int id) => DeleteAsync($"/Task/{id}");

    private Task DeleteTaskAsync(ICollection<int> ids) => PostAsync("/Task/Delete", ids);

    private TaskDto GetCodeTask(int count)
    {
        var inputs = new List<InputArguments>();
        for (int i = 0; i < count; i++)
        {
            inputs.Add(new InputArguments
            {
                FilePath =appTestSettings.TestVideo10s
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

    private int GetProcessCount() => Process.GetProcesses().Count(p => p.ProcessName.Split('.')[0] == "ffmpeg");

    private Task<DateTime?> GetScheduleTimeAsync() => GetObjectFromJsonAsync<DateTime?>("/Queue/QueueScheduleTime");

    private Task<StatusDto> GetStatusAsync() => GetObjectFromJsonAsync<StatusDto>("/Queue/Status");

    private Task<TaskInfo> GetTaskAsync(int id) => GetObjectFromJsonAsync<TaskInfo>($"/Task/Detail/{id}");

    private async Task<PagedListResponse<TaskInfo>> GetTasksAsync(int page = 1, int pageSize = 1000,
        TaskStatus? status = null)
    {
        var statusStr = status != null ? $"&status={(int)status}" : "";
        return await GetObjectFromJsonAsync<PagedListResponse<TaskInfo>>(
            $"/Task/List?page={page}&pageSize={pageSize}{statusStr}");
    }

    private Task ScheduleAsync(DateTime time) => PostAsync("/Queue/Schedule", new ScheduleRequest { Time = time });

    private Task StartQueueAsync() => PostAsync("/Queue/Start");
}