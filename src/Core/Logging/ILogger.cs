using System;

namespace DesktopFarmIdle.Core.Logging;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

public interface ILogger
{
    void Debug(string message, string? category = null);
    void Info(string message, string? category = null);
    void Warning(string message, string? category = null);
    void Error(string message, Exception? exception = null, string? category = null);
}
