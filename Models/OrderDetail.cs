namespace SchnullerkettchenMobile.Models;

// Vollständige Bestell-/Kundendaten für die Detailansicht - selbes Schema wie
// SchnullerkettchenLibary.Datenbank.Bestellungen.SelectOrders.GetOrders(string kundennr).
// Reine Anzeige, keine Bearbeitung.
public class OrderDetail
{
    public int Kundennummer { get; set; }
    public string ExternalOrderNumber { get; set; } = string.Empty;

    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string Strasse { get; set; } = string.Empty;
    public string Hausnr { get; set; } = string.Empty;
    public string PLZ { get; set; } = string.Empty;
    public string Wohnort { get; set; } = string.Empty;
    public string Land { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public bool AlternativeAdresse { get; set; }
    public string VornameRE { get; set; } = string.Empty;
    public string NachnameRE { get; set; } = string.Empty;
    public string StrasseRE { get; set; } = string.Empty;
    public string HausnrRE { get; set; } = string.Empty;
    public string PLZRE { get; set; } = string.Empty;
    public string WohnortRE { get; set; } = string.Empty;
    public string LandRE { get; set; } = string.Empty;

    public decimal Gesamtpreis { get; set; }
    public decimal Warenwert { get; set; }
    public decimal Versandkosten { get; set; }
    public DateTime? Bestelldatum { get; set; }
    public int Bestellstatus { get; set; }
    public string Zahlungsart { get; set; } = string.Empty;
    public string Nachricht { get; set; } = string.Empty;

    public string FullName => $"{Vorname} {Nachname}".Trim();
    public string Adresse => $"{Strasse} {Hausnr}, {PLZ} {Wohnort}".Trim(' ', ',');
    public string DatumText => Bestelldatum?.ToString("dd.MM.yyyy HH:mm") ?? "-";

    // Für den Etikettendruck: die tatsächlich zu bedruckende Adresse (Liefer- oder Rechnungsadresse).
    public string EtikettName => AlternativeAdresse ? $"{VornameRE} {NachnameRE}".Trim() : FullName;
    public string EtikettStrasse => AlternativeAdresse ? $"{StrasseRE} {HausnrRE}".Trim() : $"{Strasse} {Hausnr}".Trim();
    public string EtikettOrt => AlternativeAdresse ? $"{PLZRE} {WohnortRE}".Trim() : $"{PLZ} {Wohnort}".Trim();
    public string EtikettLand => AlternativeAdresse ? LandRE : Land;
}
