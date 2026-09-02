using MySqlConnector;
using SchnullerkettchenMobile.Models;
using SchnullerkettchenMobile.Util;

namespace SchnullerkettchenMobile.Data;

// Lesen/Bearbeiten von "dekoartikel" und "dekoartikel_varianten" - selbes Schema wie
// SchnullerkettchenLibary.Datenbank.Produkte (SelectProducts/UpdateProduct) in der Desktop-App.
// Die Bilder-/FTP-Upload-Pipeline liegt in Data/ImageUploadService.cs (eigene Zuständigkeit).
public class ArticlesRepository
{
    public async Task<List<ArticleSummary>> SearchArticlesAsync(string suchbegriff)
    {
        string sql = "SELECT ID, Artikelname, Preis, Verfuegbarkeit, aktiv FROM dekoartikel " +
                     "WHERE Artikelname LIKE @s OR ID LIKE @s ORDER BY ID DESC LIMIT 100";

        await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@s", $"%{suchbegriff}%");

        List<ArticleSummary> result = new();

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new ArticleSummary
            {
                ID = SafeReader.GetString(reader, "ID"),
                Artikelname = SafeReader.GetString(reader, "Artikelname"),
                Preis = SafeReader.GetDecimal(reader, "Preis"),
                Bestand = SafeReader.GetInt(reader, "Verfuegbarkeit"),
                Aktiv = SafeReader.GetBool(reader, "aktiv")
            });
        }

        return result;
    }

    public async Task<ArticleData?> GetArticleAsync(string sku)
    {
        string sql = "SELECT * FROM dekoartikel WHERE ID = @id";

        await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@id", sku);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new ArticleData
        {
            ID = SafeReader.GetString(reader, "ID"),
            Artikelname = SafeReader.GetString(reader, "Artikelname"),
            Preis = SafeReader.GetDecimal(reader, "Preis"),
            Bestand = SafeReader.GetInt(reader, "Verfuegbarkeit"),
            Aktiv = SafeReader.GetBool(reader, "aktiv"),
            Startseite = SafeReader.GetBool(reader, "startseite"),
            Google = SafeReader.GetBool(reader, "google"),
            MaxBuchstaben = SafeReader.GetInt(reader, "max_letters"),
            Beschreibung = SafeReader.GetString(reader, "Beschreibung"),
            SeoUrl = SafeReader.GetString(reader, "seo_url")
        };
    }

    public Task<bool> UpdateGrunddatenAsync(ArticleData artikel) =>
        ExecuteAsync(
            "UPDATE dekoartikel SET Artikelname=@name, Preis=@preis, Verfuegbarkeit=@bestand, " +
            "aktiv=@aktiv, startseite=@startseite, google=@google, max_letters=@maxletters WHERE ID=@id",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@name", artikel.Artikelname);
                cmd.Parameters.AddWithValue("@preis", artikel.Preis);
                cmd.Parameters.AddWithValue("@bestand", artikel.Bestand);
                cmd.Parameters.AddWithValue("@aktiv", artikel.Aktiv ? 1 : 0);
                cmd.Parameters.AddWithValue("@startseite", artikel.Startseite ? 1 : 0);
                cmd.Parameters.AddWithValue("@google", artikel.Google ? 1 : 0);
                cmd.Parameters.AddWithValue("@maxletters", artikel.MaxBuchstaben);
                cmd.Parameters.AddWithValue("@id", artikel.ID);
            });

    public Task<bool> UpdateBeschreibungAsync(string sku, string beschreibung) =>
        ExecuteAsync(
            "UPDATE dekoartikel SET Beschreibung=@b WHERE ID=@id",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@b", beschreibung);
                cmd.Parameters.AddWithValue("@id", sku);
            });

    // Wie in der Desktop-App: Duplikat-Prüfung gegen ANDERE Artikel vor dem Speichern.
    public async Task<bool> SeoUrlExistsAsync(string seoUrl, string excludeId)
    {
        await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = new("SELECT ID FROM dekoartikel WHERE seo_url = @seo AND ID != @id LIMIT 1", connection);
        command.Parameters.AddWithValue("@seo", seoUrl);
        command.Parameters.AddWithValue("@id", excludeId);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync();
    }

    public Task<bool> UpdateSeoUrlAsync(string sku, string seoUrl) =>
        ExecuteAsync(
            "UPDATE dekoartikel SET seo_url=@seo WHERE ID=@id",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@seo", seoUrl);
                cmd.Parameters.AddWithValue("@id", sku);
            });

    private const string ImageBaseUrl = "https://media.schnullerkettchen.de/images/produktbilder/kategorien/";

    // Gleiche Bild-URLs wie die Desktop-App (DBConnection.GetImage): images_250 für Vorschau,
    // images_1000 für Vollbild. Hinzufügen/Ersetzen/Löschen siehe Data/ImageUploadService.cs.
    public async Task<List<ArticleImage>> GetImagesAsync(string parentSku)
    {
        await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = new(
            "SELECT * FROM tbl_images WHERE parent_sku = @id ORDER BY position ASC", connection);
        command.Parameters.AddWithValue("@id", parentSku);

        List<ArticleImage> result = new();

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            string dateiname = SafeReader.GetString(reader, "image");
            if (string.IsNullOrWhiteSpace(dateiname))
            {
                continue;
            }

            result.Add(new ArticleImage
            {
                Imagename = dateiname,
                Position = SafeReader.GetInt(reader, "position"),
                ThumbnailUrl = $"{ImageBaseUrl}images_250/{dateiname}",
                FullUrl = $"{ImageBaseUrl}images_1000/{dateiname}"
            });
        }

        return result;
    }

    public async Task<List<ArticleVariant>> GetVariantsAsync(string parentSku)
    {
        await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = new("SELECT * FROM dekoartikel_varianten WHERE parent_id = @id", connection);
        command.Parameters.AddWithValue("@id", parentSku);

        List<ArticleVariant> result = new();

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(ReadVariant(reader, parentSku));
        }

        return result;
    }

    public async Task<ArticleVariant?> GetVariantAsync(int variantId)
    {
        await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = new("SELECT * FROM dekoartikel_varianten WHERE id = @id", connection);
        command.Parameters.AddWithValue("@id", variantId);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        string parentSku = SafeReader.GetString(reader, "parent_id");
        return ReadVariant(reader, parentSku);
    }

    private static ArticleVariant ReadVariant(MySqlDataReader reader, string parentSku) => new()
    {
        VariantId = SafeReader.GetInt(reader, "id"),
        ParentSku = parentSku,
        Sku = SafeReader.GetString(reader, "sku"),
        Name = SafeReader.GetString(reader, "productname"),
        Preis = SafeReader.GetDecimal(reader, "preis"),
        Bestand = SafeReader.GetInt(reader, "bestand"),
        Var1 = SafeReader.GetString(reader, "var1"),
        Var2 = SafeReader.GetString(reader, "var2"),
        Var3 = SafeReader.GetString(reader, "var3"),
        Aktiv = SafeReader.GetBool(reader, "aktiv"),
        Selected = SafeReader.GetBool(reader, "selected")
    };

    public Task<bool> UpdateVariantAsync(ArticleVariant variant) =>
        ExecuteAsync(
            "UPDATE dekoartikel_varianten SET productname=@name, preis=@preis, bestand=@bestand, " +
            "var1=@var1, var2=@var2, var3=@var3, aktiv=@aktiv, selected=@selected WHERE id=@id",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@name", variant.Name);
                cmd.Parameters.AddWithValue("@preis", variant.Preis);
                cmd.Parameters.AddWithValue("@bestand", variant.Bestand);
                cmd.Parameters.AddWithValue("@var1", variant.Var1);
                cmd.Parameters.AddWithValue("@var2", variant.Var2);
                cmd.Parameters.AddWithValue("@var3", variant.Var3);
                cmd.Parameters.AddWithValue("@aktiv", variant.Aktiv ? 1 : 0);
                cmd.Parameters.AddWithValue("@selected", variant.Selected ? 1 : 0);
                cmd.Parameters.AddWithValue("@id", variant.VariantId);
            });

    private static async Task<bool> ExecuteAsync(string sql, Action<MySqlCommand> configure)
    {
        try
        {
            await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
            await connection.OpenAsync();

            await using MySqlCommand command = new(sql, connection);
            configure(command);
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
