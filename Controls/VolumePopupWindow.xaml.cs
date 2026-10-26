using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Mawaqit.Models;
using Mawaqit.Services;

namespace Mawaqit.Controls;

public partial class VolumePopupWindow : Window
{
    private readonly AppConfig _config;
    private readonly AudioService _audioService;
    private readonly Action<int> _onVolumeChanged;
    private bool _updatingFromCode = true;
    private bool _isInitialized;
    private bool _isClosing;
    private DispatcherTimer? _saveDebounceTimer;

    public VolumePopupWindow(AppConfig config, AudioService audioService, ThemePalette theme, bool isDark, Action<int> onVolumeChanged)
    {
        _config = config;
        _audioService = audioService;
        _onVolumeChanged = onVolumeChanged;

        InitializeComponent();

        RootBorder.Background = theme.Card;
        RootBorder.BorderBrush = theme.Border;
        TxtIcon.Foreground = theme.Accent;
        TxtPct.Foreground = theme.Text;
        BtnClose.Foreground = theme.TextMuted;

        int currentPct = _config.Muted ? 0 : (int)Math.Round(_config.Volume * 100);
        _updatingFromCode = true;
        SliderVol.Value = currentPct;
        _updatingFromCode = false;
        UpdateLabels(currentPct);

        PositionNearTray();

        Deactivated += OnWindowDeactivated;

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape) SafeClose();
        };

        Loaded += (s, e) =>
        {
            DwmHelper.EnableWin11Styling(this, isDark, true);
        };

        Closed += (s, e) =>
        {
            _isClosing = true;
            _saveDebounceTimer?.Stop();
            try
            {
                _config.Save();
                int pct = _config.Muted ? 0 : (int)Math.Round(_config.Volume * 100);
                _onVolumeChanged?.Invoke(pct);
            }
            catch { }
        };

        _isInitialized = true;
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (_isClosing) return;

        // Use background priority dispatcher to avoid closing while WmActivate is in progress
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!_isClosing && IsLoaded)
            {
                SafeClose();
            }
        }), DispatcherPriority.Background);
    }

    public void SafeClose()
    {
        if (_isClosing) return;
        _isClosing = true;

        try
        {
            _saveDebounceTimer?.Stop();
            Close();
        }
        catch { }
    }

    private void PositionNearTray()
    {
        try
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 12;
            Top = workArea.Bottom - Height - 12;
        }
        catch { }
    }

    private void UpdateLabels(int pct)
    {
        if (TxtPct != null)
            TxtPct.Text = $"{pct}%";
        if (TxtIcon != null)
            TxtIcon.Text = (_config?.Muted == true || pct == 0) ? "🔇" : "🔊";
    }

    private void SliderVol_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isInitialized || _updatingFromCode || _config == null || _isClosing) return;

        int pct = (int)Math.Round(e.NewValue);
        _config.Muted = (pct == 0);
        if (pct > 0)
        {
            _config.Volume = pct / 100.0;
        }

        _audioService.SetVolume(_config.Volume);
        _audioService.SetMute(_config.Muted);

        UpdateLabels(pct);

        // Debounce saving config and tray update so rapid dragging does not block the UI
        _saveDebounceTimer?.Stop();
        _saveDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _saveDebounceTimer.Tick += (s, ev) =>
        {
            _saveDebounceTimer.Stop();
            try
            {
                _config.Save();
                _onVolumeChanged?.Invoke(pct);
            }
            catch { }
        };
        _saveDebounceTimer.Start();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        SafeClose();
    }
}
