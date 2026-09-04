using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using SchnullerkettchenMobile.Data;
using SchnullerkettchenMobile.Models;

namespace SchnullerkettchenMobile.Views;

// Urlaubsmodus-Einstellung, wie am Desktop in Programme/Content/Urlaub.xaml(.cs).
public partial class VacationPage : ContentPage
{
    private readonly VacationRepository repository = new();
    private VacationSettings? einstellungen;

    // Neu gewähltes Bild, das erst beim Speichern hochgeladen wird (wie am Desktop, wo der
    // Upload zwar sofort passiert, die DB-Zeile aber erst mit "Speichern" aktualisiert wird -
    // hier wird bewusst beides gemeinsam beim Speichern gemacht, um keinen verwaisten Upload
    // ohne DB-Eintrag zu riskieren, falls der Nutzer die Seite vorher verlässt).
    private byte[]? neuesBild;
    private string? neuerBildname;

    public VacationPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LadeAsync();
    }

    private async Task LadeAsync()
    {
        Ladeanzeige.IsRunning = true;
        Ladeanzeige.IsVisible = true;

        try
        {
            einstellungen = await repository.GetAsync() ?? new VacationSettings();

            AktivSwitch.IsToggled = einstellungen.Aktiv;
            VonPicker.Date = einstellungen.Von;
            BisPicker.Date = einstellungen.Bis;
            AbPicker.Date = einstellungen.WiederAb;
            UrlaubstextEditor.Text = einstellungen.Urlaubstext;
            LieferzeitVonEntry.Text = einstellungen.LieferzeitVon.ToString(CultureInfo.InvariantCulture);
            LieferzeitBisEntry.Text = einstellungen.LieferzeitBis.ToString(CultureInfo.InvariantCulture);

            if (!string.IsNullOrWhiteSpace(einstellungen.BildUrl))
            {
                BildVorschau.Source = einstellungen.BildUrl;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", "Urlaubseinstellung konnte nicht geladen werden:\n" + ex.Message, "OK");
        }
        finally
        {
            Ladeanzeige.IsRunning = false;
            Ladeanzeige.IsVisible = false;
        }
    }

    private async void OnBildAuswaehlen(object sender, EventArgs e)
    {
        string aktion = await DisplayActionSheet("Bild auswählen", "Abbrechen", null, "Kamera", "Galerie");
        if (aktion != "Kamera" && aktion != "Galerie")
        {
            return;
        }

        try
        {
            FileResult? foto = aktion == "Kamera"
                ? await MediaPicker.Default.CapturePhotoAsync()
                : await MediaPicker.Default.PickPhotoAsync();

            if (foto == null)
            {
                return;
            }

            using Stream stream = await foto.OpenReadAsync();
            using MemoryStream ms = new();
            await stream.CopyToAsync(ms);

            neuesBild = ms.ToArray();
            neuerBildname = foto.FileName;

            BildVorschau.Source = ImageSource.FromStream(() => new MemoryStream(neuesBild));
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Nicht verfügbar", "Diese Funktion wird auf diesem Gerät nicht unterstützt.", "OK");
        }
        catch (PermissionException)
        {
            await DisplayAlert("Berechtigung fehlt", "Bitte Kamera-/Fotozugriff in den Geräteeinstellungen erlauben.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", "Bild konnte nicht ausgewählt werden:\n" + ex.Message, "OK");
        }
    }

    private async void OnSpeichern(object sender, EventArgs e)
    {
        if (einstellungen == null)
        {
            return;
        }

        Ladeanzeige.IsRunning = true;
        Ladeanzeige.IsVisible = true;

        try
        {
            // Neues Bild zuerst hochladen (unverändert, kein Resize - wie am Desktop), danach
            // erst die DB-Zeile mit dem neuen Dateinamen speichern.
            if (neuesBild != null && !string.IsNullOrWhiteSpace(neuerBildname))
            {
                bool uploadOk = await repository.UploadImageAsync(neuesBild, neuerBildname);
                if (!uploadOk)
                {
                    await DisplayAlert("Fehler", "Urlaubsbild konnte nicht hochgeladen werden.", "OK");
                    return;
                }

                einstellungen.Bild = neuerBildname;
            }

            einstellungen.Aktiv = AktivSwitch.IsToggled;
            // Seit .NET MAUI 10 ist DatePicker.Date vom Typ DateTime? (nullable) -
            // fällt der Nutzer nicht extra auf "kein Datum" zurück, wird der aktuelle Wert übernommen.
            einstellungen.Von = VonPicker.Date ?? einstellungen.Von;
            einstellungen.Bis = BisPicker.Date ?? einstellungen.Bis;
            einstellungen.WiederAb = AbPicker.Date ?? einstellungen.WiederAb;
            einstellungen.Urlaubstext = UrlaubstextEditor.Text ?? string.Empty;
            einstellungen.LieferzeitVon = ParseInt(LieferzeitVonEntry.Text);
            einstellungen.LieferzeitBis = ParseInt(LieferzeitBisEntry.Text);

            bool result = await repository.SaveAsync(einstellungen);

            if (result)
            {
                neuesBild = null;
                neuerBildname = null;
            }

            await DisplayAlert(result ? "Gespeichert" : "Fehler",
                result ? "Urlaubseinstellung wurde gespeichert." : "Urlaubseinstellung konnte nicht gespeichert werden.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", "Urlaubseinstellung konnte nicht gespeichert werden:\n" + ex.Message, "OK");
        }
        finally
        {
            Ladeanzeige.IsRunning = false;
            Ladeanzeige.IsVisible = false;
        }
    }

    private static int ParseInt(string? text) =>
        int.TryParse(text, out int value) ? value : 0;
}
