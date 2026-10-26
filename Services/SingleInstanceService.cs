using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mawaqit.Services;

public class SingleInstanceService : IDisposable
{
    private const string MutexName = @"Local\Mawaqit_SingleInstance_Mutex_v200";
    private const string EventName = @"Local\Mawaqit_ShowEvent_v200";
    private const int ERROR_ALREADY_EXISTS = 183;
    private const uint EVENT_MODIFY_STATE = 0x0002;
    private const uint WAIT_OBJECT_0 = 0x00000000;

    private IntPtr _mutexHandle = IntPtr.Zero;
    private IntPtr _eventHandle = IntPtr.Zero;
    private Thread? _eventListenerThread;
    private bool _isDisposed;
    private readonly Action? _onShowRequested;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateMutexW(IntPtr lpMutexAttributes, bool bInitialOwner, string lpName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateEventW(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr OpenEventW(uint dwDesiredAccess, bool bInheritHandle, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetEvent(IntPtr hEvent);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;

    public SingleInstanceService(Action? onShowRequested = null)
    {
        _onShowRequested = onShowRequested;
    }

    public bool TryAcquire(out bool isFirstInstance)
    {
        try
        {
            _mutexHandle = CreateMutexW(IntPtr.Zero, false, MutexName);
            int err = Marshal.GetLastWin32Error();

            if (_mutexHandle == IntPtr.Zero)
            {
                isFirstInstance = true;
                return true;
            }

            isFirstInstance = (err != ERROR_ALREADY_EXISTS);
            if (isFirstInstance)
            {
                _eventHandle = CreateEventW(IntPtr.Zero, false, false, EventName);
                StartEventListener();
            }
            return isFirstInstance;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Mutex acquisition error: {ex.Message}");
            isFirstInstance = true;
            return true;
        }
    }

    public static void SignalExistingInstance()
    {
        try
        {
            IntPtr ev = OpenEventW(EVENT_MODIFY_STATE, false, EventName);
            if (ev != IntPtr.Zero)
            {
                SetEvent(ev);
                CloseHandle(ev);
            }
        }
        catch { }

        try
        {
            string[] titles = { "Mawaqit", "مواقيت الصلاة" };
            foreach (var title in titles)
            {
                IntPtr hWnd = FindWindow(null, title);
                if (hWnd != IntPtr.Zero)
                {
                    ShowWindow(hWnd, SW_RESTORE);
                    ShowWindow(hWnd, SW_SHOW);
                    SetForegroundWindow(hWnd);
                    break;
                }
            }
        }
        catch { }
    }

    private void StartEventListener()
    {
        _eventListenerThread = new Thread(() =>
        {
            while (!_isDisposed && _eventHandle != IntPtr.Zero)
            {
                try
                {
                    uint waitRes = WaitForSingleObject(_eventHandle, 1000);
                    if (waitRes == WAIT_OBJECT_0)
                    {
                        if (!_isDisposed)
                        {
                            _onShowRequested?.Invoke();
                        }
                    }
                }
                catch { break; }
            }
        })
        {
            IsBackground = true,
            Name = "MawaqitShowEventListener"
        };
        _eventListenerThread.Start();
    }

    public void Dispose()
    {
        _isDisposed = true;

        if (_eventHandle != IntPtr.Zero)
        {
            CloseHandle(_eventHandle);
            _eventHandle = IntPtr.Zero;
        }

        if (_mutexHandle != IntPtr.Zero)
        {
            CloseHandle(_mutexHandle);
            _mutexHandle = IntPtr.Zero;
        }
    }
}
