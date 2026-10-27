using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Mawaqit.Models;
using Mawaqit.Services;

namespace Mawaqit;

public partial class App : Application
{
    private SingleInstanceService? _singleInstance;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, args) =>
        {
            Logger.LogError($"Unhandled Dispatcher Exception: {args.Exception}");
            if (_mainWindow == null)
            {
                MessageBox.Show($"Mawaqit failed to start:\n{args.Exception.Message}\n\nCheck {AppConfig.ErrorLogPath} for details.", "Mawaqit Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            Logger.LogError($"Unhandled AppDomain Exception: {args.ExceptionObject}");
        };

        string[] args = e.Args;
        Logger.LogStartup("========== Mawaqit C# start ==========");
        Logger.LogStartup($"argv={string.Join(" ", args)}");

        if (args.Contains("--uninstall"))
        {
            RegistryService.HandleUninstall();
            Shutdown(0);
            return;
        }

        if (args.Contains("--enable-startup"))
        {
            var config = AppConfig.Load();
            config.StartWithWindows = true;
            config.Save();
            RegistryService.SyncStartup(true, force: true);
        }
        else if (args.Contains("--disable-startup"))
        {
            var config = AppConfig.Load();
            config.StartWithWindows = false;
            config.Save();
            RegistryService.SyncStartup(false, force: true);
        }

        // Single instance check
        _singleInstance = new SingleInstanceService(() =>
        {
            Dispatcher.Invoke(() => _mainWindow?.ShowAndActivate());
        });

        if (!_singleInstance.TryAcquire(out bool isFirst) || !isFirst)
        {
            Logger.LogStartup("another instance already running; signalling and exiting");
            SingleInstanceService.SignalExistingInstance();
            Shutdown(0);
            return;
        }

        try
        {
            bool isStartup = args.Contains("--startup");
            Logger.LogStartup($"startup_launch={isStartup}");

            _mainWindow = new MainWindow(isStartup);
            MainWindow = _mainWindow;

            if (!isStartup)
            {
                _mainWindow.Show();
                _mainWindow.Activate();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize MainWindow: {ex}");
            MessageBox.Show($"Mawaqit failed to start:\n{ex.Message}\n\nCheck {AppConfig.ErrorLogPath} for details.", "Mawaqit Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
