using System;
using System.Globalization;

namespace Mawaqit.Services;

public static class HijriDateService
{
    private static readonly UmAlQuraCalendar HijriCal = new();

    public static string FormatGregorianDate(DateTime date, string lang)
    {
        int dayOfWeekIndex = ((int)date.DayOfWeek + 6) % 7; // Monday = 0
        int monthIndex = date.Month - 1;

        if (lang == "ar")
        {
            string day = LocalizationService.ArabicDays[dayOfWeekIndex];
            string month = LocalizationService.ArabicMonths[monthIndex];
            return $"{day} {date.Day} {month} {date.Year}";
        }
        else
        {
            string day = LocalizationService.EnglishDays[dayOfWeekIndex];
            string month = LocalizationService.EnglishMonths[monthIndex];
            return $"{day} {month} {date.Day} {date.Year}";
        }
    }

    public static string FormatHijriDate(DateTime date, string lang)
    {
        try
        {
            int hy = HijriCal.GetYear(date);
            int hm = HijriCal.GetMonth(date);
            int hd = HijriCal.GetDayOfMonth(date);

            if (hm >= 1 && hm <= 12)
            {
                if (lang == "ar")
                {
                    string monthName = LocalizationService.ArabicHijriMonths[hm - 1];
                    return $"{hd} {monthName} {hy} هـ";
                }
                else
                {
                    string monthName = LocalizationService.EnglishHijriMonths[hm - 1];
                    return $"{hd} {monthName} {hy} AH";
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Hijri conversion error: {ex.Message}");
        }

        return "";
    }
}
