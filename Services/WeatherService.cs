using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using Mawaqit.Models;

namespace Mawaqit.Services;

public class WeatherResult
{
    public double? Temperature { get; set; }
    public int? WeatherCode { get; set; }
    public int IsDay { get; set; } = 1;

    public string WeatherIcon => GetWeatherIcon(WeatherCode, IsDay);
    public string DisplayText => Temperature.HasValue ? $"{WeatherIcon}  {Temperature.Value:0}°C" : "🌡  --°C";
    public string FlyoutWeatherText => Temperature.HasValue ? $"{Temperature.Value:0}°C {WeatherIcon}" : "";
    public Brush TextColor => GetTemperatureBrush(Temperature);
    public DrawingImage IconDrawing => WeatherIconProvider.GetWeatherDrawing(WeatherCode, IsDay);

    public string GetConditionText(string lang)
    {
        bool isAr = lang == "ar";
        if (!WeatherCode.HasValue) return isAr ? "غير متوفر" : "Unavailable";

        return WeatherCode.Value switch
        {
            0 => isAr ? (IsDay == 1 ? "مشمس / صافٍ" : "سماء صافية") : (IsDay == 1 ? "Sunny / Clear" : "Clear Sky"),
            1 => isAr ? "صافٍ في معظمه" : "Mainly Clear",
            2 => isAr ? "غائم جزئياً" : "Partly Cloudy",
            3 => isAr ? "غائم" : "Overcast",
            45 or 48 => isAr ? "ضباب" : "Fog",
            51 or 53 or 55 => isAr ? "رذاذ خفيف" : "Drizzle",
            56 or 57 or 66 or 67 => isAr ? "أمطار متجمدة" : "Freezing Rain",
            61 or 63 or 65 or 80 or 81 or 82 => isAr ? "أمطار" : "Rain",
            71 or 73 or 75 or 77 or 85 or 86 => isAr ? "ثلوج" : "Snow",
            95 or 96 or 99 => isAr ? "عواصف رعدية" : "Thunderstorm",
            _ => isAr ? "معتدل" : "Fair"
        };
    }

    public static string GetWeatherIcon(int? weatherCode, int isDay = 1)
    {
        if (!weatherCode.HasValue) return "🌡";

        return weatherCode.Value switch
        {
            0 => isDay == 1 ? "☀️" : "🌙",
            1 => isDay == 1 ? "🌤️" : "🌤️",
            2 => "⛅",
            3 => "☁️",
            45 or 48 => "🌫️",
            51 or 53 or 55 => isDay == 1 ? "🌦️" : "🌧️",
            56 or 57 => "🌧️❄️",
            61 or 63 or 65 => "🌧️",
            66 or 67 => "🌧️❄️",
            71 or 73 or 75 => "🌨️",
            77 => "❄️",
            80 or 81 or 82 => isDay == 1 ? "🌦️" : "🌧️",
            85 or 86 => "🌨️",
            95 or 96 or 99 => "⛈️",
            _ => isDay == 1 ? "☀️" : "🌙"
        };
    }

    public static Brush GetTemperatureBrush(double? t)
    {
        if (!t.HasValue)
        {
            var def = (SolidColorBrush)new BrushConverter().ConvertFromString("#A0A0A0")!;
            def.Freeze();
            return def;
        }
        string hex = t.Value switch
        {
            >= 35 => "#EF4444",
            >= 25 => "#F97316",
            >= 15 => "#10B981",
            >= 5 => "#38BDF8",
            _ => "#818CF8"
        };
        var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        b.Freeze();
        return b;
    }
}

public static class WeatherService
{
    private static readonly HttpClient Http;

    static WeatherService()
    {
        Http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        Http.DefaultRequestHeaders.Add("User-Agent", "Mawaqit/2.2.6 (Windows NT 10.0; Win64; x64) PrayerApp");
        Http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    private static string CacheFilePath => Path.Combine(AppConfig.AppDataFolder, "weather_cache.json");

    public static async Task<WeatherResult> FetchTemperatureAsync(double latitude, double longitude)
    {
        string url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&current=temperature_2m,weather_code,is_day";

        // Try fetch up to 2 times
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var response = await Http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("current", out var current))
                    {
                        double? temp = null;
                        int? code = null;
                        int isDay = 1;

                        if (current.TryGetProperty("temperature_2m", out var tempProp) && tempProp.ValueKind == JsonValueKind.Number)
                        {
                            temp = tempProp.GetDouble();
                        }
                        if (current.TryGetProperty("weather_code", out var codeProp) && codeProp.ValueKind == JsonValueKind.Number)
                        {
                            code = codeProp.GetInt32();
                        }
                        if (current.TryGetProperty("is_day", out var dayProp) && dayProp.ValueKind == JsonValueKind.Number)
                        {
                            isDay = dayProp.GetInt32();
                        }

                        var result = new WeatherResult
                        {
                            Temperature = temp,
                            WeatherCode = code,
                            IsDay = isDay
                        };

                        if (temp.HasValue)
                        {
                            SaveCached(result);
                        }

                        return result;
                    }
                }
                else
                {
                    Logger.LogError($"Weather fetch status: {(int)response.StatusCode} {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Weather fetch error (attempt {attempt}): {ex.Message}");
            }

            if (attempt == 1)
            {
                await Task.Delay(1500);
            }
        }

        // On complete failure, fall back to cached weather
        var cached = LoadCached();
        return cached ?? new WeatherResult { Temperature = null };
    }

    public static void SaveCached(WeatherResult result)
    {
        try
        {
            Directory.CreateDirectory(AppConfig.AppDataFolder);
            var cacheData = new
            {
                temperature = result.Temperature,
                weather_code = result.WeatherCode,
                is_day = result.IsDay,
                saved_at = DateTime.UtcNow.ToString("o")
            };
            string json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(CacheFilePath, json);
        }
        catch { }
    }

    public static WeatherResult? LoadCached()
    {
        try
        {
            if (File.Exists(CacheFilePath))
            {
                string json = File.ReadAllText(CacheFilePath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                double? temp = null;
                int? code = null;
                int isDay = 1;

                if (root.TryGetProperty("temperature", out var tp) && tp.ValueKind == JsonValueKind.Number)
                {
                    temp = tp.GetDouble();
                }
                if (root.TryGetProperty("weather_code", out var cp) && cp.ValueKind == JsonValueKind.Number)
                {
                    code = cp.GetInt32();
                }
                if (root.TryGetProperty("is_day", out var dp) && dp.ValueKind == JsonValueKind.Number)
                {
                    isDay = dp.GetInt32();
                }

                if (temp.HasValue)
                {
                    return new WeatherResult
                    {
                        Temperature = temp,
                        WeatherCode = code,
                        IsDay = isDay
                    };
                }
            }
        }
        catch { }

        return null;
    }
}
