using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Mawaqit.Models;
using Mawaqit.Services;

namespace Mawaqit.Controls;

public class SearchResultItem
{
    public Wilaya Location { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public ImageSource? FlagImage { get; set; }
}

public partial class CitySearchWindow : Window
{
    private readonly string _lang;
    private readonly ThemePalette _theme;
    private readonly bool _isDark;
    public Wilaya? SelectedLocation { get; private set; }

    public CitySearchWindow(string initialQuery, string lang, ThemePalette? theme = null, bool isDark = true)
    {
        _lang = lang;
        _isDark = isDark;
        _theme = theme ?? (isDark ? ThemePalette.Dark : ThemePalette.Light);

        // Apply theme resources before InitializeComponent
        ApplyThemeResources(_theme, _isDark);

        InitializeComponent();

        ApplyThemeElements(_theme, _isDark);

        bool isAr = lang == "ar";
        FlowDirection = isAr ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;

        TxtTitle.Text = isAr ? "🔍 البحث عن مدينة حول العالم" : "🔍 Search Worldwide Location";
        BtnSearch.Content = isAr ? "بحث" : "Search";
        BtnSelect.Content = isAr ? "اختيار" : "Select";
        BtnCancel.Content = isAr ? "إلغاء" : "Cancel";

        TxtSearchQuery.Text = initialQuery;

        Loaded += async (s, e) =>
        {
            try
            {
                DwmHelper.EnableWin11Styling(this, _isDark, true);
            }
            catch { }

            if (!string.IsNullOrWhiteSpace(initialQuery))
            {
                TxtSearchQuery.SelectAll();
                TxtSearchQuery.Focus();
                await PerformSearchAsync(initialQuery);
            }
            else
            {
                TxtSearchQuery.Focus();
            }
        };
    }

    private void ApplyThemeResources(ThemePalette theme, bool isDark)
    {
        Resources["CardBrush"] = theme.Card;
        Resources["BorderBrush"] = theme.Border;
        Resources["TextBrush"] = theme.Text;
        Resources["TextMutedBrush"] = theme.TextMuted;
        Resources["AccentBrush"] = theme.Accent;
        Resources["AccentHoverBrush"] = theme.AccentHover;
        Resources["EntryBgBrush"] = theme.EntryBg;
        Resources["BtnBgBrush"] = theme.BtnBg;
        Resources["BtnFgBrush"] = theme.BtnFg;
        Resources["Btn2BgBrush"] = theme.Btn2Bg;
        Resources["Btn2FgBrush"] = theme.Btn2Fg;
        Resources["Btn2HoverBrush"] = theme.Btn2Hover;

        if (isDark)
        {
            Resources["HoverBrush"] = theme.CardAlt;
            Resources["SelectedBrush"] = ThemePalette.HexToBrush("#1E382B"); // Rich dark emerald tint
        }
        else
        {
            Resources["HoverBrush"] = ThemePalette.HexToBrush("#EBEBEB");
            Resources["SelectedBrush"] = ThemePalette.HexToBrush("#D1FAE5"); // Soft emerald tint
        }
    }

    private void ApplyThemeElements(ThemePalette theme, bool isDark)
    {
        RootBorder.Background = theme.Card;
        RootBorder.BorderBrush = theme.Border;
        RootShadow.Opacity = isDark ? 0.45 : 0.15;

        TxtTitle.Foreground = theme.Text;

        BtnClose.Background = theme.Btn2Bg;
        BtnClose.Foreground = theme.Btn2Fg;
        BtnClose.BorderBrush = theme.Border;

        TxtSearchQuery.Background = theme.EntryBg;
        TxtSearchQuery.Foreground = theme.Text;
        TxtSearchQuery.BorderBrush = theme.Border;
        TxtSearchQuery.CaretBrush = theme.Text;

        BtnSearch.Background = theme.Accent;
        BtnSearch.Foreground = theme.BtnFg;

        BdResultsList.Background = theme.EntryBg;
        BdResultsList.BorderBrush = theme.Border;

        TxtStatus.Foreground = theme.TextMuted;

        BtnCancel.Background = theme.Btn2Bg;
        BtnCancel.Foreground = theme.Btn2Fg;
        BtnCancel.BorderBrush = theme.Border;

        BtnSelect.Background = theme.Accent;
        BtnSelect.Foreground = theme.BtnFg;
    }

    private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        string q = TxtSearchQuery.Text.Trim();
        if (string.IsNullOrEmpty(q)) return;
        await PerformSearchAsync(q);
    }

    private async void TxtSearchQuery_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            string q = TxtSearchQuery.Text.Trim();
            if (!string.IsNullOrEmpty(q))
            {
                await PerformSearchAsync(q);
            }
        }
    }

    private async System.Threading.Tasks.Task PerformSearchAsync(string query)
    {
        BtnSearch.IsEnabled = false;
        TxtStatus.Text = _lang == "ar" ? "جاري البحث..." : "Searching...";

        bool isAr = _lang == "ar";

        // 1. Search in local offline locations first
        var localMatches = Wilaya.FilterWilayas(query, isAr);

        // 2. Query online geocoding API for worldwide coverage
        var onlineMatches = await LocationService.SearchCitiesAsync(query, _lang);

        var combined = new List<Wilaya>();
        combined.AddRange(localMatches);

        foreach (var om in onlineMatches)
        {
            // Avoid duplicates
            bool exists = combined.Exists(c =>
                string.Equals(c.NameEn, om.NameEn, StringComparison.OrdinalIgnoreCase) &&
                Math.Abs(c.Latitude - om.Latitude) < 0.1 &&
                Math.Abs(c.Longitude - om.Longitude) < 0.1);

            if (!exists)
            {
                combined.Add(om);
            }
        }

        var items = combined.Select(w =>
        {
            var country = Country.FindCountry(w.CountryEn) ?? Country.FindCountry(w.CountryAr);
            return new SearchResultItem
            {
                Location = w,
                Title = isAr ? w.NameAr : w.NameEn,
                Subtitle = isAr
                    ? $"{w.CountryAr} • {w.Latitude:F2}°, {w.Longitude:F2}°"
                    : $"{w.CountryEn} • {w.Latitude:F2}°, {w.Longitude:F2}°",
                FlagImage = country?.FlagImage
            };
        }).ToList();

        LstResults.ItemsSource = items;
        BtnSearch.IsEnabled = true;

        if (items.Count > 0)
        {
            LstResults.SelectedIndex = 0;
            TxtStatus.Text = isAr ? $"تم العثور على {items.Count} نتيجة" : $"Found {items.Count} results";
        }
        else
        {
            TxtStatus.Text = isAr ? "لم يتم العثور على نتائج" : "No results found";
        }
    }

    private void BtnSelect_Click(object sender, RoutedEventArgs e)
    {
        ConfirmSelection();
    }

    private void LstResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ConfirmSelection();
    }

    private void ConfirmSelection()
    {
        if (LstResults.SelectedItem is SearchResultItem item)
        {
            SelectedLocation = item.Location;
            DialogResult = true;
            Close();
        }
        else if (LstResults.SelectedItem is Wilaya w)
        {
            SelectedLocation = w;
            DialogResult = true;
            Close();
        }
        else
        {
            string msg = _lang == "ar" ? "يرجى تحديد مدينة من القائمة أولاً" : "Please select a city from the list first";
            MessageBox.Show(msg, "Mawaqit", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

