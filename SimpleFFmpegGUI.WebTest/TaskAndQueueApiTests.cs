using System.Diagnostics;
using FluentAssertions;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;
using TaskStatus = SimpleFFmpegGUI.Enums.TaskStatus;


namespace SimpleFFmpegGUI.WebTest;

public class TaskAndQueueApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestQueueScheduleAsync()
    {
        //新增一个任务
        var ids = await AddCodecTaskAsync(1);
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
        var ids = await AddCodecTaskAsync(1);
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
    public async Task TestTaskCancelAndResetAsync()
    {
        // 创建两个任务
        var ids = await AddCodecTaskAsync(2);
        ids.Count.Should().Be(2);

        // 单体取消
        await CancelTaskAsync(ids[0]);
        var task0 = await GetTaskAsync(ids[0]);
        task0.Status.Should().Be(TaskStatus.Cancel);

        // 取消后不能再次取消（应返回错误但接口设计上仍可调用）
        // 测试 Batch/Cancel
        await CancelTasksAsync(new[] { ids[1] });
        var task1 = await GetTaskAsync(ids[1]);
        task1.Status.Should().Be(TaskStatus.Cancel);

        // 批量重置
        await ResetTasksAsync(new[] { ids[0], ids[1] });
        task0 = await GetTaskAsync(ids[0]);
        task0.Status.Should().Be(TaskStatus.Queue);
        task1 = await GetTaskAsync(ids[1]);
        task1.Status.Should().Be(TaskStatus.Queue);

        // 单体取消和重置换个顺序再测一次
        await CancelTaskAsync(ids[0]);
        task0 = await GetTaskAsync(ids[0]);
        task0.Status.Should().Be(TaskStatus.Cancel);

        await ResetTaskAsync(ids[0]);
        task0 = await GetTaskAsync(ids[0]);
        task0.Status.Should().Be(TaskStatus.Queue);
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
        var ids = await AddCodecTaskAsync(inputArguments);
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

    private Task<List<int>> AddCodecTaskAsync(TaskDto task) =>
        PostObjectFromJsonAsync<List<int>>("/Task/Transcode", task);

    private Task<List<int>> AddCodecTaskAsync(int count) => AddCodecTaskAsync(GetCodeTask(count));

    private Task CancelQueueAsync() => PostAsync("/Queue/Cancel");

    private Task CancelTaskAsync(int id) => PostAsync($"/Task/{id}/Cancel");

    private Task CancelTasksAsync(ICollection<int> ids) => PostAsync("/Task/Batch/Cancel", ids);

    private Task ResetTaskAsync(int id) => PostAsync($"/Task/{id}/Reset");

    private Task ResetTasksAsync(IEnumerable<int> ids) => PostAsync("/Task/Batch/Reset", ids);

    private Task CancelScheduleAsync() => PostAsync("/Queue/CancelSchedule");

    private Task DeleteTaskAsync(int id) => PostAsync($"/Task/{id}/Delete");

    private Task DeleteTaskAsync(ICollection<int> ids) => PostAsync("/Task/Batch/Delete", ids);

    private TaskDto GetCodeTask(int count)
    {
        var inputs = new List<InputParameters>();
        for (int i = 0; i < count; i++)
        {
            inputs.Add(new InputParameters
            {
                FilePath =appTestSettings.TestVideo10s
            });
        }

        return new TaskDto
        {
            Inputs = inputs,
            Output = "code_test_output.mp4",
            Parameter = new OutputParameters
            {
                Video = new VideoCodecParameters
                {
                    Codec = "H264",
                    AverageBitrate = 10d,
                    MaxBitrate = 20d,
                },
                Audio = new AudioCodecParameters
                {
                    Codec = "AAC",
                    Bitrate = 128,
                },
            },
        };
    }

    private int GetProcessCount() => Process.GetProcesses().Count(p => p.ProcessName.Split('.')[0] == "ffmpeg");

    private Task<DateTime?> GetScheduleTimeAsync() => GetObjectFromJsonAsync<DateTime?>("/Queue/Schedule");

    private Task<StatusDto> GetStatusAsync() => GetObjectFromJsonAsync<StatusDto>("/Queue");

    private Task<TaskEntity> GetTaskAsync(int id) => GetObjectFromJsonAsync<TaskEntity>($"/Task/{id}");

    private async Task<PagedListResponse<TaskEntity>> GetTasksAsync(int page = 1, int pageSize = 1000,
        TaskStatus? status = null)
    {
        var statusStr = status != null ? $"&status={(int)status}" : "";
        return await GetObjectFromJsonAsync<PagedListResponse<TaskEntity>>(
            $"/Task?page={page}&pageSize={pageSize}{statusStr}");
    }

    private Task ScheduleAsync(DateTime time) => PostAsync("/Queue/Schedule", new ScheduleRequest { Time = time });

    private Task StartQueueAsync() => PostAsync("/Queue/Start");
}