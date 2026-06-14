using SimpleFFmpegGUI.WPF.ViewModels;
using SimpleFFmpegGUI.WPF.ViewModels;

namespace SimpleFFmpegGUI.WPF.Messages
{
    public class ShowCodeArgumentsMessage(TaskInfoViewModel task)
    {
        public TaskInfoViewModel Task { get; } = task;
    }
}
