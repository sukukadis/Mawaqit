using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Mawaqit.Models;
using Mawaqit.Services;
using WpfFlowDirection = System.Windows.FlowDirection;

namespace Mawaqit.Controls;

public partial class ThemedMessageBox : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    public ThemedMessageBox()
    {
        InitializeComponent();
    }

    public static MessageBoxResult Show(
        Window? owner,
        string message,
        string title,
        MessageBoxButton button,
        MessageBoxImage image,
        ThemePalette palette,
        bool isDark,
        string lang = "ar")
    {
        var dlg = new ThemedMessageBox();
        if (owner != null && owner.IsVisible)
        {
            dlg.Owner = owner;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dlg.Topmost = true;
        }

        bool isAr = lang == "ar";
        dlg.FlowDirection = isAr ? WpfFlowDirection.RightToLeft : WpfFlowDirection.LeftToRight;

        dlg.TxtTitle.Text = title;
        dlg.TxtMessage.Text = message;

        // Icon
        dlg.TxtIcon.Text = image switch
        {
            MessageBoxImage.Information => "ℹ️",
            MessageBoxImage.Warning => "⚠️",
            MessageBoxImage.Error => "❌",
            MessageBoxImage.Question => "❓",
            _ => "ℹ️"
        };

        // Buttons
        if (button == MessageBoxButton.YesNo)
        {
            dlg.BtnOk.Visibility = Visibility.Collapsed;
            dlg.BtnYes.Visibility = Visibility.Visible;
            dlg.BtnNo.Visibility = Visibility.Visible;
            dlg.BtnYes.Content = isAr ? "نعم" : "Yes";
            dlg.BtnNo.Content = isAr ? "لا" : "No";
        }
        else
        {
            dlg.BtnOk.Visibility = Visibility.Visible;
            dlg.BtnYes.Visibility = Visibility.Collapsed;
            dlg.BtnNo.Visibility = Visibility.Collapsed;
            dlg.BtnOk.Content = isAr ? "حسناً" : "OK";
        }

        // Apply theme styling
        dlg.DialogBorder.Background = palette.Card;
        dlg.DialogBorder.BorderBrush = palette.Border;
        dlg.TitleBorder.Background = palette.CardAlt;
        dlg.BottomBorder.Background = palette.CardAlt;

        dlg.TxtTitle.Foreground = palette.Text;
        dlg.TxtMessage.Foreground = palette.Text;
        dlg.BtnCloseTop.Foreground = palette.TextMuted;

        // Button colors
        dlg.BtnOk.Background = palette.BtnBg;
        dlg.BtnOk.Foreground = palette.BtnFg;
        dlg.BtnOk.BorderBrush = palette.Border;
        dlg.BtnOk.BorderThickness = new Thickness(1);

        dlg.BtnYes.Background = palette.BtnBg;
        dlg.BtnYes.Foreground = palette.BtnFg;
        dlg.BtnYes.BorderBrush = palette.Border;
        dlg.BtnYes.BorderThickness = new Thickness(1);

        dlg.BtnNo.Background = palette.Btn2Bg;
        dlg.BtnNo.Foreground = palette.Btn2Fg;
        dlg.BtnNo.BorderBrush = palette.Border;
        dlg.BtnNo.BorderThickness = new Thickness(1);

        dlg.Loaded += (s, e) =>
        {
            DwmHelper.EnableWin11Styling(dlg, isDark, true);
        };

        dlg.ShowDialog();
        return dlg.Result;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.OK;
        Close();
    }

    private void BtnYes_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Yes;
        Close();
    }

    private void BtnNo_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.No;
        Close();
    }

    private void BtnCloseTop_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Cancel;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
