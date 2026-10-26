using System.Windows.Media;

namespace Mawaqit.Models;

public class ThemePalette
{
    public Brush Background { get; set; } = null!;
    public Brush Card { get; set; } = null!;
    public Brush CardAlt { get; set; } = null!;
    public Brush Border { get; set; } = null!;
    public Brush Text { get; set; } = null!;
    public Brush TextMuted { get; set; } = null!;
    public Brush Accent { get; set; } = null!;
    public Brush AccentHover { get; set; } = null!;
    public Brush AccentText { get; set; } = null!;
    public Brush NextCard { get; set; } = null!;
    public Brush NextText { get; set; } = null!;
    public Brush TabBg { get; set; } = null!;
    public Brush TabSelBg { get; set; } = null!;
    public Brush TabSelFg { get; set; } = null!;
    public Brush EntryBg { get; set; } = null!;
    public Brush BtnBg { get; set; } = null!;
    public Brush BtnFg { get; set; } = null!;
    public Brush BtnHover { get; set; } = null!;
    public Brush Btn2Bg { get; set; } = null!;
    public Brush Btn2Fg { get; set; } = null!;
    public Brush Btn2Hover { get; set; } = null!;

    public static Color HexToColor(string hex) =>
        (Color)ColorConverter.ConvertFromString(hex);

    public static SolidColorBrush HexToBrush(string hex)
    {
        var brush = new SolidColorBrush(HexToColor(hex));
        brush.Freeze();
        return brush;
    }

    public static ThemePalette Dark => new()
    {
        Background = HexToBrush("#1F1F1F"),
        Card = HexToBrush("#2B2B2B"),
        CardAlt = HexToBrush("#333333"),
        Border = HexToBrush("#3A3A3A"),
        Text = HexToBrush("#FFFFFF"),
        TextMuted = HexToBrush("#A0A0A0"),
        Accent = HexToBrush("#10B981"),
        AccentHover = HexToBrush("#34D399"),
        AccentText = HexToBrush("#0B1220"),
        NextCard = HexToBrush("#10B981"),
        NextText = HexToBrush("#0B1220"),
        TabBg = HexToBrush("#2B2B2B"),
        TabSelBg = HexToBrush("#10B981"),
        TabSelFg = HexToBrush("#0B1220"),
        EntryBg = HexToBrush("#2B2B2B"),
        BtnBg = HexToBrush("#10B981"),
        BtnFg = HexToBrush("#0B1220"),
        BtnHover = HexToBrush("#34D399"),
        Btn2Bg = HexToBrush("#333333"),
        Btn2Fg = HexToBrush("#FFFFFF"),
        Btn2Hover = HexToBrush("#404040"),
    };

    public static ThemePalette Light => new()
    {
        Background = HexToBrush("#F3F3F3"),
        Card = HexToBrush("#FFFFFF"),
        CardAlt = HexToBrush("#FAFAFA"),
        Border = HexToBrush("#E5E5E5"),
        Text = HexToBrush("#1A1A1A"),
        TextMuted = HexToBrush("#5C5C5C"),
        Accent = HexToBrush("#10B981"),
        AccentHover = HexToBrush("#059669"),
        AccentText = HexToBrush("#FFFFFF"),
        NextCard = HexToBrush("#10B981"),
        NextText = HexToBrush("#FFFFFF"),
        TabBg = HexToBrush("#EBEBEB"),
        TabSelBg = HexToBrush("#10B981"),
        TabSelFg = HexToBrush("#FFFFFF"),
        EntryBg = HexToBrush("#FFFFFF"),
        BtnBg = HexToBrush("#10B981"),
        BtnFg = HexToBrush("#FFFFFF"),
        BtnHover = HexToBrush("#059669"),
        Btn2Bg = HexToBrush("#E8E8E8"),
        Btn2Fg = HexToBrush("#1A1A1A"),
        Btn2Hover = HexToBrush("#DCDCDC"),
    };
}
