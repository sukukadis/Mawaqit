using System.Collections.Generic;

namespace Mawaqit.Models;

public class CalculationMethodItem
{
    public int Id { get; set; }
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string RegionNote { get; set; } = "";

    public string DisplayName(string lang) => lang == "ar" ? NameAr : NameEn;

    public static readonly List<CalculationMethodItem> AllMethods = new()
    {
        new() { Id = 19, NameAr = "وزارة الشؤون الدينية والأوقاف (الجزائر)", NameEn = "Ministry of Religious Affairs (Algeria)", RegionNote = "DZ" },
        new() { Id = 4,  NameAr = "جامعة أم القرى - مكة المكرمة (السعودية والخليج)", NameEn = "Umm Al-Qura University, Makkah (Saudi & Gulf)", RegionNote = "SA" },
        new() { Id = 3,  NameAr = "رابطة العالم الإسلامي (أوروبا، أمريكا والعالم)", NameEn = "Muslim World League (Europe, Americas & Global)", RegionNote = "Global" },
        new() { Id = 5,  NameAr = "الهيئة المصرية العامة للمساحة (مصر، أفريقيا والشام)", NameEn = "Egyptian General Authority of Survey (Egypt & Africa)", RegionNote = "EG" },
        new() { Id = 21, NameAr = "الهيئة العامة للشؤون الإسلامية والأوقاف (الإمارات)", NameEn = "General Authority of Islamic Affairs (UAE)", RegionNote = "AE" },
        new() { Id = 20, NameAr = "وزارة الأوقاف والشؤون والمقدسات الإسلامية (الأردن)", NameEn = "Ministry of Awqaf (Jordan)", RegionNote = "JO" },
        new() { Id = 9,  NameAr = "وزارة الأوقاف والشؤون الإسلامية (الكويت)", NameEn = "Ministry of Awqaf (Kuwait)", RegionNote = "KW" },
        new() { Id = 10, NameAr = "وزارة الأوقاف والشؤون الإسلامية (قطر)", NameEn = "Ministry of Awqaf (Qatar)", RegionNote = "QA" },
        new() { Id = 22, NameAr = "وزارة الأوقاف والشؤون الدينية (عُمان)", NameEn = "Ministry of Endowments (Oman)", RegionNote = "OM" },
        new() { Id = 1,  NameAr = "جامعة العلوم الإسلامية بكراتشي (باكستان، الهند، بنغلاديش)", NameEn = "University of Islamic Sciences, Karachi (South Asia)", RegionNote = "PK" },
        new() { Id = 2,  NameAr = "الجمعية الإسلامية لأمريكا الشمالية (ISNA - أمريكا وكندا)", NameEn = "Islamic Society of North America (ISNA - US/Canada)", RegionNote = "US" },
        new() { Id = 13, NameAr = "رئاسة الشؤون الدينية (تركيا)", NameEn = "Diyanet İşleri Başkanlığı (Turkey)", RegionNote = "TR" },
        new() { Id = 11, NameAr = "المجلس الإسلامي لسنغافورة (MUIS)", NameEn = "Majlis Ugama Islam Singapura (Singapore)", RegionNote = "SG" },
        new() { Id = 12, NameAr = "اتحاد المنظمات الإسلامية بفرنسا (UOIF)", NameEn = "Union des Organisations Islamiques de France (UOIF)", RegionNote = "FR" },
        new() { Id = 14, NameAr = "الإدارة الدينية لمسلمي روسيا", NameEn = "Spiritual Administration of Muslims of Russia", RegionNote = "RU" },
        new() { Id = 8,  NameAr = "طريقة الخليج العربي الموحدة", NameEn = "Gulf Region Unified Method", RegionNote = "Gulf" }
    };
}
