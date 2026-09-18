namespace SimpleFFmpegGUI.WPF.Messages
{
    /// <summary>
    /// 请求切换窗口的“忙碌”状态（加载环）。<paramref name="message"/> 描述这次操作在做什么，
    /// 会显示在加载环卡片里；收掉忙碌（<c>true</c>）时不需要给。
    /// </summary>
    public class WindowEnableMessage(bool isEnabled, string message = null)
    {
        public bool IsEnabled { get; } = isEnabled;

        /// <summary>加载环下方的说明文字（各调用方在发 false 时给出；为空则只显示“请稍等”）</summary>
        public string Message { get; } = message;
    }
}
