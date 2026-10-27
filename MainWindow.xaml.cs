using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Mawaqit.Controls;
using Mawaqit.Models;
using Mawaqit.Services;

namespace Mawaqit;

public class PrayerCardViewModel : INotifyPropertyChanged
{
    private string _displayName = "";
    private string _timeString = "--:--";
    private string _icon = "";
    private bool _isNext;
    private Brush _cardBackground = Brushes.Transparent;
    private Brush _cardBorder = Brushes.Transparent;
    private Brush _textColor = Brushes.White;

    public string Key { get; set; } = "";

    public string DisplayName
    {
        get => _displayName;
        set { _displayName = value; OnPropertyChanged(nameof(DisplayName)); }
    }

    public string TimeString
    {
        get => _timeString;
        set { _timeString = value; OnPropertyChanged(nameof(TimeString)); }
    }

    public string Icon
    {
        get => _icon;
        set { _icon = value; OnPropertyChanged(nameof(Icon)); }
    }

    public bool IsNext
    {
        get => _isNext;
        set { _isNext = value; OnPropertyChanged(nameof(IsNext)); }
    }

    public Brush CardBackground
    {
        get => _cardBackground;
        set { _cardBackground = value; OnPropertyChanged(nameof(CardBackground)); }
    }

    public Brush CardBorder
    {
        get => _cardBorder;
        set { _cardBorder = value; OnPropertyChanged(nameof(CardBorder)); }
    }

    public Brush TextColor
    {
        get => _textColor;
        set { _textColor = value; OnPropertyChanged(nameof(TextColor)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class MainWindow : Window
{
    private readonly AppConfig _config;
    private readonly AudioService _audioService;
    private readonly TrayService _trayService;
    private readonly DispatcherTimer _clockTimer;
    private TaskbarWidgetWindow? _taskbarWidget;
    private TaskbarFlyoutWindow? _flyoutWindow;
    private string _lastWeatherString = "";
    private WeatherResult? _lastWeatherResult;

    private Dictionary<string, string> _rawTimings = new();
    private Dictionary<string, string> _prayerData = new();
    private Dictionary<string, DateTime> _prayerDateTimes = new();
    private readonly ObservableCollection<PrayerCardViewModel> _prayerCards = new();

    private readonly HashSet<(string date, string prayer)> _adhanPlayed = new();
    private readonly HashSet<(string date, string prayer)> _reminderSent = new();
    private readonly HashSet<(string date, string prayer)> _adhanMsgShown = new();
    private readonly HashSet<(string date, string prayer)> _reminderMsgShown = new();

    private ThemePalette _theme = ThemePalette.Dark;
    private bool _isDark = true;
    private bool _isShuttingDown;
    private bool _isFetching;
    private DateTime _lastWeatherFetch = DateTime.MinValue;
    private string _currentDateStr = "";
    private string _nextPrayerName = "";
    private DateTime? _nextPrayerTime;

    public MainWindow(bool startHidden = false)
    {
        InitializeComponent();
        LoadAppLogo();

        _config = AppConfig.Load();
        _isDark = _config.ThemeMode == "dark";
        _theme = _isDark ? ThemePalette.Dark : ThemePalette.Light;

        // Resolve Audio files
        _config.AdhanFile = AudioService.ResolveAudioFile(_config.AdhanFile, "adhan.mp3");
        _config.ReminderFile = AudioService.ResolveAudioFile(_config.ReminderFile, "reminder.mp3");
        _config.Save();

        _audioService = new AudioService();
        _audioService.SetVolume(_config.Volume);
        _audioService.SetMute(_config.Muted);
        _audioService.PlaybackStopped += () => Dispatcher.Invoke(RefreshAudioTestButtons);

        _trayService = new TrayService(_config, _audioService, () => _theme, () => _isDark);
        _trayService.ShowRequested += () => Dispatcher.Invoke(ShowAndActivate);
        _trayService.ExitRequested += () => Dispatcher.Invoke(QuitApp);

        // Taskbar Flyout (Awqat Salaat WinUI popup flyout)
        _flyoutWindow = new TaskbarFlyoutWindow(
            _config,
            onOpenSettings: () => Dispatcher.Invoke(OpenSettingsTab),
            onRefresh: async () =>
            {
                await FetchPrayerTimesAsync();
                await FetchWeatherAsync();
            },
            onOpenAbout: () => Dispatcher.Invoke(OpenAboutDialog),
            onDismissAudio: () => Dispatcher.Invoke(() => _audioService.Stop())
        );
        _flyoutWindow.ApplyTheme(_theme, _isDark);

        // Taskbar Widget (Awqat Salaat WinUI style taskbar tile button)
        _taskbarWidget = new TaskbarWidgetWindow(
            _config,
            () => _theme,
            () => _isDark,
            onToggleFlyout: () => Dispatcher.Invoke(ToggleTaskbarFlyout),
            onOpenSettings: () => Dispatcher.Invoke(OpenSettingsTab),
            onShowApp: () => Dispatcher.Invoke(ShowAndActivate),
            onCloseApp: () => Dispatcher.Invoke(QuitApp)
        );

        _taskbarWidget.VisibilityChanged += (vis) =>
        {
            Dispatcher.Invoke(() =>
            {
                ChkTaskbarWidgetEnable.IsChecked = vis;
                _trayService.UpdateTaskbarWidgetCheck(vis);
            });
        };

        _trayService.TaskbarWidgetToggled += (enabled) =>
        {
            Dispatcher.Invoke(() =>
            {
                ChkTaskbarWidgetEnable.IsChecked = enabled;
                if (enabled)
                {
                    _taskbarWidget.Show();
                    _taskbarWidget.RepositionToTaskbar();
                }
                else
                {
                    _taskbarWidget.Hide();
                }
            });
        };

        PrayerCardsList.ItemsSource = _prayerCards;

        // Setup clock timer
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) => OnClockTick();

        // Perform complete background and widget initialization immediately.
        // This ensures the taskbar widget, prayer scheduler, weather, and clock
        // run even when Mawaqit is started minimized to tray on Windows boot (--startup).
        InitializeApp(startHidden);

        Loaded += (s, e) =>
        {
            DwmHelper.EnableWin11Styling(this, _isDark, false);
        };

        Closing += OnWindowClosing;
    }

    private void InitializeApp(bool startHidden)
    {
        // Load cached weather immediately so user never sees blank or --°C
        var cachedWeather = WeatherService.LoadCached();
        if (cachedWeather != null && cachedWeather.Temperature.HasValue)
        {
            _lastWeatherResult = cachedWeather;
            ImgWeatherIcon.Source = cachedWeather.IconDrawing;
            TxtTemperature.Text = $"{cachedWeather.Temperature.Value:0}°C";
            TxtTemperature.Foreground = cachedWeather.TextColor;
            _lastWeatherString = cachedWeather.FlyoutWeatherText;
            _taskbarWidget?.UpdateWeatherData(cachedWeather);
        }
        else
        {
            ImgWeatherIcon.Source = WeatherIconProvider.GetWeatherDrawing(null);
        }

        ApplyTheme();
        ApplyLanguage();
        PopulateCountries();
        PopulateWilayas();
        LoadCurrentTuningInputs();
        LoadSettingsForm();
        _clockTimer.Start();

        if (_config.TaskbarWidgetEnabled)
        {
            _taskbarWidget?.Show();
            _taskbarWidget?.RepositionToTaskbar();
        }

        if (_config.StartWithWindows)
        {
            RegistryService.SyncStartup(true);
        }

        _ = FetchPrayerTimesAsync();
        _ = FetchWeatherAsync();
    }

    private DateTime _lastToggleFlyoutTime = DateTime.MinValue;

    public void ToggleTaskbarFlyout()
    {
        if (_taskbarWidget == null || _flyoutWindow == null) return;

        // Debounce: prevent duplicate toggles within 250ms
        if ((DateTime.UtcNow - _lastToggleFlyoutTime).TotalMilliseconds < 250)
        {
            return;
        }
        _lastToggleFlyoutTime = DateTime.UtcNow;

        // Debounce: If flyout was just closed by deactivation within the last 300ms, don't reopen on same click
        if ((DateTime.UtcNow - _flyoutWindow.LastClosedTime).TotalMilliseconds < 300)
        {
            return;
        }

        if (_flyoutWindow.IsVisible)
        {
            _flyoutWindow.Hide();
        }
        else
        {
            _flyoutWindow.UpdateData(
                _prayerData,
                _prayerDateTimes,
                _nextPrayerName,
                DateTime.Now,
                _lastWeatherString,
                _audioService.IsPlaying,
                _lastWeatherResult
            );
            var bounds = _taskbarWidget.GetScreenBounds();
            _flyoutWindow.ShowAboveWidget(
                bounds.left,
                bounds.top,
                bounds.width,
                bounds.height
            );
        }
    }

    public void OpenAboutDialog()
    {
        var about = new AboutWindow(_theme, _isDark);
        about.Owner = this;
        about.ShowDialog();
    }

    public void ToggleMainWindow()
    {
        if (IsVisible && WindowState != WindowState.Minimized && IsActive)
        {
            Hide();
        }
        else
        {
            ShowAndActivate();
        }
    }

    public void OpenSettingsTab()
    {
        ShowAndActivate();
        TabSettings.IsSelected = true;
    }

    public void ShowAndActivate()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();
        Focus();
        DwmHelper.EnableWin11Styling(this, _isDark, false);
    }

    private void QuitApp()
    {
        _isShuttingDown = true;
        _clockTimer.Stop();
        _audioService.Stop();
        _flyoutWindow?.SafeClose();
        _taskbarWidget?.SafeClose();
        _trayService.Dispose();
        _config.Save();
        Application.Current.Shutdown();
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!_isShuttingDown)
        {
            e.Cancel = true;
            Hide();
        }
    }

    #region Theming & Localization

    private void ApplyTheme()
    {
        _isDark = _config.ThemeMode == "dark";
        _theme = _isDark ? ThemePalette.Dark : ThemePalette.Light;

        Background = _theme.Background;
        RootGrid.Background = _theme.Background;

        // Header
        TxtAppTitle.Foreground = _theme.Accent;
        TxtLocationHeader.Foreground = _theme.TextMuted;
        TxtHijriDate.Foreground = _theme.TextMuted;
        TxtGregorianDate.Foreground = _theme.TextMuted;

        // TabControl
        TabPrayerTimes.Background = _theme.TabBg;
        TabPrayerTimes.Foreground = _theme.Text;
        TabSettings.Background = _theme.TabBg;
        TabSettings.Foreground = _theme.Text;

        // Banner
        BannerNextPrayer.Background = _theme.NextCard;
        TxtBannerNext.Foreground = _theme.NextText;

        // Current Location & Refresh
        BtnRefresh.Background = _theme.Btn2Bg;
        BtnRefresh.Foreground = _theme.Btn2Fg;
        TxtCurrentLocRow.Foreground = _theme.TextMuted;

        // Tuning Card
        CardTuningOuter.Background = _theme.Card;
        CardTuningOuter.BorderBrush = _theme.Border;
        TxtTuneTitle.Foreground = _theme.Accent;

        foreach (var lbl in new[] { LblTuneFajr, LblTuneDhuhr, LblTuneAsr, LblTuneMaghrib, LblTuneIsha })
        {
            lbl.Foreground = _theme.Text;
        }

        foreach (var txt in new[] { TxtTuneFajr, TxtTuneDhuhr, TxtTuneAsr, TxtTuneMaghrib, TxtTuneIsha })
        {
            txt.Background = _theme.EntryBg;
            txt.Foreground = _theme.Text;
            txt.BorderBrush = _theme.Border;
        }

        BtnApplyTune.Background = _theme.BtnBg;
        BtnApplyTune.Foreground = _theme.BtnFg;

        // Settings Cards
        foreach (var title in new[] { SecLocationTitle, SecAdhanTitle, SecReminderTitle, SecNotificationsTitle, SecTaskbarWidgetTitle, SecLanguageTitle, SecThemeTitle, SecStartupTitle })
        {
            title.Foreground = _theme.Accent;
        }

        foreach (var card in new[] { CardLocation, CardAdhan, CardReminder, CardNotifications, CardTaskbarWidget, CardLanguage, CardTheme, CardStartup })
        {
            card.Background = _theme.Card;
            card.BorderBrush = _theme.Border;
        }

        LblCountry.Foreground = _theme.Text;
        LblWilaya.Foreground = _theme.Text;
        LblCalcMethod.Foreground = _theme.Text;
        BtnAutoDetectLoc.Background = _theme.Btn2Bg;
        BtnAutoDetectLoc.Foreground = _theme.Btn2Fg;
        BtnSearchCity.Background = _theme.Btn2Bg;
        BtnSearchCity.Foreground = _theme.Btn2Fg;
        LblMinutesBefore.Foreground = _theme.Text;
        TxtReminderMinutes.Background = _theme.EntryBg;
        TxtReminderMinutes.Foreground = _theme.Text;
        TxtReminderMinutes.BorderBrush = _theme.Border;

        foreach (var chk in new[] { ChkAdhanEnable, ChkReminderEnable, ChkAdhanPopup, ChkReminderPopup, ChkTaskbarWidgetEnable, ChkTaskbarWidgetShowSeconds, ChkStartup })
        {
            chk.Foreground = _theme.Text;
        }

        BtnResetTaskbarWidgetPos.Background = _theme.Btn2Bg;
        BtnResetTaskbarWidgetPos.Foreground = _theme.Btn2Fg;

        _taskbarWidget?.ApplyTheme(_theme, _isDark);
        _flyoutWindow?.ApplyTheme(_theme, _isDark);

        foreach (var rb in new[] { RbLangAr, RbLangEn, RbThemeDark, RbThemeLight })
        {
            rb.Foreground = _theme.Text;
        }

        TxtAdhanFile.Foreground = string.IsNullOrEmpty(_config.AdhanFile) ? Brushes.Red : _theme.TextMuted;
        TxtReminderFile.Foreground = string.IsNullOrEmpty(_config.ReminderFile) ? Brushes.Red : _theme.TextMuted;

        foreach (var btn in new[] { BtnPickAdhan, BtnTestAdhan, BtnPickReminder, BtnTestReminder })
        {
            btn.Background = _theme.Btn2Bg;
            btn.Foreground = _theme.Btn2Fg;
        }

        TxtStatus.Foreground = _theme.TextMuted;

        RefreshPrayerCardsVisuals();
        DwmHelper.EnableWin11Styling(this, _isDark, false);
    }

    private void ApplyLanguage()
    {
        string lang = _config.Language;
        bool isAr = lang == "ar";

        FlowDirection = isAr ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;
        Title = LocalizationService.Get("window_title", lang);

        TxtAppTitle.Text = LocalizationService.Get("app_title", lang);
        TabPrayerTimes.Header = LocalizationService.Get("tab_prayer", lang);
        TabSettings.Header = LocalizationService.Get("tab_settings", lang);

        BtnRefresh.Content = LocalizationService.Get("refresh", lang);
        TxtTuneTitle.Text = LocalizationService.Get("tune_title", lang);
        BtnApplyTune.Content = LocalizationService.Get("apply_tune", lang);

        LblTuneFajr.Text = LocalizationService.Get("Fajr", lang);
        LblTuneDhuhr.Text = LocalizationService.Get("Dhuhr", lang);
        LblTuneAsr.Text = LocalizationService.Get("Asr", lang);
        LblTuneMaghrib.Text = LocalizationService.Get("Maghrib", lang);
        LblTuneIsha.Text = LocalizationService.Get("Isha", lang);

        SecLocationTitle.Text = LocalizationService.Get("sec_location", lang);
        LblCountry.Text = LocalizationService.Get("country", lang);
        LblWilaya.Text = LocalizationService.Get("wilaya", lang);
        LblCalcMethod.Text = LocalizationService.Get("calc_method", lang);
        BtnAutoDetectLoc.Content = LocalizationService.Get("auto_detect", lang);
        BtnSearchCity.Content = LocalizationService.Get("search_city", lang);
        PopulateCalculationMethods();
        PopulateCountries();
        PopulateWilayas();

        SecAdhanTitle.Text = LocalizationService.Get("sec_adhan", lang);
        ChkAdhanEnable.Content = LocalizationService.Get("adhan_enable", lang);
        BtnPickAdhan.Content = LocalizationService.Get("pick_mp3", lang);

        SecReminderTitle.Text = LocalizationService.Get("sec_reminder", lang);
        ChkReminderEnable.Content = LocalizationService.Get("reminder_enable", lang);
        LblMinutesBefore.Text = LocalizationService.Get("minutes_before", lang);
        BtnPickReminder.Content = LocalizationService.Get("pick_mp3", lang);

        SecNotificationsTitle.Text = LocalizationService.Get("sec_notifications", lang);
        ChkAdhanPopup.Content = LocalizationService.Get("adhan_popup_enable", lang);
        ChkReminderPopup.Content = LocalizationService.Get("reminder_popup_enable", lang);

        SecTaskbarWidgetTitle.Text = LocalizationService.Get("sec_taskbar_widget", lang);
        ChkTaskbarWidgetEnable.Content = LocalizationService.Get("taskbar_widget_enable", lang);
        ChkTaskbarWidgetShowSeconds.Content = LocalizationService.Get("taskbar_widget_show_seconds", lang);
        BtnResetTaskbarWidgetPos.Content = LocalizationService.Get("taskbar_widget_reset_pos", lang);
        _taskbarWidget?.ApplyLanguage(lang);
        _flyoutWindow?.ApplyLanguage(lang);

        SecLanguageTitle.Text = LocalizationService.Get("sec_language", lang);
        RbLangAr.Content = LocalizationService.Get("lang_arabic", lang);
        RbLangEn.Content = LocalizationService.Get("lang_english", lang);

        SecThemeTitle.Text = LocalizationService.Get("sec_theme", lang);
        RbThemeLight.Content = LocalizationService.Get("theme_light", lang);
        RbThemeDark.Content = LocalizationService.Get("theme_dark", lang);

        SecStartupTitle.Text = LocalizationService.Get("sec_startup", lang);
        ChkStartup.Content = LocalizationService.Get("startup_enable", lang);

        UpdateHeaderDates();
        UpdateLocationLabels();
        RefreshAudioTestButtons();
        _trayService.RefreshLabels();
    }

    private void UpdateHeaderDates()
    {
        var now = DateTime.Now;
        string lang = _config.Language;
        TxtGregorianDate.Text = HijriDateService.FormatGregorianDate(now, lang);
        TxtHijriDate.Text = HijriDateService.FormatHijriDate(now, lang);
    }

    private void UpdateLocationLabels()
    {
        string city = _config.Language == "ar" ? _config.WilayaAr : _config.WilayaEn;
        string country = _config.Language == "ar" ? _config.CountryAr : _config.CountryEn;
        string locDisplay = (string.IsNullOrEmpty(country) || country == city) ? city : $"{city} — {country}";

        TxtLocationHeader.Text = $"📍 {locDisplay}";
        TxtCurrentLocRow.Text = LocalizationService.Get("current_location", _config.Language, locDisplay);
    }

    #endregion

    #region Country, Wilaya & Tuning Setup

    private bool _suppressCountryEvent;
    private bool _suppressWilayaEvent;

    private void PopulateCountries()
    {
        _suppressCountryEvent = true;
        try
        {
            string lang = _config.Language;
            var list = Country.AllCountries.Select(c => c.DisplayName(lang)).ToList();
            CmbCountry.ItemsSource = list;

            string target = lang == "ar" ? _config.CountryAr : _config.CountryEn;
            if (string.IsNullOrWhiteSpace(target)) target = lang == "ar" ? "الجزائر" : "Algeria";

            var matched = Country.FindCountry(target);
            if (matched != null)
            {
                int idx = Country.AllCountries.IndexOf(matched);
                CmbCountry.SelectedIndex = idx >= 0 ? idx : 0;
            }
            else
            {
                CmbCountry.SelectedIndex = 0;
            }
        }
        finally
        {
            _suppressCountryEvent = false;
        }
    }

    private void PopulateWilayas(string? countryName = null)
    {
        _suppressWilayaEvent = true;
        try
        {
            bool isAr = _config.Language == "ar";
            string country = countryName ?? (isAr ? _config.CountryAr : _config.CountryEn);
            if (string.IsNullOrWhiteSpace(country)) country = isAr ? "الجزائر" : "Algeria";

            var states = Wilaya.GetStatesForCountry(country);
            var list = states.Select(w => isAr ? w.NameAr : w.NameEn).ToList();
            CmbWilaya.ItemsSource = list;

            string currentCity = isAr ? _config.WilayaAr : _config.WilayaEn;
            var matchingItem = list.FirstOrDefault(x =>
                string.Equals(x, currentCity, StringComparison.OrdinalIgnoreCase) ||
                Wilaya.NormalizeLocationSearch(x) == Wilaya.NormalizeLocationSearch(currentCity));

            if (matchingItem != null)
            {
                CmbWilaya.SelectedItem = matchingItem;
            }
            else if (list.Count > 0)
            {
                CmbWilaya.SelectedIndex = 0;
            }
            else
            {
                CmbWilaya.Text = currentCity;
            }
        }
        finally
        {
            _suppressWilayaEvent = false;
        }
    }

    private bool _suppressCalcMethodEvent;
    private void PopulateCalculationMethods()
    {
        _suppressCalcMethodEvent = true;
        try
        {
            var list = CalculationMethodItem.AllMethods.Select(m => m.DisplayName(_config.Language)).ToList();
            CmbCalcMethod.ItemsSource = list;

            int currentMethodId = _config.CalculationMethod > 0 ? _config.CalculationMethod : 19;
            int idx = CalculationMethodItem.AllMethods.FindIndex(m => m.Id == currentMethodId);
            CmbCalcMethod.SelectedIndex = idx >= 0 ? idx : 0;
        }
        finally
        {
            _suppressCalcMethodEvent = false;
        }
    }

    private void LoadCurrentTuningInputs()
    {
        string wilaya = _config.Language == "ar" ? _config.WilayaAr : _config.WilayaEn;
        TxtTuneFajr.Text = _config.GetTune(wilaya, "fajr").ToString();
        TxtTuneDhuhr.Text = _config.GetTune(wilaya, "dhuhr").ToString();
        TxtTuneAsr.Text = _config.GetTune(wilaya, "asr").ToString();
        TxtTuneMaghrib.Text = _config.GetTune(wilaya, "maghrib").ToString();
        TxtTuneIsha.Text = _config.GetTune(wilaya, "isha").ToString();
    }

    private void LoadSettingsForm()
    {
        ChkAdhanEnable.IsChecked = _config.AdhanEnabled;
        TxtAdhanFile.Text = string.IsNullOrEmpty(_config.AdhanFile)
            ? $"⚠ {LocalizationService.Get("no_file", _config.Language)}"
            : Path.GetFileName(_config.AdhanFile);

        ChkReminderEnable.IsChecked = _config.ReminderEnabled;
        TxtReminderMinutes.Text = _config.ReminderMinutes.ToString();
        TxtReminderFile.Text = string.IsNullOrEmpty(_config.ReminderFile)
            ? $"⚠ {LocalizationService.Get("no_file", _config.Language)}"
            : Path.GetFileName(_config.ReminderFile);

        ChkAdhanPopup.IsChecked = _config.AdhanPopupEnabled;
        ChkReminderPopup.IsChecked = _config.ReminderPopupEnabled;

        ChkTaskbarWidgetEnable.IsChecked = _config.TaskbarWidgetEnabled;
        ChkTaskbarWidgetShowSeconds.IsChecked = _config.TaskbarWidgetShowSeconds;

        RbLangAr.IsChecked = _config.Language == "ar";
        RbLangEn.IsChecked = _config.Language == "en";

        RbThemeDark.IsChecked = _config.ThemeMode == "dark";
        RbThemeLight.IsChecked = _config.ThemeMode == "light";

        ChkStartup.IsChecked = _config.StartWithWindows;
        PopulateCalculationMethods();

        RefreshAudioTestButtons();
    }

    private void RefreshAudioTestButtons()
    {
        string lang = _config.Language;
        string testText = LocalizationService.Get("test_audio", lang);
        string stopText = LocalizationService.Get("stop_audio", lang);

        if (_audioService.IsPlaying && _audioService.IsTestPlayback)
        {
            bool isAdhan = string.Equals(_audioService.CurrentPlayingPath, _config.AdhanFile, StringComparison.OrdinalIgnoreCase);
            BtnTestAdhan.Content = isAdhan ? stopText : testText;
            BtnTestReminder.Content = !isAdhan ? stopText : testText;
        }
        else
        {
            BtnTestAdhan.Content = testText;
            BtnTestReminder.Content = testText;
        }
    }

    #endregion

    #region Data Fetching

    private Wilaya GetCurrentWilaya()
    {
        if (_config.Latitude != 0 && _config.Longitude != 0)
        {
            return new Wilaya(
                _config.WilayaAr,
                _config.WilayaEn,
                _config.Latitude,
                _config.Longitude,
                _config.CountryAr,
                _config.CountryEn,
                _config.CalculationMethod);
        }

        var found = Wilaya.FindLocation(_config.WilayaAr, _config.WilayaEn);
        return found ?? Wilaya.AllWilayas[15]; // Default Algiers
    }

    private async Task FetchPrayerTimesAsync()
    {
        if (_isFetching || _isShuttingDown) return;
        _isFetching = true;

        TxtStatus.Text = LocalizationService.Get("fetching", _config.Language);

        var wilaya = GetCurrentWilaya();
        int method = _config.CalculationMethod > 0 ? _config.CalculationMethod : (wilaya.DefaultMethod > 0 ? wilaya.DefaultMethod : 19);
        var (timings, err) = await PrayerTimesService.FetchTimingsFromApiAsync(wilaya.Latitude, wilaya.Longitude, method);

        if (timings != null)
        {
            _rawTimings = timings;
            PrayerTimesService.SaveRawCache(_rawTimings);

            string currentWilayaName = _config.Language == "ar" ? _config.WilayaAr : _config.WilayaEn;
            _prayerData = PrayerTimesService.ApplyTuning(_rawTimings, currentWilayaName, _config);
            PrayerTimesService.SaveTunedCache(_prayerData);

            _prayerDateTimes = PrayerTimesService.ComputePrayerDateTimes(_prayerData);
            _currentDateStr = DateTime.Now.ToString("yyyy-MM-dd");

            RenderPrayerCards();
            UpdateBanner();
            TxtStatus.Text = LocalizationService.Get("updated", _config.Language, DateTime.Now.ToString("HH:mm:ss"));
        }
        else
        {
            // Offline fallback
            var cached = PrayerTimesService.LoadCachedRaw();
            if (cached != null)
            {
                _rawTimings = cached;
                string currentWilayaName = _config.Language == "ar" ? _config.WilayaAr : _config.WilayaEn;
                _prayerData = PrayerTimesService.ApplyTuning(_rawTimings, currentWilayaName, _config);
                _prayerDateTimes = PrayerTimesService.ComputePrayerDateTimes(_prayerData);

                RenderPrayerCards();
                UpdateBanner();
                TxtStatus.Text = LocalizationService.Get("offline_cached", _config.Language);
            }
            else
            {
                TxtStatus.Text = LocalizationService.Get("error", _config.Language, err ?? "Connection error");
            }
        }

        _isFetching = false;
    }

    private async Task FetchWeatherAsync()
    {
        try
        {
            var wilaya = GetCurrentWilaya();
            var res = await WeatherService.FetchTemperatureAsync(wilaya.Latitude, wilaya.Longitude);
            if (res != null && res.Temperature.HasValue)
            {
                _lastWeatherResult = res;
                ImgWeatherIcon.Source = res.IconDrawing;
                TxtTemperature.Text = $"{res.Temperature.Value:0}°C";
                TxtTemperature.Foreground = res.TextColor;
                _lastWeatherString = res.FlyoutWeatherText;
                _lastWeatherFetch = DateTime.Now;
                _taskbarWidget?.UpdateWeatherData(res);
                UpdateBanner();
            }
            else if (_lastWeatherResult == null)
            {
                // Retry in 2 minutes if weather is still not available
                _lastWeatherFetch = DateTime.Now.AddMinutes(-43);
            }
        }
        catch { }
    }

    #endregion

    #region Prayer Cards Rendering & Banner

    private string GetPrayerDisplayName(string key, DateTime now)
    {
        if (key == "Dhuhr" && now.DayOfWeek == DayOfWeek.Friday)
        {
            return LocalizationService.Get("Jumua", _config.Language);
        }
        return LocalizationService.Get(key, _config.Language);
    }

    private void RenderPrayerCards()
    {
        _prayerCards.Clear();
        var now = DateTime.Now;

        foreach (var key in PrayerTimesData.Order)
        {
            string timeStr = _prayerData.TryGetValue(key, out var t) ? t : "--:--";
            string icon = PrayerTimesData.Icons.TryGetValue(key, out var ic) ? ic : "🕌";
            if (key == "Dhuhr" && now.DayOfWeek == DayOfWeek.Friday)
            {
                icon = PrayerTimesData.Icons["Jumua"];
            }

            var card = new PrayerCardViewModel
            {
                Key = key,
                DisplayName = GetPrayerDisplayName(key, now),
                TimeString = timeStr,
                Icon = icon,
                IsNext = (key == _nextPrayerName)
            };

            _prayerCards.Add(card);
        }

        RefreshPrayerCardsVisuals();
    }

    private void RefreshPrayerCardsVisuals()
    {
        foreach (var card in _prayerCards)
        {
            if (card.Key == _nextPrayerName)
            {
                card.IsNext = true;
                card.CardBackground = _theme.Accent;
                card.CardBorder = _theme.Accent;
                card.TextColor = _theme.AccentText;
            }
            else
            {
                card.IsNext = false;
                card.CardBackground = _theme.Card;
                card.CardBorder = _theme.Border;
                card.TextColor = _theme.Text;
            }
        }
    }

    private string FindNextPrayer(DateTime now)
    {
        if (_prayerDateTimes.Count == 0) return "";

        foreach (var name in PrayerTimesData.AdhanOrder)
        {
            if (_prayerDateTimes.TryGetValue(name, out var dt) && dt > now)
            {
                return name;
            }
        }

        return "Fajr";
    }

    private void UpdateBanner()
    {
        var now = DateTime.Now;
        string next = FindNextPrayer(now);
        _nextPrayerName = next;

        if (string.IsNullOrEmpty(next) || !_prayerDateTimes.TryGetValue(next, out var dt))
        {
            TxtBannerNext.Text = LocalizationService.Get("loading", _config.Language);
            return;
        }

        DateTime target = dt;
        while (target < now)
        {
            target = target.AddDays(1);
        }

        _nextPrayerTime = target;
        var diff = target - now;
        if (diff < TimeSpan.Zero) diff = TimeSpan.Zero;

        int h = (int)diff.TotalHours;
        int m = diff.Minutes;
        int s = diff.Seconds;

        string displayPrayer = GetPrayerDisplayName(next, now);
        string timeStr = _prayerData.TryGetValue(next, out var t) ? t : "--:--";
        string prayerIcon = PrayerTimesData.Icons.TryGetValue(next, out var ic) ? ic : "🕌";

        TxtBannerNext.Text = LocalizationService.Get("next_prayer", _config.Language, displayPrayer, timeStr, h, m, s);
        _trayService.UpdateNextPrayer(displayPrayer, timeStr, diff);

        // Check if currently within 20 minutes elapsed since Adhan
        bool isElapsed = false;
        TimeSpan elapsedSpan = TimeSpan.Zero;
        string activePrayerName = "";
        string activePrayerTimeStr = "";
        string activeIcon = "";

        foreach (var name in PrayerTimesData.AdhanOrder)
        {
            if (_prayerDateTimes.TryGetValue(name, out var pdt))
            {
                double secSince = (now - pdt).TotalSeconds;
                if (secSince >= 0 && secSince < 1200) // Within 20 minutes after Adhan
                {
                    isElapsed = true;
                    elapsedSpan = TimeSpan.FromSeconds(secSince);
                    activePrayerName = GetPrayerDisplayName(name, now);
                    activePrayerTimeStr = _prayerData.TryGetValue(name, out var at) ? at : "--:--";
                    activeIcon = PrayerTimesData.Icons.TryGetValue(name, out var aic) ? aic : "🕌";
                    break;
                }
            }
        }

        if (isElapsed)
        {
            _taskbarWidget?.UpdatePrayerData(activePrayerName, activePrayerTimeStr, activeIcon, diff, true, elapsedSpan);
        }
        else
        {
            _taskbarWidget?.UpdatePrayerData(displayPrayer, timeStr, prayerIcon, diff, false, TimeSpan.Zero);
        }

        if (_flyoutWindow != null && _flyoutWindow.IsVisible)
        {
            _flyoutWindow.UpdateData(
                _prayerData,
                _prayerDateTimes,
                _nextPrayerName,
                now,
                _lastWeatherString,
                _audioService.IsPlaying,
                _lastWeatherResult
            );
        }

        RefreshPrayerCardsVisuals();
    }

    #endregion

    #region Clock Tick & Prayer Alerts

    private void OnClockTick()
    {
        if (_isShuttingDown) return;
        var now = DateTime.Now;

        // Check date rollover (midnight)
        string todayStr = now.ToString("yyyy-MM-dd");
        if (!string.IsNullOrEmpty(_currentDateStr) && todayStr != _currentDateStr)
        {
            _currentDateStr = todayStr;
            UpdateHeaderDates();
            _ = FetchPrayerTimesAsync();
        }

        // Weather refresh: every 45 minutes, or every 2 minutes if initial fetch was null
        double weatherElapsed = (now - _lastWeatherFetch).TotalMinutes;
        if ((_lastWeatherResult?.Temperature == null && weatherElapsed >= 2) || weatherElapsed >= 45)
        {
            _ = FetchWeatherAsync();
        }

        UpdateBanner();
        CheckAdhanAndReminder(now);
    }

    private void CheckAdhanAndReminder(DateTime now)
    {
        if (_prayerDateTimes.Count == 0 || _audioService.IsTestPlayback) return;
        string today = now.ToString("yyyy-MM-dd");

        // 1. Check Adhan
        bool adhanOn = _config.AdhanEnabled;
        string adhanFile = _config.AdhanFile;

        foreach (var name in PrayerTimesData.AdhanOrder)
        {
            if (!_prayerDateTimes.TryGetValue(name, out var dt)) continue;

            double secSince = (now - dt).TotalSeconds;
            var key = (today, name);

            if (secSince < -60 && _adhanPlayed.Contains(key))
            {
                _adhanPlayed.Remove(key);
                _adhanMsgShown.Remove(key);
            }

            if (secSince >= 0 && secSince <= 60 && !_adhanPlayed.Contains(key))
            {
                if (!adhanOn)
                {
                    _adhanPlayed.Add(key);
                    continue;
                }

                if (string.IsNullOrEmpty(adhanFile) || !File.Exists(adhanFile))
                {
                    if (!_adhanMsgShown.Contains(key))
                    {
                        _adhanMsgShown.Add(key);
                        MessageBox.Show(
                            "Adhan is enabled but MP3 file is missing.\nPlease choose an MP3 file in Settings.",
                            LocalizationService.Get("audio_missing_title", _config.Language),
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    continue;
                }

                bool played = _audioService.Play(adhanFile, _config.Volume, _config.Muted, false);
                if (played)
                {
                    _adhanPlayed.Add(key);
                    Logger.LogStartup($"Adhan PLAYED: {name} at {now:HH:mm:ss}");

                    if (_config.AdhanPopupEnabled)
                    {
                        string prayerDisp = GetPrayerDisplayName(name, now);
                        var toast = new ToastNotificationWindow(
                            LocalizationService.Get("adhan_popup_title", _config.Language),
                            LocalizationService.Get("adhan_popup_body", _config.Language, prayerDisp),
                            _theme, _isDark, _audioService, true);
                        toast.Show();
                    }
                }
                break;
            }
        }

        // 2. Check Reminder
        if (!_config.ReminderEnabled) return;

        string reminderFile = _config.ReminderFile;
        int minsBefore = _config.ReminderMinutes;

        foreach (var name in PrayerTimesData.AdhanOrder)
        {
            if (!_prayerDateTimes.TryGetValue(name, out var dt)) continue;

            var target = dt.AddMinutes(-minsBefore);
            double secSince = (now - target).TotalSeconds;
            var key = (today, name);

            if (secSince < -60 && _reminderSent.Contains(key))
            {
                _reminderSent.Remove(key);
                _reminderMsgShown.Remove(key);
            }

            if (secSince >= 0 && secSince <= 60 && !_reminderSent.Contains(key))
            {
                _reminderSent.Add(key);

                bool played = false;
                if (!string.IsNullOrEmpty(reminderFile) && File.Exists(reminderFile))
                {
                    played = _audioService.Play(reminderFile, _config.Volume, _config.Muted, false);
                    if (played)
                    {
                        Logger.LogStartup($"Reminder PLAYED: {name} ({minsBefore} min before)");
                    }
                }

                if (_config.ReminderPopupEnabled)
                {
                    string prayerDisp = GetPrayerDisplayName(name, now);
                    var toast = new ToastNotificationWindow(
                        LocalizationService.Get("reminder_title", _config.Language),
                        LocalizationService.Get("reminder_body", _config.Language, minsBefore, prayerDisp),
                        _theme, _isDark, _audioService, played);
                    toast.Show();
                }
                break;
            }
        }
    }

    #endregion

    #region UI Event Handlers

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        _ = FetchPrayerTimesAsync();
        _ = FetchWeatherAsync();
    }

    private void BtnApplyTune_Click(object sender, RoutedEventArgs e)
    {
        string wilaya = _config.Language == "ar" ? _config.WilayaAr : _config.WilayaEn;

        int ParseTune(TextBox txt) => int.TryParse(txt.Text.Trim(), out int v) ? Math.Clamp(v, -60, 60) : 0;

        _config.SetTune(wilaya, "fajr", ParseTune(TxtTuneFajr));
        _config.SetTune(wilaya, "dhuhr", ParseTune(TxtTuneDhuhr));
        _config.SetTune(wilaya, "asr", ParseTune(TxtTuneAsr));
        _config.SetTune(wilaya, "maghrib", ParseTune(TxtTuneMaghrib));
        _config.SetTune(wilaya, "isha", ParseTune(TxtTuneIsha));

        _config.Save();

        if (_rawTimings.Count > 0)
        {
            _prayerData = PrayerTimesService.ApplyTuning(_rawTimings, wilaya, _config);
            PrayerTimesService.SaveTunedCache(_prayerData);
            _prayerDateTimes = PrayerTimesService.ComputePrayerDateTimes(_prayerData);

            RenderPrayerCards();
            UpdateBanner();

            TxtStatus.Text = LocalizationService.Get("tune_saved", _config.Language);
            var restoreTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            restoreTimer.Tick += (s, ev) =>
            {
                restoreTimer.Stop();
                TxtStatus.Text = LocalizationService.Get("ready", _config.Language);
            };
            restoreTimer.Start();
        }
    }

    private void CmbCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressCountryEvent) return;

        int idx = CmbCountry.SelectedIndex;
        if (idx < 0 || idx >= Country.AllCountries.Count) return;

        var country = Country.AllCountries[idx];
        _config.CountryAr = country.NameAr;
        _config.CountryEn = country.NameEn;

        if (country.DefaultMethod > 0)
        {
            _config.CalculationMethod = country.DefaultMethod;
        }

        var states = Wilaya.GetStatesForCountry(country.NameEn);
        if (states.Count > 0)
        {
            ChangeLocation(states[0]);
        }
        else
        {
            _config.Save();
            PopulateWilayas(country.NameEn);
            PopulateCalculationMethods();
            UpdateLocationLabels();
            _ = FetchPrayerTimesAsync();
            _ = FetchWeatherAsync();
        }
    }

    private void CmbWilaya_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressWilayaEvent) return;

        if (CmbWilaya.SelectedItem is string selected && !string.IsNullOrWhiteSpace(selected))
        {
            bool isAr = _config.Language == "ar";
            var states = Wilaya.GetStatesForCountry(_config.CountryEn);
            var found = states.FirstOrDefault(w =>
                (isAr ? w.NameAr : w.NameEn) == selected ||
                Wilaya.NormalizeLocationSearch(isAr ? w.NameAr : w.NameEn) == Wilaya.NormalizeLocationSearch(selected))
                ?? Wilaya.FindLocation(selected, selected);

            if (found != null)
            {
                ChangeLocation(found);
            }
        }
    }

    private async void CmbWilaya_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter)
        {
            string typed = CmbWilaya.Text.Trim();
            if (!string.IsNullOrEmpty(typed))
            {
                bool isAr = _config.Language == "ar";
                var states = Wilaya.GetStatesForCountry(_config.CountryEn);
                var match = states.FirstOrDefault(w =>
                    Wilaya.NormalizeLocationSearch(isAr ? w.NameAr : w.NameEn)
                        .StartsWith(Wilaya.NormalizeLocationSearch(typed)));

                if (match != null)
                {
                    ChangeLocation(match);
                    return;
                }

                var matches = Wilaya.FilterWilayas(typed, isAr);
                if (matches.Count > 0)
                {
                    ChangeLocation(matches[0]);
                }
                else
                {
                    var dlg = new CitySearchWindow(typed, _config.Language, _theme) { Owner = this };
                    if (dlg.ShowDialog() == true && dlg.SelectedLocation != null)
                    {
                        ChangeLocation(dlg.SelectedLocation);
                    }
                }
            }
        }
    }

    private void CmbCalcMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressCalcMethodEvent) return;

        int idx = CmbCalcMethod.SelectedIndex;
        if (idx >= 0 && idx < CalculationMethodItem.AllMethods.Count)
        {
            var selectedMethod = CalculationMethodItem.AllMethods[idx];
            if (_config.CalculationMethod != selectedMethod.Id)
            {
                _config.CalculationMethod = selectedMethod.Id;
                _config.Save();
                _ = FetchPrayerTimesAsync();
            }
        }
    }

    private async void BtnAutoDetectLoc_Click(object sender, RoutedEventArgs e)
    {
        BtnAutoDetectLoc.IsEnabled = false;
        BtnAutoDetectLoc.Content = LocalizationService.Get("detecting_loc", _config.Language);

        try
        {
            var (loc, err) = await LocationService.DetectLocationAsync();
            if (loc != null)
            {
                ChangeLocation(loc);
                string locName = _config.Language == "ar" ? loc.DisplayNameAr : loc.DisplayNameEn;
                TxtStatus.Text = LocalizationService.Get("detect_success", _config.Language, locName);
            }
            else
            {
                string msg = LocalizationService.Get("detect_fail", _config.Language);
                TxtStatus.Text = msg;
                MessageBox.Show(msg, "Mawaqit", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"BtnAutoDetectLoc exception: {ex.Message}");
        }
        finally
        {
            BtnAutoDetectLoc.IsEnabled = true;
            BtnAutoDetectLoc.Content = LocalizationService.Get("auto_detect", _config.Language);
        }
    }

    private void BtnSearchCity_Click(object sender, RoutedEventArgs e)
    {
        string current = CmbWilaya.Text?.Trim() ?? "";
        var dlg = new CitySearchWindow(current, _config.Language, _theme)
        {
            Owner = this
        };

        if (dlg.ShowDialog() == true && dlg.SelectedLocation != null)
        {
            ChangeLocation(dlg.SelectedLocation);
        }
    }

    private void ChangeWilaya(string input)
    {
        bool isAr = _config.Language == "ar";
        var states = Wilaya.GetStatesForCountry(_config.CountryEn);
        var w = states.FirstOrDefault(x =>
            (isAr && (x.DisplayNameAr == input || x.NameAr == input)) ||
            (!isAr && (x.DisplayNameEn == input || x.NameEn == input)))
            ?? Wilaya.AllLocations.FirstOrDefault(x =>
            (isAr && (x.DisplayNameAr == input || x.NameAr == input)) ||
            (!isAr && (x.DisplayNameEn == input || x.NameEn == input)));

        if (w == null)
        {
            var matches = Wilaya.FilterWilayas(input, isAr);
            if (matches.Count > 0) w = matches[0];
        }

        if (w != null)
        {
            ChangeLocation(w);
        }
    }

    private void ChangeLocation(Wilaya w)
    {
        _config.WilayaAr = w.NameAr;
        _config.WilayaEn = w.NameEn;
        _config.CountryAr = w.CountryAr;
        _config.CountryEn = w.CountryEn;
        _config.Latitude = w.Latitude;
        _config.Longitude = w.Longitude;

        if (w.DefaultMethod > 0)
        {
            _config.CalculationMethod = w.DefaultMethod;
        }

        _config.Save();

        SyncLocationControls();
        PopulateCalculationMethods();
        UpdateLocationLabels();
        LoadCurrentTuningInputs();

        _ = FetchPrayerTimesAsync();
        _ = FetchWeatherAsync();
    }

    private void SyncLocationControls()
    {
        _suppressCountryEvent = true;
        _suppressWilayaEvent = true;
        try
        {
            string targetCountry = _config.Language == "ar" ? _config.CountryAr : _config.CountryEn;
            var matchedCountry = Country.FindCountry(targetCountry);
            if (matchedCountry != null)
            {
                int cIdx = Country.AllCountries.IndexOf(matchedCountry);
                if (cIdx >= 0 && CmbCountry.SelectedIndex != cIdx)
                {
                    CmbCountry.SelectedIndex = cIdx;
                }
            }

            PopulateWilayas(_config.CountryEn);
        }
        finally
        {
            _suppressCountryEvent = false;
            _suppressWilayaEvent = false;
        }
    }

    private void ChkAdhanEnable_Changed(object sender, RoutedEventArgs e)
    {
        _config.AdhanEnabled = ChkAdhanEnable.IsChecked == true;
        _config.Save();
    }

    private void BtnPickAdhan_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = LocalizationService.Get("pick_mp3", _config.Language),
            Filter = "MP3 Audio (*.mp3)|*.mp3|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            _config.AdhanFile = dlg.FileName;
            _config.Save();
            TxtAdhanFile.Text = Path.GetFileName(dlg.FileName);
            TxtAdhanFile.Foreground = _theme.TextMuted;
        }
    }

    private void BtnTestAdhan_Click(object sender, RoutedEventArgs e)
    {
        if (_audioService.IsPlaying && _audioService.IsTestPlayback &&
            string.Equals(_audioService.CurrentPlayingPath, _config.AdhanFile, StringComparison.OrdinalIgnoreCase))
        {
            _audioService.Stop();
            RefreshAudioTestButtons();
            return;
        }

        string file = _config.AdhanFile;
        if (string.IsNullOrEmpty(file) || !File.Exists(file))
        {
            file = AudioService.ResolveAudioFile("", "adhan.mp3");
            if (!string.IsNullOrEmpty(file))
            {
                _config.AdhanFile = file;
                _config.Save();
                TxtAdhanFile.Text = Path.GetFileName(file);
            }
        }

        if (string.IsNullOrEmpty(file) || !File.Exists(file))
        {
            MessageBox.Show(LocalizationService.Get("audio_missing_title", _config.Language),
                "File not found: " + (file ?? "(none)"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _audioService.Play(file, _config.Volume, _config.Muted, true);
        RefreshAudioTestButtons();
    }

    private void ChkReminderEnable_Changed(object sender, RoutedEventArgs e)
    {
        _config.ReminderEnabled = ChkReminderEnable.IsChecked == true;
        _config.Save();
    }

    private void TxtReminderMinutes_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtReminderMinutes.Text.Trim(), out int mins))
        {
            mins = Math.Clamp(mins, 1, 60);
            _config.ReminderMinutes = mins;
            TxtReminderMinutes.Text = mins.ToString();
            _config.Save();
        }
    }

    private void BtnPickReminder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = LocalizationService.Get("pick_mp3", _config.Language),
            Filter = "MP3 Audio (*.mp3)|*.mp3|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            _config.ReminderFile = dlg.FileName;
            _config.Save();
            TxtReminderFile.Text = Path.GetFileName(dlg.FileName);
            TxtReminderFile.Foreground = _theme.TextMuted;
        }
    }

    private void BtnTestReminder_Click(object sender, RoutedEventArgs e)
    {
        if (_audioService.IsPlaying && _audioService.IsTestPlayback &&
            string.Equals(_audioService.CurrentPlayingPath, _config.ReminderFile, StringComparison.OrdinalIgnoreCase))
        {
            _audioService.Stop();
            RefreshAudioTestButtons();
            return;
        }

        string file = _config.ReminderFile;
        if (string.IsNullOrEmpty(file) || !File.Exists(file))
        {
            file = AudioService.ResolveAudioFile("", "reminder.mp3");
            if (!string.IsNullOrEmpty(file))
            {
                _config.ReminderFile = file;
                _config.Save();
                TxtReminderFile.Text = Path.GetFileName(file);
            }
        }

        if (string.IsNullOrEmpty(file) || !File.Exists(file))
        {
            MessageBox.Show(LocalizationService.Get("audio_missing_title", _config.Language),
                "File not found: " + (file ?? "(none)"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _audioService.Play(file, _config.Volume, _config.Muted, true);
        RefreshAudioTestButtons();
    }

    private void ChkAdhanPopup_Changed(object sender, RoutedEventArgs e)
    {
        _config.AdhanPopupEnabled = ChkAdhanPopup.IsChecked == true;
        _config.Save();
    }

    private void ChkReminderPopup_Changed(object sender, RoutedEventArgs e)
    {
        _config.ReminderPopupEnabled = ChkReminderPopup.IsChecked == true;
        _config.Save();
    }

    private void ChkTaskbarWidgetEnable_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        bool isChecked = ChkTaskbarWidgetEnable.IsChecked == true;
        _config.TaskbarWidgetEnabled = isChecked;
        _config.Save();

        _trayService.UpdateTaskbarWidgetCheck(isChecked);

        if (isChecked)
        {
            _taskbarWidget?.Show();
            _taskbarWidget?.RepositionToTaskbar();
            UpdateBanner();
        }
        else
        {
            _taskbarWidget?.Hide();
        }
    }

    private void ChkTaskbarWidgetShowSeconds_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        _config.TaskbarWidgetShowSeconds = ChkTaskbarWidgetShowSeconds.IsChecked == true;
        _config.Save();
        UpdateBanner();
    }


    private void BtnResetTaskbarWidgetPos_Click(object sender, RoutedEventArgs e)
    {
        _config.TaskbarWidgetDockMode = "tray_left";
        _config.TaskbarWidgetPosX = -1;
        _config.TaskbarWidgetPosY = -1;
        _config.TaskbarWidgetMonitor = 0;
        _config.Save();
        _taskbarWidget?.AttachToTaskbar();
    }

    private void RbLang_Checked(object sender, RoutedEventArgs e)
    {
        if (RbLangAr.IsChecked == true && _config.Language != "ar")
        {
            _config.Language = "ar";
            _config.Save();
            ApplyLanguage();
            PopulateWilayas();
            LoadCurrentTuningInputs();
            RenderPrayerCards();
            UpdateBanner();
        }
        else if (RbLangEn.IsChecked == true && _config.Language != "en")
        {
            _config.Language = "en";
            _config.Save();
            ApplyLanguage();
            PopulateWilayas();
            LoadCurrentTuningInputs();
            RenderPrayerCards();
            UpdateBanner();
        }
    }

    private void RbTheme_Checked(object sender, RoutedEventArgs e)
    {
        if (RbThemeDark.IsChecked == true && _config.ThemeMode != "dark")
        {
            _config.ThemeMode = "dark";
            _config.Save();
            ApplyTheme();
        }
        else if (RbThemeLight.IsChecked == true && _config.ThemeMode != "light")
        {
            _config.ThemeMode = "light";
            _config.Save();
            ApplyTheme();
        }
    }

    private void ChkStartup_Changed(object sender, RoutedEventArgs e)
    {
        _config.StartWithWindows = ChkStartup.IsChecked == true;
        _config.Save();
        RegistryService.SyncStartup(_config.StartWithWindows, force: true);
    }

    private void LoadAppLogo()
    {
        try
        {
            // 1. Try pack URI for high-res logo PNG
            try
            {
                var packPng = new Uri("pack://application:,,,/Resources/logo.png", UriKind.Absolute);
                var streamInfo = Application.GetResourceStream(packPng);
                if (streamInfo != null)
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = streamInfo.Stream;
                    bmp.EndInit();
                    bmp.Freeze();
                    ImgHeaderLogo.Source = bmp;
                }
            }
            catch { }

            // 2. Fallback to disk files for PNG
            if (ImgHeaderLogo.Source == null)
            {
                string[] pngCandidates = new[]
                {
                    Path.Combine(AppConfig.AppDataFolder, "logo.png"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png")
                };

                foreach (var path in pngCandidates)
                {
                    if (File.Exists(path))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(path, UriKind.Absolute);
                        bmp.EndInit();
                        bmp.Freeze();
                        ImgHeaderLogo.Source = bmp;
                        break;
                    }
                }
            }

            // 3. Load System.Drawing.Icon for Win32 window icon (taskbar & titlebar)
            System.Drawing.Icon? drawingIcon = null;

            string[] icoCandidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.ico"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico"),
                Path.Combine(AppConfig.AppDataFolder, "logo.ico"),
                @"C:\Users\1\Desktop\logo.ico"
            };

            foreach (var path in icoCandidates)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        drawingIcon = new System.Drawing.Icon(path);
                        break;
                    }
                    catch { }
                }
            }

            if (drawingIcon == null)
            {
                try
                {
                    var packIco = new Uri("pack://application:,,,/Resources/logo.ico", UriKind.Absolute);
                    var streamInfo = Application.GetResourceStream(packIco);
                    if (streamInfo != null)
                    {
                        drawingIcon = new System.Drawing.Icon(streamInfo.Stream);
                    }
                }
                catch { }
            }

            if (drawingIcon == null)
            {
                try
                {
                    string? exePath = Environment.ProcessPath ?? System.Windows.Forms.Application.ExecutablePath;
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        drawingIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                    }
                }
                catch { }
            }

            if (drawingIcon != null)
            {
                DwmHelper.SetWindowIcon(this, drawingIcon);
            }

            // 4. Set WPF Window.Icon
            try
            {
                foreach (var path in icoCandidates)
                {
                    if (File.Exists(path))
                    {
                        using var stream = File.OpenRead(path);
                        var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        BitmapFrame? bestFrame = null;
                        int maxDim = 0;
                        foreach (var frame in decoder.Frames)
                        {
                            if (frame.PixelWidth > maxDim)
                            {
                                maxDim = frame.PixelWidth;
                                bestFrame = frame;
                            }
                        }
                        if (bestFrame != null)
                        {
                            Icon = bestFrame;
                            if (ImgHeaderLogo.Source == null)
                                ImgHeaderLogo.Source = bestFrame;
                            break;
                        }
                    }
                }
            }
            catch { }

            if (Icon == null)
            {
                try
                {
                    var packIco = new Uri("pack://application:,,,/Resources/logo.ico", UriKind.Absolute);
                    var streamInfo = Application.GetResourceStream(packIco);
                    if (streamInfo != null)
                    {
                        var decoder = new IconBitmapDecoder(streamInfo.Stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        BitmapFrame? bestFrame = null;
                        int maxDim = 0;
                        foreach (var frame in decoder.Frames)
                        {
                            if (frame.PixelWidth > maxDim)
                            {
                                maxDim = frame.PixelWidth;
                                bestFrame = frame;
                            }
                        }
                        if (bestFrame != null)
                        {
                            Icon = bestFrame;
                            if (ImgHeaderLogo.Source == null)
                                ImgHeaderLogo.Source = bestFrame;
                        }
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    #endregion
}