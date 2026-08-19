using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WPF.Views
{
    public static class ViewHelper
    {
        public static string GetTitle(Type type)
        {
            return type.Name switch
            {
                nameof(AddTaskView) => "新增任务",
                nameof(MediaInfoView) => "媒体信息",
                nameof(PresetsView) => "所有预设",
                nameof(TasksView) => "所有任务",
                nameof(LogsView) => "日志",
                nameof(SettingView) => "设置",
                nameof(FFmpegOutputView) => "FFmpeg输出命令行",
                // 新增 View 未注册时降级使用类型名，避免运行时抛异常
                _ => type.Name,
            };
        }
    }
}