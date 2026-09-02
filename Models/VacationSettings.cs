namespace SchnullerkettchenMobile.Models;

// Entspricht SchnullerkettchenLibary.Models.ShopconfigModel (Desktop) - Tabelle "Urlaub" in der
// separaten "SchnullerkettchenConfig"-Datenbank.
public class VacationSettings
{
    public bool Aktiv { get; set; }
    public DateTime Von { get; set; } = DateTime.Today;
    public DateTime Bis { get; set; } = DateTime.Today;
    public DateTime WiederAb { get; set; } = DateTime.Today;
    public string Urlaubstext { get; set; } = string.Empty;
    public int LieferzeitVon { get; set; }
    public int LieferzeitBis { get; set; }

    // Nur der Dateiname wird in der DB gespeichert (Spalte "bild"), wie am Desktop.
    public string Bild { get; set; } = string.Empty;

    public string BildUrl => string.IsNullOrWhiteSpace(Bild)
        ? string.Empty
        : $"https://media.schnullerkettchen.de/media/layout/{Bild}";
}
