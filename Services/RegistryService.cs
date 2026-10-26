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

    public static void SyncStartup(bool enable)
    {
        try
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(RunSubKey, true);
            if (runKey != null)
            {
                if (enable)
                {
                    runKey.SetValue(ValueName, GetStartupCommand(), RegistryValueKind.String);

                    try
                    {
                        using var approvedKey = Registry.CurrentUser.OpenSubKey(StartupApprovedSubKey, true);
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
