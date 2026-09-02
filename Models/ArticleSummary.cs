namespace SchnullerkettchenMobile.Models;

// Kurzform eines Artikels für die Trefferliste der Artikelsuche.
public class ArticleSummary
{
    public string ID { get; set; } = string.Empty;
    public string Artikelname { get; set; } = string.Empty;
    public decimal Preis { get; set; }
    public int Bestand { get; set; }
    public bool Aktiv { get; set; }

    public string PreisText => Preis.ToString("C2");
    public string StatusText => Aktiv ? "Aktiv" : "Inaktiv";
}
