namespace SimpleFFmpegGUI.Models.Entities
{
    public interface IEntity
    {
        int Id { get; set; }

        bool IsDeleted { get; set; }
    }
}