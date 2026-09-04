using SchnullerkettchenMobile.Data;
using SchnullerkettchenMobile.Models;

namespace SchnullerkettchenMobile.Views;

public partial class ArticleSearchPage : ContentPage
{
    private readonly ArticlesRepository repository = new();

    public ArticleSearchPage()
    {
        InitializeComponent();
    }

    private async void OnSuchen(object sender, EventArgs e)
    {
        string begriff = SuchfeldBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(begriff))
        {
            return;
        }

        Ladeanzeige.IsRunning = true;
        Ladeanzeige.IsVisible = true;
        LeerHinweis.IsVisible = false;

        try
        {
            List<ArticleSummary> ergebnis = await repository.SearchArticlesAsync(begriff);
            ArtikelListe.ItemsSource = ergebnis;
            LeerHinweis.IsVisible = ergebnis.Count == 0;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", "Artikel konnten nicht geladen werden:\n" + ex.Message, "OK");
        }
        finally
        {
            Ladeanzeige.IsRunning = false;
            Ladeanzeige.IsVisible = false;
        }
    }

    private async void OnArtikelTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string sku)
        {
            await Shell.Current.GoToAsync($"articleDetail?sku={sku}");
        }
    }
}
