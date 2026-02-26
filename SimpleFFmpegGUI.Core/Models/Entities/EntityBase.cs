using System.ComponentModel.DataAnnotations;

namespace SimpleFFmpegGUI.Models.Entities
{
    public abstract class EntityBase : IEntity
    {
        [Key]
        public int Id { get; set; }

        public bool IsDeleted { get; set; }
    }
}