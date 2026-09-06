using System.Net.Sockets;
using SkiaSharp;

namespace SchnullerkettchenMobile.Util.BrotherQl;

// Druckt Etiketten direkt über das Netzwerk auf einem Brother QL-820NWB(C) - ganz ohne das
// proprietäre b-PAC-SDK (das ist COM-basiert und funktioniert ohnehin nur unter Windows, für
// eine plattformübergreifende MAUI-App also ungeeignet). Stattdessen wird Brothers offiziell
// dokumentiertes Raster-Protokoll direkt über eine rohe TCP-Verbindung auf Port 9100 (dem
// Standard-Rohdruckport, den der Drucker im Netzwerk anbietet) gesprochen - funktioniert
// identisch unter Windows, Android, iOS und macOS, ganz ohne installierten Treiber.
//
// Quelle für das Byte-Format: Brothers "Software Developer's Manual - Raster Command Reference
// QL-800/810W/820NWB" (download.brother.com), zusätzlich gegengeprüft mit der quelloffenen,
// seit Jahren produktiv genutzten Python-Bibliothek brother_ql (github.com/pklaus/brother_ql,
// MIT-Lizenz) - insbesondere für Bit-Reihenfolge, Spiegelung und das Statusantwort-Format, die
// in Brothers eigener PDF-Doku nicht immer eindeutig beschrieben sind.
public sealed class BrotherQlPrinter
{
    private const int RasterBreiteDots = 720; // Druckkopfbreite, fix für die gesamte QL-8xx-Serie
    private const int BytesProZeile = RasterBreiteDots / 8; // 90
    private const int InvalidateBytesAnzahl = 400; // QL-800/810W/820NWB (ältere QL-Modelle: 200)

    public string Host { get; }
    public int Port { get; }

    public BrotherQlPrinter(string host, int port = 9100)
    {
        Host = host;
        Port = port;
    }

    public BrotherQlPrinter() : this(PrinterConfig.Host, PrinterConfig.Port)
    {
    }

    // Zeichnet die übergebenen Textzeilen als reines Schwarz/Weiß-Bild in Leserichtung
    // (breiter als hoch), unabhängig davon, ob das gewählte Etikett quer oder hochkant
    // zugeführt wird - die nötige 90°-Drehung für hochkant zugeführte Etiketten (z.B.
    // DK-11201) übernimmt automatisch PrintLabelAsync.
    public static SKBitmap RenderAddressLabel(QlLabel media, IReadOnlyList<string> zeilen)
    {
        var (druckbarBreite, druckbarLaenge) = media.DotsDruckbar;
        bool hochkantZugefuehrt = druckbarBreite < druckbarLaenge;
        int breite = hochkantZugefuehrt ? druckbarLaenge : druckbarBreite;
        int hoehe = hochkantZugefuehrt ? druckbarBreite : druckbarLaenge;

        SKBitmap bitmap = new(breite, hoehe);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.White);

        int anzahlZeilen = Math.Max(zeilen.Count, 1);
        float zeilenhoehe = hoehe / (anzahlZeilen + 0.6f);
        float schriftgroesse = Math.Min(zeilenhoehe * 0.72f, hoehe * 0.3f);

        using SKPaint paint = new() { Color = SKColors.Black, IsAntialias = true };
        using SKFont font = new(SKTypeface.FromFamilyName(null, SKFontStyle.Normal), schriftgroesse);

        float x = breite * 0.04f;
        float y = zeilenhoehe * 0.85f;
        foreach (string zeile in zeilen)
        {
            canvas.DrawText(zeile, x, y, SKTextAlign.Left, font, paint);
            y += zeilenhoehe;
        }

        return bitmap;
    }

    // Öffnet die TCP-Verbindung und schickt den kompletten Druckauftrag (Initialisierung,
    // Medien-/Qualitätseinstellung, Rasterdaten, Druckbefehl) in EINEM Rutsch - danach wird,
    // rein informativ, versucht eine Statusantwort zu lesen.
    //
    // Hinweis zur Historie: ursprünglich wurde vor dem eigentlichen Druck separat der Status
    // abgefragt (ESC i S) und erst bei gültiger Antwort weitergemacht. In der Praxis hat sich
    // gezeigt, dass der QL-820NWB(C) auf eine isolierte Statusabfrage über den rohen TCP-Port
    // gar nicht antwortet (0 von 32 Bytes, kompletter Timeout) - ein separates "vorher fragen"
    // blockiert also jeden Druck, obwohl der Drucker selbst völlig in Ordnung ist. Jetzt wird
    // genau wie im offiziellen Referenzcode (brother_ql) alles in einem Stream gesendet; eine
    // eventuell doch ankommende Statusantwort danach wird ausgewertet, ihr Fehlen führt aber
    // nicht mehr dazu, dass der Druck als fehlgeschlagen gilt.
    public async Task<QlPrintStatus> PrintLabelAsync(QlLabel media, SKBitmap inhalt, bool autoCut = true,
        int connectTimeoutMs = 5000, int sendTimeoutMs = 10000, int statusReadTimeoutMs = 3000)
    {
        byte[] rasterBlock = BuildRasterBlock(media, inhalt);
        uint rasterZeilen = (uint)((rasterBlock.Length - 1) / (3 + BytesProZeile));

        using TcpClient client = new();

        try
        {
            using CancellationTokenSource connectCts = new(connectTimeoutMs);
            await client.ConnectAsync(Host, Port, connectCts.Token);
        }
        catch (OperationCanceledException ex)
        {
            throw new IOException(
                $"Drucker unter {Host}:{Port} antwortet nicht innerhalb von {connectTimeoutMs / 1000} Sekunden " +
                "(Zeitüberschreitung beim Verbindungsaufbau). Ist die IP korrekt und der Drucker eingeschaltet " +
                "bzw. aus dem WLAN-Stromsparmodus aufgewacht?", ex);
        }
        catch (SocketException ex)
        {
            throw new IOException(
                $"Drucker unter {Host}:{Port} nicht erreichbar ({ex.SocketErrorCode}). " +
                "Ist er eingeschaltet und im selben Netzwerk wie das Gerät, das druckt?", ex);
        }

        try
        {
            using NetworkStream stream = client.GetStream();

            using (MemoryStream auftrag = new())
            {
                auftrag.Write(new byte[InvalidateBytesAnzahl]);       // löscht evtl. angefangene Befehle im Druckerpuffer
                auftrag.Write(new byte[] { 0x1B, 0x40 });             // ESC @    - Initialisierung
                auftrag.Write(new byte[] { 0x1B, 0x69, 0x61, 0x01 }); // ESC i a  - Rastermodus einschalten
                auftrag.Write(new byte[] { 0x1B, 0x69, 0x53 });       // ESC i S  - Statusabfrage (Antwort optional, siehe unten)

                byte mtype = (byte)media.FormFactor;
                byte mwidth = (byte)media.TapeGroesseMm.Breite;
                byte mlength = (byte)media.TapeGroesseMm.Laenge;

                auftrag.Write(new byte[] { 0x1B, 0x69, 0x7A }); // ESC i z - Medien/Qualität
                const byte validFlags = 0b1100_1110;            // mtype+mwidth+mlength gültig, Druckqualität=fein
                auftrag.WriteByte(validFlags);
                auftrag.WriteByte(mtype);
                auftrag.WriteByte(mwidth);
                auftrag.WriteByte(mlength);
                auftrag.Write(BitConverter.GetBytes(rasterZeilen)); // 4 Byte little-endian: Gesamtzahl Rasterzeilen
                auftrag.WriteByte(0x00);                            // erste (und einzige) Seite dieses Jobs
                auftrag.WriteByte(0x00);                            // reserviert

                auftrag.Write(new byte[] { 0x1B, 0x69, 0x4D, (byte)(autoCut ? 0x40 : 0x00) }); // ESC i M - Autocut an/aus
                auftrag.Write(new byte[] { 0x1B, 0x69, 0x41, 0x01 });                          // ESC i A - nach jedem Etikett schneiden
                auftrag.Write(new byte[] { 0x1B, 0x69, 0x4B, (byte)(autoCut ? 0x08 : 0x00) }); // ESC i K - am Ende schneiden
                auftrag.Write(new byte[] { 0x1B, 0x69, 0x64 });                                // ESC i d - Vorschub
                auftrag.Write(BitConverter.GetBytes((ushort)media.VorschubDots));

                auftrag.Write(rasterBlock);

                using CancellationTokenSource sendCts = new(sendTimeoutMs);
                await stream.WriteAsync(auftrag.ToArray(), sendCts.Token);
            }

            // Statusantwort ist Kür, kein Muss: manche QL-Modelle/Firmwarestände antworten auf
            // dem rohen TCP-Port gar nicht von sich aus - dann bleibt der Status "unbekannt",
            // was in der aufrufenden UI NICHT als Fehler behandelt wird (siehe QlPrintStatus.HasError).
            using CancellationTokenSource statusCts = new(statusReadTimeoutMs);
            return await LiesStatusAsync(stream, statusCts.Token);
        }
        catch (IOException)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            throw new IOException(
                $"Verbindung zu {Host}:{Port} stand, aber das Senden der Druckdaten hat innerhalb von " +
                $"{sendTimeoutMs / 1000} Sekunden nicht vollständig geklappt.", ex);
        }
        catch (SocketException ex)
        {
            throw new IOException($"Verbindung zu {Host}:{Port} wurde während der Übertragung unterbrochen: {ex.Message}", ex);
        }
    }

    // Baut aus dem Inhaltsbild die reinen Rasterzeilen-Befehle ('g'-Kommandos) plus
    // abschließenden Druckbefehl (0x1A). Initialisierung/Statusabfrage/Medien-Setup werden in
    // PrintLabelAsync separat davor in denselben Stream geschrieben.
    private static byte[] BuildRasterBlock(QlLabel media, SKBitmap inhalt)
    {
        var (druckbarBreite, druckbarLaenge) = media.DotsDruckbar;

        SKBitmap ausgerichtet = inhalt;
        bool eigenesBitmap = false;
        if (druckbarBreite != druckbarLaenge && inhalt.Width == druckbarLaenge && inhalt.Height == druckbarBreite)
        {
            // Bild wurde in Leserichtung gezeichnet, Etikett wird aber hochkant zugeführt (z.B.
            // DK-11201) -> um 90° drehen, damit es zur physischen Zuführrichtung passt.
            ausgerichtet = Rotiere90(inhalt);
            eigenesBitmap = true;
        }
        else if (inhalt.Width != druckbarBreite || inhalt.Height != druckbarLaenge)
        {
            throw new ArgumentException(
                $"Bildgröße {inhalt.Width}x{inhalt.Height} passt nicht zum Etikett \"{media.Bezeichnung}\" " +
                $"({druckbarBreite}x{druckbarLaenge} Dots, auch nicht gedreht).");
        }

        using SKBitmap voll = new(RasterBreiteDots, ausgerichtet.Height);
        using (SKCanvas c = new(voll))
        {
            c.Clear(SKColors.White);
            int einfuegeX = RasterBreiteDots - ausgerichtet.Width - media.RandRechtsDots;
            c.DrawBitmap(ausgerichtet, einfuegeX, 0);
        }
        if (eigenesBitmap)
        {
            ausgerichtet.Dispose();
        }

        // Vor dem Zerlegen in Zeilen horizontal spiegeln - exakt wie im offiziellen
        // Referenzcode: die Bit-Reihenfolge einer Rasterzeile entspricht sonst nicht der
        // physischen Reihenfolge der Druckkopfelemente, das Etikett käme seitenverkehrt raus.
        using SKBitmap gespiegelt = Spiegele(voll);

        byte[] gepackt = PackeAlleZeilen(gespiegelt);

        using MemoryStream ms = new();
        for (int zeile = 0; zeile < gespiegelt.Height; zeile++)
        {
            ms.WriteByte(0x67); // 'g' = unkomprimierte Rasterzeile
            ms.WriteByte(0x00);
            ms.WriteByte(BytesProZeile); // = 90, passt immer in ein Byte
            ms.Write(gepackt, zeile * BytesProZeile, BytesProZeile);
        }
        ms.WriteByte(0x1A); // Druckbefehl (letzte/einzige Seite dieses Jobs)
        return ms.ToArray();
    }

    // Wandelt das Bitmap in gepackte 1-Bit-Zeilen um (1 = schwarz/drucken, 0 = weiß). Ein
    // fester Schwellwert statt Dithering reicht für reinen Adresstext völlig aus und ist auf
    // einem 1-Bit-Thermodruckkopf deutlich zuverlässiger.
    private static byte[] PackeAlleZeilen(SKBitmap bild)
    {
        SKColor[] pixel = bild.Pixels; // ein Aufruf statt vieler einzelner GetPixel-Aufrufe
        int breite = bild.Width;
        int hoehe = bild.Height;
        byte[] ergebnis = new byte[hoehe * BytesProZeile];

        for (int y = 0; y < hoehe; y++)
        {
            int zeilenBasis = y * breite;
            int ausgabeBasis = y * BytesProZeile;
            for (int byteIndex = 0; byteIndex < BytesProZeile; byteIndex++)
            {
                byte b = 0;
                int xStart = byteIndex * 8;
                for (int bit = 0; bit < 8; bit++)
                {
                    int x = xStart + bit;
                    if (x >= breite) break;
                    if (pixel[zeilenBasis + x].Red < 128)
                    {
                        b |= (byte)(0x80 >> bit);
                    }
                }
                ergebnis[ausgabeBasis + byteIndex] = b;
            }
        }

        return ergebnis;
    }

    private static SKBitmap Rotiere90(SKBitmap quelle)
    {
        SKBitmap ziel = new(quelle.Height, quelle.Width);
        using SKCanvas canvas = new(ziel);
        canvas.Clear(SKColors.White);
        canvas.Translate(ziel.Width, 0);
        canvas.RotateDegrees(90);
        canvas.DrawBitmap(quelle, 0, 0);
        return ziel;
    }

    private static SKBitmap Spiegele(SKBitmap quelle)
    {
        SKBitmap ziel = new(quelle.Width, quelle.Height);
        using SKCanvas canvas = new(ziel);
        canvas.Clear(SKColors.White);
        canvas.Scale(-1, 1, quelle.Width / 2f, 0);
        canvas.DrawBitmap(quelle, 0, 0);
        return ziel;
    }

    // Liest genau 32 Bytes (die feste Länge einer Brother-Statusantwort). Schluckt keine
    // Fehler stillschweigend, sondern gibt in QlPrintStatus.Diagnose mit, was tatsächlich
    // passiert ist (0 Bytes = Drucker hat die Verbindung sofort wieder geschlossen, Timeout =
    // Verbindung stand, aber es kam gar nichts, Exception-Text bei sonstigen Netzwerkfehlern) -
    // das war vorher ein stiller Fehlschlucker und hat das Eingrenzen unnötig erschwert.
    private static async Task<QlPrintStatus> LiesStatusAsync(NetworkStream stream, CancellationToken token)
    {
        byte[] puffer = new byte[32];
        int gelesen = 0;
        try
        {
            while (gelesen < 32)
            {
                int n = await stream.ReadAsync(puffer.AsMemory(gelesen, 32 - gelesen), token);
                if (n == 0)
                {
                    return QlPrintStatus.Unbekannt(
                        $"Drucker hat die Verbindung geschlossen, nachdem {gelesen} von 32 Bytes angekommen waren.");
                }
                gelesen += n;
            }
            return QlPrintStatus.Parse(puffer);
        }
        catch (OperationCanceledException)
        {
            return QlPrintStatus.Unbekannt($"Zeitüberschreitung beim Lesen der Statusantwort ({gelesen} von 32 Bytes empfangen).");
        }
        catch (Exception ex)
        {
            return QlPrintStatus.Unbekannt($"{ex.GetType().Name} beim Lesen der Statusantwort: {ex.Message}");
        }
    }
}
