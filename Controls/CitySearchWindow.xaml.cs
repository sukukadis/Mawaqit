using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Mawaqit.Models;
using Mawaqit.Services;

namespace Mawaqit.Controls;

public partial class CitySearchWindow : Window
{
    private readonly string _lang;
    public Wilaya? SelectedLocation { get; private set; }

    public CitySearchWindow(string initialQuery, string lang, ThemePalette? theme = null)
    {
        _lang = lang;
        InitializeComponent();

        bool isAr = lang == "ar";
        FlowDirection = isAr ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;

        TxtTitle.Text = isAr ? "🔍 البحث عن مدينة حول العالم" : "🔍 Search Worldwide Location";
        BtnSearch.Content = isAr ? "بحث" : "Search";
        BtnSelect.Content = isAr ? "اختيار" : "Select";
        BtnCancel.Content = isAr ? "إلغاء" : "Cancel";

        if (theme != null)
        {
            RootBorder.Background = theme.Card;
            RootBorder.BorderBrush = theme.Border;
            BtnSelect.Background = theme.Accent;
        }

        TxtSearchQuery.Text = initialQuery;
        if (!string.IsNullOrWhiteSpace(initialQuery))
        {
            Loaded += async (s, e) =>
            {
                TxtSearchQuery.SelectAll();
                TxtSearchQuery.Focus();
                await PerformSearchAsync(initialQuery);
            };
        }
        else
        {
            Loaded += (s, e) =>
            {
                TxtSearchQuery.Focus();
            };
        }
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

        // 1. Search in local offline locations first
        var localMatches = Wilaya.FilterWilayas(query, _lang == "ar");

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

        LstResults.ItemsSource = combined;
        BtnSearch.IsEnabled = true;

        if (combined.Count > 0)
        {
            LstResults.SelectedIndex = 0;
            TxtStatus.Text = _lang == "ar" ? $"تم العثور على {combined.Count} نتيجة" : $"Found {combined.Count} results";
        }
        else
        {
            TxtStatus.Text = _lang == "ar" ? "لم يتم العثور على نتائج" : "No results found";
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
        if (LstResults.SelectedItem is Wilaya w)
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
