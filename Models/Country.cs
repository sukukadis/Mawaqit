using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mawaqit.Models;

public static class FlagHelper
{
    private static readonly Dictionary<string, BitmapImage?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public static BitmapImage? GetFlagImage(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return null;
        string code = countryCode.Trim().ToLowerInvariant();

        if (_cache.TryGetValue(code, out var cached))
            return cached;

        BitmapImage? result = null;

        // 1. Try WPF Pack URI embedded resource
        try
        {
            var uri = new Uri($"pack://application:,,,/Resources/Flags/{code}.png", UriKind.Absolute);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = uri;
            bmp.EndInit();
            bmp.Freeze();
            result = bmp;
        }
        catch { }

        // 2. Fallback to application base directory / Resources / Flags
        if (result == null)
        {
            try
            {
                string[] candidates = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Flags", $"{code}.png"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Flags", $"{code}.png")
                };

                foreach (var path in candidates)
                {
                    if (File.Exists(path))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(path, UriKind.Absolute);
                        bmp.EndInit();
                        bmp.Freeze();
                        result = bmp;
                        break;
                    }
                }
            }
            catch { }
        }

        _cache[code] = result;
        return result;
    }
}

public class CountryViewModel
{
    public Country Country { get; set; } = null!;
    public string Name { get; set; } = "";
    public ImageSource? FlagImage => Country.FlagImage;
    public override string ToString() => Name;
}

public class Country
{
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string Code { get; set; } = "";
    public string Flag { get; set; } = "";
    public int DefaultMethod { get; set; } = 3;
    public string[] Aliases { get; set; } = Array.Empty<string>();

    public ImageSource? FlagImage => FlagHelper.GetFlagImage(Code);

    public string DisplayNameAr => NameAr;
    public string DisplayNameEn => NameEn;
    public string DisplayName(string lang) => lang == "ar" ? DisplayNameAr : DisplayNameEn;

    public Country() { }

    public Country(string ar, string en, string code, string flag, int defaultMethod = 3, params string[] aliases)
    {
        NameAr = ar;
        NameEn = en;
        Code = code;
        Flag = flag;
        DefaultMethod = defaultMethod;
        Aliases = aliases;
    }

    public static Country? FindCountry(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        string trimmed = query.Trim();
        string norm = Wilaya.NormalizeLocationSearch(trimmed);

        // 1. Exact ISO Code match
        var byCode = AllCountries.FirstOrDefault(c => string.Equals(c.Code, trimmed, StringComparison.OrdinalIgnoreCase));
        if (byCode != null) return byCode;

        // 2. Exact name or alias match
        var exact = AllCountries.FirstOrDefault(c =>
            Wilaya.NormalizeLocationSearch(c.NameAr) == norm ||
            Wilaya.NormalizeLocationSearch(c.NameEn) == norm ||
            Wilaya.NormalizeLocationSearch(c.DisplayNameAr) == norm ||
            Wilaya.NormalizeLocationSearch(c.DisplayNameEn) == norm ||
            c.Aliases.Any(a => Wilaya.NormalizeLocationSearch(a) == norm));
        if (exact != null) return exact;

        // 3. Substring / contains match
        return AllCountries.FirstOrDefault(c =>
            norm.Contains(Wilaya.NormalizeLocationSearch(c.NameAr)) ||
            norm.Contains(Wilaya.NormalizeLocationSearch(c.NameEn)) ||
            Wilaya.NormalizeLocationSearch(c.NameAr).Contains(norm) ||
            Wilaya.NormalizeLocationSearch(c.NameEn).Contains(norm) ||
            c.Aliases.Any(a => norm.Contains(Wilaya.NormalizeLocationSearch(a)) ||
                               Wilaya.NormalizeLocationSearch(a).Contains(norm)));
    }

    public static readonly List<Country> AllCountries = new()
    {
        // 1. Arab League Countries (22 Nations)
        new("الجزائر", "Algeria", "DZ", "🇩🇿", 19, "الجزائرية"),
        new("المملكة العربية السعودية", "Saudi Arabia", "SA", "🇸🇦", 4, "السعودية", "السعوديه", "Saudi", "KSA"),
        new("جمهورية مصر العربية", "Egypt", "EG", "🇪🇬", 5, "مصر", "Egypt"),
        new("المملكة المغربية", "Morocco", "MA", "🇲🇦", 3, "المغرب", "Morocco"),
        new("الجمهورية التونسية", "Tunisia", "TN", "🇹🇳", 3, "تونس", "Tunisia"),
        new("دولة فلسطين", "Palestine", "PS", "🇵🇸", 5, "فلسطين", "Palestine"),
        new("المملكة الأردنية الهاشمية", "Jordan", "JO", "🇯🇴", 20, "الأردن", "الاردن", "Jordan"),
        new("دولة الإمارات العربية المتحدة", "United Arab Emirates", "AE", "🇦🇪", 21, "الإمارات", "الامارات", "UAE"),
        new("دولة الكويت", "Kuwait", "KW", "🇰🇼", 9, "الكويت", "Kuwait"),
        new("دولة قطر", "Qatar", "QA", "🇶🇦", 10, "قطر", "Qatar"),
        new("مملكة البحرين", "Bahrain", "BH", "🇧🇭", 8, "البحرين", "Bahrain"),
        new("سلطنة عُمان", "Oman", "OM", "🇴🇲", 22, "عمان", "سلطنة عمان", "Oman"),
        new("الجمهورية العراقية", "Iraq", "IQ", "🇮🇶", 3, "العراق", "Iraq"),
        new("الجمهورية العربية السورية", "Syria", "SY", "🇸🇾", 5, "سوريا", "سورية", "سورية", "Syria"),
        new("الجمهورية اللبنانية", "Lebanon", "LB", "🇱🇧", 5, "لبنان", "Lebanon"),
        new("دولة ليبيا", "Libya", "LY", "🇱🇾", 5, "ليبيا", "Libya"),
        new("الجمهورية اليمنية", "Yemen", "YE", "🇾🇪", 4, "اليمن", "Yemen"),
        new("جمهورية السودان", "Sudan", "SD", "🇸🇩", 5, "السودان", "Sudan"),
        new("الجمهورية الإسلامية الموريتانية", "Mauritania", "MR", "🇲🇷", 3, "موريتانيا", "Mauritania"),
        new("جمهورية الصومال الفيدرالية", "Somalia", "SO", "🇸🇴", 5, "الصومال", "Somalia"),
        new("جمهورية جيبوتي", "Djibouti", "DJ", "🇩🇯", 5, "جيبوتي", "Djibouti"),
        new("جمهورية القمر المتحدة", "Comoros", "KM", "🇰🇲", 5, "جزر القمر", "القمر", "Comoros"),

        // 2. Middle East & Islamic Countries
        new("الجمهورية التركية", "Turkey", "TR", "🇹🇷", 13, "تركيا", "Turkey", "Türkiye"),
        new("جمهورية إيران الإسلامية", "Iran", "IR", "🇮🇷", 7, "إيران", "ايران", "Iran"),
        new("جمهورية باكستان الإسلامية", "Pakistan", "PK", "🇵🇰", 1, "باكستان", "Pakistan"),
        new("جمهورية بنغلاديش الشعبية", "Bangladesh", "BD", "🇧🇩", 1, "بنغلاديش", "Bangladesh"),
        new("جمهورية إندونيسيا", "Indonesia", "ID", "🇮🇩", 3, "إندونيسيا", "اندونيسيا", "Indonesia"),
        new("ماليزيا", "Malaysia", "MY", "🇲🇾", 3, "ماليزيا", "Malaysia"),
        new("جمهورية سنغافورة", "Singapore", "SG", "🇸🇬", 11, "سنغافورة", "Singapore"),
        new("جمهورية أفغانستان الإسلامية", "Afghanistan", "AF", "🇦🇫", 1, "أفغانستان", "افغانستان", "Afghanistan"),
        new("جمهورية أوزبكستان", "Uzbekistan", "UZ", "🇺🇿", 1, "أوزبكستان", "اوزبكستان", "Uzbekistan"),
        new("جمهورية كازاخستان", "Kazakhstan", "KZ", "🇰🇿", 13, "كازاخستان", "Kazakhstan"),
        new("جمهورية أذربيجان", "Azerbaijan", "AZ", "🇦🇿", 13, "أذربيجان", "اذربيجان", "Azerbaijan"),
        new("تركمانستان", "Turkmenistan", "TM", "🇹🇲", 13, "تركمانستان", "Turkmenistan"),
        new("جمهورية قيرغيزستان", "Kyrgyzstan", "KG", "🇰🇬", 1, "قيرغيزستان", "Kyrgyzstan"),
        new("جمهورية طاجيكستان", "Tajikistan", "TJ", "🇹🇯", 1, "طاجيكستان", "Tajikistan"),

        // 3. Europe
        new("الجمهورية الفرنسية", "France", "FR", "🇫🇷", 12, "فرنسا", "France"),
        new("المملكة المتحدة", "United Kingdom", "GB", "🇬🇧", 3, "بريطانيا", "المملكة المتحدة", "إنجلترا", "UK", "Britain", "England"),
        new("جمهورية ألمانيا الاتحادية", "Germany", "DE", "🇩🇪", 3, "ألمانيا", "المانيا", "Germany"),
        new("مملكة إسبانيا", "Spain", "ES", "🇪🇸", 3, "إسبانيا", "اسبانيا", "Spain"),
        new("الجمهورية الإيطالية", "Italy", "IT", "🇮🇹", 3, "إيطاليا", "ايطاليا", "Italy"),
        new("مملكة بلجيكا", "Belgium", "BE", "🇧🇪", 3, "بلجيكا", "Belgium"),
        new("مملكة هولندا", "Netherlands", "NL", "🇳🇱", 3, "هولندا", "Netherlands", "Holland"),
        new("الاتحاد السويسري", "Switzerland", "CH", "🇨🇭", 3, "سويسرا", "Switzerland"),
        new("مملكة السويد", "Sweden", "SE", "🇸🇪", 3, "السويد", "Sweden"),
        new("مملكة النرويج", "Norway", "NO", "🇳🇴", 3, "النرويج", "Norway"),
        new("مملكة الدنمارك", "Denmark", "DK", "🇩🇰", 3, "الدنمارك", "Denmark"),
        new("جمهورية النمسا", "Austria", "AT", "🇦🇹", 3, "النمسا", "Austria"),
        new("البوسنة والهرسك", "Bosnia and Herzegovina", "BA", "🇧🇦", 3, "البوسنة", "Bosnia"),
        new("جمهورية ألبانيا", "Albania", "AL", "🇦🇱", 3, "ألبانيا", "Albania"),
        new("جمهورية كوسوفو", "Kosovo", "XK", "🇽🇰", 3, "كوسوفو", "Kosovo"),
        new("روسيا الاتحادية", "Russia", "RU", "🇷🇺", 14, "روسيا", "Russia"),
        new("أوكرانيا", "Ukraine", "UA", "🇺🇦", 3, "أوكرانيا", "اوكرانيا", "Ukraine"),
        new("جمهورية بولندا", "Poland", "PL", "🇵🇱", 3, "بولندا", "Poland"),
        new("رومانيا", "Romania", "RO", "🇷🇴", 3, "رومانيا", "Romania"),
        new("الجمهورية الهيلينية", "Greece", "GR", "🇬🇷", 3, "اليونان", "Greece"),
        new("الجمهورية البرتغالية", "Portugal", "PT", "🇵🇹", 3, "البرتغال", "Portugal"),
        new("إيرلندا", "Ireland", "IE", "🇮🇪", 3, "إيرلندا", "ايرلندا", "Ireland"),
        new("جمهورية فنلندا", "Finland", "FI", "🇫🇮", 3, "فنلندا", "Finland"),
        new("جمهورية التشيك", "Czech Republic", "CZ", "🇨🇿", 3, "التشيك", "Czech"),
        new("هنغاريا / المجر", "Hungary", "HU", "🇭🇺", 3, "المجر", "Hungary"),
        new("جمهورية بلغاريا", "Bulgaria", "BG", "🇧🇬", 3, "بلغاريا", "Bulgaria"),
        new("جمهورية صربيا", "Serbia", "RS", "🇷🇸", 3, "صربيا", "Serbia"),
        new("جمهورية كرواتيا", "Croatia", "HR", "🇭🇷", 3, "كرواتيا", "Croatia"),
        new("مقدونيا الشمالية", "North Macedonia", "MK", "🇲🇰", 3, "مقدونيا", "Macedonia"),
        new("الجبل الأسود", "Montenegro", "ME", "🇲🇪", 3, "الجبل الأسود", "Montenegro"),
        new("جمهورية قبرص", "Cyprus", "CY", "🇨🇾", 3, "قبرص", "Cyprus"),

        // 4. North & South America
        new("الولايات المتحدة الأمريكية", "United States", "US", "🇺🇸", 2, "أمريكا", "الولايات المتحدة", "امريكا", "USA", "US", "America"),
        new("كندا", "Canada", "CA", "🇨🇦", 2, "كندا", "Canada"),
        new("الولايات المكسيكية المتحدة", "Mexico", "MX", "🇲🇽", 2, "المكسيك", "Mexico"),
        new("جمهورية البرازيل الاتحادية", "Brazil", "BR", "🇧🇷", 3, "البرازيل", "Brazil"),
        new("جمهورية الأرجنتين", "Argentina", "AR", "🇦🇷", 3, "الأرجنتين", "الارجنتين", "Argentina"),
        new("جمهورية كولومبيا", "Colombia", "CO", "🇨🇴", 3, "كولومبيا", "Colombia"),
        new("جمهورية تشيلي", "Chile", "CL", "🇨🇱", 3, "تشيلي", "Chile"),
        new("جمهورية بيرو", "Peru", "PE", "🇵🇪", 3, "بيرو", "Peru"),
        new("جمهورية فنزويلا", "Venezuela", "VE", "🇻🇪", 3, "فنزويلا", "Venezuela"),

        // 5. Asia & Oceania
        new("جمهورية الهند", "India", "IN", "🇮🇳", 1, "الهند", "India"),
        new("جمهورية الصين الشعبية", "China", "CN", "🇨🇳", 3, "الصين", "China"),
        new("اليابان", "Japan", "JP", "🇯🇵", 3, "اليابان", "Japan"),
        new("جمهورية كوريا", "South Korea", "KR", "🇰🇷", 3, "كوريا الجنوبية", "South Korea"),
        new("مملكة تايلاند", "Thailand", "TH", "🇹🇭", 3, "تايلاند", "Thailand"),
        new("جمهورية الفلبين", "Philippines", "PH", "🇵🇭", 3, "الفلبين", "Philippines"),
        new("جمهورية فيتنام", "Vietnam", "VN", "🇻🇳", 3, "فيتنام", "Vietnam"),
        new("كومنولث أستراليا", "Australia", "AU", "🇦🇺", 3, "أستراليا", "استراليا", "Australia"),
        new("نيوزيلندا", "New Zealand", "NZ", "🇳🇿", 3, "نيوزيلندا", "New Zealand"),

        // 6. Africa
        new("جمهورية نيجيريا الاتحادية", "Nigeria", "NG", "🇳🇬", 5, "نيجيريا", "Nigeria"),
        new("جمهورية جنوب أفريقيا", "South Africa", "ZA", "🇿🇦", 3, "جنوب أفريقيا", "جنوب افريقيا", "South Africa"),
        new("جمهورية السنغال", "Senegal", "SN", "🇸🇳", 3, "السنغال", "Senegal"),
        new("جمهورية مالي", "Mali", "ML", "🇲🇱", 3, "مالي", "Mali"),
        new("جمهورية النيجر", "Niger", "NE", "🇳🇪", 3, "النيجر", "Niger"),
        new("جمهورية تشاد", "Chad", "TD", "🇹🇩", 23, "تشاد", "Chad"),
        new("جمهورية كينيا", "Kenya", "KE", "🇰🇪", 3, "كينيا", "Kenya"),
        new("جمهورية تنزانيا", "Tanzania", "TZ", "🇹🇿", 3, "تنزانيا", "Tanzania"),
        new("جمهورية إثيوبيا", "Ethiopia", "ET", "🇪🇹", 3, "إثيوبيا", "اثيوبيا", "Ethiopia"),
        new("جمهورية غانا", "Ghana", "GH", "🇬🇭", 3, "غانا", "Ghana"),
        new("جمهورية ساحل العاج", "Ivory Coast", "CI", "🇨🇮", 3, "ساحل العاج", "Ivory Coast", "Côte d'Ivoire"),
        new("جمهورية الكاميرون", "Cameroon", "CM", "🇨🇲", 3, "الكاميرون", "Cameroon"),
        new("جمهورية أوغندا", "Uganda", "UG", "🇺🇬", 3, "أوغندا", "اوغندا", "Uganda"),
        new("جمهورية غينيا", "Guinea", "GN", "🇬🇳", 3, "غينيا", "Guinea"),
        new("بوركينا فاسو", "Burkina Faso", "BF", "🇧🇫", 3, "بوركينا فاسو", "Burkina Faso")
    };
}
