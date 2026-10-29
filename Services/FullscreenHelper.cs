using System;
using System.Runtime.InteropServices;

namespace Mawaqit.Services;

public static class FullscreenHelper
{
    private enum QUERY_USER_NOTIFICATION_STATE
    {
        QUNS_NOT_PRESENT = 1,
        QUNS_BUSY = 2,
        QUNS_RUNNING_D3D_FULL_SCREEN = 3,
        QUNS_PRESENTATION_MODE = 4,
        QUNS_ACCEPTS_NOTIFICATIONS = 5,
        QUNS_QUIET_TIME = 6,
        QUNS_APP = 7
    }

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out QUERY_USER_NOTIFICATION_STATE pquns);

    /// <summary>
    /// Checks whether an exclusive Direct3D fullscreen game/application
    /// or presentation is currently active.
    /// In this state, desktop windows should NOT be presented to avoid D3D surface reset.
    /// In Borderless Windowed mode, notifications can safely appear without focus loss.
    /// </summary>
    public static bool IsExclusiveFullscreenRunning()
    {
        try
        {
            if (SHQueryUserNotificationState(out var state) == 0)
            {
                if (state == QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN ||
                    state == QUERY_USER_NOTIFICATION_STATE.QUNS_PRESENTATION_MODE)
                {
                    return true;
                }
            }
        }
        catch { }

        return false;
    }
}
