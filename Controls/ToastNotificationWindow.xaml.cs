using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Mawaqit.Models;
using Mawaqit.Services;

namespace Mawaqit.Controls;

public partial class ToastNotificationWindow : Window
{
    private readonly AudioService? _audioService;
    private readonly bool _hasAudio;
    private readonly DispatcherTimer _timer;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int MA_NOACTIVATE = 3;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public ToastNotificationWindow(string title, string message, ThemePalette theme, bool isDark, AudioService? audioService = null, bool hasAudio = false)
    {
        InitializeComponent();

        _audioService = audioService;
        _hasAudio = hasAudio;

        RootBorder.Background = theme.Card;
        RootBorder.BorderBrush = theme.Border;

        TxtTitle.Text = title;
        TxtTitle.Foreground = theme.Accent;

        TxtMessage.Text = message;
        TxtMessage.Foreground = theme.Text;

        BtnClose.Foreground = theme.TextMuted;

        PositionBottomRight();

        _timer = new DispatcherTimer();
        if (_hasAudio)
        {
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                if (_audioService == null || !_audioService.IsPlaying)
                {
                    _timer.Stop();
                    Close();
                }
            };
        }
        else
        {
            _timer.Interval = TimeSpan.FromSeconds(6);
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                Close();
            };
        }
        _timer.Start();

        Loaded += (s, e) =>
        {
            DwmHelper.EnableWin11Styling(this, isDark, true);
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

                var source = HwndSource.FromHwnd(hwnd);
                source?.AddHook(WndProc);

                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }
        catch { }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_MOUSEACTIVATE)
        {
            handled = true;
            return (IntPtr)MA_NOACTIVATE;
        }
        return IntPtr.Zero;
    }

    private void PositionBottomRight()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 16;
        Top = workArea.Bottom - Height - 16;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        if (_hasAudio && _audioService != null)
        {
            _audioService.Stop();
        }
        Close();
    }
}
