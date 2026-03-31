using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace OTPilot.Views;

public partial class ScreenCaptureOverlay : Window
{
    private readonly System.Drawing.Bitmap _screenshot;
    private readonly TaskCompletionSource<System.Drawing.Rectangle?> _tcs = new();

    private bool _isDragging;
    private Point _startPoint;
    private double _dpiX = 1.0;
    private double _dpiY = 1.0;

    public ScreenCaptureOverlay(System.Drawing.Bitmap screenshot)
    {
        InitializeComponent();
        _screenshot = screenshot;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Get DPI scale for coordinate conversion
        var source = PresentationSource.FromVisual(this);
        _dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        _dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        // Display screenshot as background
        ScreenshotImage.Source = ConvertToImageSource(_screenshot);

        // Size instruction banner to full width
        InstructionBanner.Width = ActualWidth > 0 ? ActualWidth : SystemParameters.PrimaryScreenWidth;

        // Initialise dark mask to cover entire screen
        UpdateMask(0, 0, 0, 0);
    }

    /// <summary>
    /// Shows the overlay and returns the selected region in physical pixels,
    /// or null if the user cancelled.
    /// </summary>
    public Task<System.Drawing.Rectangle?> GetSelectionAsync()
    {
        Show();
        return _tcs.Task;
    }

    // ── Mouse events ──────────────────────────────────────────────────────

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _startPoint = e.GetPosition(OverlayCanvas);
        OverlayCanvas.CaptureMouse();

        InstructionBanner.Visibility = Visibility.Collapsed;
        SelectionRect.Visibility = Visibility.Visible;
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var current = e.GetPosition(OverlayCanvas);
        UpdateSelectionVisuals(_startPoint, current);
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;

        _isDragging = false;
        OverlayCanvas.ReleaseMouseCapture();

        var end = e.GetPosition(OverlayCanvas);
        var rect = GetNormalisedRect(_startPoint, end);

        if (rect.Width < 10 || rect.Height < 10)
        {
            // Too small — likely an accidental click
            _tcs.SetResult(null);
        }
        else
        {
            // Convert WPF DIPs → physical pixels for screen capture
            var physRect = new System.Drawing.Rectangle(
                (int)(rect.X * _dpiX),
                (int)(rect.Y * _dpiY),
                (int)(rect.Width * _dpiX),
                (int)(rect.Height * _dpiY));

            _tcs.SetResult(physRect);
        }

        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _tcs.TrySetResult(null);
            Close();
        }
    }

    // ── Visuals ───────────────────────────────────────────────────────────

    private void UpdateSelectionVisuals(Point start, Point current)
    {
        var rect = GetNormalisedRect(start, current);

        // Selection rectangle border
        Canvas.SetLeft(SelectionRect, rect.X);
        Canvas.SetTop(SelectionRect, rect.Y);
        SelectionRect.Width = rect.Width;
        SelectionRect.Height = rect.Height;

        UpdateMask(rect.X, rect.Y, rect.Width, rect.Height);
    }

    private void UpdateMask(double x, double y, double w, double h)
    {
        var totalW = OverlayCanvas.ActualWidth;
        var totalH = OverlayCanvas.ActualHeight;

        if (totalW <= 0 || totalH <= 0) return;

        // Top strip
        Canvas.SetLeft(MaskTop, 0);
        Canvas.SetTop(MaskTop, 0);
        MaskTop.Width = totalW;
        MaskTop.Height = y;

        // Bottom strip
        Canvas.SetLeft(MaskBottom, 0);
        Canvas.SetTop(MaskBottom, y + h);
        MaskBottom.Width = totalW;
        MaskBottom.Height = Math.Max(0, totalH - y - h);

        // Left strip (between top and bottom)
        Canvas.SetLeft(MaskLeft, 0);
        Canvas.SetTop(MaskLeft, y);
        MaskLeft.Width = x;
        MaskLeft.Height = h;

        // Right strip (between top and bottom)
        Canvas.SetLeft(MaskRight, x + w);
        Canvas.SetTop(MaskRight, y);
        MaskRight.Width = Math.Max(0, totalW - x - w);
        MaskRight.Height = h;
    }

    private static Rect GetNormalisedRect(Point a, Point b) => new(
        Math.Min(a.X, b.X),
        Math.Min(a.Y, b.Y),
        Math.Abs(b.X - a.X),
        Math.Abs(b.Y - a.Y));

    // ── Bitmap → WPF ImageSource ──────────────────────────────────────────

    private static BitmapSource ConvertToImageSource(System.Drawing.Bitmap bitmap)
    {
        var handle = bitmap.GetHbitmap();
        try
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                handle,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(handle);
        }
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
