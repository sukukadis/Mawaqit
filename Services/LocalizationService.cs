using System;
using System.Collections.Generic;

namespace Mawaqit.Services;

public static class LocalizationService
{
    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        ["ar"] = new()
        {
            ["app_title"] = "مواقيت الصلاة",
            ["window_title"] = "مواقيت الصلاة",
            ["tab_prayer"] = "🕰️ مواقيت الصلاة",
            ["tab_settings"] = "⚙️ الإعدادات",
            ["loading"] = "جاري تحميل مواقيت الصلاة...",
            ["current_location"] = "الموقع الحالي: {0}",
            ["refresh"] = "🔄 تحديث",
            ["next_prayer"] = "الصلاة القادمة: {0}  •  {1}  •  متبقٍ {2}:{3:D2}:{4:D2}",
            ["ready"] = "جاهز",
            ["updated"] = "تم التحديث في {0}",
            ["fetching"] = "جاري تحميل المواقيت...",
            ["error"] = "خطأ: {0}",
            ["offline_cached"] = "غير متصل — عرض آخر بيانات محفوظة",
            ["no_file"] = "لم يتم اختيار ملف",
            ["sec_location"] = "📍 الموقع وطريقة الحساب",
            ["country"] = "الدولة:",
            ["wilaya"] = "الولاية / المحافظة / الإقليم:",
            ["calc_method"] = "طريقة الحساب:",
            ["auto_detect"] = "📍 تحديد موقعي تلقائياً",
            ["search_city"] = "🔍 بحث حول العالم",
            ["detecting_loc"] = "جاري تحديد موقعك...",
            ["detect_success"] = "تم تحديد الموقع بنجاح: {0}",
            ["detect_fail"] = "تعذر تحديد الموقع تلقائياً، يرجى البحث أو الاختيار يدوياً",
            ["sec_adhan"] = "🔊 الأذان",
            ["adhan_enable"] = "تشغيل الأذان وقت الصلاة",
            ["pick_mp3"] = "📂 اختيار ملف MP3",
            ["test_audio"] = "▶ تجربة",
            ["stop_audio"] = "⏹ إيقاف",
            ["sec_reminder"] = "⏰ التذكير قبل الأذان",
            ["reminder_enable"] = "التذكير قبل موعد الأذان",
            ["minutes_before"] = "دقائق قبل الأذان:",
            ["sec_notifications"] = "🔔 الإشعارات",
            ["adhan_popup_enable"] = "إشعار عند دخول وقت الأذان",
            ["reminder_popup_enable"] = "إشعار التذكير قبل الأذان",
            ["adhan_popup_title"] = "حان وقت الأذان",
            ["adhan_popup_body"] = "حان الآن وقت صلاة {0}",
            ["sec_theme"] = "🎨 المظهر",
            ["theme_light"] = "☀️ فاتح",
            ["theme_dark"] = "🌙 داكن",
            ["sec_language"] = "🌐 اللغة",
            ["lang_arabic"] = "العربية",
            ["lang_english"] = "English",
            ["sec_startup"] = "🚀 بدء التشغيل",
            ["startup_enable"] = "بدء التشغيل مع النظام",
            ["tune_title"] = "⏱️ تعديل المواقيت يدوياً بالدقائق (+/-) - خاص بالولاية الحالية",
            ["apply_tune"] = "تأكيد التعديل",
            ["tune_saved"] = "تم تطبيق التعديلات وتحديث الواجهة",
            ["Fajr"] = "الفجر",
            ["Sunrise"] = "الشروق",
            ["Dhuhr"] = "الظهر",
            ["Asr"] = "العصر",
            ["Maghrib"] = "المغرب",
            ["Isha"] = "العشاء",
            ["Jumua"] = "الجمعة",
            ["reminder_title"] = "تذكير بالصلاة",
            ["reminder_body"] = "متبقي {0} دقائق على صلاة {1}",
            ["invalid_mp3_title"] = "ملف غير صالح",
            ["invalid_mp3_body"] = "الملف المختار ليس ملف MP3 صالحاً.",
            ["tray_show"] = "إظهار التطبيق",
            ["tray_mute"] = "كتم الصوت",
            ["tray_unmute"] = "إلغاء الكتم",
            ["tray_volume"] = "الصوت",
            ["tray_custom"] = "مخصص…",
            ["tray_about"] = "حول التطبيق",
            ["tray_exit"] = "خروج",
            ["tray_next"] = "الصلاة القادمة",
            ["tray_loading"] = "جاري التحميل…",
            ["tray_left"] = "متبقٍ",
            ["tray_volume_muted"] = "مكتوم",
            ["tray_volume_fmt"] = "الصوت: {0}%",
            ["audio_missing_title"] = "ملف الصوت مفقود",
            ["audio_test_failed"] = "تعذّر تشغيل الملف",
            ["tray_taskbar_widget"] = "ودجت شريط المهام",
            ["sec_taskbar_widget"] = "⏱️ ودجت شريط المهام",
            ["taskbar_widget_enable"] = "إظهار عداد الصلاة على شريط المهام",
            ["taskbar_widget_show_seconds"] = "إظهار الثواني في العداد (01:23:45)",
            ["taskbar_widget_show_time"] = "إظهار وقت الصلاة بجانب الاسم",
            ["taskbar_widget_compact"] = "الوضع المضغوط (أيقونة + عداد فقط)",
            ["taskbar_widget_reset_pos"] = "📍 محاذاة تلقائية بجانب علبة النظام",
            ["taskbar_widget_menu_dock"] = "📌 محاذاة بجانب علبة النظام",
            ["taskbar_widget_menu_show_sec"] = "⏱️ إظهار الثواني",
            ["taskbar_widget_menu_show_time"] = "🕒 إظهار وقت الصلاة",
            ["taskbar_widget_menu_compact"] = "📏 الوضع المضغوط",
            ["taskbar_widget_menu_hide"] = "❌ إخفاء ودجت شريط المهام",
            ["taskbar_widget_time_now"] = "حان الآن وقت الصلاة!",
            ["taskbar_widget_elapsed"] = "مضى على الأذان: +{0}:{1:D2}:{2:D2}",
            ["taskbar_widget_tooltip"] = "مواقيت — {0} ({1}) • متبقٍ {2}:{3:D2}:{4:D2}\nانقر لإظهار التطبيق • اسحب للتحريك • انقر بالزر الأيمن للخيارات",
            ["taskbar_widget_tooltip_now"] = "مواقيت — حان الآن وقت صلاة {0} ({1})!\nانقر لإظهار التطبيق • اسحب للتحريك • انقر بالزر الأيمن للخيارات"
        },
        ["en"] = new()
        {
            ["app_title"] = "Mawaqit",
            ["window_title"] = "Mawaqit",
            ["tab_prayer"] = "🕰️ Prayer Times",
            ["tab_settings"] = "⚙️ Settings",
            ["loading"] = "Loading prayer times...",
            ["current_location"] = "Current location: {0}",
            ["refresh"] = "🔄 Refresh",
            ["next_prayer"] = "Next prayer: {0}  •  {1}  •  in {2}:{3:D2}:{4:D2}",
            ["ready"] = "Ready",
            ["updated"] = "Updated {0}",
            ["fetching"] = "Fetching prayer times...",
            ["error"] = "Error: {0}",
            ["offline_cached"] = "Offline — showing last saved data",
            ["no_file"] = "No file selected",
            ["sec_location"] = "📍 Location & Calculation Method",
            ["country"] = "Country:",
            ["wilaya"] = "State / Province / Wilaya:",
            ["calc_method"] = "Calculation Method:",
            ["auto_detect"] = "📍 Auto-Detect Location",
            ["search_city"] = "🔍 Search Worldwide",
            ["detecting_loc"] = "Detecting your location...",
            ["detect_success"] = "Location detected: {0}",
            ["detect_fail"] = "Could not auto-detect location, please search or select manually",
            ["sec_adhan"] = "🔊 Adhan",
            ["adhan_enable"] = "Play Adhan at prayer time",
            ["pick_mp3"] = "📂 Select MP3 file",
            ["test_audio"] = "▶ Test",
            ["stop_audio"] = "⏹ Stop",
            ["sec_reminder"] = "⏰ Pre-prayer Reminder",
            ["reminder_enable"] = "Enable reminder before prayer",
            ["minutes_before"] = "Minutes before:",
            ["sec_notifications"] = "🔔 Notifications",
            ["adhan_popup_enable"] = "Notify when prayer time arrives",
            ["reminder_popup_enable"] = "Show reminder notification",
            ["adhan_popup_title"] = "Prayer Time",
            ["adhan_popup_body"] = "It is now time for {0}",
            ["sec_theme"] = "🎨 Theme",
            ["theme_light"] = "☀️ Light",
            ["theme_dark"] = "🌙 Dark",
            ["sec_language"] = "🌐 Language",
            ["lang_arabic"] = "العربية",
            ["lang_english"] = "English",
            ["sec_startup"] = "🚀 Startup",
            ["startup_enable"] = "Start with Windows",
            ["tune_title"] = "⏱️ Manual Time Tuning (+/- minutes) - Specific to current Wilaya",
            ["apply_tune"] = "Apply Tuning",
            ["tune_saved"] = "Tuning applied and UI updated",
            ["Fajr"] = "Fajr",
            ["Sunrise"] = "Sunrise",
            ["Dhuhr"] = "Dhuhr",
            ["Asr"] = "Asr",
            ["Maghrib"] = "Maghrib",
            ["Isha"] = "Isha",
            ["Jumua"] = "Jumu'ah",
            ["reminder_title"] = "Prayer Reminder",
            ["reminder_body"] = "{0} minutes left for {1}",
            ["invalid_mp3_title"] = "Invalid file",
            ["invalid_mp3_body"] = "The selected file is not a valid MP3 file.",
            ["tray_show"] = "Show App",
            ["tray_mute"] = "Mute",
            ["tray_unmute"] = "Unmute",
            ["tray_volume"] = "Volume",
            ["tray_custom"] = "Custom...",
            ["tray_about"] = "About",
            ["tray_exit"] = "Exit",
            ["tray_next"] = "Next prayer",
            ["tray_loading"] = "Loading…",
            ["tray_left"] = "left",
            ["tray_volume_muted"] = "muted",
            ["tray_volume_fmt"] = "Volume: {0}%",
            ["audio_missing_title"] = "Audio file missing",
            ["audio_test_failed"] = "Could not play the file",
            ["tray_taskbar_widget"] = "Taskbar Widget",
            ["sec_taskbar_widget"] = "⏱️ Taskbar Widget",
            ["taskbar_widget_enable"] = "Show prayer countdown widget on taskbar",
            ["taskbar_widget_show_seconds"] = "Show seconds in countdown (01:23:45)",
            ["taskbar_widget_show_time"] = "Show prayer time next to name",
            ["taskbar_widget_compact"] = "Compact mode (icon + countdown only)",
            ["taskbar_widget_reset_pos"] = "📍 Reset position next to system tray",
            ["taskbar_widget_menu_dock"] = "📌 Dock next to system tray",
            ["taskbar_widget_menu_show_sec"] = "⏱️ Show seconds",
            ["taskbar_widget_menu_show_time"] = "🕒 Show prayer time",
            ["taskbar_widget_menu_compact"] = "📏 Compact mode",
            ["taskbar_widget_menu_hide"] = "❌ Hide Taskbar Widget",
            ["taskbar_widget_time_now"] = "Prayer time now!",
            ["taskbar_widget_elapsed"] = "Since Adhan: +{0}:{1:D2}:{2:D2}",
            ["taskbar_widget_tooltip"] = "Mawaqit — {0} ({1}) • {2}:{3:D2}:{4:D2} left\nClick to show app • Drag to reposition • Right click for options",
            ["taskbar_widget_tooltip_now"] = "Mawaqit — It is now time for {0} ({1})!\nClick to show app • Drag to reposition • Right click for options"
        }
    };

    public static readonly string[] ArabicDays = { "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت", "الأحد" };
    public static readonly string[] EnglishDays = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

    public static readonly string[] ArabicMonths = { "جانفي", "فيفري", "مارس", "أفريل", "ماي", "جوان", "جويلية", "أوت", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };
    public static readonly string[] EnglishMonths = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

    public static readonly string[] ArabicHijriMonths = { "محرم", "صفر", "ربيع الأول", "ربيع الثاني", "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة" };
    public static readonly string[] EnglishHijriMonths = { "Muharram", "Safar", "Rabi' al-Awwal", "Rabi' al-Thani", "Jumada al-Ula", "Jumada al-Akhirah", "Rajab", "Sha'ban", "Ramadan", "Shawwal", "Dhu al-Qi'dah", "Dhu al-Hijjah" };

    public static string Get(string key, string lang = "ar", params object[] args)
    {
        if (!Strings.TryGetValue(lang, out var dict) || !dict.TryGetValue(key, out var template))
        {
            if (Strings["ar"].TryGetValue(key, out var fallback))
                template = fallback;
            else
                return key;
        }

        if (args.Length > 0)
        {
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }
        return template;
    }
}
