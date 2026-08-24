using System.Diagnostics;
using FluentAssertions;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.Enums;
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
        task = await WaitForStatusAsync(id, TaskStatus.Processing);
        task.Status.Should().Be(TaskStatus.Processing);
        // 任务可能很快完成，轮询确认队列处于处理中（避免瞬时竞态）
        var status = await WaitForProcessingAsync(TimeSpan.FromSeconds(5));
        status.IsProcessing.Should().BeTrue();

        //取消任务：Cancel 已幂等（队列未运行也可取消）；测试视频极小、转码可能快于 HTTP 往返，
        //等待任务到达任一终态（Cancel/Done/Error）而非固定断言 Cancel
        await CancelQueueAsync();
        var cancelSw = Stopwatch.StartNew();
        while (cancelSw.Elapsed < TimeSpan.FromSeconds(30))
        {
            task = await GetTaskAsync(id);
            if (task.Status is TaskStatus.Cancel or TaskStatus.Done or TaskStatus.Error)
            {
                break;
            }
            await Task.Delay(200);
        }
        task.Status.Should().BeOneOf(TaskStatus.Cancel, TaskStatus.Done, TaskStatus.Error);
        status = await GetStatusAsync();
        status.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public async Task TestQueueHasPendingAsync()
    {
        // 先清掉可能遗留的计划与运行中的队列，避免新加入的排队任务被自动处理导致断言不稳定
        await CancelScheduleAsync();
        var status = await GetStatusAsync();
        if (status.IsProcessing)
        {
            await CancelQueueAsync();
        }

        // 取消是异步收尾，等队列真正停止后再继续，避免新任务被自动拾取造成 HasPending 断言竞态
        var cancelSw = Stopwatch.StartNew();
        while (cancelSw.Elapsed < TimeSpan.FromSeconds(30))
        {
            status = await GetStatusAsync();
            if (!status.IsProcessing)
            {
                break;
            }
            await Task.Delay(200);
        }

        // 没有任务时：不应存在待执行任务，开始队列按钮应置灰
        (await GetObjectFromJsonAsync<bool>("/api/Queue/HasPending")).Should().BeFalse();

        // 新增一个排队任务后：应返回有待执行任务
        await AddCodecTaskAsync(1);
        (await GetObjectFromJsonAsync<bool>("/api/Queue/HasPending")).Should().BeTrue();
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
        task = await WaitForStatusAsync(id, TaskStatus.Processing);
        task.Status.Should().Be(TaskStatus.Processing);

        //取消队列：Cancel 已幂等；转码可能快于 HTTP 往返，等待任务到达任一终态
        await CancelQueueAsync();
        var cancelSw = Stopwatch.StartNew();
        while (cancelSw.Elapsed < TimeSpan.FromSeconds(30))
        {
            task = await GetTaskAsync(id);
            if (task.Status is TaskStatus.Cancel or TaskStatus.Done or TaskStatus.Error)
            {
                break;
            }
            await Task.Delay(200);
        }
        task.Status.Should().BeOneOf(TaskStatus.Cancel, TaskStatus.Done, TaskStatus.Error);
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

    /// <summary>
    /// 合并音视频（Mux）任务不应被误建为对比（QualityCheck）任务（P1-1 回归）
    /// </summary>
    [Fact]
    public async Task TestCreateMuxAndQualityCheckTasksAsync()
    {
        var inputs = new List<InputParameters>
        {
            new() { FilePath = appTestSettings.TestVideo10s },
            new() { FilePath = appTestSettings.TestVideo10s },
        };

        var muxIds = await PostObjectFromJsonAsync<List<int>>("/api/Task/Mux", new TaskDto
        {
            Inputs = inputs,
            Output = "mux_test_output.mp4",
            Parameter = new OutputParameters { Mux = new MuxParameters { Shortest = true } },
        });
        muxIds.Count.Should().Be(1);
        var muxTask = await GetTaskAsync(muxIds[0]);
        muxTask.Type.Should().Be(TaskType.Mux);
        // 「裁剪到最短媒体」参数应完整保存（P1-7 回归）
        muxTask.Parameters.Mux.Shortest.Should().BeTrue();

        var qcIds = await PostObjectFromJsonAsync<List<int>>("/api/Task/QualityCheck", new TaskDto
        {
            Inputs = inputs,
            Output = "qc_test_output.mp4",
        });
        qcIds.Count.Should().Be(1);
        var qcTask = await GetTaskAsync(qcIds[0]);
        qcTask.Type.Should().Be(TaskType.QualityCheck);
    }

    /// <summary>
    /// 拼接（Concat）与自定义（Custom）任务的创建（P4-3 缺失用例）
    /// </summary>
    [Fact]
    public async Task TestCreateConcatAndCustomTasksAsync()
    {
        var inputs = new List<InputParameters>
        {
            new() { FilePath = appTestSettings.TestVideo10s },
            new() { FilePath = appTestSettings.TestVideo10s },
        };

        var concatIds = await PostObjectFromJsonAsync<List<int>>("/api/Task/Concat", new TaskDto
        {
            Inputs = inputs,
            Output = "concat_test_output.mp4",
        });
        concatIds.Count.Should().Be(1);
        var concatTask = await GetTaskAsync(concatIds[0]);
        concatTask.Type.Should().Be(TaskType.Concat);

        var customIds = await PostObjectFromJsonAsync<List<int>>("/api/Task/Custom", new TaskDto
        {
            Inputs = new List<InputParameters>(),
            Parameter = new OutputParameters { Extra = "-threads 4" },
        });
        customIds.Count.Should().Be(1);
        var customTask = await GetTaskAsync(customIds[0]);
        customTask.Type.Should().Be(TaskType.Custom);

        // 自定义任务缺少 Extra 应被拒绝
        var act = async () => await PostObjectFromJsonAsync<List<int>>("/api/Task/Custom", new TaskDto
        {
            Inputs = new List<InputParameters>(),
        });
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 不指定输出路径时，默认输出应落在 OutputDir 下（P1-8 回归）
    /// </summary>
    [Fact]
    public async Task TestDefaultOutputPathInOutputDirAsync()
    {
        var task = GetCodeTask(1);
        task.Output = null;
        var ids = await AddCodecTaskAsync(task);
        var t = await GetTaskAsync(ids[0]);
        t.Output.Should().NotBeNullOrWhiteSpace();
        t.Output.Should().StartWith(Path.GetFullPath(appSettings.OutputDir));
        t.Output.Should().EndWith(Path.GetFileName(appTestSettings.TestVideo10s));
    }

    /// <summary>
    /// 参数预览（PreviewArguments）与容器格式列表（Formats）接口
    /// </summary>
    [Fact]
    public async Task TestPreviewArgumentsAndFormatsAsync()
    {
        // 参数预览：返回纯文本的 ffmpeg 输出参数
        var previewResponse = await PostAsync("/api/Task/PreviewArguments", new OutputParameters
        {
            Video = new VideoCodecParameters { Strategy = StreamStrategy.Copy },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Copy },
        });
        var preview = await previewResponse.Content.ReadAsStringAsync();
        preview.Should().NotBeNullOrWhiteSpace();
        preview.Should().Contain("-c:v copy");

        // 容器格式列表
        var formats = await GetObjectFromJsonAsync<VideoFormat[]>("/api/Task/Formats");
        formats.Should().NotBeEmpty();
        formats.Should().Contain(p => p.Name == "mp4");
    }

    /// <summary>
    /// 队列无任务运行时暂停/恢复应报错（边界行为）
    /// </summary>
    [Fact]
    public async Task TestQueuePauseResumeWithoutTaskAsync()
    {
        var act = async () => await PostAsync("/api/Queue/Pause");
        await act.Should().ThrowAsync<Exception>();

        act = async () => await PostAsync("/api/Queue/Resume");
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task TestTasksCurdAsync()
    {
        // 状态计数断言（Queue=15、Processing=0）的前提是队列未运行；
        // 前置队列测试失败可能残留运行中的队列（新任务会被自动处理），这里确保队列停止
        var queueStatus = await GetStatusAsync();
        if (queueStatus.IsProcessing)
        {
            await CancelQueueAsync();
            // 取消是异步收尾，等待队列完全停止
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(30))
            {
                queueStatus = await GetStatusAsync();
                if (!queueStatus.IsProcessing)
                {
                    break;
                }
                await Task.Delay(200);
            }
        }

        var tasks = await GetTasksAsync();
        tasks.List.Count.Should().Be(0);
        var inputArguments = GetCodeTask(15);
        inputArguments.Inputs[0].FilePath =
            Path.GetRelativePath(appSettings.InputDir, inputArguments.Inputs[0].FilePath);
        //测试Add
        var ids = await AddCodecTaskAsync(inputArguments);
        ids.Count.Should().Be(15);
        tasks = await GetTasksAsync();
        tasks.List.Count.Should().Be(15);
        // 相对路径应被归一化为 InputDir 下的绝对路径（P3-1）
        var normalizedPath = tasks.List[0].Inputs[0].FilePath;
        Path.IsPathFullyQualified(normalizedPath).Should().BeTrue();
        normalizedPath.Should().StartWith(Path.GetFullPath(appSettings.InputDir));

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

    /// <summary>
    /// 查询不存在任务应返回 404
    /// </summary>
    [Fact]
    public async Task TestGetTaskNonExistentAsync()
    {
        var act = async () => await GetTaskAsync(999999);
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 创建任务：空 body / 非法 type 应报错
    /// </summary>
    [Fact]
    public async Task TestAddTaskValidationAsync()
    {
        var act = async () => await PostAsync("/api/Task/Transcode");
        await act.Should().ThrowAsync<Exception>();

        act = async () => await PostAsync("/api/Task/InvalidType", new TaskDto { Inputs = new List<InputParameters>() });
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 单条取消/删除/重置不存在的任务应返回 404
    /// </summary>
    [Fact]
    public async Task TestCancelDeleteResetNonExistentAsync()
    {
        var act = async () => await CancelTaskAsync(999999);
        await act.Should().ThrowAsync<Exception>();

        act = async () => await DeleteTaskAsync(999999);
        await act.Should().ThrowAsync<Exception>();

        act = async () => await ResetTaskAsync(999999);
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 批量取消传入 null ids 应返回 400
    /// </summary>
    [Fact]
    public async Task TestBatchCancelNullAsync()
    {
        var act = async () => await CancelTasksAsync(null);
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 参数预览：空 body 应报错
    /// </summary>
    [Fact]
    public async Task TestPreviewArgumentsNullAsync()
    {
        var act = async () => await PostAsync("/api/Task/PreviewArguments");
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 计划时间早于当前应返回 400；空 body 应报错
    /// </summary>
    [Fact]
    public async Task TestScheduleValidationAsync()
    {
        var act = async () => await ScheduleAsync(DateTime.Now.AddMinutes(-5));
        await act.Should().ThrowAsync<Exception>();

        act = async () => await PostAsync("/api/Queue/Schedule");
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 队列未运行时取消应幂等成功
    /// </summary>
    [Fact]
    public async Task TestCancelQueueNotRunningAsync()
    {
        await CancelQueueAsync();
    }

    private Task<List<int>> AddCodecTaskAsync(TaskDto task) =>
        PostObjectFromJsonAsync<List<int>>("/api/Task/Transcode", task);

    private Task<List<int>> AddCodecTaskAsync(int count) => AddCodecTaskAsync(GetCodeTask(count));

    private Task CancelQueueAsync() => PostAsync("/api/Queue/Cancel");

    private Task CancelTaskAsync(int id) => PostAsync($"/api/Task/{id}/Cancel");

    private Task CancelTasksAsync(ICollection<int> ids) => PostAsync("/api/Task/Batch/Cancel", ids);

    private Task ResetTaskAsync(int id) => PostAsync($"/api/Task/{id}/Reset");

    private Task ResetTasksAsync(IEnumerable<int> ids) => PostAsync("/api/Task/Batch/Reset", ids);

    private Task CancelScheduleAsync() => PostAsync("/api/Queue/CancelSchedule");

    private Task DeleteTaskAsync(int id) => PostAsync($"/api/Task/{id}/Delete");

    private Task DeleteTaskAsync(ICollection<int> ids) => PostAsync("/api/Task/Batch/Delete", ids);

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
            // 输出文件名唯一：多个测试/多个任务共用固定文件名时，前一个任务的 ffmpeg 进程可能仍持有文件锁，
            // 导致后一个任务 "Error opening output file: Permission denied"
            Output = $"code_test_output_{Guid.NewGuid():N}.mp4",
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

    /// <summary>
    /// 轮询等待队列进入处理中状态（代替固定 sleep）
    /// </summary>
    private async Task<StatusDto> WaitForProcessingAsync(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            var status = await GetStatusAsync();
            if (status.IsProcessing)
            {
                return status;
            }
            await Task.Delay(200);
        }
        throw new TimeoutException($"等待队列进入处理中状态超时（{timeout}）");
    }

    /// <summary>
    /// 轮询等待任务到达指定状态（代替固定 sleep，P4-1）
    /// </summary>
    private async Task<TaskEntity> WaitForStatusAsync(int id, TaskStatus status, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            var task = await GetTaskAsync(id);
            if (task.Status == status)
            {
                return task;
            }
            await Task.Delay(200);
        }
        var current = await GetTaskAsync(id);
        throw new TimeoutException($"等待任务{id}状态为{status}超时（{timeout}），当前状态：{current.Status}");
    }

    private Task<DateTime?> GetScheduleTimeAsync() => GetObjectFromJsonAsync<DateTime?>("/api/Queue/Schedule");

    private Task<StatusDto> GetStatusAsync() => GetObjectFromJsonAsync<StatusDto>("/api/Queue");

    private Task<TaskEntity> GetTaskAsync(int id) => GetObjectFromJsonAsync<TaskEntity>($"/api/Task/{id}");

    private async Task<PagedListResponse<TaskEntity>> GetTasksAsync(int page = 1, int pageSize = 1000,
        TaskStatus? status = null)
    {
        var statusStr = status != null ? $"&status={(int)status}" : "";
        return await GetObjectFromJsonAsync<PagedListResponse<TaskEntity>>(
            $"/api/Task?page={page}&pageSize={pageSize}{statusStr}");
    }

    private Task ScheduleAsync(DateTime time) => PostAsync("/api/Queue/Schedule", new ScheduleRequest { Time = time });

    private Task StartQueueAsync() => PostAsync("/api/Queue/Start");
}