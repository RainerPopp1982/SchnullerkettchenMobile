namespace SchnullerkettchenMobile.Data;

// Gleiche Zugangsdaten/Server wie die Desktop-Anwendung
// (SchnullerkettchenLibary/Datenbank/DBConnection.cs) - bewusst direkter DB-Zugriff ohne
// eigenes API-Backend (siehe Absprache: v1 spiegelt den Desktop-Ansatz).
//
// ACHTUNG: Da diese Zugangsdaten Teil des App-Pakets werden, sind sie bei einer Android-/iOS-App
// grundsätzlich leichter extrahierbar als bei einer Windows-EXE. Für produktiven mobilen Einsatz
// sollte perspektivisch ein schlankes API-Backend mit eigenem Auth-Mechanismus vorgeschaltet werden.
public static class DatabaseConfig
{
    public const string ConnectionString =
        "server=82.165.127.61;uid=schnullerkettchen.de;pwd=Akwaadoq1982!!;database=usr_web324_1";
}
