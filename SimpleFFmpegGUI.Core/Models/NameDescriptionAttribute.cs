using System;

namespace SimpleFFmpegGUI.Model
{
    [AttributeUsage(AttributeTargets.All)]
    public class NameDescriptionAttribute : Attribute
    {
        public NameDescriptionAttribute()
        {
        }

        public NameDescriptionAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; set; }
        public string Description { get; set; }
    }
}