using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Mawaqit.Models;
using Mawaqit.Services;

namespace Mawaqit.Controls;

public partial class TaskbarFlyoutWindow : Window
{
    #region Win32 ToolWindow

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    #endregion

    private readonly AppConfig _config;
    private readonly Action _onOpenSettings;
    private readonly Func<Task> _onRefresh;
    private readonly Action _onOpenAbout;
    private readonly Action _onDismissAudio;

    private bool _isClosing;
    private ThemePalette? _currentTheme;
    private bool _isDarkTheme = true;
    private DateTime _openedTime = DateTime.MinValue;
    private DateTime _closedTime = DateTime.MinValue;
    public DateTime LastClosedTime => _closedTime;

    public TaskbarFlyoutWindow(
        AppConfig config,
        Action onOpenSettings,
        Func<Task> onRefresh,
        Action onOpenAbout,
        Action onDismissAudio)
    {
        _config = config;
        _onOpenSettings = onOpenSettings;
        _onRefresh = onRefresh;
        _onOpenAbout = onOpenAbout;
        _onDismissAudio = onDismissAudio;

        InitializeComponent();
        LoadLogo();

        ApplyLanguage(_config.Language);

        Deactivated += (s, e) =>
        {
            if (_isClosing) return;
            // Grace period: ignore deactivate events fired during window opening transition
            if ((DateTime.UtcNow - _openedTime).TotalMilliseconds < 500)
            {
                return;
            }

            _closedTime = DateTime.UtcNow;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsLoaded && IsVisible)
                {
                    Hide();
                }
            }), DispatcherPriority.Background);
        };
    }

    public void ApplyLanguage(string lang)
    {
        bool isAr = lang == "ar";
        FlowDirection = isAr ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;
        TxtAppTitle.Text = isAr ? "مواقيت" : "Mawaqit";
        BtnSettings.ToolTip = isAr ? "الإعدادات" : "Settings";
        BtnRefresh.ToolTip = isAr ? "تحديث" : "Refresh";
        BtnAbout.ToolTip = isAr ? "حول البرنامج" : "About";
    }

    public void ApplyTheme(ThemePalette theme, bool isDark)
    {
        _currentTheme = theme;
        _isDarkTheme = isDark;

        FlyoutBorder.Background = theme.Card;
        FlyoutBorder.BorderBrush = theme.Border;
        FlyoutShadow.Opacity = isDark ? 0.45 : 0.18;

        TxtAppTitle.Foreground = theme.Accent;
        TxtBtnSettings.Foreground = theme.TextMuted;
        TxtBtnRefresh.Foreground = theme.TextMuted;
        TxtBtnAbout.Foreground = theme.TextMuted;

        TxtDayOfWeek.Foreground = theme.TextMuted;
        TxtHijriDate.Foreground = theme.Text;
        TxtLocation.Foreground = theme.TextMuted;
        TxtWeatherDot.Foreground = theme.TextMuted;
        TxtLocationWeather.Foreground = theme.TextMuted;

        CardActivePrayer.Background = theme.Accent;
        IconActive.Foreground = theme.AccentText;
        NameActive.Foreground = theme.AccentText;
        TimeActive.Foreground = theme.AccentText;
        BtnDismiss.Foreground = theme.AccentText;

        VerseBorder.Background = theme.CardAlt;
        VerseBorder.BorderBrush = theme.Border;
        TxtVerse.Foreground = theme.Text;
        TxtCitation.Foreground = theme.TextMuted;
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
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
            }
        }
        catch { }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public void ShowAboveWidget(double widgetLeft, double widgetTop, double widgetWidth, double widgetHeight)
    {
        UpdateLayout();
        Measure(new System.Windows.Size(388, double.PositiveInfinity));
        double flyoutWidth = ActualWidth > 100 ? ActualWidth : (DesiredSize.Width > 100 ? DesiredSize.Width : 388);
        double flyoutHeight = DesiredSize.Height > 100 ? DesiredSize.Height : (ActualHeight > 100 ? ActualHeight : 480);

        // Center flyout horizontally above the countdown widget so the countdown is in the center under the widget menu
        double targetLeft = widgetLeft + (widgetWidth / 2.0) - (flyoutWidth / 2.0);

        try
        {
            var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
            double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

            int physicalX = (int)Math.Round((widgetLeft + (widgetWidth / 2.0)) * dpiX);
            int physicalY = (int)Math.Round((widgetTop + (widgetHeight / 2.0)) * dpiY);

            var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(physicalX, physicalY));

            double workLeft = screen.WorkingArea.Left / dpiX;
            double workTop = screen.WorkingArea.Top / dpiY;
            double workWidth = screen.WorkingArea.Width / dpiX;
            double workHeight = screen.WorkingArea.Height / dpiY;

            if (targetLeft + flyoutWidth > workLeft + workWidth - 8)
            {
                targetLeft = workLeft + workWidth - flyoutWidth - 8;
            }
            if (targetLeft < workLeft + 8)
            {
                targetLeft = workLeft + 8;
            }

            bool isTopTaskbar = widgetTop < (workTop + (workHeight / 2.0));
            double targetTop;
            if (isTopTaskbar)
            {
                // Top taskbar: flyout opens below it
                targetTop = widgetTop + widgetHeight + 6;
                if (targetTop + flyoutHeight > workTop + workHeight - 8)
                {
                    targetTop = workTop + workHeight - flyoutHeight - 8;
                }
            }
            else
            {
                // Bottom taskbar (default Windows 10 & 11): flyout opens ABOVE it
                targetTop = widgetTop - flyoutHeight - 6;
                // Never push below taskbar offscreen! Clamp to top of work area:
                if (targetTop < workTop + 8)
                {
                    targetTop = workTop + 8;
                }
            }

            Left = targetLeft;
            Top = targetTop;
        }
        catch
        {
            double screenW = SystemParameters.VirtualScreenWidth;
            if (targetLeft + flyoutWidth > screenW - 10)
            {
                targetLeft = screenW - flyoutWidth - 10;
            }
            if (targetLeft < 10) targetLeft = 10;

            double targetTop = Math.Max(10, widgetTop - flyoutHeight - 6);
            Left = targetLeft;
            Top = targetTop;
        }

        _openedTime = DateTime.UtcNow;
        Topmost = true;
        Show();
        Activate();
        Focus();

        try
        {
            var helper = new WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                SetForegroundWindow(helper.Handle);
            }
        }
        catch { }
    }

    public void UpdateData(
        Dictionary<string, string> prayerData,
        Dictionary<string, DateTime> prayerDateTimes,
        string nextPrayer,
        DateTime now,
        string weatherString,
        bool isAudioPlaying,
        WeatherResult? weatherResult = null)
    {
        string lang = _config.Language;
        bool isAr = lang == "ar";

        FlowDirection = isAr ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;
        TxtAppTitle.Text = isAr ? "مواقيت" : "Mawaqit";

        // Day of Week
        int dayOfWeekIndex = ((int)now.DayOfWeek + 6) % 7;
        TxtDayOfWeek.Text = isAr
            ? LocalizationService.ArabicDays[dayOfWeekIndex]
            : LocalizationService.EnglishDays[dayOfWeekIndex];

        // Hijri Date
        TxtHijriDate.Text = HijriDateService.FormatHijriDate(now, lang);

        // Location & Colorful Windows 11 Weather
        string wilaya = isAr ? _config.WilayaAr : _config.WilayaEn;
        string country = isAr ? "الجزائر" : "Algeria";
        TxtLocation.Text = $"{wilaya} — {country}";

        if (weatherResult != null && weatherResult.Temperature.HasValue)
        {
            TxtWeatherDot.Visibility = Visibility.Visible;
            ImgFlyoutWeather.Visibility = Visibility.Visible;
            ImgFlyoutWeather.Source = weatherResult.IconDrawing;
            TxtFlyoutTemp.Visibility = Visibility.Visible;
            TxtFlyoutTemp.Text = $"{weatherResult.Temperature.Value:0}°C";
            TxtFlyoutTemp.Foreground = weatherResult.TextColor;
            string weatherTip = $"{weatherResult.GetConditionText(lang)} • {weatherResult.Temperature.Value:0}°C";
            TxtFlyoutTemp.ToolTip = weatherTip;
            ImgFlyoutWeather.ToolTip = weatherTip;
        }
        else if (!string.IsNullOrEmpty(weatherString))
        {
            TxtWeatherDot.Visibility = Visibility.Visible;
            TxtFlyoutTemp.Visibility = Visibility.Visible;
            TxtFlyoutTemp.Text = weatherString;
            ImgFlyoutWeather.Visibility = Visibility.Collapsed;
        }
        else
        {
            TxtWeatherDot.Visibility = Visibility.Collapsed;
            ImgFlyoutWeather.Visibility = Visibility.Collapsed;
            TxtFlyoutTemp.Visibility = Visibility.Collapsed;
        }

        BtnDismiss.Content = isAr ? "إيقاف" : "Dismiss";
        BtnDismiss.Visibility = isAudioPlaying ? Visibility.Visible : Visibility.Collapsed;

        // Render each prayer
        string[] prayerKeys = { "Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha" };

        foreach (var key in prayerKeys)
        {
            string timeStr = prayerData.TryGetValue(key, out var t) ? t : "--:--";
            string displayName = GetPrayerDisplayName(key, now, lang);

            bool isNextOrActive = string.Equals(key, nextPrayer, StringComparison.OrdinalIgnoreCase);
            bool isPassed = prayerDateTimes.TryGetValue(key, out var dt) && dt <= now && !isNextOrActive;

            if (isNextOrActive)
            {
                // Place into the active highlighted banner
                NameActive.Text = displayName;
                TimeActive.Text = timeStr;
                IconActive.Text = "🕒";
                SetRowVisibility(key, false);
            }
            else
            {
                SetRowVisibility(key, true);
                SetRowData(key, displayName, timeStr, isPassed);
            }
        }
    }

    private string GetPrayerDisplayName(string key, DateTime now, string lang)
    {
        if (key == "Dhuhr" && now.DayOfWeek == DayOfWeek.Friday)
        {
            return lang == "ar" ? "الجمعة" : "Jumu'ah";
        }
        if (key == "Sunrise")
        {
            return lang == "ar" ? "الشروق" : "Shuruq";
        }
        return LocalizationService.Get(key, lang);
    }

    private void SetRowVisibility(string key, bool visible)
    {
        var vis = visible ? Visibility.Visible : Visibility.Collapsed;
        switch (key)
        {
            case "Fajr": RowFajr.Visibility = vis; break;
            case "Sunrise": RowShuruq.Visibility = vis; break;
            case "Dhuhr": RowDhuhr.Visibility = vis; break;
            case "Asr": break;
            case "Maghrib": RowMaghrib.Visibility = vis; break;
            case "Isha": RowIsha.Visibility = vis; break;
        }
    }

    private void SetRowData(string key, string name, string time, bool isPassed)
    {
        string icon = isPassed ? "✓" : "🕒";
        var textColor = isPassed 
            ? (_currentTheme?.TextMuted ?? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#94A3B8")!) 
            : (_currentTheme?.Text ?? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#FFFFFF")!);

        switch (key)
        {
            case "Fajr":
                IconFajr.Text = icon;
                IconFajr.Foreground = textColor;
                NameFajr.Text = name;
                NameFajr.Foreground = textColor;
                TimeFajr.Text = time;
                TimeFajr.Foreground = textColor;
                break;
            case "Sunrise":
                IconShuruq.Text = icon;
                IconShuruq.Foreground = textColor;
                NameShuruq.Text = name;
                NameShuruq.Foreground = textColor;
                TimeShuruq.Text = time;
                TimeShuruq.Foreground = textColor;
                break;
            case "Dhuhr":
                IconDhuhr.Text = icon;
                IconDhuhr.Foreground = textColor;
                NameDhuhr.Text = name;
                NameDhuhr.Foreground = textColor;
                TimeDhuhr.Text = time;
                TimeDhuhr.Foreground = textColor;
                break;
            case "Maghrib":
                IconMaghrib.Text = icon;
                IconMaghrib.Foreground = textColor;
                NameMaghrib.Text = name;
                NameMaghrib.Foreground = textColor;
                TimeMaghrib.Text = time;
                TimeMaghrib.Foreground = textColor;
                break;
            case "Isha":
                IconIsha.Text = icon;
                IconIsha.Foreground = textColor;
                NameIsha.Text = name;
                NameIsha.Foreground = textColor;
                TimeIsha.Text = time;
                TimeIsha.Foreground = textColor;
                break;
        }
    }

    #region Action Buttons

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        _onOpenSettings();
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _onRefresh();
        }
        catch { }
    }

    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        _onOpenAbout();
    }

    private void BtnDismiss_Click(object sender, RoutedEventArgs e)
    {
        _onDismissAudio();
        BtnDismiss.Visibility = Visibility.Collapsed;
    }

    #endregion

    private void LoadLogo()
    {
        try
        {
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
                    ImgFlyoutLogo.Source = bmp;
                    return;
                }
            }
            catch { }

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
                    ImgFlyoutLogo.Source = bmp;
                    return;
                }
            }

            string icoPath = Path.Combine(AppConfig.AppDataFolder, "logo.ico");
            if (!File.Exists(icoPath))
                icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.ico");

            if (File.Exists(icoPath))
            {
                using var stream = File.OpenRead(icoPath);
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
                    ImgFlyoutLogo.Source = bestFrame;
                }
            }
        }
        catch { }
    }

    public void SafeClose()
    {
        _isClosing = true;
        try
        {
            Close();
        }
        catch { }
    }
}
