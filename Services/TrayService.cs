using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Mawaqit.Controls;
using Mawaqit.Models;

namespace Mawaqit.Services;

public class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly AppConfig _config;
    private readonly AudioService _audioService;
    private readonly Func<ThemePalette> _getTheme;
    private readonly Func<bool> _isDark;

    private ToolStripMenuItem _showItem = null!;
    private ToolStripMenuItem _nextPrayerItem = null!;
    private ToolStripMenuItem _taskbarWidgetItem = null!;
    private ToolStripMenuItem _volumeParentItem = null!;
    private ToolStripMenuItem[] _volRadioItems = null!;
    private ToolStripMenuItem _customVolItem = null!;
    private ToolStripMenuItem _muteItem = null!;
    private ToolStripMenuItem _checkUpdatesItem = null!;
    private ToolStripMenuItem _aboutItem = null!;
    private ToolStripMenuItem _exitItem = null!;
    private VolumePopupWindow? _volumePopup;

    public event Action? ShowRequested;
    public event Action? ExitRequested;
    public event Action<bool>? TaskbarWidgetToggled;
    public event Action? CheckUpdatesRequested;

    public TrayService(AppConfig config, AudioService audioService, Func<ThemePalette> getTheme, Func<bool> isDark)
    {
        _config = config;
        _audioService = audioService;
        _getTheme = getTheme;
        _isDark = isDark;

        _notifyIcon = new NotifyIcon();
        LoadIcon();

        BuildContextMenu();

        _notifyIcon.DoubleClick += (s, e) => ShowRequested?.Invoke();
        _notifyIcon.Visible = true;
    }

    private void LoadIcon()
    {
        try
        {
            var smallSize = System.Windows.Forms.SystemInformation.SmallIconSize;

            string[] candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.ico"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico"),
                Path.Combine(AppConfig.AppDataFolder, "logo.ico"),
                @"C:\Users\1\Desktop\logo.ico"
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        _notifyIcon.Icon = new Icon(path, smallSize.Width, smallSize.Height);
                        return;
                    }
                    catch { }
                }
            }

            try
            {
                var packIco = new Uri("pack://application:,,,/Resources/logo.ico", UriKind.Absolute);
                var streamInfo = System.Windows.Application.GetResourceStream(packIco);
                if (streamInfo != null)
                {
                    using var s = streamInfo.Stream;
                    _notifyIcon.Icon = new Icon(s, smallSize.Width, smallSize.Height);
                    return;
                }
            }
            catch { }

            try
            {
                string? exePath = Environment.ProcessPath ?? System.Windows.Forms.Application.ExecutablePath;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    var extracted = Icon.ExtractAssociatedIcon(exePath);
                    if (extracted != null)
                    {
                        _notifyIcon.Icon = new Icon(extracted, smallSize.Width, smallSize.Height);
                        return;
                    }
                }
            }
            catch { }
        }
        catch { }

        _notifyIcon.Icon = SystemIcons.Application;
    }

    public void BuildContextMenu()
    {
        string lang = _config.Language;
        bool isAr = lang == "ar";
        var menu = new ContextMenuStrip();

        _showItem = new ToolStripMenuItem(LocalizationService.Get("tray_show", lang), null, (s, e) => ShowRequested?.Invoke())
        {
            Font = new Font(menu.Font, System.Drawing.FontStyle.Bold)
        };
        menu.Items.Add(_showItem);
        menu.Items.Add(new ToolStripSeparator());

        _nextPrayerItem = new ToolStripMenuItem(LocalizationService.Get("tray_loading", lang))
        {
            Enabled = false
        };
        menu.Items.Add(_nextPrayerItem);

        // Taskbar Widget Toggle
        _taskbarWidgetItem = new ToolStripMenuItem(LocalizationService.Get("tray_taskbar_widget", lang), null, (s, e) =>
        {
            bool newState = !_config.TaskbarWidgetEnabled;
            _config.TaskbarWidgetEnabled = newState;
            _config.Save();
            _taskbarWidgetItem.Checked = newState;
            TaskbarWidgetToggled?.Invoke(newState);
        })
        {
            Checked = _config.TaskbarWidgetEnabled
        };
        menu.Items.Add(_taskbarWidgetItem);



        menu.Items.Add(new ToolStripSeparator());

        // Volume Submenu
        _volumeParentItem = new ToolStripMenuItem(GetVolumeParentText());
        int[] levels = { 0, 25, 50, 75, 100 };
        _volRadioItems = new ToolStripMenuItem[levels.Length];

        int currentPct = _config.Muted ? 0 : (int)Math.Round(_config.Volume * 100);

        for (int i = 0; i < levels.Length; i++)
        {
            int level = levels[i];
            var item = new ToolStripMenuItem($"{level}%", null, (s, e) => SetVolumeFromTray(level))
            {
                Checked = (_config.Muted && level == 0) || (!_config.Muted && currentPct == level)
            };
            _volRadioItems[i] = item;
            _volumeParentItem.DropDownItems.Add(item);
        }

        _volumeParentItem.DropDownItems.Add(new ToolStripSeparator());
        _customVolItem = new ToolStripMenuItem(LocalizationService.Get("tray_custom", lang), null, (s, e) => OpenCustomVolumePopup());
        _volumeParentItem.DropDownItems.Add(_customVolItem);

        menu.Items.Add(_volumeParentItem);

        // Mute / Unmute
        _muteItem = new ToolStripMenuItem(GetMuteItemText(), null, (s, e) => ToggleMute());
        menu.Items.Add(_muteItem);
        menu.Items.Add(new ToolStripSeparator());

        // Check for updates
        _checkUpdatesItem = new ToolStripMenuItem(isAr ? "🔄 التحقق من وجود تحديثات" : "🔄 Check for Updates", null, (s, e) => CheckUpdatesRequested?.Invoke());
        menu.Items.Add(_checkUpdatesItem);

        // About
        _aboutItem = new ToolStripMenuItem(LocalizationService.Get("tray_about", lang), null, (s, e) => OpenAboutDialog());
        menu.Items.Add(_aboutItem);
        menu.Items.Add(new ToolStripSeparator());

        // Exit
        _exitItem = new ToolStripMenuItem(LocalizationService.Get("tray_exit", lang), null, (s, e) => ExitRequested?.Invoke());
        menu.Items.Add(_exitItem);

        _notifyIcon.ContextMenuStrip = menu;
    }

    private string GetVolumeParentText()
    {
        string lang = _config.Language;
        if (_config.Muted)
        {
            return $"🔇 {LocalizationService.Get("tray_volume", lang)}: {LocalizationService.Get("tray_volume_muted", lang)}";
        }
        int pct = (int)Math.Round(_config.Volume * 100);
        return $"🔊 {LocalizationService.Get("tray_volume_fmt", lang, pct)}";
    }

    private string GetMuteItemText()
    {
        string lang = _config.Language;
        return _config.Muted
            ? $"🔇 {LocalizationService.Get("tray_unmute", lang)}"
            : $"🔊 {LocalizationService.Get("tray_mute", lang)}";
    }

    public void RefreshLabels()
    {
        string lang = _config.Language;
        bool isAr = lang == "ar";
        _showItem.Text = LocalizationService.Get("tray_show", lang);
        _taskbarWidgetItem.Text = LocalizationService.Get("tray_taskbar_widget", lang);
        _taskbarWidgetItem.Checked = _config.TaskbarWidgetEnabled;
        if (_checkUpdatesItem != null) _checkUpdatesItem.Text = isAr ? "🔄 التحقق من وجود تحديثات" : "🔄 Check for Updates";
        _volumeParentItem.Text = GetVolumeParentText();
        _customVolItem.Text = LocalizationService.Get("tray_custom", lang);
        _muteItem.Text = GetMuteItemText();
        _aboutItem.Text = LocalizationService.Get("tray_about", lang);
        _exitItem.Text = LocalizationService.Get("tray_exit", lang);

        int currentPct = _config.Muted ? 0 : (int)Math.Round(_config.Volume * 100);
        int[] levels = { 0, 25, 50, 75, 100 };
        for (int i = 0; i < levels.Length; i++)
        {
            _volRadioItems[i].Checked = (_config.Muted && levels[i] == 0) || (!_config.Muted && currentPct == levels[i]);
        }
    }

    public void UpdateTaskbarWidgetCheck(bool enabled)
    {
        if (_taskbarWidgetItem != null)
        {
            _taskbarWidgetItem.Checked = enabled;
        }
    }

    private void SetVolumeFromTray(int pct)
    {
        _config.Muted = (pct == 0);
        if (pct > 0)
        {
            _config.Volume = pct / 100.0;
        }

        _audioService.SetVolume(_config.Volume);
        _audioService.SetMute(_config.Muted);
        _config.Save();

        RefreshLabels();
    }

    private void ToggleMute()
    {
        _config.Muted = !_config.Muted;
        if (_config.Muted)
        {
            _audioService.Stop();
        }
        _audioService.SetMute(_config.Muted);
        _config.Save();

        RefreshLabels();
    }

    private void OpenCustomVolumePopup()
    {
        try
        {
            if (_volumePopup != null && _volumePopup.IsLoaded)
            {
                _volumePopup.Activate();
                return;
            }

            _volumePopup = new VolumePopupWindow(_config, _audioService, _getTheme(), _isDark(), pct =>
            {
                try
                {
                    if (_notifyIcon.ContextMenuStrip?.InvokeRequired == true)
                    {
                        _notifyIcon.ContextMenuStrip.BeginInvoke(new Action(RefreshLabels));
                    }
                    else
                    {
                        RefreshLabels();
                    }
                }
                catch { }
            });

            _volumePopup.Closed += (s, e) => _volumePopup = null;
            _volumePopup.Show();
            _volumePopup.Activate();
        }
        catch (Exception ex)
        {
            Logger.LogError($"OpenCustomVolumePopup error: {ex.Message}");
        }
    }

    private void OpenAboutDialog()
    {
        var about = new AboutWindow(_getTheme(), _isDark());
        about.ShowDialog();
    }

    public void UpdateNextPrayer(string prayerDisplayName, string timeStr, TimeSpan remaining)
    {
        string lang = _config.Language;
        int h = (int)remaining.TotalHours;
        int m = remaining.Minutes;
        int s = remaining.Seconds;

        string nextStr = $"⏳ {prayerDisplayName} {timeStr} — {h}:{m:D2}:{s:D2}";
        _nextPrayerItem.Text = nextStr;

        string tooltip = $"Mawaqit — {prayerDisplayName} {LocalizationService.Get("tray_left", lang)} {h}:{m:D2}:{s:D2}";
        if (tooltip.Length >= 64) tooltip = tooltip.Substring(0, 63); // NotifyIcon max length = 63
        try
        {
            _notifyIcon.Text = tooltip;
        }
        catch { }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
