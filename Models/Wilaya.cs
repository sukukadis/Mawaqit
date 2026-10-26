using System;
using System.Collections.Generic;
using System.Linq;

namespace Mawaqit.Models;

public class Wilaya
{
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public Wilaya() { }

    public Wilaya(string ar, string en, double lat, double lng)
    {
        NameAr = ar;
        NameEn = en;
        Latitude = lat;
        Longitude = lng;
    }

    public static readonly List<Wilaya> AllWilayas = new()
    {
        new("أدرار", "Adrar", 27.9766155, -0.20396),
        new("الشلف", "Chlef", 36.1691245, 1.3539002),
        new("الأغواط", "Laghouat", 33.7873735, 2.8829115),
        new("أم البواقي", "Oum El Bouaghi", 35.8726014, 7.1180248),
        new("باتنة", "Batna", 35.5784266, 6.1083422),
        new("بجاية", "Bejaia", 36.7695969, 5.0085855),
        new("بسكرة", "Biskra", 34.8515041, 5.7246709),
        new("بشار", "Bechar", 31.5965368, -2.4145248),
        new("البليدة", "Blida", 36.4803023, 2.8009379),
        new("البويرة", "Bouira", 36.2084234, 3.925049),
        new("تمنراست", "Tamanrasset", 22.2746227, 5.6754684),
        new("تبسة", "Tebessa", 35.4117259, 8.110545),
        new("تلمسان", "Tlemcen", 34.8959541, -1.3150979),
        new("تيارت", "Tiaret", 35.3599899, 1.3916159),
        new("تيزي وزو", "Tizi Ouzou", 36.7002068, 4.075957),
        new("الجزائر", "Alger", 36.7538259, 3.057841),
        new("الجلفة", "Djelfa", 34.6672467, 3.2993118),
        new("جيجل", "Jijel", 36.7962714, 5.7504845),
        new("سطيف", "Setif", 36.1905173, 5.4202134),
        new("سعيدة", "Saida", 34.841945, 0.1483583),
        new("سكيكدة", "Skikda", 36.8777912, 6.9357204),
        new("سيدي بلعباس", "Sidi Bel Abbes", 35.206334, -0.6301368),
        new("عنابة", "Annaba", 36.9184345, 7.7452755),
        new("قالمة", "Guelma", 36.4569088, 7.4334312),
        new("قسنطينة", "Constantine", 36.319475, 6.7370571),
        new("المدية", "Medea", 36.2838408, 2.7728462),
        new("مستغانم", "Mostaganem", 35.9751841, 0.1149273),
        new("المسيلة", "M'Sila", 35.7211476, 4.5187283),
        new("معسكر", "Mascara", 35.382998, 0.1542592),
        new("ورقلة", "Ouargla", 31.8974, 5.3421),
        new("وهران", "Oran", 35.7066928, -0.6405861),
        new("البيض", "El Bayadh", 33.6146, 1.019),
        new("إليزي", "Illizi", 26.5065999, 8.480587),
        new("برج بوعريريج", "Bordj Bou Arreridj", 36.0686488, 4.7691823),
        new("بومرداس", "Boumerdes", 36.7564181, 3.4917212),
        new("الطارف", "El Tarf", 36.7534258, 8.2984543),
        new("تندوف", "Tindouf", 27.5972, -8.1491),
        new("تسمسيلت", "Tissemsilt", 35.6021906, 1.802187),
        new("الوادي", "El Oued", 33.3714492, 6.8573436),
        new("خنشلة", "Khenchela", 35.4263293, 7.1414137),
        new("سوق أهراس", "Souk Ahras", 36.277849, 7.9592299),
        new("تيبازة", "Tipaza", 36.5980966, 2.4085379),
        new("ميلة", "Mila", 36.4514882, 6.2487316),
        new("عين الدفلى", "Ain Defla", 36.1283915, 2.1772514),
        new("النعامة", "Naama", 33.195, -0.3131),
        new("عين تموشنت", "Ain Temouchent", 35.404044, -1.0580975),
        new("غرداية", "Ghardaia", 32.5891743, 3.7455655),
        new("غليزان", "Relizane", 35.6696, 0.5493),
        new("تيميمون", "Timimoun", 29.258333, 0.230556),
        new("برج باجي مختار", "Bordj Badji Mokhtar", 21.327778, 0.955556),
        new("أولاد جلال", "Ouled Djellal", 34.433333, 5.066667),
        new("بني عباس", "Beni Abbes", 30.133333, -2.166667),
        new("عين صالح", "In Salah", 27.197222, 2.483333),
        new("عين قزام", "In Guezzam", 19.572222, 5.769444),
        new("تقرت", "Touggourt", 33.108333, 6.063889),
        new("جانت", "Djanet", 24.554167, 9.484722),
        new("المغير", "El M'Ghair", 33.947222, 5.922222),
        new("المنيعة", "El Meniaa", 30.579167, 2.879167),
        new("أفلو", "Aflou", 34.1167, 2.1),
        new("بريكة", "Barika", 35.3833, 5.3667),
        new("القنطرة", "El Kantara", 35.2167, 5.7),
        new("بئر العاتر", "Bir El Ater", 34.75, 8.05),
        new("العريشة", "El Aricha", 34.25, -1.35),
        new("قصر الشلالة", "Ksar Chellala", 35.1667, 2.3167),
        new("عين وسارة", "Ain Oussara", 35.45, 2.9),
        new("مسعد", "Messaad", 34.1667, 3.5),
        new("قصر البخاري", "Ksar El Boukhari", 35.8833, 2.75),
        new("بوسعادة", "Bou Saada", 35.2167, 4.1833),
        new("الأبيض سيدي الشيخ", "El Abiodh Sidi Cheikh", 32.9, 0.5333),
        new("الحميز الدار البيضاء", "El Hamiz Dar El Beïda", 36.729819, 3.230513),
        new("الخروب قسنطينة", "El Khroub Constantine", 36.2633, 6.6936)
    };

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
        if (string.IsNullOrWhiteSpace(query))
        {
            return isArabic 
                ? AllWilayas.OrderBy(w => w.NameAr).ToList() 
                : AllWilayas.OrderBy(w => w.NameEn).ToList();
        }

        string normQ = NormalizeLocationSearch(query);
        string qNoAl = (normQ.StartsWith("ال") && normQ.Length > 3) ? normQ[2..] : normQ;
        var words = normQ.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wordsNoAl = words.Select(w => (w.StartsWith("ال") && w.Length > 3) ? w[2..] : w).ToArray();

        var exactPrefix = new List<Wilaya>();
        var alPrefix = new List<Wilaya>();
        var otherMatches = new List<Wilaya>();
        var tokenMatches = new List<Wilaya>();

        foreach (var w in AllWilayas)
        {
            string targetName = isArabic ? w.NameAr : w.NameEn;
            string normName = NormalizeLocationSearch(targetName);
            string normNameNoAl = normName.StartsWith("ال") ? normName[2..] : normName;

            if (normName.StartsWith(normQ))
            {
                exactPrefix.Add(w);
            }
            else if (normNameNoAl.StartsWith(qNoAl))
            {
                alPrefix.Add(w);
            }
            else if (normName.Contains(normQ) || normNameNoAl.Contains(qNoAl))
            {
                otherMatches.Add(w);
            }
            else if (words.Length > 1 && words.All(word => normName.Contains(word) || normNameNoAl.Contains(word)))
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
