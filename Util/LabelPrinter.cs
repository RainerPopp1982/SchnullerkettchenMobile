using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using SkiaSharp;

namespace SchnullerkettchenMobile.Util;

public static class LabelPrinter
{
    private const int WidthPx = 496;  // 62 mm bei 203 dpi
    private const int HeightPx = 232; // 29 mm bei 203 dpi

    public static string CreateAddressLabel(string name, string strasse, string ort, string land)
    {
        using SKBitmap bitmap = new(WidthPx, HeightPx);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.White);
        using SKPaint textPaint = new() { Color = SKColors.Black, IsAntialias = true };
        using SKFont nameFont = new(SKTypeface.FromFamilyName(null, SKFontStyle.Bold), 34);
        using SKFont textFont = new(SKTypeface.FromFamilyName(null), 30);
        const float x = 24;
        const float lineHeight = 42;
        float y = 56;
        canvas.DrawText(name, x, y, SKTextAlign.Left, nameFont, textPaint);
        y += lineHeight;
        canvas.DrawText(strasse, x, y, SKTextAlign.Left, textFont, textPaint);
        y += lineHeight;
        canvas.DrawText(ort, x, y, SKTextAlign.Left, textFont, textPaint);
        if (!string.IsNullOrWhiteSpace(land) && !land.Equals("Deutschland", StringComparison.OrdinalIgnoreCase))
        {
            y += lineHeight;
            canvas.DrawText(land, x, y, SKTextAlign.Left, textFont, textPaint);
        }
        string path = Path.Combine(FileSystem.CacheDirectory, $"etikett_{DateTime.Now:yyyyMMddHHmmss}.png");
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        using FileStream stream = File.OpenWrite(path);
        data.SaveTo(stream);
        return path;
    }

    public static Task ShareAsync(string filePath, string title = "Adressetikett") =>
        Share.Default.RequestAsync(new ShareFileRequest { Title = title, File = new ShareFile(filePath) });
}
