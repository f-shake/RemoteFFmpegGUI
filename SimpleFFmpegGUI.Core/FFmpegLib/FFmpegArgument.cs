namespace SimpleFFmpegGUI.FFmpegLib
{
    public class FFmpegArgumentItem
    {
        public FFmpegArgumentItem(string key)
        {
            Key = key;
        }

        public FFmpegArgumentItem(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public FFmpegArgumentItem(string key, string value, string parent, char separator) : this(key, value)
        {
            Parent = parent;
            Separator = separator;
        }

        /// <summary>
        /// 参数的名称，即“-”后面的内容
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 参数的值
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 如果该参数为某一参数的子参数，则该属性为父参数的Key
        /// </summary>
        public string Parent { get; set; }

        /// <summary>
        /// 如果该参数为某一参数的子参数，则该属性为划分该父参数下子参数的分隔符
        /// </summary>
        public char Separator { get; }

        /// <summary>
        /// 用于串联多个参数
        /// </summary>
        public FFmpegArgumentItem Other { get; set; }
    }

}
