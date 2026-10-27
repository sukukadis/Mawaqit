using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace Mawaqit.Services;

public static class RegistryService
{
    private const string RunSubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupApprovedSubKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ValueName = "Mawaqit";

    public static string GetStartupCommand()
    {
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        }
        return $"\"{exePath}\" --startup";
    }

    public static void SyncStartup(bool enable, bool force = false)
    {
        try
        {
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            }

            // Avoid registering temporary debug build paths into Windows Run registry unless explicitly forced
            if (!force && (exePath.Contains(@"\bin\Debug") || exePath.Contains(@"\bin\Release")))
            {
                return;
            }

            using var runKey = Registry.CurrentUser.CreateSubKey(RunSubKey, true);
            if (runKey != null)
            {
                if (enable)
                {
                    string startupCmd = GetStartupCommand();
                    runKey.SetValue(ValueName, startupCmd, RegistryValueKind.String);
                    Logger.LogStartup($"Registry Run key set: {startupCmd}");

                    try
                    {
                        using var approvedKey = Registry.CurrentUser.CreateSubKey(StartupApprovedSubKey, true);
                        if (approvedKey != null)
                        {
                            byte[] enabledBytes = new byte[12] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                            approvedKey.SetValue(ValueName, enabledBytes, RegistryValueKind.Binary);
                        }
                    }
                    catch { }
                }
                else
                {
                    if (runKey.GetValue(ValueName) != null)
                    {
                        runKey.DeleteValue(ValueName, false);
                        Logger.LogStartup("Registry Run key removed");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"SyncStartup error: {ex.Message}");
        }
    }

    public static void HandleUninstall()
    {
        Logger.LogStartup("========== Mawaqit uninstall ==========");
        int currentPid = Environment.ProcessId;
        string exeName = "Mawaqit";

        try
        {
            var processes = Process.GetProcessesByName(exeName);
            foreach (var p in processes)
            {
                if (p.Id != currentPid)
                {
                    try
                    {
                        Logger.LogStartup($"uninstall: killing pid {p.Id}");
                        p.Kill();
                        p.WaitForExit(3000);
                    }
                    catch { }
                }
            }
        }
        catch { }

        Thread.Sleep(500);

        try
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(RunSubKey, true);
            runKey?.DeleteValue(ValueName, false);

            using var approvedKey = Registry.CurrentUser.OpenSubKey(StartupApprovedSubKey, true);
            approvedKey?.DeleteValue(ValueName, false);
            Logger.LogStartup("uninstall: removed registry run keys");
        }
        catch (Exception ex)
        {
            Logger.LogError($"uninstall registry removal failed: {ex.Message}");
        }

        Thread.Sleep(500);
        Logger.LogStartup("========== Mawaqit uninstall done ==========");
        Environment.Exit(0);
    }
}
