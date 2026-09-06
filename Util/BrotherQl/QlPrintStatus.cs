namespace SchnullerkettchenMobile.Util.BrotherQl;

// Interpretiert die 32-Byte-Statusantwort, die der QL-820NWB nach einer Statusabfrage
// (ESC i S) bzw. nach Druckende über dieselbe TCP-Verbindung zurückschickt. Byte-Layout laut
// Brothers "Raster Command Reference" bzw. gegengeprüft mit brother_ql/reader.py
// (github.com/pklaus/brother_ql, MIT-Lizenz) - Antwort beginnt immer mit den 3 Kennbytes
// 80 20 42, Fehlerbits stehen in Byte 8 ("Error information 1") und 9 ("Error information 2").
public sealed class QlPrintStatus
{
    public bool IstGueltig { get; private init; }
    public bool NoMedia { get; private init; }
    public bool CoverOpen { get; private init; }
    public bool CutterJam { get; private init; }
    public bool PrinterOff { get; private init; }
    public bool TransmissionError { get; private init; }
    public bool SystemError { get; private init; }
    public IReadOnlyList<string> Fehler { get; private init; } = Array.Empty<string>();
    public bool HasError => Fehler.Count > 0;
    public byte StatusType { get; private init; }
    public byte PhaseType { get; private init; }
    public int MediaBreiteMm { get; private init; }

    // Nur gesetzt, wenn IstGueltig=false - beschreibt, WARUM keine gültige Antwort ausgewertet
    // werden konnte (z.B. wie viele Bytes tatsächlich ankamen, oder welche Exception auftrat).
    // Wichtig zum Eingrenzen von Netzwerk-/Protokollproblemen ohne echten Drucker zum Testen.
    public string? Diagnose { get; private init; }

    // Wird zurückgegeben, wenn der Drucker gar nicht (rechtzeitig oder nicht im erwarteten
    // Format) geantwortet hat - z.B. bei Netzwerkproblemen. Kein Fehlerstatus im engeren Sinn,
    // aber auch keine Bestätigung.
    public static QlPrintStatus Unbekannt(string? diagnose = null) => new() { IstGueltig = false, Diagnose = diagnose };

    public static QlPrintStatus Parse(byte[] daten)
    {
        if (daten.Length < 32)
        {
            return Unbekannt($"Nur {daten.Length} von 32 Bytes empfangen: {HexDump(daten)}");
        }
        if (daten[0] != 0x80 || daten[1] != 0x20 || daten[2] != 0x42)
        {
            return Unbekannt($"Antwort beginnt nicht mit der erwarteten Kennung 80 20 42, sondern mit: {HexDump(daten[..Math.Min(8, daten.Length)])}");
        }

        byte error1 = daten[8];
        byte error2 = daten[9];
        List<string> fehler = new();

        if ((error1 & 0x01) != 0) fehler.Add("Kein Etikettenband eingelegt");
        if ((error1 & 0x02) != 0) fehler.Add("Etikettenband zu Ende");
        if ((error1 & 0x04) != 0) fehler.Add("Schneidemesser blockiert");
        if ((error1 & 0x20) != 0) fehler.Add("Drucker ausgeschaltet");
        if ((error2 & 0x01) != 0) fehler.Add("Etikettenband falsch eingelegt");
        if ((error2 & 0x02) != 0) fehler.Add("Druckerpuffer voll");
        if ((error2 & 0x04) != 0) fehler.Add("Übertragungsfehler");
        if ((error2 & 0x10) != 0) fehler.Add("Klappe wurde während des Drucks geöffnet");
        if ((error2 & 0x40) != 0) fehler.Add("Etikettenband wird nicht transportiert");
        if ((error2 & 0x80) != 0) fehler.Add("Systemfehler");

        return new QlPrintStatus
        {
            IstGueltig = true,
            NoMedia = (error1 & 0x01) != 0,
            CoverOpen = (error2 & 0x10) != 0,
            CutterJam = (error1 & 0x04) != 0,
            PrinterOff = (error1 & 0x20) != 0,
            TransmissionError = (error2 & 0x04) != 0,
            SystemError = (error2 & 0x80) != 0,
            Fehler = fehler,
            StatusType = daten[18],
            PhaseType = daten[19],
            MediaBreiteMm = daten[10],
        };
    }

    private static string HexDump(byte[] daten) => daten.Length == 0
        ? "(keine Daten)"
        : string.Join(" ", daten.Select(b => b.ToString("X2")));
}
