using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;

// Disambiguate against System.Drawing (from WinForms global using)
using Point = System.Windows.Point;
using Pen = System.Windows.Media.Pen;

namespace Mawaqit.Services;

/// <summary>
/// Provides rich, colorful Fluent vector weather icons matching the Windows 11 Widget aesthetic.
/// Built using WPF DrawingImage with vibrant gradients and hardware-accelerated vectors.
/// </summary>
public static class WeatherIconProvider
{
    private static readonly ConcurrentDictionary<(int code, int isDay), DrawingImage> Cache = new();
    private static DrawingImage? _defaultThermometer;

    private static readonly Geometry BaseCloudGeo;
    private static readonly Geometry BaseMoonGeo1;
    private static readonly Geometry BaseMoonGeo2;
    private static readonly Geometry BaseBoltGeo;

    static WeatherIconProvider()
    {
        BaseCloudGeo = Geometry.Parse("M 8,22 C 5,22 3,20 3,17.5 C 3,15 5,13.2 7.5,13 C 8.5,9.5 12,7 16,7 C 20.5,7 24,10 24.5,13.5 C 27,14 29,15.8 29,18 C 29,20.2 27,22 24.5,22 Z");
        BaseCloudGeo.Freeze();

        BaseMoonGeo1 = Geometry.Parse("M 10,5 C 5,10 5,20 12,26 C 18,29 24,25 25,23 C 18,25 12,20 12,14 C 12,9 15,6 17,5 C 14,4.5 11.5,4.7 10,5 Z");
        BaseMoonGeo1.Freeze();

        BaseMoonGeo2 = Geometry.Parse("M 9,5 C 5,9 5,17 11,21 C 15,23 20,20 21,18 C 15,20 10,16 10,11 C 10,7 12,5 14,4 C 11.5,4 9.5,4.5 9,5 Z");
        BaseMoonGeo2.Freeze();

        BaseBoltGeo = Geometry.Parse("M 16.5,13 L 11.5,19.5 L 15,19.5 L 12,28 L 21,18.5 L 16.5,18.5 Z");
        BaseBoltGeo.Freeze();
    }

    /// <summary>
    /// Gets a cached, frozen DrawingImage of the colorful Windows 11 weather icon for the given WMO weather code.
    /// </summary>
    public static DrawingImage GetWeatherDrawing(int? weatherCode, int isDay = 1)
    {
        if (!weatherCode.HasValue)
        {
            return GetDefaultThermometer();
        }

        int keyDay = isDay == 0 ? 0 : 1;
        return Cache.GetOrAdd((weatherCode.Value, keyDay), k =>
        {
            try
            {
                var drawing = CreateWeatherDrawing(k.code, k.isDay);
                var image = new DrawingImage(drawing);
                image.Freeze();
                return image;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Weather drawing error: {ex.Message}");
                return GetDefaultThermometer();
            }
        });
    }

    private static DrawingImage GetDefaultThermometer()
    {
        if (_defaultThermometer != null) return _defaultThermometer;

        var group = new DrawingGroup();
        using (var dc = group.Open())
        {
            // Glass tube and bulb
            var bulbBrush = new RadialGradientBrush(
                (Color)ColorConverter.ConvertFromString("#EF4444"),
                (Color)ColorConverter.ConvertFromString("#B91C1C"))
            {
                Center = new Point(0.4, 0.4),
                GradientOrigin = new Point(0.4, 0.4)
            };
            var stemBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#38BDF8"),
                (Color)ColorConverter.ConvertFromString("#EF4444"),
                new Point(0, 0), new Point(0, 1));

            var outlinePen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), 1.5)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };

            // Outer glass outline
            dc.DrawRoundedRectangle(null, outlinePen, new Rect(13.5, 4, 5, 16), 2.5, 2.5);
            dc.DrawEllipse(null, outlinePen, new Point(16, 23), 5.5, 5.5);

            // Colored liquid
            dc.DrawRoundedRectangle(stemBrush, null, new Rect(14.5, 8, 3, 12), 1.5, 1.5);
            dc.DrawEllipse(bulbBrush, null, new Point(16, 23), 4.5, 4.5);

            // Scale tick marks
            var tickPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")), 1.0);
            dc.DrawLine(tickPen, new Point(11, 7), new Point(13, 7));
            dc.DrawLine(tickPen, new Point(11, 11), new Point(13, 11));
            dc.DrawLine(tickPen, new Point(11, 15), new Point(13, 15));
        }

        _defaultThermometer = new DrawingImage(group);
        _defaultThermometer.Freeze();
        return _defaultThermometer;
    }

    private static DrawingGroup CreateWeatherDrawing(int code, int isDay)
    {
        var group = new DrawingGroup();
        using var dc = group.Open();

        switch (code)
        {
            // Clear / Sunny
            case 0:
                if (isDay == 1) DrawSun(dc);
                else DrawClearNight(dc);
                break;

            // Mainly clear
            case 1:
                if (isDay == 1) DrawPartlyCloudyDay(dc, lightCloud: true);
                else DrawPartlyCloudyNight(dc);
                break;

            // Partly cloudy
            case 2:
                if (isDay == 1) DrawPartlyCloudyDay(dc, lightCloud: false);
                else DrawPartlyCloudyNight(dc);
                break;

            // Overcast / Cloudy
            case 3:
                DrawOvercast(dc);
                break;

            // Fog / Depositing rime fog
            case 45:
            case 48:
                DrawFog(dc);
                break;

            // Drizzle: Light, moderate, dense
            case 51:
            case 53:
            case 55:
                DrawDrizzle(dc);
                break;

            // Freezing Drizzle / Sleet
            case 56:
            case 57:
            case 66:
            case 67:
                DrawFreezingRain(dc);
                break;

            // Rain: Slight, moderate, heavy
            case 61:
            case 63:
            case 65:
            case 80:
            case 81:
            case 82:
                DrawRain(dc);
                break;

            // Snow: Slight, moderate, heavy
            case 71:
            case 73:
            case 75:
            case 77:
            case 85:
            case 86:
                DrawSnow(dc);
                break;

            // Thunderstorm
            case 95:
            case 96:
            case 99:
                DrawThunderstorm(dc);
                break;

            default:
                if (isDay == 1) DrawSun(dc);
                else DrawClearNight(dc);
                break;
        }

        return group;
    }

    #region Individual Icon Visual Builders (Windows 11 Fluent 3D Styling)

    private static void DrawSun(DrawingContext dc)
    {
        // 8 Radiant Rounded Sun Rays
        var rayPen = new Pen(CreateSunRayBrush(), 2.2)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

        Point center = new Point(16, 16);
        double rInner = 10.5;
        double rOuter = 14.5;

        for (int i = 0; i < 8; i++)
        {
            double angle = i * Math.PI / 4.0;
            Point p1 = new Point(center.X + rInner * Math.Cos(angle), center.Y + rInner * Math.Sin(angle));
            Point p2 = new Point(center.X + rOuter * Math.Cos(angle), center.Y + rOuter * Math.Sin(angle));
            dc.DrawLine(rayPen, p1, p2);
        }

        // Sun Core Disc with rich warm radial gradient & soft halo
        var sunBrush = new RadialGradientBrush
        {
            Center = new Point(0.35, 0.35),
            GradientOrigin = new Point(0.35, 0.35),
            GradientStops =
            {
                new GradientStop((Color)ColorConverter.ConvertFromString("#FFFBEB"), 0.0),
                new GradientStop((Color)ColorConverter.ConvertFromString("#FDE047"), 0.35),
                new GradientStop((Color)ColorConverter.ConvertFromString("#F59E0B"), 0.75),
                new GradientStop((Color)ColorConverter.ConvertFromString("#D97706"), 1.0)
            }
        };

        dc.DrawEllipse(sunBrush, null, center, 7.5, 7.5);
    }

    private static void DrawClearNight(DrawingContext dc)
    {
        // Glowing Crescent Moon
        var moonBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0.2, 0.1),
            EndPoint = new Point(0.8, 0.9),
            GradientStops =
            {
                new GradientStop((Color)ColorConverter.ConvertFromString("#F0F9FF"), 0.0),
                new GradientStop((Color)ColorConverter.ConvertFromString("#7DD3FC"), 0.4),
                new GradientStop((Color)ColorConverter.ConvertFromString("#38BDF8"), 0.75),
                new GradientStop((Color)ColorConverter.ConvertFromString("#2563EB"), 1.0)
            }
        };

        dc.DrawGeometry(moonBrush, null, BaseMoonGeo1);

        // Twinkling Diamond Stars
        DrawSparkleStar(dc, 23, 8, 3.2, "#FEF08A");
        DrawSparkleStar(dc, 25, 17, 2.2, "#BAE6FD");
    }

    private static void DrawPartlyCloudyDay(DrawingContext dc, bool lightCloud)
    {
        // Sun peeking from top-left behind cloud
        Point sunCenter = new Point(11.5, 11);
        var sunBrush = new RadialGradientBrush
        {
            Center = new Point(0.35, 0.35),
            GradientOrigin = new Point(0.35, 0.35),
            GradientStops =
            {
                new GradientStop((Color)ColorConverter.ConvertFromString("#FFFBEB"), 0.0),
                new GradientStop((Color)ColorConverter.ConvertFromString("#FDE047"), 0.35),
                new GradientStop((Color)ColorConverter.ConvertFromString("#F59E0B"), 0.8),
                new GradientStop((Color)ColorConverter.ConvertFromString("#D97706"), 1.0)
            }
        };

        // Sun rays (visible in top-left quadrant)
        var rayPen = new Pen(CreateSunRayBrush(), 2.0)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        double[] angles = { Math.PI, 5 * Math.PI / 4, 3 * Math.PI / 2, 7 * Math.PI / 4 };
        foreach (var a in angles)
        {
            Point p1 = new Point(sunCenter.X + 7.5 * Math.Cos(a), sunCenter.Y + 7.5 * Math.Sin(a));
            Point p2 = new Point(sunCenter.X + 11.0 * Math.Cos(a), sunCenter.Y + 11.0 * Math.Sin(a));
            dc.DrawLine(rayPen, p1, p2);
        }

        dc.DrawEllipse(sunBrush, null, sunCenter, 5.5, 5.5);

        // Puffy 3D Fluent Cloud in front
        DrawFluentCloud(dc, offsetX: 2, offsetY: 2, isDark: false);
    }

    private static void DrawPartlyCloudyNight(DrawingContext dc)
    {
        // Crescent moon peeking from top-left
        var moonBrush = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString("#E0F2FE"),
            (Color)ColorConverter.ConvertFromString("#38BDF8"),
            new Point(0, 0), new Point(1, 1));
        dc.DrawGeometry(moonBrush, null, BaseMoonGeo2);

        DrawSparkleStar(dc, 25, 7, 2.5, "#BAE6FD");

        // Cloud in front
        DrawFluentCloud(dc, offsetX: 2, offsetY: 2, isDark: true);
    }

    private static void DrawOvercast(DrawingContext dc)
    {
        // Back darker cloud
        DrawFluentCloud(dc, offsetX: -2, offsetY: -3, isDark: true, scale: 0.88);

        // Front lighter cloud
        DrawFluentCloud(dc, offsetX: 2, offsetY: 2, isDark: false, scale: 1.0);
    }

    private static void DrawFog(DrawingContext dc)
    {
        // Soft cloud puff at top
        DrawFluentCloud(dc, offsetX: 0, offsetY: -3, isDark: false, scale: 0.85);

        // 3 Horizontal rounded mist bars
        var mistBrush1 = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString("#38BDF8"),
            (Color)ColorConverter.ConvertFromString("#CBD5E1"),
            new Point(0, 0), new Point(1, 0));
        var mistBrush2 = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString("#60A5FA"),
            (Color)ColorConverter.ConvertFromString("#94A3B8"),
            new Point(0, 0), new Point(1, 0));
        var mistBrush3 = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString("#94A3B8"),
            (Color)ColorConverter.ConvertFromString("#CBD5E1"),
            new Point(0, 0), new Point(1, 0));

        dc.DrawRoundedRectangle(mistBrush1, null, new Rect(6, 17, 20, 2.2), 1.1, 1.1);
        dc.DrawRoundedRectangle(mistBrush2, null, new Rect(4, 21, 24, 2.2), 1.1, 1.1);
        dc.DrawRoundedRectangle(mistBrush3, null, new Rect(8, 25, 16, 2.2), 1.1, 1.1);
    }

    private static void DrawDrizzle(DrawingContext dc)
    {
        DrawFluentCloud(dc, offsetX: 0, offsetY: -2, isDark: false, scale: 0.95);

        var dropBrush = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString("#38BDF8"),
            (Color)ColorConverter.ConvertFromString("#0284C7"),
            new Point(0, 0), new Point(0, 1));
        var dropPen = new Pen(dropBrush, 2.0)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

        dc.DrawLine(dropPen, new Point(11, 21), new Point(11, 25));
        dc.DrawLine(dropPen, new Point(16, 22), new Point(16, 26.5));
        dc.DrawLine(dropPen, new Point(21, 21), new Point(21, 25));
    }

    private static void DrawRain(DrawingContext dc)
    {
        // Darker storm cloud
        DrawFluentCloud(dc, offsetX: 0, offsetY: -2, isDark: true, scale: 0.95);

        // Angled rain streaks
        var rainBrush = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString("#38BDF8"),
            (Color)ColorConverter.ConvertFromString("#0284C7"),
            new Point(0, 0), new Point(0, 1));
        var rainPen = new Pen(rainBrush, 2.0)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

        dc.DrawLine(rainPen, new Point(9.5, 20.5), new Point(8, 26.5));
        dc.DrawLine(rainPen, new Point(14.5, 21), new Point(13, 27.5));
        dc.DrawLine(rainPen, new Point(19.5, 21), new Point(18, 27.5));
        dc.DrawLine(rainPen, new Point(24.5, 20.5), new Point(23, 26.5));
    }

    private static void DrawFreezingRain(DrawingContext dc)
    {
        DrawFluentCloud(dc, offsetX: 0, offsetY: -2, isDark: true, scale: 0.95);

        var rainBrush = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString("#38BDF8"),
            (Color)ColorConverter.ConvertFromString("#0284C7"),
            new Point(0, 0), new Point(0, 1));
        var rainPen = new Pen(rainBrush, 1.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        dc.DrawLine(rainPen, new Point(10, 20.5), new Point(8.5, 26));
        dc.DrawLine(rainPen, new Point(22, 20.5), new Point(20.5, 26));

        // Ice pellets / snow sparkles in middle
        DrawSnowflake(dc, 15.5, 23.5, 2.8);
    }

    private static void DrawSnow(DrawingContext dc)
    {
        DrawFluentCloud(dc, offsetX: 0, offsetY: -3, isDark: false, scale: 0.95);

        DrawSnowflake(dc, 10.5, 22.5, 2.6);
        DrawSnowflake(dc, 16.5, 24.5, 3.0);
        DrawSnowflake(dc, 22.5, 22.5, 2.6);
    }

    private static void DrawThunderstorm(DrawingContext dc)
    {
        // Deep stormy cloud
        DrawFluentCloud(dc, offsetX: 0, offsetY: -3, isDark: true, deepStorm: true, scale: 0.95);

        // Electric Lightning Bolt
        var boltBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0.3, 0.0),
            EndPoint = new Point(0.7, 1.0),
            GradientStops =
            {
                new GradientStop((Color)ColorConverter.ConvertFromString("#FEF08A"), 0.0),
                new GradientStop((Color)ColorConverter.ConvertFromString("#FACC15"), 0.5),
                new GradientStop((Color)ColorConverter.ConvertFromString("#F59E0B"), 1.0)
            }
        };

        var boltPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706")), 0.7);
        dc.DrawGeometry(boltBrush, boltPen, BaseBoltGeo);

        // Blue rain streaks on edges
        var rainPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")), 1.8)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        dc.DrawLine(rainPen, new Point(8, 20.5), new Point(6.5, 25.5));
        dc.DrawLine(rainPen, new Point(24.5, 20.5), new Point(23, 25.5));
    }

    #endregion

    #region Helper Geometries & Brushes

    private static void DrawFluentCloud(DrawingContext dc, double offsetX, double offsetY, bool isDark, bool deepStorm = false, double scale = 1.0)
    {
        LinearGradientBrush cloudBrush;

        if (deepStorm)
        {
            cloudBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0.2, 0.0),
                EndPoint = new Point(0.8, 1.0),
                GradientStops =
                {
                    new GradientStop((Color)ColorConverter.ConvertFromString("#475569"), 0.0),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#1E293B"), 0.65),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#0F172A"), 1.0)
                }
            };
        }
        else if (isDark)
        {
            cloudBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0.2, 0.0),
                EndPoint = new Point(0.8, 1.0),
                GradientStops =
                {
                    new GradientStop((Color)ColorConverter.ConvertFromString("#94A3B8"), 0.0),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#64748B"), 0.55),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#475569"), 1.0)
                }
            };
        }
        else
        {
            // Crisp, luminous 3D white/silver cloud
            cloudBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0.2, 0.0),
                EndPoint = new Point(0.8, 1.0),
                GradientStops =
                {
                    new GradientStop((Color)ColorConverter.ConvertFromString("#FFFFFF"), 0.0),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#F1F5F9"), 0.45),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#CBD5E1"), 0.8),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#94A3B8"), 1.0)
                }
            };
        }

        bool hasScale = Math.Abs(scale - 1.0) > 0.01;
        bool hasOffset = Math.Abs(offsetX) > 0.01 || Math.Abs(offsetY) > 0.01;

        if (hasScale || hasOffset)
        {
            var transform = new TransformGroup();
            if (hasScale)
            {
                transform.Children.Add(new ScaleTransform(scale, scale, 16, 17));
            }
            if (hasOffset)
            {
                transform.Children.Add(new TranslateTransform(offsetX, offsetY));
            }
            transform.Freeze();

            dc.PushTransform(transform);
            dc.DrawGeometry(cloudBrush, null, BaseCloudGeo);
            dc.Pop();
        }
        else
        {
            dc.DrawGeometry(cloudBrush, null, BaseCloudGeo);
        }
    }

    private static void DrawSparkleStar(DrawingContext dc, double cx, double cy, double size, string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        var geo = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(cx, cy - size), IsClosed = true, IsFilled = true };
        fig.Segments.Add(new QuadraticBezierSegment(new Point(cx, cy), new Point(cx + size, cy), true));
        fig.Segments.Add(new QuadraticBezierSegment(new Point(cx, cy), new Point(cx, cy + size), true));
        fig.Segments.Add(new QuadraticBezierSegment(new Point(cx, cy), new Point(cx - size, cy), true));
        fig.Segments.Add(new QuadraticBezierSegment(new Point(cx, cy), new Point(cx, cy - size), true));
        geo.Figures.Add(fig);
        dc.DrawGeometry(brush, null, geo);
    }

    private static void DrawSnowflake(DrawingContext dc, double cx, double cy, double r)
    {
        var pen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")), 1.2)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        var centerBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));

        // Cross bars
        dc.DrawLine(pen, new Point(cx, cy - r), new Point(cx, cy + r));
        dc.DrawLine(pen, new Point(cx - r, cy), new Point(cx + r, cy));
        double d = r * 0.7071;
        dc.DrawLine(pen, new Point(cx - d, cy - d), new Point(cx + d, cy + d));
        dc.DrawLine(pen, new Point(cx - d, cy + d), new Point(cx + d, cy - d));

        // Center dot
        dc.DrawEllipse(centerBrush, null, new Point(cx, cy), 0.9, 0.9);
    }

    private static Brush CreateSunRayBrush()
    {
        return new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString("#FBBF24"),
            (Color)ColorConverter.ConvertFromString("#F59E0B"),
            new Point(0, 0), new Point(1, 1));
    }

    #endregion
}
