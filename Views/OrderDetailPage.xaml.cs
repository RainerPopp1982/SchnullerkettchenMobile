using SchnullerkettchenMobile.Data;
using SchnullerkettchenMobile.Models;
using SchnullerkettchenMobile.Util;
using SchnullerkettchenMobile.Util.BrotherQl;
using SkiaSharp;

namespace SchnullerkettchenMobile.Views;

[QueryProperty(nameof(Kundennummer), "kundennr")]
public partial class OrderDetailPage : ContentPage
{
    private readonly OrdersRepository repository = new();
    private OrderDetail? bestellung;

    public string Kundennummer { get; set; } = string.Empty;

    public OrderDetailPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LadeBestellungAsync();
    }

    private async Task LadeBestellungAsync()
    {
        if (!int.TryParse(Kundennummer, out int kundennr))
        {
            return;
        }

        Ladeanzeige.IsRunning = true;
        Ladeanzeige.IsVisible = true;

        try
        {
            bestellung = await repository.GetOrderAsync(kundennr);

            if (bestellung == null)
            {
                await DisplayAlert("Hinweis", "Bestellung wurde nicht gefunden.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            BestellnummerLabel.Text = string.IsNullOrWhiteSpace(bestellung.ExternalOrderNumber)
                ? $"Kundennummer: {bestellung.Kundennummer}"
                : $"Kundennummer: {bestellung.Kundennummer}  ·  Externe Bestellnr.: {bestellung.ExternalOrderNumber}";

            NameLabel.Text = bestellung.FullName;
            AdresseLabel.Text = bestellung.Adresse;
            LandLabel.Text = bestellung.Land;
            EmailLabel.Text = bestellung.Email;

            bool abweichend = bestellung.AlternativeAdresse;
            AbweichendeAdresseHeader.IsVisible = abweichend;
            LieferNameLabel.IsVisible = abweichend;
            LieferAdresseLabel.IsVisible = abweichend;
            LieferLandLabel.IsVisible = abweichend;

            if (abweichend)
            {
                LieferNameLabel.Text = $"{bestellung.VornameRE} {bestellung.NachnameRE}".Trim();
                LieferAdresseLabel.Text = $"{bestellung.StrasseRE} {bestellung.HausnrRE}, {bestellung.PLZRE} {bestellung.WohnortRE}".Trim(' ', ',');
                LieferLandLabel.Text = bestellung.LandRE;
            }

            DatumLabel.Text = $"Datum: {bestellung.DatumText}";
            // Der genaue Wertebereich von "bestaetigt" (0/1 = offen, alles andere unbekannt) ist
            // app-seitig nicht abschließend bekannt - deshalb bewusst nur als Rohzahl angezeigt.
            StatusLabel.Text = $"Status (bestaetigt): {bestellung.Bestellstatus}";
            ZahlungsartLabel.Text = string.IsNullOrWhiteSpace(bestellung.Zahlungsart) ? string.Empty : $"Zahlungsart: {bestellung.Zahlungsart}";
            PreisLabel.Text = $"Gesamtpreis: {bestellung.Gesamtpreis:C2}";

            NachrichtLabel.IsVisible = !string.IsNullOrWhiteSpace(bestellung.Nachricht);
            NachrichtLabel.Text = bestellung.Nachricht;

            var artikel = await repository.GetOrderItemsAsync(kundennr);
            BindableLayout.SetItemsSource(ArtikelListe, artikel);
            KeineArtikelHinweis.IsVisible = artikel.Count == 0;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", "Bestellung konnte nicht geladen werden:\n" + ex.Message, "OK");
        }
        finally
        {
            Ladeanzeige.IsRunning = false;
            Ladeanzeige.IsVisible = false;
        }
    }

    // Etikett wie am Desktop: normale Adresse oder Rechnungs-/Lieferadresse (address_re), je
    // nachdem was in OrderDetail als "abweichend" markiert ist (siehe Models/OrderDetail.cs).
    // Gedruckt wird direkt über das Netzwerk auf dem Brother QL-820NWB(C) (Util/BrotherQl/) -
    // klappt das nicht (Drucker aus/nicht im Netz erreichbar), fällt die App automatisch auf
    // das Share-Sheet zurück (Etikettenbild an z.B. "Brother iPrint&Label" weitergeben).
    private async void OnEtikettDrucken(object sender, EventArgs e)
    {
        if (bestellung == null)
        {
            return;
        }

        bool zeigeLand = !string.IsNullOrWhiteSpace(bestellung.EtikettLand)
            && !bestellung.EtikettLand.Equals("Deutschland", StringComparison.OrdinalIgnoreCase);

        List<string> zeilen = new() { bestellung.EtikettName, bestellung.EtikettStrasse, bestellung.EtikettOrt };
        if (zeigeLand)
        {
            zeilen.Add(bestellung.EtikettLand);
        }

        try
        {
            QlLabel media = QlMediaCatalog.Adressetikett_62x29;
            using SKBitmap bitmap = BrotherQlPrinter.RenderAddressLabel(media, zeilen);

            BrotherQlPrinter printer = new();
            QlPrintStatus status = await printer.PrintLabelAsync(media, bitmap);

            if (status.HasError)
            {
                await DisplayAlert("Druckerfehler", string.Join("\n", status.Fehler), "OK");
            }
        }
        catch (Exception ex)
        {
            // Netzwerkdrucker nicht erreichbar o.ä. - als Rückfallebene das bisherige
            // Bild+Share-Sheet-Verfahren anbieten, statt komplett zu scheitern.
            bool viaShareSheet = await DisplayAlert("Drucker nicht erreichbar",
                ex.Message + "\n\nStattdessen als Bild zum manuellen Drucken teilen?", "Ja", "Abbrechen");

            if (!viaShareSheet)
            {
                return;
            }

            try
            {
                string pfad = LabelPrinter.CreateAddressLabel(
                    bestellung.EtikettName, bestellung.EtikettStrasse, bestellung.EtikettOrt, bestellung.EtikettLand);
                await LabelPrinter.ShareAsync(pfad);
            }
            catch (Exception fallbackEx)
            {
                await DisplayAlert("Fehler", "Etikett konnte nicht erzeugt werden:\n" + fallbackEx.Message, "OK");
            }
        }
    }
}
