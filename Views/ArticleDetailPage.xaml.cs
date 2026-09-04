using System.Globalization;
using SchnullerkettchenMobile.Data;
using SchnullerkettchenMobile.Models;
using SchnullerkettchenMobile.Util;
using Microsoft.Maui.Media;
using Microsoft.Maui.ApplicationModel;

namespace SchnullerkettchenMobile.Views;

[QueryProperty(nameof(Sku), "sku")]
public partial class ArticleDetailPage : ContentPage
{
    private readonly ArticlesRepository repository = new();
    private readonly ImageUploadService imageUploadService = new();
    private ArticleData? artikel;

    public string Sku { get; set; } = string.Empty;

    public ArticleDetailPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LadeArtikelAsync();
    }

    private async Task LadeArtikelAsync()
    {
        if (string.IsNullOrWhiteSpace(Sku))
        {
            return;
        }

        Ladeanzeige.IsRunning = true;
        Ladeanzeige.IsVisible = true;

        try
        {
            artikel = await repository.GetArticleAsync(Sku);

            if (artikel == null)
            {
                await DisplayAlert("Hinweis", "Artikel wurde nicht gefunden.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            ArtikelIdLabel.Text = $"Artikelnummer: {artikel.ID}";
            NameEntry.Text = artikel.Artikelname;
            PreisEntry.Text = artikel.Preis.ToString(CultureInfo.InvariantCulture);
            BestandEntry.Text = artikel.Bestand.ToString(CultureInfo.InvariantCulture);
            MaxBuchstabenEntry.Text = artikel.MaxBuchstaben.ToString(CultureInfo.InvariantCulture);
            AktivSwitch.IsToggled = artikel.Aktiv;
            StartseiteSwitch.IsToggled = artikel.Startseite;
            GoogleSwitch.IsToggled = artikel.Google;
            BeschreibungEditor.Text = artikel.Beschreibung;
            SeoUrlEntry.Text = artikel.SeoUrl;

            await LadeBilderAsync();

            var varianten = await repository.GetVariantsAsync(artikel.ID);
            BindableLayout.SetItemsSource(VariantenListe, varianten);
            KeineVariantenHinweis.IsVisible = varianten.Count == 0;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", "Artikel konnte nicht geladen werden:\n" + ex.Message, "OK");
        }
        finally
        {
            Ladeanzeige.IsRunning = false;
            Ladeanzeige.IsVisible = false;
        }
    }

    private async Task LadeBilderAsync()
    {
        if (artikel == null)
        {
            return;
        }

        var bilder = await repository.GetImagesAsync(artikel.ID);
        BindableLayout.SetItemsSource(BilderListe, bilder);
        BilderScroll.IsVisible = bilder.Count > 0;
        KeineBilderHinweis.IsVisible = bilder.Count == 0;
    }

    private async void OnBildHinzufuegen(object sender, EventArgs e)
    {
        if (artikel == null)
        {
            return;
        }

        byte[]? bytes = await BildAuswaehlenAsync();
        if (bytes == null)
        {
            return;
        }

        BildLadeanzeige.IsRunning = true;
        BildLadeanzeige.IsVisible = true;

        try
        {
            var bilder = await repository.GetImagesAsync(artikel.ID);
            int naechstePosition = bilder.Count == 0 ? 1 : bilder.Max(b => b.Position) + 1;

            bool ok = await imageUploadService.UploadNewImageAsync(artikel.ID, artikel.Artikelname, bytes, naechstePosition);

            if (ok)
            {
                await LadeBilderAsync();
            }
            else
            {
                await DisplayAlert("Fehler", "Bild konnte nicht hochgeladen werden.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", "Bild konnte nicht hochgeladen werden:\n" + ex.Message, "OK");
        }
        finally
        {
            BildLadeanzeige.IsRunning = false;
            BildLadeanzeige.IsVisible = false;
        }
    }

    private async void OnBildTapped(object sender, TappedEventArgs e)
    {
        if (artikel == null || e.Parameter is not ArticleImage bild)
        {
            return;
        }

        string aktion = await DisplayActionSheet(bild.Imagename, "Abbrechen", "Löschen", "Ersetzen");

        if (aktion == "Ersetzen")
        {
            byte[]? bytes = await BildAuswaehlenAsync();
            if (bytes == null)
            {
                return;
            }

            BildLadeanzeige.IsRunning = true;
            BildLadeanzeige.IsVisible = true;

            try
            {
                bool ok = await imageUploadService.ReplaceImageAsync(bild.Imagename, bytes);

                if (ok)
                {
                    await LadeBilderAsync();
                }
                else
                {
                    await DisplayAlert("Fehler", "Bild konnte nicht ersetzt werden.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Fehler", "Bild konnte nicht ersetzt werden:\n" + ex.Message, "OK");
            }
            finally
            {
                BildLadeanzeige.IsRunning = false;
                BildLadeanzeige.IsVisible = false;
            }
        }
        else if (aktion == "Löschen")
        {
            bool bestaetigt = await DisplayAlert("Bild löschen", $"\"{bild.Imagename}\" wirklich löschen?", "Löschen", "Abbrechen");
            if (!bestaetigt)
            {
                return;
            }

            BildLadeanzeige.IsRunning = true;
            BildLadeanzeige.IsVisible = true;

            try
            {
                bool ok = await imageUploadService.DeleteImageAsync(artikel.ID, bild.Imagename);

                if (ok)
                {
                    await LadeBilderAsync();
                }
                else
                {
                    await DisplayAlert("Fehler", "Bild konnte nicht gelöscht werden.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Fehler", "Bild konnte nicht gelöscht werden:\n" + ex.Message, "OK");
            }
            finally
            {
                BildLadeanzeige.IsRunning = false;
                BildLadeanzeige.IsVisible = false;
            }
        }
    }

    private async Task<byte[]?> BildAuswaehlenAsync()
    {
        string aktion = await DisplayActionSheet("Bild auswählen", "Abbrechen", null, "Kamera", "Galerie");
        if (aktion != "Kamera" && aktion != "Galerie")
        {
            return null;
        }

        try
        {
            FileResult? foto = aktion == "Kamera"
                ? await MediaPicker.Default.CapturePhotoAsync()
                : await MediaPicker.Default.PickPhotoAsync();

            if (foto == null)
            {
                return null;
            }

            using Stream stream = await foto.OpenReadAsync();
            using MemoryStream ms = new();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Nicht verfügbar", "Diese Funktion wird auf diesem Gerät nicht unterstützt.", "OK");
            return null;
        }
        catch (PermissionException)
        {
            await DisplayAlert("Berechtigung fehlt", "Bitte Kamera-/Fotozugriff in den Geräteeinstellungen erlauben.", "OK");
            return null;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", "Bild konnte nicht ausgewählt werden:\n" + ex.Message, "OK");
            return null;
        }
    }

    private async void OnGrunddatenSpeichern(object sender, EventArgs e)
    {
        if (artikel == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlert("Hinweis", "Bitte einen Artikelnamen angeben.", "OK");
            return;
        }

        artikel.Artikelname = NameEntry.Text.Trim();
        artikel.Preis = ParseDecimal(PreisEntry.Text);
        artikel.Bestand = ParseInt(BestandEntry.Text);
        artikel.MaxBuchstaben = ParseInt(MaxBuchstabenEntry.Text);
        artikel.Aktiv = AktivSwitch.IsToggled;
        artikel.Startseite = StartseiteSwitch.IsToggled;
        artikel.Google = GoogleSwitch.IsToggled;

        bool result = await repository.UpdateGrunddatenAsync(artikel);
        await DisplayAlert(result ? "Gespeichert" : "Fehler",
            result ? "Grunddaten wurden gespeichert." : "Grunddaten konnten nicht gespeichert werden.", "OK");
    }

    private async void OnBeschreibungSpeichern(object sender, EventArgs e)
    {
        if (artikel == null)
        {
            return;
        }

        bool result = await repository.UpdateBeschreibungAsync(artikel.ID, BeschreibungEditor.Text ?? string.Empty);
        await DisplayAlert(result ? "Gespeichert" : "Fehler",
            result ? "Beschreibung wurde gespeichert." : "Beschreibung konnte nicht gespeichert werden.", "OK");
    }

    private async void OnSeoNeuErzeugen(object sender, EventArgs e)
    {
        if (artikel == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(artikel.SeoUrl))
        {
            SeoUrlEntry.Text = artikel.SeoUrl;
            await DisplayAlert("Hinweis", "Es ist bereits eine SEO-URL hinterlegt und wurde nicht verändert.", "OK");
            return;
        }

        string basisSlug = SeoUrlHelper.Build(NameEntry.Text ?? artikel.Artikelname);
        SeoUrlEntry.Text = await SeoUrlHelper.EnsureUniqueAsync(repository, basisSlug, artikel.ID);
    }

    private async void OnSeoSpeichern(object sender, EventArgs e)
    {
        if (artikel == null)
        {
            return;
        }

        string sanitized = SeoUrlHelper.Build(SeoUrlEntry.Text ?? string.Empty);

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            await DisplayAlert("Hinweis", "Bitte eine gültige SEO-URL angeben.", "OK");
            return;
        }

        string eindeutig = await SeoUrlHelper.EnsureUniqueAsync(repository, sanitized, artikel.ID);

        if (eindeutig != sanitized)
        {
            await DisplayAlert("Hinweis",
                $"Die SEO-URL \"{sanitized}\" ist bereits bei einem anderen Artikel vergeben.\nEs wird stattdessen \"{eindeutig}\" gespeichert.", "OK");
        }

        SeoUrlEntry.Text = eindeutig;

        bool result = await repository.UpdateSeoUrlAsync(artikel.ID, eindeutig);

        if (result)
        {
            artikel.SeoUrl = eindeutig;
        }

        await DisplayAlert(result ? "Gespeichert" : "Fehler",
            result ? "SEO-URL wurde gespeichert." : "SEO-URL konnte nicht gespeichert werden.", "OK");
    }

    private async void OnVarianteTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is int variantId)
        {
            await Shell.Current.GoToAsync($"variantEdit?id={variantId}");
        }
    }

    private static decimal ParseDecimal(string? text) =>
        decimal.TryParse(text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value) ? value : 0m;

    private static int ParseInt(string? text) =>
        int.TryParse(text, out int value) ? value : 0;
}
