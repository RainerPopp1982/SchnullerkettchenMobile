using SkiaSharp;

namespace SchnullerkettchenMobile.Util;

// Erzeugt aus einem Original-Produktbild drei verkleinerte JPEG-Varianten für den Bilder-Upload,
// analog zur Desktop-Logik in ImageConverterSingle (Programme/Artikel/Bilder).
public static class ImageResizer
{
    public const int SizeThumbnail = 250;
    public const int SizeMedium = 500;
    public const int SizeFull = 1200;

    private const int JpegQuality = 85;

    public static (byte[] bild250, byte[] bild500, byte[] bild1200) ResizeToThreeSizes(byte[] originalBytes)
    {
        using SKBitmap original = SKBitmap.Decode(originalBytes)
            ?? throw new InvalidOperationException("Bilddaten konnten nicht decodiert werden.");

        byte[] bild250 = ResizeTo(original, SizeThumbnail);
        byte[] bild500 = ResizeTo(original, SizeMedium);
        byte[] bild1200 = ResizeTo(original, SizeFull);

        return (bild250, bild500, bild1200);
    }

    // Verkleinert proportional, sodass die längere Kante "maxKante" Pixel misst - kein Zuschneiden,
    // und kleinere Originale werden nicht über ihre eigentliche Größe hinaus aufgeblasen.
    private static byte[] ResizeTo(SKBitmap original, int maxKante)
    {
        int breite = original.Width;
        int hoehe = original.Height;

        double faktor = Math.Min(1.0, (double)maxKante / Math.Max(breite, hoehe));
        int zielBreite = Math.Max(1, (int)Math.Round(breite * faktor));
        int zielHoehe = Math.Max(1, (int)Math.Round(hoehe * faktor));

        using SKBitmap resized = original.Resize(new SKImageInfo(zielBreite, zielHoehe), SKSamplingOptions.Default);
        if (resized == null)
        {
            throw new InvalidOperationException("Bild konnte nicht skaliert werden.");
        }

        using SKImage image = SKImage.FromBitmap(resized);
        using SKData data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
        return data.ToArray();
    }
}
