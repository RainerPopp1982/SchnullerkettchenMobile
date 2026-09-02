namespace SchnullerkettchenMobile.Models;

// Eine Position aus "bestellung_artikel" - selbes Schema wie
// SchnullerkettchenLibary.Datenbank.Bestellungen.SelectOrders.GetOrderProducts.
public class OrderItem
{
    public string Sku { get; set; } = string.Empty;
    public string Artikelname { get; set; } = string.Empty;
    public int Menge { get; set; }
    public decimal Einzelpreis { get; set; }
    public decimal Gesamtpreis { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    public string PreisText => Gesamtpreis.ToString("C2");
}
