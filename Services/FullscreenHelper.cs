using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Mawaqit.Services;

public static class FullscreenHelper
{
    private enum QUERY_USER_NOTIFICATION_STATE
    {
        QUNS_NOT_PRESENT = 1,
        QUNS_BUSY = 2,
        QUNS_RUNNING_D3D_FULL_SCREEN = 3,
        QUNS_PRESENTATION_MODE = 4,
        QUNS_ACCEPTS_NOTIFICATIONS = 5,
        QUNS_QUIET_TIME = 6,
        QUNS_APP = 7
    }

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out QUERY_USER_NOTIFICATION_STATE pquns);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>
    /// Checks whether an exclusive Direct3D game, full-screen presentation,
    /// or full-screen application window is currently active.
    /// </summary>
    public static bool IsFullscreenRunning()
    {
        // 1. Direct3D Fullscreen / Presentation mode check via Windows Shell API
        try
        {
            if (SHQueryUserNotificationState(out var state) == 0)
            {
                if (state == QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN ||
                    state == QUERY_USER_NOTIFICATION_STATE.QUNS_PRESENTATION_MODE ||
                    state == QUERY_USER_NOTIFICATION_STATE.QUNS_BUSY)
                {
                    return true;
                }
            }
        }
        catch { }

        // 2. Foreground Window Fullscreen bounds check
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero || hwnd == GetDesktopWindow() || hwnd == GetShellWindow())
            {
                return false;
            }

            var sb = new StringBuilder(256);
            GetClassName(hwnd, sb, sb.Capacity);
            string cls = sb.ToString();

            // Ignore Windows shell and desktop windows
            if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd")
            {
                return false;
            }

            if (GetWindowRect(hwnd, out RECT rect))
            {
                foreach (Screen screen in Screen.AllScreens)
                {
                    var b = screen.Bounds;
                    // If the active foreground window covers or exceeds the entire monitor
                    if (rect.Left <= b.Left && rect.Top <= b.Top &&
                        rect.Right >= b.Right && rect.Bottom >= b.Bottom)
                    {
                        return true;
                    }
                }
            }
        }
        catch { }

        return false;
    }
}

