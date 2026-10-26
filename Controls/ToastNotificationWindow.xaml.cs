using System;
using System.Windows;
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
