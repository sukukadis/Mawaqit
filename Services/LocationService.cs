using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Mawaqit.Models;

namespace Mawaqit.Services;

public static class LocationService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    static LocationService()
    {
        Http.DefaultRequestHeaders.Add("User-Agent", "Mawaqit/2.2.5 LocationClient");
    }

    /// <summary>
    /// Auto-detects the user's current city, country, and coordinates via IP geolocation
    /// </summary>
    public static async Task<(Wilaya? Location, string? Error)> DetectLocationAsync()
    {
        // 1. Try ipapi.co (reliable, provides country, city, coordinates)
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://ipapi.co/json/");
            var resp = await Http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                string json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string? city = root.TryGetProperty("city", out var cProp) ? cProp.GetString() : null;
                string? countryName = root.TryGetProperty("country_name", out var cnProp) ? cnProp.GetString() : null;
                string? countryCode = root.TryGetProperty("country_code", out var ccProp) ? ccProp.GetString() : null;
                double? lat = root.TryGetProperty("latitude", out var latProp) && latProp.ValueKind == JsonValueKind.Number ? latProp.GetDouble() : null;
                double? lon = root.TryGetProperty("longitude", out var lonProp) && lonProp.ValueKind == JsonValueKind.Number ? lonProp.GetDouble() : null;

                if (!string.IsNullOrEmpty(city) && lat.HasValue && lon.HasValue)
                {
                    string safeCountry = countryName ?? "Unknown";
                    int method = GetRecommendedMethod(countryCode);
                    var loc = new Wilaya(city, city, lat.Value, lon.Value, safeCountry, safeCountry, method);
                    return (loc, null);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"ipapi.co detection failed: {ex.Message}");
        }

        // 2. Fallback to ip-api.com
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "http://ip-api.com/json/");
            var resp = await Http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                string json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var st) && st.GetString() == "success")
                {
                    string? city = root.TryGetProperty("city", out var cProp) ? cProp.GetString() : null;
                    string? countryName = root.TryGetProperty("country", out var cnProp) ? cnProp.GetString() : null;
                    string? countryCode = root.TryGetProperty("countryCode", out var ccProp) ? ccProp.GetString() : null;
                    double? lat = root.TryGetProperty("lat", out var latProp) && latProp.ValueKind == JsonValueKind.Number ? latProp.GetDouble() : null;
                    double? lon = root.TryGetProperty("lon", out var lonProp) && lonProp.ValueKind == JsonValueKind.Number ? lonProp.GetDouble() : null;

                    if (!string.IsNullOrEmpty(city) && lat.HasValue && lon.HasValue)
                    {
                        string safeCountry = countryName ?? "Unknown";
                        int method = GetRecommendedMethod(countryCode);
                        var loc = new Wilaya(city, city, lat.Value, lon.Value, safeCountry, safeCountry, method);
                        return (loc, null);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"ip-api.com detection failed: {ex.Message}");
        }

        return (null, "Could not determine location automatically.");
    }

    /// <summary>
    /// Searches for worldwide cities using Open-Meteo Geocoding API (free, fast, no API key needed)
    /// </summary>
    public static async Task<List<Wilaya>> SearchCitiesAsync(string query, string lang = "ar")
    {
        var results = new List<Wilaya>();
        if (string.IsNullOrWhiteSpace(query)) return results;

        try
        {
            string urlLang = lang == "ar" ? "ar" : "en";
            string url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query.Trim())}&count=15&language={urlLang}";

            using var resp = await Http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return results;

            string json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("results", out var arr) || arr.ValueKind != JsonValueKind.Array)
            {
                return results;
            }

            foreach (var item in arr.EnumerateArray())
            {
                string? name = item.TryGetProperty("name", out var nProp) ? nProp.GetString() : null;
                if (string.IsNullOrEmpty(name)) continue;

                string country = item.TryGetProperty("country", out var cProp) ? (cProp.GetString() ?? "") : "";
                string countryCode = item.TryGetProperty("country_code", out var ccProp) ? (ccProp.GetString() ?? "") : "";
                string admin1 = item.TryGetProperty("admin1", out var aProp) ? (aProp.GetString() ?? "") : "";

                double lat = item.TryGetProperty("latitude", out var latProp) ? latProp.GetDouble() : 0;
                double lon = item.TryGetProperty("longitude", out var lonProp) ? lonProp.GetDouble() : 0;

                int method = GetRecommendedMethod(countryCode);

                string fullCountry = !string.IsNullOrEmpty(admin1) && admin1 != name && admin1 != country
                    ? $"{admin1}, {country}"
                    : country;

                var w = new Wilaya(name, name, lat, lon, fullCountry, fullCountry, method);
                results.Add(w);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Geocoding search failed: {ex.Message}");
        }

        return results;
    }

    /// <summary>
    /// Maps a 2-letter ISO country code to the most widely accepted Islamic calculation method
    /// </summary>
    public static int GetRecommendedMethod(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return 3; // Muslim World League default

        return countryCode.Trim().ToUpperInvariant() switch
        {
            "DZ" => 19, // Algerian Ministry of Religious Affairs
            "SA" => 4,  // Umm Al-Qura University, Makkah
            "EG" => 5,  // Egyptian General Authority of Survey
            "AE" => 21, // UAE General Authority of Islamic Affairs
            "JO" => 20, // Jordan Ministry of Awqaf
            "KW" => 9,  // Kuwait Ministry of Awqaf
            "QA" => 10, // Qatar Ministry of Awqaf
            "OM" => 22, // Oman Ministry of Endowments
            "PK" or "IN" or "BD" or "AF" => 1, // University of Islamic Sciences, Karachi
            "US" or "CA" => 2, // ISNA (Islamic Society of North America)
            "TR" => 13, // Diyanet Turkey
            "SG" => 11, // Majlis Ugama Islam Singapura
            "FR" => 12, // Union des Organisations Islamiques de France (UOIF)
            "RU" => 14, // Spiritual Administration of Muslims of Russia
            "BH" or "YE" => 4, // Gulf / Umm Al-Qura
            "SD" or "LY" or "SY" or "LB" or "PS" => 5, // Egyptian Authority / Levant
            "TD" => 23, // Chad Higher Council of Islamic Affairs
            _ => 3 // Muslim World League (Europe, Far East, Global default)
        };
    }
}
