using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Mawaqit.Models;
using Mawaqit.Services;

namespace Mawaqit.Controls;

public partial class TaskbarWidgetWindow : Window
{
    #region Win32 Interop

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern uint RegisterWindowMessage(string lpString);

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int WS_POPUP = unchecked((int)0x80000000);
    private const int WS_CHILD = 0x40000000;
    private const int WS_CLIPSIBLINGS = 0x04000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    #endregion

    private readonly AppConfig _config;
    private readonly Func<ThemePalette> _getTheme;
    private readonly Func<bool> _isDark;
    private readonly Action _onToggleFlyout;
    private readonly Action _onOpenSettings;
    private readonly Action _onShowApp;
    private readonly Action _onCloseApp;

    private readonly DispatcherTimer _watchdogTimer;

    private IntPtr _hwndTray = IntPtr.Zero;
    private uint _msgTaskbarCreated;
    private bool _isAttachedToTray;

    private System.Windows.Point _dragStartPos;
    private bool _isMouseDown;
    private bool _hasDragged;
    private bool _isClosing;
    private bool _isElapsedState;

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private POINT _win32DownPt;
    private bool _win32IsDown;
    private bool _win32HasDragged;

    public event Action<bool>? VisibilityChanged;

    public TaskbarWidgetWindow(
        AppConfig config,
        Func<ThemePalette> getTheme,
        Func<bool> isDark,
        Action onToggleFlyout,
        Action onOpenSettings,
        Action? onShowApp = null,
        Action? onCloseApp = null)
    {
        _config = config;
        _getTheme = getTheme;
        _isDark = isDark;
        _onToggleFlyout = onToggleFlyout;
        _onOpenSettings = onOpenSettings;
        _onShowApp = onShowApp ?? (() => { });
        _onCloseApp = onCloseApp ?? (() => Application.Current.Shutdown());

        InitializeComponent();
        System.Windows.Controls.ToolTipService.SetIsEnabled(RootBorder, false);
        RootBorder.ToolTip = null;

        ApplyLanguage(_config.Language);
        SyncSettingsToggles();

        // Periodic watchdog timer: maintains integration with the taskbar of the configured monitor
        _watchdogTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _watchdogTimer.Tick += (s, e) =>
        {
            if (_isClosing || !IsVisible || _isMouseDown || _hasDragged) return;

            var screens = System.Windows.Forms.Screen.AllScreens;
            int monitorIdx = _config.TaskbarWidgetMonitor;
            if (monitorIdx < 0 || monitorIdx >= screens.Length) monitorIdx = 0;
            var targetScreen = screens[monitorIdx];
            IntPtr expectedTray = GetTaskbarForScreen(targetScreen);

            if (expectedTray != IntPtr.Zero)
            {
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    IntPtr currentParent = GetParent(helper.Handle);
                    if (_hwndTray != expectedTray || currentParent != expectedTray || !IsWindow(_hwndTray))
                    {
                        AttachToTaskbar();
                    }
                    else if (_config.TaskbarWidgetDockMode == "tray_left")
                    {
                        RepositionToTaskbar();
                    }
                }
            }
        };
        _watchdogTimer.Start();

        Loaded += (s, e) =>
        {
            AttachToTaskbar();
            // During Windows boot / login, taskbar layout shifts as system tray icons load;
            // reposition after brief delays so the widget is always docked cleanly in place.
            Dispatcher.InvokeAsync(async () =>
            {
                await System.Threading.Tasks.Task.Delay(500);
                if (!_isClosing && IsVisible) AttachToTaskbar();
                await System.Threading.Tasks.Task.Delay(1500);
                if (!_isClosing && IsVisible) RepositionToTaskbar();
            });
        };

        SizeChanged += (s, e) =>
        {
            if (_config.TaskbarWidgetDockMode == "tray_left")
            {
                RepositionToTaskbar();
            }
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        try
        {
            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;
            if (hwnd != IntPtr.Zero)
            {
                int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

                // Register hook to handle TaskbarCreated message when explorer.exe restarts
                var source = HwndSource.FromHwnd(hwnd);
                source?.AddHook(WndProc);

                _msgTaskbarCreated = RegisterWindowMessage("TaskbarCreated");

                AttachToTaskbar();
            }
        }
        catch { }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_msgTaskbarCreated != 0 && msg == (int)_msgTaskbarCreated)
        {
            // Windows Explorer restarted -> re-inject natively into taskbar
            _hwndTray = IntPtr.Zero;
            _isAttachedToTray = false;
            AttachToTaskbar();
            handled = true;
        }
        else if (msg == WM_LBUTTONDOWN)
        {
            GetCursorPos(out _win32DownPt);
            _win32IsDown = true;
            _win32HasDragged = false;
        }
        else if (msg == 0x0200 /* WM_MOUSEMOVE */ && _win32IsDown && !_win32HasDragged)
        {
            if (GetCursorPos(out POINT curPt))
            {
                if (Math.Abs(curPt.X - _win32DownPt.X) > 8 || Math.Abs(curPt.Y - _win32DownPt.Y) > 8)
                {
                    _win32HasDragged = true;
                }
            }
        }
        else if (msg == WM_LBUTTONUP)
        {
            if (_win32IsDown && !_win32HasDragged)
            {
                _win32IsDown = false;
                _onToggleFlyout?.Invoke();
            }
            _win32IsDown = false;
        }
        return IntPtr.Zero;
    }

    public static IntPtr GetTaskbarForScreen(System.Windows.Forms.Screen screen)
    {
        if (screen.Primary)
        {
            IntPtr primaryTray = FindWindow("Shell_TrayWnd", null);
            if (primaryTray != IntPtr.Zero) return primaryTray;
        }

        // Search through secondary taskbars
        IntPtr hwndSecondary = IntPtr.Zero;
        while ((hwndSecondary = FindWindowEx(IntPtr.Zero, hwndSecondary, "Shell_SecondaryTrayWnd", null)) != IntPtr.Zero)
        {
            if (GetWindowRect(hwndSecondary, out RECT r))
            {
                int midX = (r.Left + r.Right) / 2;
                int midY = (r.Top + r.Bottom) / 2;
                if (screen.Bounds.Contains(midX, midY))
                {
                    return hwndSecondary;
                }
            }
        }

        if (screen.Primary)
        {
            return FindWindow("Shell_TrayWnd", null);
        }

        IntPtr tray = FindWindow("Shell_TrayWnd", null);
        if (tray != IntPtr.Zero && GetWindowRect(tray, out RECT pr))
        {
            int midX = (pr.Left + pr.Right) / 2;
            int midY = (pr.Top + pr.Bottom) / 2;
            if (screen.Bounds.Contains(midX, midY))
            {
                return tray;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Attaches this window as a native Win32 child of the taskbar on the configured screen.
    /// This prevents the widget from disappearing when Task Manager is hovered,
    /// when other windows are focused, or when Win+D is pressed.
    /// </summary>
    public void AttachToTaskbar()
    {
        if (_isClosing) return;

        try
        {
            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            var screens = System.Windows.Forms.Screen.AllScreens;
            int monitorIdx = _config.TaskbarWidgetMonitor;
            if (monitorIdx < 0 || monitorIdx >= screens.Length)
            {
                monitorIdx = 0;
                _config.TaskbarWidgetMonitor = 0;
            }
            var targetScreen = screens[monitorIdx];
            IntPtr hwndTray = GetTaskbarForScreen(targetScreen);

            if (hwndTray != IntPtr.Zero)
            {
                _hwndTray = hwndTray;
                IntPtr currentParent = GetParent(hwnd);

                if (currentParent != hwndTray)
                {
                    // Reparent into target taskbar
                    SetParent(hwnd, hwndTray);

                    // Change window style to WS_CHILD and WS_CLIPSIBLINGS, remove WS_POPUP
                    int style = GetWindowLong(hwnd, GWL_STYLE);
                    style &= ~WS_POPUP;
                    style |= WS_CHILD | WS_CLIPSIBLINGS;
                    SetWindowLong(hwnd, GWL_STYLE, style);

                    int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

                    _isAttachedToTray = true;
                }

                RepositionToTaskbar();
            }
            else
            {
                // Fallback for screens without a taskbar (or custom shell)
                _isAttachedToTray = false;
                _hwndTray = IntPtr.Zero;
                SetParent(hwnd, IntPtr.Zero);

                int style = GetWindowLong(hwnd, GWL_STYLE);
                style &= ~WS_CHILD;
                style |= WS_POPUP;
                SetWindowLong(hwnd, GWL_STYLE, style);

                int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                RepositionToTaskbar();
            }
        }
        catch { }
    }

    private static bool IsWindows11()
    {
        return Environment.OSVersion.Version.Build >= 22000;
    }

    private static bool IsTaskbarCentered()
    {
        if (!IsWindows11()) return false;
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            if (key != null)
            {
                object? val = key.GetValue("TaskbarAl");
                if (val is int intVal) return intVal == 1;
            }
        }
        catch { }
        return true; // Windows 11 default is centered
    }

    private int _minLeftZone = 10;
    private int _maxLeftZone = 10;
    private int _minRightZone = 10;
    private int _maxRightZone = 500;
    private bool _hasLeftZone;
    private bool _hasRightZone = true;

    private static int DetectWindowsWidgetRight(IntPtr hwndTray, RECT trayRect, double dpiX)
    {
        int detectedRight = 0;
        int trayWidth = trayRect.Right - trayRect.Left;

        // 1. Try UI Automation search for Windows 11 Widget control on taskbar
        try
        {
            var trayEl = System.Windows.Automation.AutomationElement.FromHandle(hwndTray);
            if (trayEl != null)
            {
                // Check direct known automation IDs
                string[] widgetIds = { "WidgetsButton", "TaskbarWidgetsControl", "TaskbarWidgetsControlAutomationId", "Widgets", "Taskbar.WidgetsControl" };
                foreach (var id in widgetIds)
                {
                    try
                    {
                        var cond = new System.Windows.Automation.PropertyCondition(
                            System.Windows.Automation.AutomationElement.AutomationIdProperty, id);
                        var btn = trayEl.FindFirst(System.Windows.Automation.TreeScope.Descendants, cond);
                        if (btn != null)
                        {
                            var r = btn.Current.BoundingRectangle;
                            int relL = (int)(r.Left - trayRect.Left);
                            int relR = (int)(r.Right - trayRect.Left);
                            int width = (int)r.Width;
                            if (width > 20 && relL < trayWidth / 3 && relR > detectedRight)
                            {
                                detectedRight = relR;
                                break;
                            }
                        }
                    }
                    catch { }
                }

                // Also check by known ClassName
                if (detectedRight == 0)
                {
                    try
                    {
                        string[] classNames = { "Taskbar.AugmentedEntryPointButton", "AugmentedEntryPointButton", "TaskbarWidgetsControl" };
                        foreach (var cls in classNames)
                        {
                            var cond = new System.Windows.Automation.PropertyCondition(
                                System.Windows.Automation.AutomationElement.ClassNameProperty, cls);
                            var btn = trayEl.FindFirst(System.Windows.Automation.TreeScope.Descendants, cond);
                            if (btn != null)
                            {
                                var r = btn.Current.BoundingRectangle;
                                int relL = (int)(r.Left - trayRect.Left);
                                int relR = (int)(r.Right - trayRect.Left);
                                if (r.Width > 20 && relL < trayWidth / 3 && relR > detectedRight)
                                {
                                    detectedRight = relR;
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }

                // If not found by AutomationId, search children near the left edge of taskbar
                if (detectedRight == 0)
                {
                    try
                    {
                        var children = trayEl.FindAll(System.Windows.Automation.TreeScope.Children,
                            System.Windows.Automation.Condition.TrueCondition);
                        foreach (System.Windows.Automation.AutomationElement child in children)
                        {
                            var r = child.Current.BoundingRectangle;
                            int relL = (int)(r.Left - trayRect.Left);
                            int relR = (int)(r.Right - trayRect.Left);
                            string name = child.Current.Name ?? "";
                            string autoId = child.Current.AutomationId ?? "";

                            bool isWidget = autoId.Contains("Widget", StringComparison.OrdinalIgnoreCase) ||
                                            name.Contains("Widget", StringComparison.OrdinalIgnoreCase) ||
                                            name.Contains("Weather", StringComparison.OrdinalIgnoreCase) ||
                                            name.Contains("الطقس", StringComparison.OrdinalIgnoreCase) ||
                                            name.Contains("واجهة", StringComparison.OrdinalIgnoreCase);

                            if ((isWidget || relL <= 12) && r.Width >= 28 && relL < 60 && relR < trayWidth / 3)
                            {
                                if (relR > detectedRight)
                                {
                                    detectedRight = relR;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        // 2. Win32 child window check (XAML Island / child window at X=0)
        if (detectedRight == 0)
        {
            try
            {
                IntPtr child = IntPtr.Zero;
                while ((child = FindWindowEx(hwndTray, child, null, null)) != IntPtr.Zero)
                {
                    if (GetWindowRect(child, out RECT cr) && cr.Right > cr.Left)
                    {
                        int relL = cr.Left - trayRect.Left;
                        int relR = cr.Right - trayRect.Left;
                        int width = cr.Right - cr.Left;
                        if (relL >= 0 && relL <= 16 && width >= 28 && relR < trayWidth / 3)
                        {
                            detectedRight = Math.Max(detectedRight, relR);
                        }
                    }
                }
            }
            catch { }
        }

        // 3. Fallback for Windows 11 primary monitor if widgets are enabled in registry
        if (detectedRight == 0 && IsWindows11())
        {
            try
            {
                // Check if TaskbarDa (Widgets enabled) is 1 in registry (default in Win11 is 1)
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                bool widgetsEnabled = true;
                if (key != null)
                {
                    object? val = key.GetValue("TaskbarDa");
                    if (val is int intVal) widgetsEnabled = intVal == 1;
                }

                if (widgetsEnabled)
                {
                    // Default Windows 11 Weather Widget pill width is typically ~160-200 physical pixels at 100% DPI
                    detectedRight = (int)Math.Round(180 * dpiX);
                }
            }
            catch { }
        }

        return detectedRight;
    }

    private static void DetectStartAndIconsBounds(
        IntPtr hwndTray,
        RECT trayRect,
        int widgetsRight,
        int freeMaxRight,
        double dpiX,
        out int iconsLeft,
        out int iconsRight)
    {
        iconsLeft = -1;
        iconsRight = -1;

        bool isCentered = IsTaskbarCentered();
        int trayWidth = trayRect.Right - trayRect.Left;

        try
        {
            var trayEl = System.Windows.Automation.AutomationElement.FromHandle(hwndTray);
            if (trayEl != null)
            {
                double minL = double.MaxValue;
                double maxR = double.MinValue;
                bool found = false;

                // 1. Locate StartButton specifically (the leftmost element of centered cluster)
                try
                {
                    var startCond = new System.Windows.Automation.PropertyCondition(
                        System.Windows.Automation.AutomationElement.AutomationIdProperty, "StartButton");
                    var startBtn = trayEl.FindFirst(System.Windows.Automation.TreeScope.Descendants, startCond);
                    if (startBtn != null)
                    {
                        var r = startBtn.Current.BoundingRectangle;
                        int relL = (int)(r.Left - trayRect.Left);
                        int relR = (int)(r.Right - trayRect.Left);
                        if (r.Width > 0 && relL > widgetsRight)
                        {
                            minL = Math.Min(minL, relL);
                            maxR = Math.Max(maxR, relR);
                            found = true;
                        }
                    }
                }
                catch { }

                // Also try StartButton by name or class if not found
                if (!found)
                {
                    try
                    {
                        string[] startNames = { "Start", "ابدأ", "Starten", "Démarrer" };
                        foreach (var name in startNames)
                        {
                            var cond = new System.Windows.Automation.PropertyCondition(
                                System.Windows.Automation.AutomationElement.NameProperty, name);
                            var startBtn = trayEl.FindFirst(System.Windows.Automation.TreeScope.Descendants, cond);
                            if (startBtn != null)
                            {
                                var r = startBtn.Current.BoundingRectangle;
                                int relL = (int)(r.Left - trayRect.Left);
                                int relR = (int)(r.Right - trayRect.Left);
                                if (r.Width > 0 && relL > widgetsRight)
                                {
                                    minL = Math.Min(minL, relL);
                                    maxR = Math.Max(maxR, relR);
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }

                // 2. Locate other centered icon elements (Search, TaskView, Copilot, etc.)
                string[] iconIds = { "SearchButton", "SearchBoxAutomationId", "SearchBox", "TaskViewButton", "CopilotButton", "ChatButton" };
                foreach (var id in iconIds)
                {
                    try
                    {
                        var cond = new System.Windows.Automation.PropertyCondition(
                            System.Windows.Automation.AutomationElement.AutomationIdProperty, id);
                        var el = trayEl.FindFirst(System.Windows.Automation.TreeScope.Descendants, cond);
                        if (el != null)
                        {
                            var r = el.Current.BoundingRectangle;
                            int relL = (int)(r.Left - trayRect.Left);
                            int relR = (int)(r.Right - trayRect.Left);
                            if (r.Width > 0 && relL > widgetsRight)
                            {
                                minL = Math.Min(minL, relL);
                                maxR = Math.Max(maxR, relR);
                                found = true;
                            }
                        }
                    }
                    catch { }
                }

                // 3. Locate TaskList container or items
                try
                {
                    var taskListCond = new System.Windows.Automation.PropertyCondition(
                        System.Windows.Automation.AutomationElement.AutomationIdProperty, "TaskListAutomationId");
                    var taskList = trayEl.FindFirst(System.Windows.Automation.TreeScope.Descendants, taskListCond);
                    if (taskList != null)
                    {
                        var r = taskList.Current.BoundingRectangle;
                        int relL = (int)(r.Left - trayRect.Left);
                        int relR = (int)(r.Right - trayRect.Left);
                        if (r.Width > 0 && relL > widgetsRight + (int)(40 * dpiX))
                        {
                            minL = Math.Min(minL, relL);
                            maxR = Math.Max(maxR, relR);
                            found = true;
                        }
                    }
                }
                catch { }

                if (found && maxR > minL && minL > widgetsRight)
                {
                    iconsLeft = (int)minL;
                    iconsRight = (int)maxR;
                }
            }
        }
        catch { }

        // Fallback for Windows 11 centered icons
        if ((iconsLeft < 0 || iconsRight < 0) && isCentered)
        {
            int center = trayWidth / 2;
            int halfSpan = (int)(260 * dpiX);
            iconsLeft = Math.Max(widgetsRight + (int)(40 * dpiX), center - halfSpan);
            iconsRight = Math.Min(freeMaxRight - (int)(40 * dpiX), center + halfSpan);
        }
        else if (iconsLeft < 0 || iconsRight < 0)
        {
            // Windows 10 left-aligned icons
            iconsLeft = widgetsRight;
            iconsRight = Math.Min((int)(500 * dpiX), freeMaxRight - 150);
        }
    }

    private void UpdateTaskbarZones(IntPtr hwndTray, int pixelWidth, double dpiX)
    {
        if (!GetWindowRect(hwndTray, out RECT trayRect) || trayRect.Right <= trayRect.Left)
        {
            _minLeftZone = (int)(10 * dpiX);
            _maxLeftZone = (int)(10 * dpiX);
            _minRightZone = (int)(20 * dpiX);
            _maxRightZone = (int)(500 * dpiX);
            _hasLeftZone = false;
            _hasRightZone = true;
            return;
        }

        int trayWidth = trayRect.Right - trayRect.Left;

        // 1. Rightmost boundary: TrayNotifyWnd (System Tray & Windows Date & Time)
        int freeMaxRight = trayWidth - (int)(8 * dpiX);

        IntPtr hwndNotify = FindWindowEx(hwndTray, IntPtr.Zero, "TrayNotifyWnd", null);
        if (hwndNotify != IntPtr.Zero && GetWindowRect(hwndNotify, out RECT notifyRect) && notifyRect.Right > notifyRect.Left)
        {
            int notifyLeft = notifyRect.Left - trayRect.Left;
            if (notifyLeft > 10 && notifyLeft <= trayWidth)
            {
                freeMaxRight = notifyLeft - (int)(4 * dpiX);
            }
        }
        else
        {
            // For secondary taskbar or Windows 11 secondary tray, check rightmost child windows
            IntPtr child = FindWindowEx(hwndTray, IntPtr.Zero, null, null);
            while (child != IntPtr.Zero)
            {
                var helper = new WindowInteropHelper(this);
                if (child != helper.Handle && IsWindowVisible(child))
                {
                    if (GetWindowRect(child, out RECT cr) && cr.Right > cr.Left)
                    {
                        int cl = cr.Left - trayRect.Left;
                        if (cl > trayWidth / 2 && cl < freeMaxRight)
                        {
                            freeMaxRight = cl - (int)(4 * dpiX);
                        }
                    }
                }
                child = FindWindowEx(hwndTray, child, null, null);
            }
        }

        // 2. Windows Widget detection (at far left of taskbar)
        int widgetsRight = DetectWindowsWidgetRight(hwndTray, trayRect, dpiX);

        // 3. Start Button and Centered Icons detection
        DetectStartAndIconsBounds(hwndTray, trayRect, widgetsRight, freeMaxRight, dpiX, out int iconsLeft, out int iconsRight);

        // 4. Compute Left and Right Free Zones
        // Left zone: Starts immediately after the Windows Widget (stops before Windows Widget, never hovers over it!)
        // and ends before the Start button (never hovers over Start button)
        _minLeftZone = widgetsRight > 0 ? widgetsRight + (int)(10 * dpiX) : (int)(10 * dpiX);
        _maxLeftZone = iconsLeft - pixelWidth - (int)(10 * dpiX);

        // Right zone: Starts after the last taskbar icon and ends before system tray
        _minRightZone = iconsRight + (int)(10 * dpiX);
        _maxRightZone = freeMaxRight - pixelWidth;

        // Determine validity of zones
        _hasLeftZone = (_maxLeftZone >= _minLeftZone);
        _hasRightZone = (_maxRightZone >= _minRightZone);

        // Fallback safety: ensure at least one valid zone exists
        if (!_hasLeftZone && !_hasRightZone)
        {
            _hasRightZone = true;
            _minRightZone = Math.Max(4, freeMaxRight - pixelWidth);
            _maxRightZone = _minRightZone;
        }
    }

    private int ClampToFreeZones(int rawX)
    {
        if (_hasLeftZone && _hasRightZone)
        {
            if (rawX <= _maxLeftZone)
            {
                return Math.Clamp(rawX, _minLeftZone, _maxLeftZone);
            }
            if (rawX >= _minRightZone)
            {
                return Math.Clamp(rawX, _minRightZone, _maxRightZone);
            }
            // In the forbidden icon zone: snap to nearest free zone
            int iconCenter = (_maxLeftZone + _minRightZone) / 2;
            return rawX < iconCenter ? _maxLeftZone : _minRightZone;
        }
        else if (_hasLeftZone)
        {
            return Math.Clamp(rawX, _minLeftZone, _maxLeftZone);
        }
        else
        {
            return Math.Clamp(rawX, _minRightZone, _maxRightZone);
        }
    }

    public void RepositionToTaskbar()
    {
        if (_isClosing || (_hasDragged && _isMouseDown)) return;

        try
        {
            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            var dpi = VisualTreeHelper.GetDpi(this);
            double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

            int pixelWidth = (int)Math.Ceiling(120 * dpiX);
            int pixelHeight = (int)Math.Ceiling(36 * dpiY);

            if (_isAttachedToTray && _hwndTray != IntPtr.Zero && GetWindowRect(_hwndTray, out RECT trayRect))
            {
                int trayHeight = trayRect.Bottom - trayRect.Top;
                int targetY = Math.Max(0, (trayHeight - pixelHeight) / 2);

                UpdateTaskbarZones(_hwndTray, pixelWidth, dpiX);

                int targetX;
                if (_config.TaskbarWidgetDockMode == "custom" && _config.TaskbarWidgetPosX >= 0)
                {
                    targetX = (int)_config.TaskbarWidgetPosX;
                    targetX = ClampToFreeZones(targetX);
                }
                else
                {
                    // Default: dock immediately to the left of the system notification area (TrayNotifyWnd / Windows Date)
                    targetX = _maxRightZone;
                }

                SetWindowPos(hwnd, IntPtr.Zero, targetX, targetY, pixelWidth, pixelHeight,
                    SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            else
            {
                // Fallback for top-level floating window on target screen
                var screens = System.Windows.Forms.Screen.AllScreens;
                int monitorIdx = _config.TaskbarWidgetMonitor;
                if (monitorIdx < 0 || monitorIdx >= screens.Length) monitorIdx = 0;
                var screen = screens[monitorIdx];

                double width = ActualWidth > 0 ? ActualWidth : 120;
                double height = ActualHeight > 0 ? ActualHeight : 36;

                double workRight = screen.WorkingArea.Right / dpiX;
                double workBottom = screen.WorkingArea.Bottom / dpiY;

                if (_config.TaskbarWidgetDockMode == "custom" && _config.TaskbarWidgetPosX >= 0 && _config.TaskbarWidgetPosY >= 0)
                {
                    Left = _config.TaskbarWidgetPosX / dpiX;
                    Top = workBottom - height - 4;
                }
                else
                {
                    Left = workRight - width - 20;
                    Top = workBottom - height - 4;
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// Returns screen coordinates for opening flyout dialogs accurately
    /// </summary>
    public (double left, double top, double width, double height) GetScreenBounds()
    {
        try
        {
            var helper = new WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero && GetWindowRect(helper.Handle, out RECT r) && r.Right > r.Left && r.Bottom > r.Top)
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
                return (r.Left / dpiX, r.Top / dpiY, (r.Right - r.Left) / dpiX, (r.Bottom - r.Top) / dpiY);
            }
        }
        catch { }

        if (GetCursorPos(out POINT curPt))
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
            return (curPt.X / dpiX - 60, curPt.Y / dpiY - 18, 120, 36);
        }

        return (Left, Top, ActualWidth > 0 ? ActualWidth : 120, ActualHeight > 0 ? ActualHeight : 36);
    }

    #region Prayer Times & Countdown Updates

    public void UpdatePrayerData(
        string prayerName,
        string prayerTime,
        string icon,
        TimeSpan countdown,
        bool isElapsed,
        TimeSpan elapsed)
    {
        if (_isClosing) return;

        bool isAr = _config.Language == "ar";
        _isElapsedState = isElapsed;

        TxtPrayerName.Text = prayerName;
        TxtPrayerTime.Text = prayerTime;

        bool showSec = _config.TaskbarWidgetShowSeconds;

        if (isElapsed)
        {
            // Active / Adhan elapsed state (e.g. Asr 15:20 | After +00:08)
            TxtTimerLabel.Text = isAr ? "مضى" : "After";
            TxtPlusSign.Visibility = Visibility.Visible;
            NotificationIndicator.Opacity = 0.85;

            int h = (int)elapsed.TotalHours;
            int m = elapsed.Minutes;
            int s = elapsed.Seconds;

            TxtCountdown.Text = (h > 0 || showSec) 
                ? (h > 0 ? $"{h:D2}:{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}")
                : $"{m:D2}";
        }
        else
        {
            // Upcoming countdown state (e.g. Asr 15:20 | Left 01:23:45)
            TxtTimerLabel.Text = isAr ? "متبقٍ" : "Left";
            TxtPlusSign.Visibility = Visibility.Collapsed;

            // Subtle indicator if prayer is near (within 15 minutes)
            NotificationIndicator.Opacity = (countdown.TotalMinutes <= 15 && countdown.TotalSeconds > 0) ? 0.35 : 0;

            int h = (int)countdown.TotalHours;
            int m = countdown.Minutes;
            int s = countdown.Seconds;

            TxtCountdown.Text = showSec ? $"{h:D2}:{m:D2}:{s:D2}" : $"{h:D2}:{m:D2}";
        }

    }

    public void UpdateWeatherData(WeatherResult? weather)
    {
        // Weather and timer tooltip on hover has been removed
    }

    #endregion

    #region Theming & Localization

    public void ApplyTheme(ThemePalette theme, bool isDark)
    {
        RootBorder.Background = Brushes.Transparent;
        RootBorder.BorderThickness = new Thickness(0);
        NotificationIndicator.Background = theme.Accent;
    }

    public void ApplyLanguage(string lang)
    {
        bool isAr = lang == "ar";
        FlowDirection = isAr ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;

        MenuNextMonitor.Header = isAr ? "🖥️ نقل إلى الشاشة التالية" : "🖥️ Move to Next Monitor";
        MenuLockTaskbar.Header = isAr ? "🔒 قفل بجانب علبة النظام (افتراضي)" : "🔒 Lock Next to System Tray (Default)";
        MenuShowSeconds.Header = isAr ? "⏱️ إظهار الثواني" : "⏱️ Show Seconds";
        MenuSettings.Header = isAr ? "⚙️ الإعدادات…" : "⚙️ Settings…";
        MenuHide.Header = isAr ? "👁 إخفاء ودجت شريط المهام" : "👁 Hide Taskbar Widget";
        MenuCloseApp.Header = isAr ? "🚪 إغلاق التطبيق" : "🚪 Close Application";

        SyncSettingsToggles();
    }

    private void SyncSettingsToggles()
    {
        MenuLockTaskbar.IsChecked = _config.TaskbarWidgetDockMode != "custom";
        MenuShowSeconds.IsChecked = _config.TaskbarWidgetShowSeconds;
    }

    #endregion

    #region Mouse Dragging & Click Handling

    private void RootBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPos = e.GetPosition(this);
        _isMouseDown = true;
        _hasDragged = false;
    }

    private void RootBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isMouseDown) return;

        // If locked next to system tray, dragging is locked and disabled
        if (MenuLockTaskbar.IsChecked) return;

        System.Windows.Point current = e.GetPosition(this);
        // Only trigger horizontal dragging when horizontal movement exceeds threshold (8px)
        if (!_hasDragged && Math.Abs(current.X - _dragStartPos.X) > 8)
        {
            _hasDragged = true;
            _win32HasDragged = true;
            RootBorder.CaptureMouse();
            _config.TaskbarWidgetDockMode = "custom";
            if (_hwndTray != IntPtr.Zero)
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                int pixelWidth = (int)Math.Ceiling(120 * dpiX);
                UpdateTaskbarZones(_hwndTray, pixelWidth, dpiX);
            }
        }

        if (_hasDragged)
        {
            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            var dpi = VisualTreeHelper.GetDpi(this);
            double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

            int pixelWidth = (int)Math.Ceiling(120 * dpiX);
            int pixelHeight = (int)Math.Ceiling(36 * dpiY);

            if (GetCursorPos(out POINT pt))
            {
                // Check if cursor moved to another monitor
                var cursorScreen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(pt.X, pt.Y));
                IntPtr targetTray = GetTaskbarForScreen(cursorScreen);

                if (targetTray != IntPtr.Zero && targetTray != _hwndTray)
                {
                    _hwndTray = targetTray;
                    SetParent(hwnd, targetTray);

                    int style = GetWindowLong(hwnd, GWL_STYLE);
                    style &= ~WS_POPUP;
                    style |= WS_CHILD | WS_CLIPSIBLINGS;
                    SetWindowLong(hwnd, GWL_STYLE, style);

                    var allScreens = System.Windows.Forms.Screen.AllScreens;
                    for (int i = 0; i < allScreens.Length; i++)
                    {
                        if (allScreens[i].DeviceName == cursorScreen.DeviceName)
                        {
                            _config.TaskbarWidgetMonitor = i;
                            break;
                        }
                    }

                    UpdateTaskbarZones(_hwndTray, pixelWidth, dpiX);
                }

                if (_hwndTray != IntPtr.Zero && GetWindowRect(_hwndTray, out RECT trayRect))
                {
                    int trayHeight = trayRect.Bottom - trayRect.Top;
                    // Y IS STRICTLY LOCKED: Centered vertically in taskbar, NEVER moves up or down!
                    int localY = Math.Max(0, (trayHeight - pixelHeight) / 2);

                    // X moves horizontally, clamped strictly within free space (never hovers over icons, widgets, or date)
                    int rawX = pt.X - trayRect.Left - (int)(_dragStartPos.X * dpiX);
                    int localX = ClampToFreeZones(rawX);

                    SetWindowPos(hwnd, IntPtr.Zero, localX, localY, pixelWidth, pixelHeight,
                        SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);

                    _config.TaskbarWidgetDockMode = "custom";
                    _config.TaskbarWidgetPosX = localX;
                    _config.TaskbarWidgetPosY = localY;
                }
            }
        }
    }

    private void RootBorder_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (RootBorder.IsMouseCaptured)
        {
            RootBorder.ReleaseMouseCapture();
        }

        bool wasMouseDown = _isMouseDown;
        bool wasDragged = _hasDragged;

        _isMouseDown = false;
        _hasDragged = false;

        if (wasDragged)
        {
            _config.Save();
            SyncSettingsToggles();
        }
        else if (wasMouseDown)
        {
            // Clean left click: Toggle flyout window right above the button!
            _onToggleFlyout?.Invoke();
            e.Handled = true;
        }
    }

    private void RootBorder_MouseEnter(object sender, MouseEventArgs e)
    {
        // Subtle Windows taskbar hover effect (pill shape highlight)
        HoverIndicator.Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255));
    }

    private void RootBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        HoverIndicator.Background = Brushes.Transparent;
    }

    #endregion

    #region Context Menu Actions

    private void MenuNextMonitor_Click(object sender, RoutedEventArgs e)
    {
        MoveToNextMonitor();
    }

    public void MoveToNextMonitor()
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        if (screens.Length <= 1)
        {
            // If only 1 screen, re-dock to primary
            _config.TaskbarWidgetMonitor = 0;
            _config.TaskbarWidgetDockMode = "tray_left";
            _config.TaskbarWidgetPosX = -1;
            _config.TaskbarWidgetPosY = -1;
            _config.Save();
            AttachToTaskbar();
            return;
        }

        int current = _config.TaskbarWidgetMonitor;
        if (current < 0 || current >= screens.Length)
        {
            current = 0;
        }
        int next = (current + 1) % screens.Length;
        _config.TaskbarWidgetMonitor = next;
        _config.TaskbarWidgetDockMode = "tray_left";
        _config.TaskbarWidgetPosX = -1;
        _config.TaskbarWidgetPosY = -1;
        _config.Save();

        AttachToTaskbar();
    }

    private void MenuCloseApp_Click(object sender, RoutedEventArgs e)
    {
        _onCloseApp?.Invoke();
    }

    private void MenuLockTaskbar_Click(object sender, RoutedEventArgs e)
    {
        if (MenuLockTaskbar.IsChecked)
        {
            _config.TaskbarWidgetDockMode = "tray_left";
            _config.TaskbarWidgetPosX = -1;
            _config.TaskbarWidgetPosY = -1;
        }
        else
        {
            _config.TaskbarWidgetDockMode = "custom";
        }
        _config.Save();
        SyncSettingsToggles();
        RepositionToTaskbar();
    }

    private void MenuShowSeconds_Click(object sender, RoutedEventArgs e)
    {
        _config.TaskbarWidgetShowSeconds = MenuShowSeconds.IsChecked;
        _config.Save();
        SyncSettingsToggles();
    }

    private void MenuSettings_Click(object sender, RoutedEventArgs e)
    {
        _onOpenSettings();
    }

    private void MenuHide_Click(object sender, RoutedEventArgs e)
    {
        _config.TaskbarWidgetEnabled = false;
        _config.Save();
        Hide();
        VisibilityChanged?.Invoke(false);
    }

    #endregion

    public void SafeClose()
    {
        _isClosing = true;
        _watchdogTimer.Stop();
        try
        {
            Close();
        }
        catch { }
    }
}
