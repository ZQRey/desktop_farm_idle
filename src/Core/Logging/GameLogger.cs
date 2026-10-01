using System;
using System.IO;

namespace DesktopFarmIdle.Core.Logging;

public class GameLogger : ILogger
{
    private static GameLogger? _instance;
    public static GameLogger Instance => _instance ??= new GameLogger();

    private readonly object _lock = new();
    private readonly string _logFilePath;
    public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

    public GameLogger()
    {
        string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        try
        {
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            _logFilePath = Path.Combine(logDir, $"farm_{DateTime.Now:yyyyMMdd}.log");
        }
        catch
        {
            _logFilePath = "farm.log";
        }
    }

    public void Debug(string message, string? category = null) => Log(LogLevel.Debug, message, category);
    public void Info(string message, string? category = null) => Log(LogLevel.Info, message, category);
    public void Warning(string message, string? category = null) => Log(LogLevel.Warning, message, category);
    public void Error(string message, Exception? exception = null, string? category = null)
    {
        string fullMessage = exception != null ? $"{message}\nException: {exception}" : message;
        Log(LogLevel.Error, fullMessage, category);
    }

    private void Log(LogLevel level, string message, string? category)
    {
        if (level < MinimumLevel)
            return;

        string catStr = string.IsNullOrEmpty(category) ? "General" : category;
        string formatted = $"[{DateTime.Now:HH:mm:ss.fff}] [{level.ToString().ToUpperInvariant()}] [{catStr}] {message}";

        lock (_lock)
        {
            Console.WriteLine(formatted);
            try
            {
                File.AppendAllText(_logFilePath, formatted + Environment.NewLine);
            }
            catch
            {
                // Silently ignore log write errors
            }
        }
    }
}
