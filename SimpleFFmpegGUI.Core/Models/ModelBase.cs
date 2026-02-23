using System.ComponentModel.DataAnnotations;

namespace SimpleFFmpegGUI.Model
{
    public abstract class ModelBase : IModel
    {
        [Key]
        public int Id { get; set; }

        public bool IsDeleted { get; set; }
    }
}