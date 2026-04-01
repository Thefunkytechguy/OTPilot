using System.Windows;
using System.Windows.Controls;
using OTPilot.Models;
using OTPilot.Services;
using OTPilot.ViewModels;

namespace OTPilot.Views;

public partial class AddAccountWindow : Window
{
    private readonly AddAccountViewModel _viewModel = new();
    private bool _advancedVisible = false;

    public TotpAccount? Result { get; private set; }

    public AddAccountWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.Confirmed += () =>
        {
            Result = _viewModel.Result;
            DialogResult = true;
        };

        _viewModel.Cancelled += () => DialogResult = false;

        // Wire up QR scan — overlay takes a screenshot then user selects region
        _viewModel.ScanRequested += ScanQrFromScreenAsync;

        // Show error messages in the UI
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AddAccountViewModel.ErrorMessage))
            {
                var hasError = !string.IsNullOrEmpty(_viewModel.ErrorMessage);
                ErrorBorder.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
                ErrorText.Text = _viewModel.ErrorMessage;
            }
        };
    }

    private void ToggleAdvanced_Click(object sender, RoutedEventArgs e)
    {
        _advancedVisible = !_advancedVisible;
        AdvancedPanel.Visibility = _advancedVisible ? Visibility.Visible : Visibility.Collapsed;
        AdvancedArrow.Text = _advancedVisible ? "▼" : "▶";
    }

    private async void ScanQr_Click(object sender, RoutedEventArgs e)
    {
        var uri = await ScanQrFromScreenAsync();
        if (string.IsNullOrWhiteSpace(uri))
        {
            if (uri is not null) // null = cancelled; empty string = no QR found
                ShowError("No QR code found in the selected area. Please try again.");
            return;
        }

        var account = OtpAuthUriParser.Parse(uri);
        if (account is null)
        {
            ShowError("QR code found but could not be read as an authenticator account.");
            return;
        }

        _viewModel.PopulateFromAccount(account);
        HideError();
    }

    private async Task<string?> ScanQrFromScreenAsync()
    {
        // 1. Take a full-screen screenshot BEFORE showing the overlay
        var screenshot = CaptureFullScreen();
        if (screenshot is null) return null;

        // 2. Show the selection overlay — it is Topmost + Maximized so it
        //    covers everything. Do NOT Hide/Show this dialog window as that
        //    breaks WPF's modal dialog state and causes a crash on close.
        var overlay = new ScreenCaptureOverlay(screenshot);
        var selection = await overlay.GetSelectionAsync();
        Activate();

        if (selection is null)
        {
            screenshot.Dispose();
            return null; // User cancelled
        }

        // 3. Crop and decode
        try
        {
            using var cropped = CropBitmap(screenshot, selection.Value);
            screenshot.Dispose();
            var result = QrScanService.DecodeQrCode(cropped);
            return result ?? string.Empty; // empty string = no QR found
        }
        catch
        {
            screenshot.Dispose();
            return string.Empty;
        }
    }

    private static System.Drawing.Bitmap? CaptureFullScreen()
    {
        try
        {
            // Use physical screen dimensions
            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            // Get DPI scale — SystemParameters uses DIPs, CopyFromScreen uses physical pixels
            var source = PresentationSource.FromVisual(Application.Current.MainWindow);
            var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

            var physWidth = (int)(screenWidth * dpiX);
            var physHeight = (int)(screenHeight * dpiY);

            return QrScanService.CaptureScreen(0, 0, physWidth, physHeight);
        }
        catch
        {
            return null;
        }
    }

    private static System.Drawing.Bitmap CropBitmap(System.Drawing.Bitmap source, System.Drawing.Rectangle rect)
    {
        // Clamp rect to source bounds
        var x = Math.Max(0, rect.X);
        var y = Math.Max(0, rect.Y);
        var w = Math.Min(rect.Width, source.Width - x);
        var h = Math.Min(rect.Height, source.Height - y);

        if (w <= 0 || h <= 0)
            return new System.Drawing.Bitmap(1, 1);

        var cropped = new System.Drawing.Bitmap(w, h);
        using var g = System.Drawing.Graphics.FromImage(cropped);
        g.DrawImage(source, new System.Drawing.Rectangle(0, 0, w, h),
                    new System.Drawing.Rectangle(x, y, w, h),
                    System.Drawing.GraphicsUnit.Pixel);
        return cropped;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorBorder.Visibility = Visibility.Collapsed;
        ErrorText.Text = string.Empty;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e) =>
        _viewModel.ConfirmCommand.Execute(null);

    private void Cancel_Click(object sender, RoutedEventArgs e) =>
        _viewModel.CancelCommand.Execute(null);
}
