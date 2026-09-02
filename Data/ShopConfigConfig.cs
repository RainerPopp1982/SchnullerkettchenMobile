namespace SchnullerkettchenMobile.Data;

// Eigene Datenbank/eigener DB-User für die Shop-Konfiguration (Urlaubsmodus), getrennt vom
// normalen Shop-DB-Zugang - genau wie am Desktop (SchnullerkettchenLibary/Datenbank/DBConnection.cs,
// Konstruktor DBConnection(string database), aufgerufen mit "SchnullerkettchenConfig").
public static class ShopConfigConfig
{
    public const string ConnectionString =
        "server=82.165.127.61;uid=SchnullerkettchenConfig;pwd=Fncsje91V*n*Inh0;database=SchnullerkettchenConfig";
}
