using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mawaqit.Models;

public class AppConfig
{
    [JsonPropertyName("config_version")]
    public int ConfigVersion { get; set; } = 0;

    [JsonPropertyName("wilaya_ar")]
    public string WilayaAr { get; set; } = "الجزائر";

    [JsonPropertyName("wilaya_en")]
    public string WilayaEn { get; set; } = "Alger";

    [JsonPropertyName("country_ar")]
    public string CountryAr { get; set; } = "الجزائر";

    [JsonPropertyName("country_en")]
    public string CountryEn { get; set; } = "Algeria";

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; } = 36.7538259;

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; } = 3.057841;

    [JsonPropertyName("calculation_method")]
    public int CalculationMethod { get; set; } = 19;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "ar";

    [JsonPropertyName("adhan_enabled")]
    public bool AdhanEnabled { get; set; } = false;

    [JsonPropertyName("adhan_file")]
    public string AdhanFile { get; set; } = "";

    [JsonIgnore]
    public bool UseDedicatedFajrAdhan { get; set; } = false;

    [JsonIgnore]
    public string FajrAdhanFile { get; set; } = "";

    [JsonPropertyName("prayer_adhan_enabled")]
    public Dictionary<string, bool> PrayerAdhanEnabled { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Fajr"] = true,
        ["Dhuhr"] = true,
        ["Asr"] = true,
        ["Maghrib"] = true,
        ["Isha"] = true
    };

    [JsonPropertyName("prayer_reminder_enabled")]
    public Dictionary<string, bool> PrayerReminderEnabled { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Fajr"] = true,
        ["Dhuhr"] = true,
        ["Asr"] = true,
        ["Maghrib"] = true,
        ["Isha"] = true
    };

    [JsonPropertyName("audio_fade_in")]
    public bool AudioFadeIn { get; set; } = true;

    [JsonPropertyName("hijri_offset_days")]
    public int HijriOffsetDays { get; set; } = 0;

    [JsonIgnore]
    public bool FridayReminderEnabled { get; set; } = false;

    [JsonPropertyName("reminder_enabled")]
    public bool ReminderEnabled { get; set; } = false;

    [JsonPropertyName("reminder_file")]
    public string ReminderFile { get; set; } = "";

    [JsonPropertyName("reminder_minutes")]
    public int ReminderMinutes { get; set; } = 10;

    [JsonPropertyName("start_with_windows")]
    public bool StartWithWindows { get; set; } = true;

    [JsonPropertyName("theme_mode")]
    public string ThemeMode { get; set; } = "system";

    [JsonPropertyName("muted")]
    public bool Muted { get; set; } = false;

    [JsonPropertyName("volume")]
    public double Volume { get; set; } = 0.33;

    [JsonPropertyName("adhan_popup_enabled")]
    public bool AdhanPopupEnabled { get; set; } = true;

    [JsonPropertyName("reminder_popup_enabled")]
    public bool ReminderPopupEnabled { get; set; } = true;


    [JsonPropertyName("taskbar_widget_enabled")]
    public bool TaskbarWidgetEnabled { get; set; } = true;

    [JsonPropertyName("taskbar_widget_show_seconds")]
    public bool TaskbarWidgetShowSeconds { get; set; } = true;

    [JsonPropertyName("taskbar_widget_show_time")]
    public bool TaskbarWidgetShowTime { get; set; } = true;

    [JsonPropertyName("taskbar_widget_compact")]
    public bool TaskbarWidgetCompact { get; set; } = false;

    [JsonPropertyName("taskbar_widget_dock_mode")]
    public string TaskbarWidgetDockMode { get; set; } = "tray_left";

    [JsonPropertyName("taskbar_widget_pos_x")]
    public double TaskbarWidgetPosX { get; set; } = -1;

    [JsonPropertyName("taskbar_widget_pos_y")]
    public double TaskbarWidgetPosY { get; set; } = -1;

    [JsonPropertyName("taskbar_widget_monitor")]
    public int TaskbarWidgetMonitor { get; set; } = 0;

    [JsonPropertyName("tune_by_wilaya")]
    public Dictionary<string, Dictionary<string, int>> TuneByWilaya { get; set; } = new();

    public static string AppDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mawaqit");

    public static string ConfigPath => Path.Combine(AppDataFolder, "config.json");
    public static string CachePath => Path.Combine(AppDataFolder, "last_timings.json");
    public static string CacheRawPath => Path.Combine(AppDataFolder, "last_timings_raw.json");
    public static string ErrorLogPath => Path.Combine(AppDataFolder, "error.log");
    public static string StartupLogPath => Path.Combine(AppDataFolder, "startup.log");
    public static string MediaFolder => Path.Combine(AppDataFolder, "Media");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (config != null)
                {
                    config.TuneByWilaya ??= new();
                    config.PrayerAdhanEnabled ??= new(StringComparer.OrdinalIgnoreCase);
                    config.PrayerReminderEnabled ??= new(StringComparer.OrdinalIgnoreCase);
                    config.EnsureCoordinates();
                    if (config.ConfigVersion < 2)
                    {
                        config.AdhanEnabled = false;
                        config.ReminderEnabled = false;
                        config.ThemeMode = "system";
                        config.PrayerAdhanEnabled ??= new(StringComparer.OrdinalIgnoreCase);
                        foreach (var p in new[] { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha" })
                        {
                            config.PrayerAdhanEnabled[p] = true;
                        }
                        config.ConfigVersion = 2;
                        config.Save();
                    }
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            Services.Logger.LogError($"Failed to load config: {ex.Message}");
        }

        var def = new AppConfig { ConfigVersion = 2 };
        def.EnsureCoordinates();
        return def;
    }

    public void EnsureCoordinates()
    {
        if (Latitude == 0 && Longitude == 0)
        {
            var found = Wilaya.FindLocation(WilayaAr, WilayaEn);
            if (found != null)
            {
                Latitude = found.Latitude;
                Longitude = found.Longitude;
                CountryAr = found.CountryAr;
                CountryEn = found.CountryEn;
                if (CalculationMethod <= 0 || (CalculationMethod == 19 && found.CountryEn != "Algeria"))
                {
                    CalculationMethod = found.DefaultMethod;
                }
            }
            else
            {
                Latitude = 36.7538259;
                Longitude = 3.057841;
                CountryAr = "الجزائر";
                CountryEn = "Algeria";
                if (CalculationMethod <= 0) CalculationMethod = 19;
            }
        }
        else if (CalculationMethod <= 0)
        {
            CalculationMethod = 19;
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppDataFolder);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            Services.Logger.LogError($"Failed to save config: {ex.Message}");
        }
    }

    public bool IsPrayerSpecificAdhanEnabled(string prayer) =>
        !PrayerAdhanEnabled.TryGetValue(prayer, out bool v) || v;

    public bool IsAdhanEnabledFor(string prayer) =>
        AdhanEnabled && IsPrayerSpecificAdhanEnabled(prayer);

    private bool _legacyIsAdhan(string prayer) =>
        AdhanEnabled && (!PrayerAdhanEnabled.TryGetValue(prayer, out bool v) || v);

    public bool IsReminderEnabledFor(string prayer) =>
        ReminderEnabled && (!PrayerReminderEnabled.TryGetValue(prayer, out bool v) || v);

    public int GetTune(string wilaya, string prayerKey)
    {
        if (TuneByWilaya.TryGetValue(wilaya, out var tunes) && tunes.TryGetValue(prayerKey.ToLowerInvariant(), out int val))
        {
            return val;
        }
        return 0;
    }

    public void SetTune(string wilaya, string prayerKey, int minutes)
    {
        if (!TuneByWilaya.TryGetValue(wilaya, out var tunes))
        {
            tunes = new Dictionary<string, int>();
            TuneByWilaya[wilaya] = tunes;
        }
        tunes[prayerKey.ToLowerInvariant()] = minutes;
    }
}
