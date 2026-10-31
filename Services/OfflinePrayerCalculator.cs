using System;
using System.Collections.Generic;
using System.Globalization;

namespace Mawaqit.Services;

/// <summary>
/// High-precision astronomical prayer times calculator.
/// Computes solar positions and Islamic prayer times offline using standard astronomical algorithms
/// without requiring an active internet connection.
/// </summary>
public static class OfflinePrayerCalculator
{
    public class MethodParameters
    {
        public double FajrAngle { get; set; } = 18.0;
        public double IshaAngle { get; set; } = 17.0;
        public bool IshaIsMinutesAfterMaghrib { get; set; } = false;
        public double IshaMinutes { get; set; } = 90.0;
        public int AsrJuristic { get; set; } = 1; // 1 = Standard (Maliki/Shafi'i/Hanbali), 2 = Hanafi
        public double DhuhrMinutesBuffer { get; set; } = 1.0;
    }

    public static MethodParameters GetParameters(int methodId)
    {
        return methodId switch
        {
            19 => new MethodParameters { FajrAngle = 18.0, IshaAngle = 17.0, AsrJuristic = 1 }, // Algerian Ministry
            4  => new MethodParameters { FajrAngle = 18.5, IshaIsMinutesAfterMaghrib = true, IshaMinutes = 90.0, AsrJuristic = 1 }, // Umm Al-Qura
            3  => new MethodParameters { FajrAngle = 18.0, IshaAngle = 17.0, AsrJuristic = 1 }, // Muslim World League
            5  => new MethodParameters { FajrAngle = 19.5, IshaAngle = 17.5, AsrJuristic = 1 }, // Egyptian General Authority
            21 => new MethodParameters { FajrAngle = 18.2, IshaAngle = 18.2, AsrJuristic = 1 }, // UAE
            20 => new MethodParameters { FajrAngle = 18.0, IshaAngle = 18.0, AsrJuristic = 1 }, // Jordan
            9  => new MethodParameters { FajrAngle = 18.0, IshaAngle = 17.5, AsrJuristic = 1 }, // Kuwait
            10 => new MethodParameters { FajrAngle = 18.0, IshaIsMinutesAfterMaghrib = true, IshaMinutes = 90.0, AsrJuristic = 1 }, // Qatar
            22 => new MethodParameters { FajrAngle = 18.0, IshaAngle = 18.0, AsrJuristic = 1 }, // Oman
            1  => new MethodParameters { FajrAngle = 18.0, IshaAngle = 18.0, AsrJuristic = 2 }, // Karachi (Hanafi Asr)
            2  => new MethodParameters { FajrAngle = 15.0, IshaAngle = 15.0, AsrJuristic = 1 }, // ISNA
            13 => new MethodParameters { FajrAngle = 18.0, IshaAngle = 17.0, AsrJuristic = 1 }, // Diyanet Turkey
            11 => new MethodParameters { FajrAngle = 20.0, IshaAngle = 18.0, AsrJuristic = 1 }, // Singapore
            12 => new MethodParameters { FajrAngle = 12.0, IshaAngle = 12.0, AsrJuristic = 1 }, // UOIF France
            14 => new MethodParameters { FajrAngle = 16.0, IshaAngle = 15.0, AsrJuristic = 1 }, // Russia
            8  => new MethodParameters { FajrAngle = 19.5, IshaIsMinutesAfterMaghrib = true, IshaMinutes = 90.0, AsrJuristic = 1 }, // Gulf Unified
            _  => new MethodParameters { FajrAngle = 18.0, IshaAngle = 17.0, AsrJuristic = 1 }
        };
    }

    private static double DegToRad(double d) => d * Math.PI / 180.0;
    private static double RadToDeg(double r) => r * 180.0 / Math.PI;
    private static double FixHour(double a) { a = a - 24.0 * Math.Floor(a / 24.0); return a < 0 ? a + 24.0 : a; }
    private static double FixAngle(double a) { a = a - 360.0 * Math.Floor(a / 360.0); return a < 0 ? a + 360.0 : a; }

    /// <summary>
    /// Computes prayer times for the given date, latitude, longitude, and calculation method.
    /// </summary>
    public static Dictionary<string, string> CalculateTimings(DateTime date, double latitude, double longitude, int methodId = 19, double? timezoneOffset = null)
    {
        var p = GetParameters(methodId);
        double tz = timezoneOffset ?? TimeZoneInfo.Local.GetUtcOffset(date).TotalHours;

        // Julian Day calculation
        int Y = date.Year;
        int M = date.Month;
        int D = date.Day;
        if (M <= 2)
        {
            Y -= 1;
            M += 12;
        }

        double A = Math.Floor(Y / 100.0);
        double B = 2.0 - A + Math.Floor(A / 4.0);
        double JD = Math.Floor(365.25 * (Y + 4716)) + Math.Floor(30.6001 * (M + 1)) + D + B - 1524.5;
        double d = JD - 2451545.0;

        // Solar coordinates
        double g = FixAngle(357.529 + 0.98560028 * d);
        double q = FixAngle(280.459 + 0.98564736 * d);
        double L = FixAngle(q + 1.915 * Math.Sin(DegToRad(g)) + 0.020 * Math.Sin(DegToRad(2.0 * g)));
        double e = 23.439 - 0.00000036 * d;
        double delta = Math.Asin(Math.Sin(DegToRad(e)) * Math.Sin(DegToRad(L)));
        double RA = RadToDeg(Math.Atan2(Math.Cos(DegToRad(e)) * Math.Sin(DegToRad(L)), Math.Cos(DegToRad(L)))) / 15.0;
        RA = FixHour(RA);
        double EqT = (q / 15.0) - RA;
        if (EqT > 12.0) EqT -= 24.0;
        if (EqT < -12.0) EqT += 24.0;

        // Solar noon (Dhuhr base)
        double noon = FixHour(12.0 + tz - (longitude / 15.0) - EqT);

        double HourAngle(double angle)
        {
            double cosH = (-Math.Sin(DegToRad(angle)) - Math.Sin(DegToRad(latitude)) * Math.Sin(delta)) /
                          (Math.Cos(DegToRad(latitude)) * Math.Cos(delta));
            if (cosH > 1.0 || cosH < -1.0) return 0.0;
            return RadToDeg(Math.Acos(cosH)) / 15.0;
        }

        // Asr calculation
        double asrAngle = RadToDeg(Math.Atan(1.0 / (p.AsrJuristic + Math.Tan(Math.Abs(DegToRad(latitude) - delta)))));
        double cosAsrH = (Math.Sin(DegToRad(asrAngle)) - Math.Sin(DegToRad(latitude)) * Math.Sin(delta)) /
                         (Math.Cos(DegToRad(latitude)) * Math.Cos(delta));
        double asrHA = Math.Abs(cosAsrH) <= 1.0 ? RadToDeg(Math.Acos(cosAsrH)) / 15.0 : 0.0;

        // Sun elevation at horizon (0.8333 deg for refraction & disc radius)
        double horizonAngle = 0.8333;
        double haHorizon = HourAngle(horizonAngle);

        double fajr = FixHour(noon - HourAngle(p.FajrAngle));
        double sunrise = FixHour(noon - haHorizon);
        double dhuhr = FixHour(noon + (p.DhuhrMinutesBuffer / 60.0));
        double asr = FixHour(noon + asrHA);
        double maghrib = FixHour(noon + haHorizon);
        double isha = p.IshaIsMinutesAfterMaghrib
            ? FixHour(maghrib + (p.IshaMinutes / 60.0))
            : FixHour(noon + HourAngle(p.IshaAngle));

        string Format(double h)
        {
            int hrs = (int)Math.Floor(h);
            int mins = (int)Math.Round((h - hrs) * 60.0);
            if (mins >= 60)
            {
                hrs += 1;
                mins -= 60;
            }
            hrs %= 24;
            return $"{hrs:D2}:{mins:D2}";
        }

        return new Dictionary<string, string>
        {
            ["Fajr"] = Format(fajr),
            ["Sunrise"] = Format(sunrise),
            ["Dhuhr"] = Format(dhuhr),
            ["Asr"] = Format(asr),
            ["Maghrib"] = Format(maghrib),
            ["Isha"] = Format(isha)
        };
    }
}

