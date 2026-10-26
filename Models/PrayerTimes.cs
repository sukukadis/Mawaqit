using System;
using System.Collections.Generic;

namespace Mawaqit.Models;

public class PrayerItem
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string TimeString { get; set; } = "--:--";
    public DateTime Time { get; set; }
    public string Icon { get; set; } = "";
    public bool IsNext { get; set; }
    public bool IsAdhan { get; set; }
}

public class PrayerTimesData
{
    public string Date { get; set; } = "";
    public Dictionary<string, string> Timings { get; set; } = new();

    public static readonly string[] Order = { "Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha" };
    public static readonly string[] AdhanOrder = { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha" };

    public static readonly Dictionary<string, string> Icons = new()
    {
        { "Fajr", "🌅" },
        { "Sunrise", "☀️" },
        { "Dhuhr", "🌞" },
        { "Jumua", "🕌" },
        { "Asr", "🌤️" },
        { "Maghrib", "🌇" },
        { "Isha", "🌙" }
    };
}
