using System;
using System.Collections.Generic;
using System.Linq;

namespace Mawaqit.Models;

public partial class Wilaya
{
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string CountryAr { get; set; } = "الجزائر";
    public string CountryEn { get; set; } = "Algeria";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int DefaultMethod { get; set; } = 19;

    public string DisplayNameAr => string.IsNullOrEmpty(CountryAr) || CountryAr == NameAr ? NameAr : $"{NameAr} ({CountryAr})";
    public string DisplayNameEn => string.IsNullOrEmpty(CountryEn) || CountryEn == NameEn ? NameEn : $"{NameEn} ({CountryEn})";
    public string DisplayName(string lang) => lang == "ar" ? DisplayNameAr : DisplayNameEn;

    public Wilaya() { }

    public Wilaya(string ar, string en, double lat, double lng, string countryAr = "الجزائر", string countryEn = "Algeria", int defaultMethod = 19)
    {
        NameAr = ar;
        NameEn = en;
        Latitude = lat;
        Longitude = lng;
        CountryAr = countryAr;
        CountryEn = countryEn;
        DefaultMethod = defaultMethod;
    }

    /// <summary>
    /// Backward-compatible access to Algerian Wilayas
    /// </summary>
    public static List<Wilaya> AllWilayas => AlgerianWilayas;

    private static List<Wilaya>? _allLocations;

    /// <summary>
    /// Complete database of all wilayas, states, provinces, and governorates across the globe
    /// </summary>
    public static List<Wilaya> AllLocations
    {
        get
        {
            if (_allLocations == null)
            {
                var list = new List<Wilaya>(AlgerianWilayas.Count + ArabStates.Count + WorldStates.Count);
                list.AddRange(AlgerianWilayas);
                list.AddRange(ArabStates);
                list.AddRange(WorldStates);
                _allLocations = list;
            }
            return _allLocations;
        }
    }

    /// <summary>
    /// Gets all states/provinces/governorates for a specific country
    /// </summary>
    public static List<Wilaya> GetStatesForCountry(string? country)
    {
        if (string.IsNullOrWhiteSpace(country)) return AlgerianWilayas;

        string norm = NormalizeLocationSearch(country);

        // Find the matching country definition if any
        var c = Country.FindCountry(country);
        string normAr = c != null ? NormalizeLocationSearch(c.NameAr) : norm;
        string normEn = c != null ? NormalizeLocationSearch(c.NameEn) : norm;

        var matches = AllLocations.Where(w =>
        {
            string wAr = NormalizeLocationSearch(w.CountryAr);
            string wEn = NormalizeLocationSearch(w.CountryEn);
            return wAr == normAr || wEn == normEn ||
                   (c != null && (wAr == NormalizeLocationSearch(c.NameAr) || wEn == NormalizeLocationSearch(c.NameEn))) ||
                   wAr.Contains(norm) || wEn.Contains(norm);
        }).ToList();

        if (matches.Count > 0) return matches;

        if (normAr.Contains("جزائر") || normEn.Contains("alger"))
        {
            return AlgerianWilayas;
        }

        // Fallback: return a default location entry for the country
        string nameAr = c?.NameAr ?? country;
        string nameEn = c?.NameEn ?? country;
        int defMethod = c?.DefaultMethod ?? 3;
        return new List<Wilaya> { new(nameAr, nameEn, 0, 0, nameAr, nameEn, defMethod) };
    }

    public static Wilaya? FindLocation(string? ar, string? en)
    {
        if (string.IsNullOrWhiteSpace(ar) && string.IsNullOrWhiteSpace(en)) return null;

        string normAr = !string.IsNullOrWhiteSpace(ar) ? NormalizeLocationSearch(ar) : "";
        string normEn = !string.IsNullOrWhiteSpace(en) ? NormalizeLocationSearch(en) : "";

        // 1. Exact match
        var exact = AllLocations.FirstOrDefault(w =>
            (!string.IsNullOrEmpty(normAr) && (
                NormalizeLocationSearch(w.NameAr) == normAr ||
                NormalizeLocationSearch(w.DisplayNameAr) == normAr)) ||
            (!string.IsNullOrEmpty(normEn) && (
                NormalizeLocationSearch(w.NameEn) == normEn ||
                NormalizeLocationSearch(w.DisplayNameEn) == normEn)));
        if (exact != null) return exact;

        // 2. Contains match (handles "Chicago" in "Illinois / Chicago", "القدس" in "القدس الشريف", etc.)
        var partial = AllLocations.FirstOrDefault(w =>
            (!string.IsNullOrEmpty(normAr) && (
                NormalizeLocationSearch(w.NameAr).Contains(normAr) ||
                normAr.Contains(NormalizeLocationSearch(w.NameAr)))) ||
            (!string.IsNullOrEmpty(normEn) && (
                NormalizeLocationSearch(w.NameEn).Contains(normEn) ||
                normEn.Contains(NormalizeLocationSearch(w.NameEn)))));
        if (partial != null) return partial;

        // 3. Fallback to FilterWilayas
        if (!string.IsNullOrEmpty(ar))
        {
            var matches = FilterWilayas(ar, true);
            if (matches.Count > 0) return matches[0];
        }
        if (!string.IsNullOrEmpty(en))
        {
            var matches = FilterWilayas(en, false);
            if (matches.Count > 0) return matches[0];
        }

        return null;
    }

    public static string NormalizeLocationSearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        string s = text.Trim().ToLowerInvariant();

        s = s.Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا').Replace('ٱ', 'ا');
        s = s.Replace('ة', 'ه').Replace('ى', 'ي').Replace('ڨ', 'ق');
        s = s.Replace("\u0640", ""); // tatweel

        char[] diacritics = { '\u064b', '\u064c', '\u064d', '\u064e', '\u064f', '\u0650', '\u0651', '\u0652' };
        foreach (char d in diacritics)
        {
            s = s.Replace(d.ToString(), "");
        }

        s = s.Replace('é', 'e').Replace('è', 'e').Replace('ê', 'e').Replace('ë', 'e')
             .Replace('à', 'a').Replace('â', 'a').Replace('ô', 'o')
             .Replace('î', 'i').Replace('ï', 'i').Replace('ù', 'u')
             .Replace('û', 'u').Replace('ç', 'c');

        return s;
    }

    public static List<Wilaya> FilterWilayas(string query, bool isArabic)
    {
        var source = AllLocations;

        if (string.IsNullOrWhiteSpace(query))
        {
            return isArabic
                ? source.OrderBy(w => w.CountryAr != "الجزائر").ThenBy(w => w.NameAr).ToList()
                : source.OrderBy(w => w.CountryEn != "Algeria").ThenBy(w => w.NameEn).ToList();
        }

        string normQ = NormalizeLocationSearch(query);
        string qNoAl = (normQ.StartsWith("ال") && normQ.Length > 3) ? normQ[2..] : normQ;
        var words = normQ.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var exactPrefix = new List<Wilaya>();
        var alPrefix = new List<Wilaya>();
        var otherMatches = new List<Wilaya>();
        var tokenMatches = new List<Wilaya>();

        foreach (var w in source)
        {
            string targetName = isArabic ? w.DisplayNameAr : w.DisplayNameEn;
            string rawName = isArabic ? w.NameAr : w.NameEn;
            string country = isArabic ? w.CountryAr : w.CountryEn;

            string normName = NormalizeLocationSearch(targetName);
            string normRaw = NormalizeLocationSearch(rawName);
            string normCountry = NormalizeLocationSearch(country);
            string normNameNoAl = normName.StartsWith("ال") ? normName[2..] : normName;

            if (normRaw.StartsWith(normQ) || normName.StartsWith(normQ))
            {
                exactPrefix.Add(w);
            }
            else if (normNameNoAl.StartsWith(qNoAl))
            {
                alPrefix.Add(w);
            }
            else if (normName.Contains(normQ) || normRaw.Contains(normQ) || normCountry.Contains(normQ))
            {
                otherMatches.Add(w);
            }
            else if (words.Length > 1 && words.All(word => normName.Contains(word) || normCountry.Contains(word)))
            {
                tokenMatches.Add(w);
            }
        }

        var result = new List<Wilaya>();
        result.AddRange(exactPrefix);
        result.AddRange(alPrefix);
        result.AddRange(otherMatches);
        result.AddRange(tokenMatches);
        return result.Distinct().ToList();
    }
}
