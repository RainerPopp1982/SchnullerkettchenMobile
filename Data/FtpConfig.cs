namespace SchnullerkettchenMobile.Data;

// Gleiche FTP-Zugangsdaten wie die Desktop-App (FTPUpload/UploadFiles.cs).
// ACHTUNG: Wie bei DatabaseConfig gilt auch hier, dass diese Zugangsdaten Teil des App-Pakets
// werden und damit auf Android/iOS leichter extrahierbar sind als bei einer Windows-EXE.
public static class FtpConfig
{
    public const string Host = "82.165.127.61";
    public const string Username = "schnullerkettchen.de";
    public const string Password = "Akw@@doq1982!!";

    // Gleiche Zielpfade wie am Desktop (ImageUpload.cs): drei Größenordner je Bildgröße.
    public const string RemoteBasePath = "/media.schnullerkettchen.de/images/produktbilder/kategorien";

    // Zielordner für das Urlaubsbild (Programme/Content/Urlaub.xaml.cs am Desktop) - anderer
    // Ordner als die Produktbilder, kein Resize, Originaldatei wird 1:1 hochgeladen.
    public const string LayoutRemotePath = "/media.schnullerkettchen.de/media/layout";
}
