namespace SimpleFFmpegGUI.Model
{
    public enum StreamChannel
    {
        Video = 0x01,
        Audio = 0x02,
        Subtitle = 0x04,
        All = Video | Audio | Subtitle,
    }
}