using SchnullerkettchenMobile.Views;

namespace SchnullerkettchenMobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Unterseiten werden per Shell.GoToAsync("route?param=wert") aufgerufen und sind
        // hier nicht als ShellContent im Menü sichtbar, sondern reine Navigationsziele.
        Routing.RegisterRoute("orders", typeof(OrdersPage));
        Routing.RegisterRoute("orderDetail", typeof(OrderDetailPage));
        Routing.RegisterRoute("articles", typeof(ArticleSearchPage));
        Routing.RegisterRoute("articleDetail", typeof(ArticleDetailPage));
        Routing.RegisterRoute("variantEdit", typeof(VariantEditPage));
        Routing.RegisterRoute("vacation", typeof(VacationPage));
    }
}
