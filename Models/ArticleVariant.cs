namespace SchnullerkettchenMobile.Models;

// Zeile aus "dekoartikel_varianten".
public class ArticleVariant
{
    public int VariantId { get; set; }
    public string ParentSku { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Preis { get; set; }
    public int Bestand { get; set; }
    public string Var1 { get; set; } = string.Empty;
    public string Var2 { get; set; } = string.Empty;
    public string Var3 { get; set; } = string.Empty;
    public bool Aktiv { get; set; }
    public bool Selected { get; set; }

    public string PreisText => Preis.ToString("C2");
}
