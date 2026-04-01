using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using OTPilot.Models;
using OTPilot.Services;
using OTPilot.ViewModels;
using OTPilot.Views;

namespace OTPilot;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private static bool _isExiting;
    private static Mutex? _singleInstanceMutex;
    private static EventWaitHandle? _showWindowEvent;
    private Thread? _listenerThread;

    public static bool IsExiting => _isExiting;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // ── Single-instance guard ──────────────────────────────────────────
        _singleInstanceMutex = new Mutex(true, "Global\\OTPilot_SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
        {
            // Signal the running instance to show its window, then exit
            try
            {
                var ev = EventWaitHandle.OpenExisting("Global\\OTPilot_ShowWindow");
                ev.Set();
            }
            catch { }
            Shutdown();
            return;
        }

        // Listen for show-window signals from any future second instance
        _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Global\\OTPilot_ShowWindow");
        _listenerThread  = new Thread(() =>
        {
            while (true)
            {
                _showWindowEvent.WaitOne();
                Dispatcher.Invoke(ShowMainWindow);
            }
        })
        { IsBackground = true };
        _listenerThread.Start();

        base.OnStartup(e);

        // ── Load or create config ──────────────────────────────────────────
        var config = await AppConfigService.LoadAsync();

        if (!config.SetupComplete)
        {
            var setup = new StorageSetupWindow();
            if (setup.ShowDialog() != true || setup.ResultConfig is null)
            {
                // User closed the wizard without completing — exit cleanly
                Shutdown();
                return;
            }
            config = setup.ResultConfig;
            await AppConfigService.SaveAsync(config);
        }

        // ── Wire up storage provider based on config ───────────────────────
        IStorageProvider storage = config.StorageType switch
        {
            "OneDrive" => new OneDriveStorageProvider(),
            "Custom"   => new LocalStorageProvider(
                              Path.Combine(config.CustomPath!, "OTPilot", "vault.dat")),
            _          => new LocalStorageProvider(
                              Path.Combine(
                                  Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                  "OTPilot", "vault.dat"))
        };

        var vaultService = new VaultService(storage);
        var totpService  = new TotpService();

        // ── Setup system tray ──────────────────────────────────────────────
        _trayIcon = new TaskbarIcon
        {
            Icon        = LoadTrayIcon(),
            ToolTipText = "OTPilot — Authenticator"
        };
        _trayIcon.TrayMouseDoubleClick += (_, _) => ToggleMainWindow();

        var menu     = new ContextMenu();
        var showItem = new MenuItem { Header = "Show OTPilot" };
        showItem.Click += (_, _) => ShowMainWindow();
        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => ExitApp();

        menu.Items.Add(showItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);
        _trayIcon.ContextMenu = menu;

        // ── Create main window ─────────────────────────────────────────────
        var viewModel  = new MainViewModel(vaultService, totpService, storage.DisplayName);
        var mainWindow = new MainWindow(viewModel);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _showWindowEvent?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void ToggleMainWindow()
    {
        if (MainWindow?.IsVisible == true)
            MainWindow.Hide();
        else
            ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        MainWindow?.Show();
        if (MainWindow?.WindowState == WindowState.Minimized)
            MainWindow.WindowState = WindowState.Normal;
        MainWindow?.Activate();
    }

    private void ExitApp()
    {
        _isExiting = true;
        _trayIcon?.Dispose();
        Shutdown();
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var sri = Application.GetResourceStream(
                new Uri("pack://application:,,,/Resources/OTPilot.ico"));
            if (sri != null)
                return new System.Drawing.Icon(sri.Stream);
        }
        catch { }

        return System.Drawing.SystemIcons.Application;
    }
}
