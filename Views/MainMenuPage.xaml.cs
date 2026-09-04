namespace SchnullerkettchenMobile.Views;

public partial class MainMenuPage : ContentPage
{
    public MainMenuPage()
    {
        InitializeComponent();
    }

    private async void OnBestellungenClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("orders");
    }

    private async void OnArtikelsucheClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("articles");
    }

    private async void OnUrlaubClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("vacation");
    }
}
