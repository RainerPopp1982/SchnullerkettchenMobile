using SchnullerkettchenMobile.Data;
using SchnullerkettchenMobile.Models;

namespace SchnullerkettchenMobile.Views;

public partial class OrdersPage : ContentPage
{
    private readonly OrdersRepository repository = new();

    public OrdersPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LadeListeAsync();
    }

    private async void OnFilterChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value)
        {
            return; // nur auf das neu ausgewählte RadioButton reagieren
        }

        SuchfeldBox.Text = string.Empty;
        await LadeListeAsync();
    }

    private async void OnSuchen(object sender, EventArgs e)
    {
        string begriff = SuchfeldBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(begriff))
        {
            await LadeListeAsync();
            return;
        }

        await LadeAsync(() => repository.SearchOrdersAsync(begriff));
    }

    private Task LadeListeAsync()
    {
        if (RadioAlle.IsChecked)
        {
            return LadeAsync(() => repository.GetAllOrdersAsync());
        }

        return LadeAsync(() => repository.GetOpenOrdersAsync());
    }

    private async Task LadeAsync(Func<Task<List<OrderSummary>>> ladeFunktion)
    {
        Ladeanzeige.IsRunning = true;
        Ladeanzeige.IsVisible = true;
        LeerHinweis.IsVisible = false;

        try
        {
            List<OrderSummary> bestellungen = await ladeFunktion();
            BestellungenListe.ItemsSource = bestellungen;
            LeerHinweis.IsVisible = bestellungen.Count == 0;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", "Bestellungen konnten nicht geladen werden:\n" + ex.Message, "OK");
        }
        finally
        {
            Ladeanzeige.IsRunning = false;
            Ladeanzeige.IsVisible = false;
        }
    }

    private async void OnBestellungTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is int kundennr)
        {
            await Shell.Current.GoToAsync($"orderDetail?kundennr={kundennr}");
        }
    }
}
