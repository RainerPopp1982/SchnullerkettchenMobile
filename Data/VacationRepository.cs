using FluentFTP;
using MySqlConnector;
using SchnullerkettchenMobile.Models;
using SchnullerkettchenMobile.Util;

namespace SchnullerkettchenMobile.Data;

// Urlaubsmodus-Einstellung, wie am Desktop in Programme/Content/Urlaub.xaml.cs +
// Shopkonfiguration/GetconfigData.cs + Shopkonfiguration/UpdateConfigContent.cs. Nutzt die
// separate "SchnullerkettchenConfig"-Datenbank (ShopConfigConfig), nicht die normale Shop-DB.
// Anders als am Desktop wird hier parametrisiert gespeichert (Desktop baut die UPDATE-Anweisung
// per String-Interpolation zusammen - funktional identisch, aber ohne SQL-Injection-Risiko).
public class VacationRepository
{
    // Es existiert genau eine Konfigurationszeile in der Tabelle "Urlaub" (wie am Desktop).
    public async Task<VacationSettings?> GetAsync()
    {
        await using MySqlConnection connection = new(ShopConfigConfig.ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = new("SELECT * FROM Urlaub LIMIT 1", connection);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        string aktivText = SafeReader.GetString(reader, "aktiv");

        return new VacationSettings
        {
            Aktiv = bool.TryParse(aktivText, out bool aktiv) && aktiv,
            Von = SafeReader.GetDateTimeOrNull(reader, "von") ?? DateTime.Today,
            Bis = SafeReader.GetDateTimeOrNull(reader, "bis") ?? DateTime.Today,
            WiederAb = SafeReader.GetDateTimeOrNull(reader, "ab") ?? DateTime.Today,
            Urlaubstext = SafeReader.GetString(reader, "urlaubstext"),
            Bild = SafeReader.GetString(reader, "bild"),
            LieferzeitVon = SafeReader.GetInt(reader, "lieferzeitVon"),
            LieferzeitBis = SafeReader.GetInt(reader, "lieferzeitBis")
        };
    }

    public async Task<bool> SaveAsync(VacationSettings settings)
    {
        try
        {
            await using MySqlConnection connection = new(ShopConfigConfig.ConnectionString);
            await connection.OpenAsync();

            // Wie am Desktop: "bild" nur mit-speichern, wenn tatsächlich ein neues Bild gewählt
            // wurde, sonst bleibt die bereits hinterlegte Datei unangetastet.
            string bildTeil = string.IsNullOrWhiteSpace(settings.Bild) ? "" : ", bild=@bild";

            await using MySqlCommand command = new(
                "UPDATE Urlaub SET aktiv=@aktiv, von=@von, bis=@bis, ab=@ab, urlaubstext=@text, " +
                $"lieferzeitVon=@lieferzeitVon, lieferzeitBis=@lieferzeitBis{bildTeil}", connection);

            command.Parameters.AddWithValue("@aktiv", settings.Aktiv.ToString());
            command.Parameters.AddWithValue("@von", settings.Von.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@bis", settings.Bis.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@ab", settings.WiederAb.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@text", settings.Urlaubstext);
            command.Parameters.AddWithValue("@lieferzeitVon", settings.LieferzeitVon);
            command.Parameters.AddWithValue("@lieferzeitBis", settings.LieferzeitBis);

            if (!string.IsNullOrWhiteSpace(settings.Bild))
            {
                command.Parameters.AddWithValue("@bild", settings.Bild);
            }

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Lädt das Urlaubsbild unverändert (kein Resize, anders als bei Artikelbildern) in den
    // Layout-Ordner hoch, wie am Desktop (Button_OpenImage).
    public async Task<bool> UploadImageAsync(byte[] bytes, string dateiname)
    {
        try
        {
            AsyncFtpClient client = new(FtpConfig.Host, FtpConfig.Username, FtpConfig.Password);
            try
            {
                await client.AutoConnect();
                await client.UploadBytes(bytes, $"{FtpConfig.LayoutRemotePath}/{dateiname}", FtpRemoteExists.Overwrite, createRemoteDir: true);
            }
            finally
            {
                await client.Disconnect();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
