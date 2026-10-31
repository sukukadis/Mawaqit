using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Mawaqit.Models;
using Mawaqit.Services;

namespace Mawaqit.Controls;

public partial class AboutWindow : Window
{
    public AboutWindow(ThemePalette theme, bool isDark)
    {
        InitializeComponent();

        TxtVersion.Text = $"Version {GetType().Assembly.GetName().Version?.ToString(3) ?? "2.2.6"}";
        ApplyTheme(theme, isDark);
        LoadLogo();

        KeyDown += (s, e) =>
        {
            if (e.Key is Key.Escape or Key.Enter)
            {
                Close();
            }
        };

        Loaded += (s, e) =>
        {
            try
            {
                DwmHelper.EnableWin11Styling(this, isDark, true);
            }
            catch { }
        };
    }

    private void ApplyTheme(ThemePalette theme, bool isDark)
    {
        // Root card background and border identical to the app theme
        RootBorder.Background = theme.Card;
        RootBorder.BorderBrush = theme.Border;
        RootShadow.Opacity = isDark ? 0.45 : 0.15;

        // Title text directly under logo
        TxtAppTitle.Foreground = theme.Text;

        // Version badge
        BdVersion.Background = theme.Btn2Bg;
        BdVersion.BorderBrush = theme.Border;
        TxtVersion.Foreground = theme.TextMuted;

        // Divider
        SepDivider.Background = theme.Border;

        // Developer info & copyright
        TxtDevBy.Foreground = theme.Text;
        TxtCopyright.Foreground = theme.TextMuted;

        // GitHub button in application signature emerald green color
        BtnGitHub.Background = theme.Accent;
        BtnGitHub.BorderBrush = theme.Accent;
        BtnGitHub.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#FFFFFF")!;
        BtnGitHub.MouseEnter += (s, e) => BtnGitHub.Background = theme.AccentHover;
        BtnGitHub.MouseLeave += (s, e) => BtnGitHub.Background = theme.Accent;

        // Close button matching app button styling
        BtnClose.Background = theme.Btn2Bg;
        BtnClose.BorderBrush = theme.Border;
        BtnClose.Foreground = theme.Btn2Fg;
        BtnClose.MouseEnter += (s, e) => BtnClose.Background = theme.Btn2Hover;
        BtnClose.MouseLeave += (s, e) => BtnClose.Background = theme.Btn2Bg;
    }

    private void LoadLogo()
    {
        try
        {
            string[] pngCandidates = new[]
            {
                Path.Combine(AppConfig.AppDataFolder, "logo.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png")
            };

            foreach (var path in pngCandidates)
            {
                if (File.Exists(path))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(path, UriKind.Absolute);
                    bmp.EndInit();
                    bmp.Freeze();
                    ImgLogo.Source = bmp;
                    return;
                }
            }

            try
            {
                var packUri = new Uri("pack://application:,,,/Resources/logo.png", UriKind.RelativeOrAbsolute);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = packUri;
                bmp.EndInit();
                bmp.Freeze();
                ImgLogo.Source = bmp;
                return;
            }
            catch { }

            string icoPath = Path.Combine(AppConfig.AppDataFolder, "logo.ico");
            if (!File.Exists(icoPath))
            {
                icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.ico");
            }

            if (File.Exists(icoPath))
            {
                using var stream = File.OpenRead(icoPath);
                var decoder = new IconBitmapDecoder(
                    stream,
                    BitmapCreateOptions.None,
                    BitmapCacheOption.OnLoad);

                // Select the highest resolution frame for maximum sharpness
                BitmapFrame? bestFrame = null;
                int maxDim = 0;
                foreach (var frame in decoder.Frames)
                {
                    if (frame.PixelWidth > maxDim)
                    {
                        maxDim = frame.PixelWidth;
                        bestFrame = frame;
                    }
                }

                if (bestFrame != null)
                {
                    ImgLogo.Source = bestFrame;
                    return;
                }
            }
        }
        catch { }

        try
        {
            var packUri = new Uri("pack://application:,,,/Resources/logo.ico", UriKind.RelativeOrAbsolute);
            ImgLogo.Source = new BitmapImage(packUri);
        }
        catch { }
    }

    private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
            }
            catch { }
        }
    }

    private void BtnGitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/sukukadis/")
            {
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
