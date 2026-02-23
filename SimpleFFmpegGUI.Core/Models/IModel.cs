namespace SimpleFFmpegGUI.Model
{
    public interface IModel
    {
        int Id { get; set; }

        bool IsDeleted { get; set; }
    }
}