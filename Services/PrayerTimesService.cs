using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Mawaqit.Models;

namespace Mawaqit.Services;

public class CachedTimingsPayload
{
    public string Date { get; set; } = "";
    public Dictionary<string, string> Timings { get; set; } = new();
}

public class PrayerTimesService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public const int DefaultCalculationMethod = 19; // Algerian Ministry of Religious Affairs

    public static async Task<(Dictionary<string, string>? RawTimings, string? Error)> FetchTimingsFromApiAsync(double lat, double lng, int calculationMethod = 19)
    {
        int method = calculationMethod > 0 ? calculationMethod : DefaultCalculationMethod;
        string latStr = lat.ToString(CultureInfo.InvariantCulture);
        string lngStr = lng.ToString(CultureInfo.InvariantCulture);

        var endpoints = new[]
        {
            $"https://api.aladhan.com/v1/timings?latitude={latStr}&longitude={lngStr}&method={method}&school=0",
            $"http://api.aladhan.com/v1/timings?latitude={latStr}&longitude={lngStr}&method={method}&school=0",
            $"https://api.aladhan.com/v1/calendar?latitude={latStr}&longitude={lngStr}&method={method}&school=0&month={DateTime.Now.Month}&year={DateTime.Now.Year}"
        };

        string lastError = "";

        foreach (var url in endpoints)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mawaqit/2.2.6");

                var response = await Http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    lastError = $"HTTP {response.StatusCode}";
                    continue;
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var code) && code.GetInt32() != 200)
                {
                    lastError = "Response code not 200";
                    continue;
                }

                JsonElement timingsElement;
                if (url.Contains("/calendar"))
                {
                    string todayStr = DateTime.Now.ToString("dd-MM-yyyy");
                    if (!root.TryGetProperty("data", out var dataArr) || dataArr.ValueKind != JsonValueKind.Array)
                        continue;

                    bool foundToday = false;
                    timingsElement = default;

                    foreach (var dayObj in dataArr.EnumerateArray())
                    {
                        if (dayObj.TryGetProperty("date", out var dateObj) &&
                            dateObj.TryGetProperty("gregorian", out var gregObj) &&
                            gregObj.TryGetProperty("date", out var dateVal) &&
                            dateVal.GetString() == todayStr)
                        {
                            if (dayObj.TryGetProperty("timings", out timingsElement))
                            {
                                foundToday = true;
                                CalibrateHijri(dateObj);
                                break;
                            }
                        }
                    }

                    if (!foundToday) continue;
                }
                else
                {
                    if (!root.TryGetProperty("data", out var dataObj) ||
                        !dataObj.TryGetProperty("timings", out timingsElement))
                    {
                        continue;
                    }

                    if (dataObj.TryGetProperty("date", out var dateObj))
                    {
                        CalibrateHijri(dateObj);
                    }
                }

                var dict = new Dictionary<string, string>();
                foreach (var prayer in PrayerTimesData.Order)
                {
                    if (timingsElement.TryGetProperty(prayer, out var val))
                    {
                        string? rawStr = val.GetString();
                        if (rawStr != null)
                        {
                            dict[prayer] = NormalizeTime(rawStr);
                        }
                    }
                }

                if (PrayerTimesData.Order.All(dict.ContainsKey))
                {
                    return (dict, null);
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }
        }

        return (null, lastError);
    }

    public static void CalibrateHijri(JsonElement dateObj)
    {
        try
        {
            if (dateObj.TryGetProperty("hijri", out var hijriObj) &&
                hijriObj.TryGetProperty("day", out var dayVal) &&
                int.TryParse(dayVal.GetString(), out int apiDay))
            {
                int apiMonth = 0;
                if (hijriObj.TryGetProperty("month", out var monthObj) &&
                    monthObj.TryGetProperty("number", out var numVal))
                {
                    if (numVal.ValueKind == JsonValueKind.Number) apiMonth = numVal.GetInt32();
                    else if (numVal.ValueKind == JsonValueKind.String && int.TryParse(numVal.GetString(), out int m)) apiMonth = m;
                }

                int apiYear = 0;
                if (hijriObj.TryGetProperty("year", out var yearVal))
                {
                    if (yearVal.ValueKind == JsonValueKind.Number) apiYear = yearVal.GetInt32();
                    else if (yearVal.ValueKind == JsonValueKind.String && int.TryParse(yearVal.GetString(), out int y)) apiYear = y;
                }

                if (apiMonth > 0 && apiYear > 0)
                {
                    var uq = new System.Globalization.UmAlQuraCalendar();
                    for (int diff = -3; diff <= 3; diff++)
                    {
                        var testDate = DateTime.Today.AddDays(diff);
                        if (uq.GetYear(testDate) == apiYear && uq.GetMonth(testDate) == apiMonth && uq.GetDayOfMonth(testDate) == apiDay)
                        {
                            IslamicEventsService.CountryCalibratedOffset = diff;
                            return;
                        }
                    }
                }

                int uqDay = new System.Globalization.UmAlQuraCalendar().GetDayOfMonth(DateTime.Today);
                IslamicEventsService.CountryCalibratedOffset = apiDay - uqDay;
            }
        }
        catch { }
    }

    public static Dictionary<string, string> GetTimingsForDate(
        DateTime date,
        double lat,
        double lng,
        int calculationMethod,
        string wilaya,
        AppConfig config)
    {
        var timings = OfflinePrayerCalculator.CalculateTimings(date, lat, lng, calculationMethod);
        return ApplyTuning(timings, wilaya, config);
    }

    public static string NormalizeTime(string timeStr)
    {
        try
        {
            string clean = timeStr.Trim();
            if (clean.Length >= 5 && clean.Contains(':'))
            {
                clean = clean.Substring(0, 5);
                if (DateTime.TryParseExact(clean, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    return dt.ToString("HH:mm");
                }
            }
            if (DateTime.TryParse(timeStr, out var parsed))
            {
                return parsed.ToString("HH:mm");
            }
        }
        catch { }
        return timeStr.Length >= 5 ? timeStr.Substring(0, 5) : timeStr;
    }

    public static Dictionary<string, string> ApplyTuning(Dictionary<string, string> rawTimings, string wilaya, AppConfig config)
    {
        var result = new Dictionary<string, string>();

        string Apply(string key, string prayerLower)
        {
            if (!rawTimings.TryGetValue(key, out var t)) return "--:--";
            int tune = config.GetTune(wilaya, prayerLower);
            if (tune == 0) return t;

            try
            {
                if (DateTime.TryParseExact(t, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    return dt.AddMinutes(tune).ToString("HH:mm");
                }
            }
            catch { }
            return t;
        }

        result["Fajr"] = Apply("Fajr", "fajr");
        result["Sunrise"] = rawTimings.TryGetValue("Sunrise", out var s) ? s : "--:--";
        result["Dhuhr"] = Apply("Dhuhr", "dhuhr");
        result["Asr"] = Apply("Asr", "asr");
        result["Maghrib"] = Apply("Maghrib", "maghrib");
        result["Isha"] = Apply("Isha", "isha");

        return result;
    }

    public static void SaveRawCache(Dictionary<string, string> raw)
    {
        try
        {
            Directory.CreateDirectory(AppConfig.AppDataFolder);
            var payload = new CachedTimingsPayload
            {
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Timings = raw
            };
            string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AppConfig.CacheRawPath, json);
        }
        catch (Exception ex)
        {
            Logger.LogError($"SaveRawCache error: {ex.Message}");
        }
    }

    public static void SaveTunedCache(Dictionary<string, string> tuned)
    {
        try
        {
            Directory.CreateDirectory(AppConfig.AppDataFolder);
            var payload = new CachedTimingsPayload
            {
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Timings = tuned
            };
            string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AppConfig.CachePath, json);
        }
        catch (Exception ex)
        {
            Logger.LogError($"SaveTunedCache error: {ex.Message}");
        }
    }

    public static Dictionary<string, string>? LoadCachedRaw()
    {
        try
        {
            if (File.Exists(AppConfig.CacheRawPath))
            {
                string json = File.ReadAllText(AppConfig.CacheRawPath);
                var payload = JsonSerializer.Deserialize<CachedTimingsPayload>(json);
                if (payload?.Timings != null && PrayerTimesData.Order.All(payload.Timings.ContainsKey))
                {
                    return payload.Timings;
                }
            }
        }
        catch { }

        try
        {
            if (File.Exists(AppConfig.CachePath))
            {
                string json = File.ReadAllText(AppConfig.CachePath);
                var payload = JsonSerializer.Deserialize<CachedTimingsPayload>(json);
                if (payload?.Timings != null && PrayerTimesData.Order.All(payload.Timings.ContainsKey))
                {
                    return payload.Timings;
                }
            }
        }
        catch { }

        return null;
    }

    public static Dictionary<string, DateTime> ComputePrayerDateTimes(Dictionary<string, string> timings)
    {
        var result = new Dictionary<string, DateTime>();
        var today = DateTime.Today;

        foreach (var (key, timeStr) in timings)
        {
            try
            {
                string[] parts = timeStr.Split(':');
                if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out int h) &&
                    int.TryParse(parts[1], out int m))
                {
                    result[key] = new DateTime(today.Year, today.Month, today.Day, h, m, 0);
                }
            }
            catch { }
        }

        return result;
    }
}
