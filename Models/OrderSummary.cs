namespace SchnullerkettchenMobile.Models;

// Eine Zeile aus "kunden" LEFT JOIN "bestellnr" - Datenbasis wie in
// SchnullerkettchenLibary.Datenbank.Bestellungen.SelectOrders.
public class OrderSummary
{
    public int Kundennummer { get; set; }
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string Strasse { get; set; } = string.Empty;
    public string Hausnr { get; set; } = string.Empty;
    public string PLZ { get; set; } = string.Empty;
    public string Wohnort { get; set; } = string.Empty;
    public string Land { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Gesamtpreis { get; set; }
    public DateTime? Bestelldatum { get; set; }

    // Rohwert aus bestellnr.bestaetigt. Die genaue Bedeutung der einzelnen Zahlen (0,1,2,...)
    // ist app-seitig nicht abschließend bekannt - "offen" bedeutet laut Vorgabe 0 oder 1.
    public int Bestellstatus { get; set; }

    public string FullName => $"{Vorname} {Nachname}".Trim();
    public string Adresse => $"{Strasse} {Hausnr}, {PLZ} {Wohnort}".Trim(' ', ',');
    public bool IstOffen => Bestellstatus is 0 or 1;
    public string DatumText => Bestelldatum?.ToString("dd.MM.yyyy") ?? "-";
    public string PreisText => Gesamtpreis.ToString("C2");
}
