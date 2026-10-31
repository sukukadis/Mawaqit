using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mawaqit.Services;

public class UpdateCheckResult
{
    public bool IsUpdateAvailable { get; set; }
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string? Error { get; set; }
}

public static class UpdateService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public static string GetCurrentVersion()
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        if (ver != null && (ver.Major != 0 || ver.Minor != 0))
        {
            return $"{ver.Major}.{ver.Minor}.{ver.Build}";
        }
        return "2.2.6";
    }

    public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        string currentVer = GetCurrentVersion();
        var result = new UpdateCheckResult { CurrentVersion = currentVer };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/sukukadis/Mawaqit/releases/latest");
            request.Headers.Add("User-Agent", "Mawaqit-UpdateChecker");
            request.Headers.Add("Accept", "application/vnd.github+json");

            var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                result.Error = $"HTTP {(int)response.StatusCode}";
                return result;
            }

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tag = root.TryGetProperty("tag_name", out var tProp) ? tProp.GetString() ?? "" : "";
            string name = root.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? "" : "";
            string body = root.TryGetProperty("body", out var bProp) ? bProp.GetString() ?? "" : "";
            string htmlUrl = root.TryGetProperty("html_url", out var uProp) ? uProp.GetString() ?? "" : "https://github.com/sukukadis/Mawaqit/releases";

            // Extract version numbers from name, tag, or body
            string latestVerStr = ExtractVersion(name);
            if (string.IsNullOrEmpty(latestVerStr))
            {
                latestVerStr = ExtractVersion(tag);
            }
            if (string.IsNullOrEmpty(latestVerStr))
            {
                latestVerStr = ExtractVersion(body);
            }
            if (string.IsNullOrEmpty(latestVerStr))
            {
                latestVerStr = tag.TrimStart('v', 'V');
            }

            result.LatestVersion = latestVerStr;
            result.ReleaseNotes = body;
            result.DownloadUrl = htmlUrl;

            result.IsUpdateAvailable = IsNewerVersion(latestVerStr, currentVer);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Update check error: {ex.Message}");
            result.Error = ex.Message;
            return result;
        }
    }

    public static bool IsNewerVersion(string latestVerStr, string currentVerStr)
    {
        try
        {
            var lMatch = Regex.Match(latestVerStr ?? "", @"\d+(\.\d+)+");
            var cMatch = Regex.Match(currentVerStr ?? "", @"\d+(\.\d+)+");
            if (!lMatch.Success || !cMatch.Success) return false;

            var lParts = lMatch.Value.Split('.');
            var cParts = cMatch.Value.Split('.');

            int maxLen = Math.Max(lParts.Length, cParts.Length);
            for (int i = 0; i < maxLen; i++)
            {
                int l = i < lParts.Length && int.TryParse(lParts[i], out int lv) ? lv : 0;
                int c = i < cParts.Length && int.TryParse(cParts[i], out int cv) ? cv : 0;

                if (l > c) return true;
                if (l < c) return false;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string ExtractVersion(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        // Match version like "2.2.6", "v2.2.6", "Mawaqit v2.2.6", etc.
        var match = Regex.Match(text, @"(?:\b|[vV])(\d+\.\d+(?:\.\d+)*)");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return "";
    }
}
