using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Mawaqit.Services;

public static class DwmHelper
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    private const int DWMWCP_ROUND = 2;
    private const int DWMSBT_MAINWINDOW = 2;
    private const int DWMSBT_TRANSIENTWINDOW = 3;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const uint WM_SETICON = 0x0080;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;

    public static void EnableWin11Styling(Window window, bool isDark, bool isTransient = false)
    {
        try
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hwnd = helper.EnsureHandle();
            if (hwnd == IntPtr.Zero) return;

            int darkMode = isDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            int corner = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));

            int backdrop = isTransient ? DWMSBT_TRANSIENTWINDOW : DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
        }
        catch { }
    }

    public static void SetWindowIcon(Window window, System.Drawing.Icon? icon)
    {
        if (icon == null) return;
        try
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hwnd = helper.EnsureHandle();
            if (hwnd == IntPtr.Zero) return;

            using var smallIco = new System.Drawing.Icon(icon, 16, 16);
            using var bigIco = new System.Drawing.Icon(icon, 32, 32);

            SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, smallIco.Handle);
            SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, bigIco.Handle);
        }
        catch { }
    }
}
