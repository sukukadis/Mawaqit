<p align="center">
  <img src="logo.png" width="120" height="120" alt="Mawaqit Logo" />
</p>

<h1 align="center">Mawaqit (مواقيت الصلاة) - C# / .NET 8 WPF Edition</h1>

<p align="center">
  <b>A modern, high-performance Windows desktop application for Islamic prayer times worldwide</b><br>
  Built with <b>C# (.NET 8 WPF)</b> • Windows 11 Fluent Design & Mica • Real-time Taskbar Countdown Widget • Adhan Audio & Notifications • Worldwide Coverage
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Version-v2.2.4-blue?logo=github" alt="Version: v2.2.4" />
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D4?logo=windows" alt="Platform: Windows" />
  <img src="https://img.shields.io/badge/Framework-.NET%208%20WPF-512BD4?logo=dotnet" alt="Framework: .NET 8 WPF" />
  <img src="https://img.shields.io/badge/Language-C%23%2012-239120?logo=csharp" alt="Language: C#" />
  <img src="https://img.shields.io/badge/Coverage-Worldwide%20(100%2B%20Countries)-10B981" alt="Coverage: Worldwide" />
  <img src="https://img.shields.io/badge/Locations-720%2B%20States%20%26%20Cities-0284C7" alt="Locations: 720+ States" />
  <img src="https://img.shields.io/badge/Methods-23%20Calculation%20Methods-8B5CF6" alt="Methods: 23" />
</p>

---

> [!NOTE]
> **🌍 Worldwide Coverage & Support / دعم كامل لجميع دول ومدن العالم:**
> - **English:** Mawaqit supports **accurate Islamic prayer times worldwide across all countries and cities globally**. Select your country from over 100+ nations with comprehensive states, provinces, and governorates (720+ offline locations) sorted alphabetically for instant search, find any location using the online worldwide city search dialog, or click "Auto-Detect My Location" via IP geolocation. The app supports 23 official international calculation methods (including Umm Al-Qura, Algerian Ministry, Egyptian General Authority, MWL, ISNA, Karachi, Diyanet, Dubai, Kuwait, Qatar, France, Russia, Singapore, and more) plus custom per-city manual fine-tuning.
> - **العربية:** يدعم تطبيق مواقيت حساب مواقيت الصلاة بدقة فائقة **لجميع دول ومدن العالم دون استثناء**. يمكنك اختيار دولتك من بين أكثر من 100 دولة وقائمة شاملة تضم أكثر من 720 ولاية ومحافظة وإقليم مرتبة أبجدياً لسهولة البحث والوصول، أو البحث المباشر عن أي مدينة حول العالم بنافذة البحث المتطورة المتوافقة مع مظهر التطبيق، أو تحديد موقعك تلقائياً بنقرة واحدة عبر الـ IP. كما يدعم التطبيق 23 طريقة حساب إسلامية معتمدة دولياً (أم القرى، وزارة الشؤون الدينية الجزائرية، الهيئة المصرية العامة للمساحة، رابطة العالم الإسلامي، الاتحاد الإسلامي بأمريكا الشمالية، رئاسة الشؤون الدينية التركية، دائرة الشؤون الإسلامية بدبي، الكويت، قطر، فرنسا، وغيرها) مع إمكانية المعايرة والضبط اليدوي الدقيق لكل مدينة.

A high-performance, modern Windows desktop application for Islamic prayer times worldwide, made in **C# (.NET 8 WPF)**.

---

## 📸 Application Showcase

### 🖥️ Main Dashboard & Quick Glance Flyout
The main application dashboard provides an elegant view of all daily prayer times with live countdown to the next prayer, active prayer highlighting, weather integration, and per-city manual calibration. The quick taskbar flyout opens instantly from the tray with completed prayer checkmarks and Quranic verse.

<p align="center">
  <img src="screenshots/main_dashboard.jpg" width="48%" alt="Main Prayer Times Dashboard & Manual Tuning" />
  &nbsp;
  <img src="screenshots/taskbar_flyout.png" width="48%" alt="Taskbar Quick Flyout Window with Quranic Verse" />
</p>

### ⏱️ Real-Time Taskbar Countdown Widget
A dedicated, borderless Fluent widget sitting directly on your Windows taskbar next to the notification area. Always know how much time is left until the next prayer at a glance, with seconds precision, elapsed indicators, and seamless tray docking.

<p align="center">
  <img src="screenshots/taskbar_widget.png" width="85%" alt="Windows Taskbar Countdown Timer Widget" />
</p>

### ⚙️ Rich Customization & Settings
Tailor every aspect of the application: select from 100+ countries and 720+ states/provinces (arranged alphabetically), search any global city, select from 23 calculation methods, pick and test custom Adhan & Pre-prayer reminder `.mp3` audio files, customize notification timing, toggle taskbar widget options, switch between Dark/Light themes, and enable Windows auto-start.

<p align="center">
  <img src="screenshots/settings_audio_notifications.jpg" width="48%" alt="Location, Adhan & Notifications Settings" />
  &nbsp;
  <img src="screenshots/settings_theme_startup.jpg" width="48%" alt="Theme, Language & Windows Startup Settings" />
</p>

---

## 🌟 Key Features & Improvements

1. **Native Windows Typography & DirectWrite BiDi Rendering**:
   - Unlike Tkinter which required reversing Arabic words manually, WPF natively uses DirectWrite / Uniscribe and HarfBuzz text shaping.
   - Arabic characters connect smoothly and correctly with full cursive ligatures and native `FlowDirection="RightToLeft"` layout.
2. **Windows 11 Fluent Design & Mica / Glass**:
   - Native integration with Windows 11 Desktop Window Manager (DWM) for rounded corners (`DWMWCP_ROUND`), immersive dark mode (`DWMWA_USE_IMMERSIVE_DARK_MODE`), and system backdrops (`DWMWA_SYSTEMBACKDROP_TYPE`).
   - Emerald green accent (`#10B981`) highlighting the next prayer card with active live countdown.
3. **Worldwide Coverage (100+ Countries & 720+ States/Provinces)**:
   - Comprehensive offline database covering all Arab countries (Algeria, Saudi Arabia, Egypt, Morocco, UAE, Palestine, Syria, Iraq, Jordan, etc.) and global nations (USA, UK, Canada, France, Germany, Turkey, Indonesia, Malaysia, etc.).
   - Countries and cities/states are sorted alphabetically according to the active language (Arabic collation for Arabic, Ordinal for English) for rapid searching.
   - Built-in online geocoding city search that finds any city, municipality, or village worldwide matching the application theme.
   - One-click automatic location detection via IP.
4. **23 Official International Calculation Methods**:
   - Algerian Ministry of Religious Affairs, Umm Al-Qura (Makkah), Egyptian General Authority of Survey, Muslim World League (MWL), Islamic Society of North America (ISNA), University of Islamic Sciences (Karachi), Diyanet İşleri Başkanlığı (Turkey), Dubai (IACAD), Kuwait, Qatar, France (UOIF), Russia (SAM), Singapore (MUIS), and more.
   - Automatically pre-selects the recognized local calculation method for each country upon selection.
   - Friday rule: On Friday, Dhuhr is automatically displayed as **Jumu'ah** (الجمعة).
5. **Manual Time Tuning per Location**:
   - Adjust +/- 60 minutes for Fajr, Dhuhr, Asr, Maghrib, and Isha directly on the Prayer Times tab.
   - Changes are saved per location in `%APPDATA%\Mawaqit\config.json` and immediately update the countdown and cards.
6. **Custom Audio & Notifications**:
   - Play Adhan at exact prayer time.
   - Pre-prayer reminder (1–60 minutes before Adhan).
   - Custom floating Toast Notifications (with auto-dismiss or stay open during Adhan).
   - Audio file picker with interactive **Test (▶) / Stop (⏹)** toggle.
   - Self-healing audio path resolver that locates MP3s across AppData, application folders, or user paths.
7. **Taskbar Timer Countdown Widget (Awqat Salaat Style)**:
   - Dedicated, borderless Fluent widget directly on the Windows Taskbar showing real-time ticking countdown to the next prayer.
   - Sits docked seamlessly beside the system tray (with DPI awareness for 100%–200% scaling).
   - Can be freely dragged across the taskbar or desktop with auto-saved custom coordinates.
   - Shows prayer icon, name, prayer time, and countdown (`-01:23:45`).
   - During active prayer / Adhan window (0–20 mins), displays an elapsed indicator (`+00:04:12`) with pulsing amber/emerald badge.
   - Left-click toggles the main window; right-click context menu provides quick controls (Dock next to tray, toggle seconds, toggle prayer time, compact view, Settings, hide).
8. **Fluent / Mica Taskbar Flyout**:
   - One-click popup directly from the tray with next prayer highlight, completed prayers checkmarks, current weather, and daily Quranic verse.
9. **System Tray Integration**:
   - Lives in the system notification tray with live tooltip: `Mawaqit — Fajr left 6:26:45`.
   - Context Menu: Show App, Live Countdown, Volume submenu (0%, 25%, 50%, 75%, 100%, Custom slider), Mute/Unmute, About dialog with Quranic verse, and Exit.
10. **Single Instance Guard & IPC**:
    - Named Win32 Mutex `Global\Mawaqit_SingleInstance_Mutex_v4` and Event `Global\Mawaqit_ShowEvent_v4`.
    - When a second instance launches, it signals the existing instance to bring its window to the foreground and exits immediately with code 0.
11. **Weather & Live Temperature**:
    - Live temperature from Open-Meteo API for the selected location's coordinates, updated every 45 minutes with color coding (<5°C, <15°C, <25°C, <35°C, >=35°C).
12. **Gregorian & Hijri Calendars**:
    - Dual calendar display using `System.Globalization.UmAlQuraCalendar`.

---

## 🚀 Command-Line Flags

| Flag | Description |
|---|---|
| `--startup` | Launches silently minimized to the Windows system tray. |
| `--uninstall` | Terminates running instances and removes registry keys (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`). |
| `--enable-startup` | Enables Windows auto-start in registry. |
| `--disable-startup` | Disables Windows auto-start in registry. |

---

## 🛠️ Project Architecture

```
Mawaqit/
├── Mawaqit.csproj                    # Project file (net8.0-windows, WPF + NotifyIcon)
├── App.xaml / App.xaml.cs            # Entry point, single-instance guard, CLI handling
├── MainWindow.xaml / .xaml.cs        # Main dashboard, tabs, prayer cards, countdown, tuning
├── GlobalUsings.cs                   # Clean global namespaces
├── screenshots/                      # Application preview screenshots
│   ├── main_dashboard.jpg            # Main prayer dashboard & tuning view
│   ├── taskbar_flyout.png            # Fluent / Mica taskbar quick flyout
│   ├── taskbar_widget.png            # Windows taskbar live countdown widget
│   ├── settings_audio_notifications.jpg # Audio & notification settings
│   └── settings_theme_startup.jpg    # Theme, language & startup settings
├── Models/
│   ├── Country.cs                    # 100+ countries with codes, aliases, and localized names
│   ├── Wilaya.cs                     # 720+ locations database (Arab & global) with search filter
│   ├── CalculationMethod.cs          # 23 international prayer calculation methods
│   ├── PrayerTimes.cs                # Prayer models, order, and icons
│   ├── AppConfig.cs                  # JSON configuration schema & persistence
│   └── ThemeColors.cs                # Dark & Light palettes (#10B981 emerald accent)
├── Services/
│   ├── Logger.cs                     # startup.log and error.log logger
│   ├── SingleInstanceService.cs      # Win32 Mutex & ShowEvent IPC
│   ├── RegistryService.cs            # Windows Startup registry manager
│   ├── LocalizationService.cs        # Arabic & English text resources
│   ├── HijriDateService.cs           # UmAlQura calendar conversion
│   ├── WeatherService.cs             # Open-Meteo temperature API
│   ├── PrayerTimesService.cs         # AlAdhan API, tuning & offline caching
│   ├── AudioService.cs               # WPF MediaPlayer audio manager
│   ├── TrayService.cs                # System tray icon, context menu & volume popups
│   └── DwmHelper.cs                  # Windows 11 DWM glass & dark mode styling
├── Controls/
│   ├── AboutWindow.xaml / .cs        # Quranic verse [An-Nisa: 103] & author modal
│   ├── TaskbarFlyoutWindow.xaml / .cs# Fluent taskbar quick glance flyout
│   ├── TaskbarWidgetWindow.xaml / .cs# Taskbar timer countdown widget (Awqat Salaat WinUI style)
│   ├── ToastNotificationWindow.xaml  # Floating corner toast notification
│   └── VolumePopupWindow.xaml       # Slim tray volume slider
└── Resources/
    ├── logo.ico                      # Application icon
    ├── logo.png                      # Application logo
    └── Media/
        ├── adhan.mp3                 # Adhan audio file
        └── reminder.mp3              # Pre-prayer reminder audio file
```

---

## 🔨 How to Build and Run

### Run from Source:
```powershell
dotnet run --project C:\Users\1\.gemini\antigravity-ide\scratch\Mawaqit
```

### Publish Single-File Executable:
```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o C:\Users\1\.gemini\antigravity-ide\scratch\Mawaqit\publish
```

The resulting standalone executable is located at:
`C:\Users\1\.gemini\antigravity-ide\scratch\Mawaqit\publish\Mawaqit.exe`

### Build Installer (Setup.exe):

You can build the installer with one command or double-click:

1. **One-Click Build (Batch File)**:
   Double-click `build_installer.bat` in the project root.

2. **PowerShell Script**:
   ```powershell
   # Standard lightweight installer (~6 MB, requires .NET 8 Desktop Runtime)
   .\build_installer.ps1

   # Standalone installer (~67 MB, includes .NET 8 runtime, works on any PC)
   .\build_installer.ps1 -Standalone

   # Build both installers at once
   .\build_installer.ps1 -Both
   ```

3. **Direct Inno Setup Compiler**:
   ```powershell
   & "C:\Program Files\Inno Setup 7\ISCC.exe" "MawaqitSetup.iss"
   ```

The compiled installers are saved in:
- `installer_output\Mawaqit-2.2.4-Setup.exe` (Standard)
- `installer_output\Mawaqit-2.2.4-Standalone-Setup.exe` (Standalone)
