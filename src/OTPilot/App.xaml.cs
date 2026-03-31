using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using OTPilot.Services;
using OTPilot.ViewModels;
using OTPilot.Views;

namespace OTPilot;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private static bool _isExiting;

    // Expose flag so MainWindow.Closing can check it
    public static bool IsExiting => _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Wire up services ──────────────────────────────────────────────
        var vaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OTPilot", "vault.dat");

        var storage = new LocalStorageProvider(vaultPath);
        var vaultService = new VaultService(storage);
        var totpService = new TotpService();

        // ── Setup system tray icon ────────────────────────────────────────
        _trayIcon = new TaskbarIcon
        {
            Icon = CreateTrayIcon(),
            ToolTipText = "OTPilot — Authenticator"
        };
        _trayIcon.TrayMouseDoubleClick += (_, _) => ToggleMainWindow();

        var menu = new ContextMenu();

        var showItem = new MenuItem { Header = "Show OTPilot" };
        showItem.Click += (_, _) => ShowMainWindow();

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => ExitApp();

        menu.Items.Add(showItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);
        _trayIcon.ContextMenu = menu;

        // ── Create main window ────────────────────────────────────────────
        var viewModel = new MainViewModel(vaultService, totpService);
        var mainWindow = new MainWindow(viewModel);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }

    // ── Tray helpers ──────────────────────────────────────────────────────

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

    // ── Programmatic tray icon ────────────────────────────────────────────

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private static System.Drawing.Icon CreateTrayIcon()
    {
        using var bitmap = new System.Drawing.Bitmap(32, 32);
        using var g = System.Drawing.Graphics.FromImage(bitmap);

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);

        // Blue circle background
        g.FillEllipse(
            new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(37, 99, 235)),
            1, 1, 30, 30);

        // White "O" letter
        using var font = new System.Drawing.Font(
            "Segoe UI", 14, System.Drawing.FontStyle.Bold,
            System.Drawing.GraphicsUnit.Pixel);

        var sf = new System.Drawing.StringFormat
        {
            Alignment = System.Drawing.StringAlignment.Center,
            LineAlignment = System.Drawing.StringAlignment.Center
        };

        g.DrawString("O", font, System.Drawing.Brushes.White,
            new System.Drawing.RectangleF(0, 0, 32, 32), sf);

        // Clone the icon properly (GetHicon handle must be released)
        var handle = bitmap.GetHicon();
        var icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(handle).Clone();
        DestroyIcon(handle);
        return icon;
    }
}
