using System.Globalization;
using SchnullerkettchenMobile.Data;
using SchnullerkettchenMobile.Models;

namespace SchnullerkettchenMobile.Views;

[QueryProperty(nameof(Id), "id")]
public partial class VariantEditPage : ContentPage
{
    private readonly ArticlesRepository repository = new();
    private ArticleVariant? variante;

    public string Id { get; set; } = string.Empty;

    public VariantEditPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LadeVarianteAsync();
    }

    private async Task LadeVarianteAsync()
    {
        if (!int.TryParse(Id, out int variantId))
        {
            return;
        }

        Ladeanzeige.IsRunning = true;
        Ladeanzeige.IsVisible = true;

        try
        {
            variante = await repository.GetVariantAsync(variantId);

            if (variante == null)
            {
                await DisplayAlert("Hinweis", "Variante wurde nicht gefunden.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            SkuLabel.Text = $"SKU: {variante.Sku}";
            NameEntry.Text = variante.Name;
            PreisEntry.Text = variante.Preis.ToString(CultureInfo.InvariantCulture);
            BestandEntry.Text = variante.Bestand.ToString(CultureInfo.InvariantCulture);
            Var1Entry.Text = variante.Var1;
            Var2Entry.Text = variante.Var2;
            Var3Entry.Text = variante.Var3;
            AktivSwitch.IsToggled = variante.Aktiv;
            SelectedSwitch.IsToggled = variante.Selected;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", "Variante konnte nicht geladen werden:\n" + ex.Message, "OK");
        }
        finally
        {
            Ladeanzeige.IsRunning = false;
            Ladeanzeige.IsVisible = false;
        }
    }

    private async void OnSpeichern(object sender, EventArgs e)
    {
        if (variante == null)
        {
            return;
        }

        variante.Name = NameEntry.Text ?? string.Empty;
        variante.Preis = ParseDecimal(PreisEntry.Text);
        variante.Bestand = ParseInt(BestandEntry.Text);
        variante.Var1 = Var1Entry.Text ?? string.Empty;
        variante.Var2 = Var2Entry.Text ?? string.Empty;
        variante.Var3 = Var3Entry.Text ?? string.Empty;
        variante.Aktiv = AktivSwitch.IsToggled;
        variante.Selected = SelectedSwitch.IsToggled;

        bool result = await repository.UpdateVariantAsync(variante);

        await DisplayAlert(result ? "Gespeichert" : "Fehler",
            result ? "Variante wurde gespeichert." : "Variante konnte nicht gespeichert werden.", "OK");

        if (result)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    private static decimal ParseDecimal(string? text) =>
        decimal.TryParse(text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value) ? value : 0m;

    private static int ParseInt(string? text) =>
        int.TryParse(text, out int value) ? value : 0;
}
