using Serilog;
using System;

namespace SimpleFFmpegGUI.WPF
{
    /// <summary>
    /// 保持 log4net 风格的日志调用 API（Info/Warn/Error），内部转发到 Serilog，
    /// 使现有 App.AppLog.* 调用点无需逐一改造。
    /// </summary>
    public sealed class WpfLogger
    {
        private readonly ILogger logger;

        public WpfLogger(ILogger logger) => this.logger = logger;

        public void Info(string message) => logger.Information(message);
        public void Warn(string message) => logger.Warning(message);
        public void Error(string message) => logger.Error(message);
        public void Error(string message, Exception exception) => logger.Error(exception, message);

        public void Error(Exception exception) => logger.Error(exception, exception?.Message ?? "未知异常");
    }
}
