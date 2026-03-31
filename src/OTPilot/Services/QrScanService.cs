using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;

namespace OTPilot.Services;

public static class QrScanService
{
    /// <summary>
    /// Captures a region of the physical screen and attempts to decode a QR code from it.
    /// Coordinates are in physical (device) pixels.
    /// </summary>
    public static string? CaptureAndDecode(int x, int y, int width, int height)
    {
        using var bitmap = CaptureScreen(x, y, width, height);
        return DecodeQrCode(bitmap);
    }

    /// <summary>
    /// Attempts to decode a QR code from a System.Drawing.Bitmap.
    /// </summary>
    public static string? DecodeQrCode(Bitmap bitmap)
    {
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            var bytes = new byte[Math.Abs(bitmapData.Stride) * bitmap.Height];
            Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);

            var luminance = new RGBLuminanceSource(
                bytes, bitmap.Width, bitmap.Height,
                RGBLuminanceSource.BitmapFormat.BGRA32);

            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                }
            };

            var result = reader.Decode(luminance);
            return result?.Text;
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    public static Bitmap CaptureScreen(int x, int y, int width, int height)
    {
        var bitmap = new Bitmap(Math.Max(1, width), Math.Max(1, height), PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        return bitmap;
    }
}
