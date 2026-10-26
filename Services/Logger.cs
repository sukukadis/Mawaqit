using System;
using System.IO;
using Mawaqit.Models;

namespace Mawaqit.Services;

public static class Logger
{
    private static readonly object _lock = new();

    public static void LogStartup(string message)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(AppConfig.AppDataFolder);
                using var sw = File.AppendText(AppConfig.StartupLogPath);
                sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            }
        }
        catch { }
    }

    public static void LogError(string message)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(AppConfig.AppDataFolder);
                using var sw = File.AppendText(AppConfig.ErrorLogPath);
                sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            }
        }
        catch { }
    }
}
