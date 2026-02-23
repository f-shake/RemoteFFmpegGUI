namespace SimpleFFmpegGUI.Model
{
    public class MediaInfoItem
    {
        public string Name { get; set; } 
        public object Value { get; set; } 
        public MediaInfoItem()
        {

        }
        public MediaInfoItem(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}