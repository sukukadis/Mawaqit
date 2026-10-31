using System;
using System.Collections.Generic;
using System.Globalization;

namespace Mawaqit.Services;

public record IslamicOccasion(
    string NameAr,
    string NameEn,
    string Icon,
    bool IsMajorHoliday,
    string DescriptionAr = "",
    string DescriptionEn = "");

public record UpcomingMilestone(
    string Key,
    string NameAr,
    string NameEn,
    string Icon,
    DateTime GregorianDate,
    int DaysRemaining);

public static class IslamicEventsService
{
    private static readonly UmAlQuraCalendar HijriCal = new();

    /// <summary>
    /// Calibrated daily from Aladhan API response for the selected country.
    /// </summary>
    public static int CountryCalibratedOffset { get; set; } = 0;

    public static (int Year, int Month, int Day) GetHijriDate(DateTime date, int offsetDays = 0)
    {
        try
        {
            int totalOffset = offsetDays + CountryCalibratedOffset;
            DateTime adjusted = totalOffset != 0 ? date.AddDays(totalOffset) : date;
            return (HijriCal.GetYear(adjusted), HijriCal.GetMonth(adjusted), HijriCal.GetDayOfMonth(adjusted));
        }
        catch
        {
            return (1448, 1, 1);
        }
    }

    /// <summary>
    /// Checks if a given date corresponds to one of the 9 special Islamic occasions.
    /// </summary>
    public static IslamicOccasion? GetOccasion(DateTime date, int offsetDays = 0)
    {
        var (_, hm, hd) = GetHijriDate(date, offsetDays);

        // 1. Hijri New Year (1 Muharram)
        if (hm == 1 && hd == 1)
        {
            return new IslamicOccasion("رأس السنة الهجرية 🌙", "Hijri New Year 🌙", "🌙", true, "غرة شهر محرم وبداية العام الهجري الجديد", "First day of Muharram & Islamic New Year");
        }

        // 2. Day of Ashura (10 Muharram)
        if (hm == 1 && hd == 10)
        {
            return new IslamicOccasion("يوم عاشوراء 🕊️", "Day of Ashura 🕊️", "🕊️", false, "يوم العاشر من محرم (صيام يكفر سنة ماضية)", "10th of Muharram, blessed fasting day");
        }
        if (hm == 1 && hd == 9)
        {
            return new IslamicOccasion("تاسوعاء 🕊️", "Tasu'a 🕊️", "🕊️", false, "اليوم التاسع من محرم، مستحب صيامه مع عاشوراء", "9th of Muharram, recommended fasting before Ashura");
        }

        // 3. Laylat al-Isra' wal-Mi'raj (27 Rajab)
        if (hm == 7 && hd == 27)
        {
            return new IslamicOccasion("ليلة الإسراء والمعراج 🌌", "Laylat al-Isra’ wal-Mi’raj 🌌", "🌌", false, "ذكرى معجزة الإسراء والمعراج بالنبي ﷺ", "The Night Journey and Ascension of Prophet Muhammad ﷺ");
        }

        // 4. Mid-Sha'ban (15 Sha'ban)
        if (hm == 8 && hd == 15)
        {
            return new IslamicOccasion("ليلة النصف من شعبان 🌕", "Mid-Sha’ban 🌕", "🌕", false, "ليلة مباركة يستحب فيها قيام الليل والدعاء", "Blessed night of Mid-Sha'ban");
        }

        // 6. Laylat al-Qadr (27 Ramadan - prioritized over regular Ramadan day)
        if (hm == 9 && hd == 27)
        {
            return new IslamicOccasion("ليلة القدر المباركة 🌟", "Laylat al-Qadr 🌟", "🌟", true, "ليلة القدر خير من ألف شهر", "The Night of Decree, better than a thousand months");
        }

        // 5. The Blessed Month of Ramadan (Month 9)
        if (hm == 9)
        {
            if (hd == 1)
            {
                return new IslamicOccasion("غرة شهر رمضان المبارك 🌙", "1st Day of Ramadan 🌙", "🌙", true, "أول أيام شهر رمضان الفضيل المبارك", "First day of Ramadan");
            }
            return new IslamicOccasion("شهر رمضان المبارك 🌙", "Blessed Month of Ramadan 🌙", "🌙", false, $"اليوم {hd} من شهر رمضان المبارك", $"Day {hd} of Ramadan");
        }

        // 9. Eid al-Fitr (1-3 Shawwal)
        if (hm == 10)
        {
            if (hd == 1)
            {
                return new IslamicOccasion("عيد الفطر المبارك 🎉", "Eid al-Fitr 🎉", "🎉", true, "أول أيام عيد الفطر المبارك (تقبل الله منا ومنكم)", "First day of Eid al-Fitr");
            }
            if (hd == 2 || hd == 3)
            {
                return new IslamicOccasion("أيام عيد الفطر 🎉", "Eid al-Fitr Days 🎉", "🎉", true, $"اليوم {hd} من عيد الفطر المبارك", $"Day {hd} of Eid al-Fitr");
            }
        }

        // 7. Day of Arafah (9 Dhu al-Hijjah)
        if (hm == 12 && hd == 9)
        {
            return new IslamicOccasion("يوم عرفة 🕋", "Day of Arafah 🕋", "🕋", true, "يوم الحج الأكبر، وصيامه يكفر سنة ماضية وقادمة", "Day of Arafah, pinnacle of Hajj");
        }

        // 8. Eid al-Adha (10-13 Dhu al-Hijjah)
        if (hm == 12)
        {
            if (hd == 10)
            {
                return new IslamicOccasion("عيد الأضحى المبارك 🐑", "Eid al-Adha 🐑", "🐑", true, "يوم النحر وأول أيام عيد الأضحى المبارك", "First day of Eid al-Adha");
            }
            if (hd >= 11 && hd <= 13)
            {
                return new IslamicOccasion("أيام التشريق (عيد الأضحى) 🐑", "Days of Tashreeq (Eid al-Adha) 🐑", "🐑", true, $"اليوم {hd - 9} من عيد الأضحى", $"Day {hd - 9} of Eid al-Adha");
            }
        }

        return null;
    }

    /// <summary>
    /// Computes the upcoming Gregorian dates and countdowns for Ramadan, Eid al-Fitr, and Eid al-Adha.
    /// Takes into account the country/user Hijri offset.
    /// </summary>
    public static List<UpcomingMilestone> GetUpcomingMilestones(DateTime baseDate, int offsetDays = 0)
    {
        var list = new List<UpcomingMilestone>();

        UpcomingMilestone? ramadan = null;
        UpcomingMilestone? fitr = null;
        UpcomingMilestone? adha = null;

        // Check up to 365 days ahead
        for (int i = 0; i <= 365; i++)
        {
            var date = baseDate.AddDays(i);
            var (_, m, d) = GetHijriDate(date, offsetDays);

            if (ramadan == null && m == 9 && d == 1)
            {
                ramadan = new UpcomingMilestone("ramadan", "رمضان المبارك", "Ramadan", "🌙", date, i);
            }
            if (fitr == null && m == 10 && d == 1)
            {
                fitr = new UpcomingMilestone("fitr", "عيد الفطر", "Eid al-Fitr", "🎉", date, i);
            }
            if (adha == null && m == 12 && d == 10)
            {
                adha = new UpcomingMilestone("adha", "عيد الأضحى", "Eid al-Adha", "🕋", date, i);
            }

            if (ramadan != null && fitr != null && adha != null)
                break;
        }

        if (ramadan != null) list.Add(ramadan);
        if (fitr != null) list.Add(fitr);
        if (adha != null) list.Add(adha);

        return list;
    }
}
