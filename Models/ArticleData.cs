namespace SchnullerkettchenMobile.Models;

// Bearbeitbare Artikeldaten (Tabelle "dekoartikel") - bewusst ohne Bilder/FTP-Pipeline
// (siehe Entscheidung: Version 1 der App deckt nur Textdaten ab, kein Bild-Upload).
public class ArticleData
{
    public string ID { get; set; } = string.Empty;
    public string Artikelname { get; set; } = string.Empty;
    public decimal Preis { get; set; }
    public int Bestand { get; set; }
    public bool Aktiv { get; set; }
    public bool Startseite { get; set; }
    public bool Google { get; set; }
    public int MaxBuchstaben { get; set; }
    public string Beschreibung { get; set; } = string.Empty;
    public string SeoUrl { get; set; } = string.Empty;
}
