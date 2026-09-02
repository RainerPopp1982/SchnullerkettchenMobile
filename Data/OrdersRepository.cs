using MySqlConnector;
using SchnullerkettchenMobile.Models;
using SchnullerkettchenMobile.Util;

namespace SchnullerkettchenMobile.Data;

// Liest Bestellungen aus "kunden" LEFT JOIN "bestellnr" - selbes Schema wie
// SchnullerkettchenLibary.Datenbank.Bestellungen.SelectOrders in der Desktop-App.
// Reiner Lesezugriff: Bestellungen werden in dieser App nur angezeigt, nicht bearbeitet.
public class OrdersRepository
{
    // "Offen" laut Vorgabe = bestellnr.bestaetigt 0 oder 1.
    public Task<List<OrderSummary>> GetOpenOrdersAsync() =>
        QueryOrdersAsync(
            "SELECT * FROM kunden k LEFT JOIN bestellnr b ON k.kundennr = b.bestellnr " +
            "WHERE b.bestaetigt IN (0, 1) ORDER BY k.kundennr DESC LIMIT 200");

    public Task<List<OrderSummary>> GetAllOrdersAsync() =>
        QueryOrdersAsync(
            "SELECT * FROM kunden k LEFT JOIN bestellnr b ON k.kundennr = b.bestellnr " +
            "ORDER BY k.kundennr DESC LIMIT 200");

    // Suche nach E-Mail oder Adresse (Straße, PLZ, Ort) wie gewünscht.
    public async Task<List<OrderSummary>> SearchOrdersAsync(string suchbegriff)
    {
        string sql =
            "SELECT * FROM kunden k LEFT JOIN bestellnr b ON k.kundennr = b.bestellnr " +
            "WHERE k.email LIKE @s OR k.strasse LIKE @s OR k.plz LIKE @s OR k.wohnort LIKE @s " +
            "ORDER BY k.kundennr DESC LIMIT 200";

        await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@s", $"%{suchbegriff}%");

        return await ReadOrdersAsync(command);
    }

    private async Task<List<OrderSummary>> QueryOrdersAsync(string sql)
    {
        await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = new(sql, connection);
        return await ReadOrdersAsync(command);
    }

    private static async Task<List<OrderSummary>> ReadOrdersAsync(MySqlCommand command)
    {
        List<OrderSummary> result = new();

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new OrderSummary
            {
                Kundennummer = SafeReader.GetInt(reader, "kundennr"),
                Vorname = SafeReader.GetString(reader, "vorname"),
                Nachname = SafeReader.GetString(reader, "name"),
                Strasse = SafeReader.GetString(reader, "strasse"),
                Hausnr = SafeReader.GetString(reader, "hausnr"),
                PLZ = SafeReader.GetString(reader, "plz"),
                Wohnort = SafeReader.GetString(reader, "wohnort"),
                Land = SafeReader.GetString(reader, "land"),
                Email = SafeReader.GetString(reader, "email"),
                Gesamtpreis = SafeReader.GetDecimal(reader, "gesamtpreis"),
                Bestelldatum = SafeReader.GetDateTimeOrNull(reader, "erstellt"),
                Bestellstatus = SafeReader.IsNull(reader, "bestaetigt") ? -1 : SafeReader.GetInt(reader, "bestaetigt")
            });
        }

        return result;
    }

    // Volle Bestell-/Kundendaten für die Detailansicht (öffnen einer einzelnen Bestellung).
    public async Task<OrderDetail?> GetOrderAsync(int kundennr)
    {
        await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = new(
            "SELECT * FROM kunden k LEFT JOIN bestellnr b ON k.kundennr = b.bestellnr WHERE k.kundennr = @id", connection);
        command.Parameters.AddWithValue("@id", kundennr);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new OrderDetail
        {
            Kundennummer = SafeReader.GetInt(reader, "kundennr"),
            ExternalOrderNumber = SafeReader.GetString(reader, "externe_bestellnr"),
            Vorname = SafeReader.GetString(reader, "vorname"),
            Nachname = SafeReader.GetString(reader, "name"),
            Strasse = SafeReader.GetString(reader, "strasse"),
            Hausnr = SafeReader.GetString(reader, "hausnr"),
            PLZ = SafeReader.GetString(reader, "plz"),
            Wohnort = SafeReader.GetString(reader, "wohnort"),
            Land = SafeReader.GetString(reader, "land"),
            Email = SafeReader.GetString(reader, "email"),
            // "address_re" wird in der Desktop-App als Text ("true"/"false") gespeichert, nicht
            // als Zahl - hier bewusst genauso ausgelesen statt als bool/int zu interpretieren.
            AlternativeAdresse = SafeReader.GetString(reader, "address_re") == "true",
            VornameRE = SafeReader.GetString(reader, "vorname_re"),
            NachnameRE = SafeReader.GetString(reader, "name_re"),
            StrasseRE = SafeReader.GetString(reader, "strasse_re"),
            HausnrRE = SafeReader.GetString(reader, "hausnr_re"),
            PLZRE = SafeReader.GetString(reader, "plz_re"),
            WohnortRE = SafeReader.GetString(reader, "ort_re"),
            LandRE = SafeReader.GetString(reader, "land_re"),
            Gesamtpreis = SafeReader.GetDecimal(reader, "gesamtpreis"),
            Warenwert = SafeReader.GetDecimal(reader, "warenwert"),
            Versandkosten = SafeReader.GetDecimal(reader, "versandkosten"),
            Bestelldatum = SafeReader.GetDateTimeOrNull(reader, "erstellt"),
            Bestellstatus = SafeReader.IsNull(reader, "bestaetigt") ? -1 : SafeReader.GetInt(reader, "bestaetigt"),
            Zahlungsart = SafeReader.GetString(reader, "zahlungsart"),
            Nachricht = SafeReader.GetString(reader, "nachricht")
        };
    }

    private const string ImageBaseUrl = "https://media.schnullerkettchen.de/images/produktbilder/kategorien/images_250/";

    public async Task<List<OrderItem>> GetOrderItemsAsync(int kundennr)
    {
        await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = new(
            "SELECT * FROM bestellung_artikel b LEFT JOIN tbl_images img ON b.artikelnr = img.sku AND img.position = 1 " +
            "WHERE b.bestellnr = @id ORDER BY b.artikelnr ASC", connection);
        command.Parameters.AddWithValue("@id", kundennr);

        List<OrderItem> result = new();

        await using MySqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            string bildname = SafeReader.GetString(reader, "image");

            result.Add(new OrderItem
            {
                Sku = SafeReader.GetString(reader, "artikelnr"),
                Artikelname = SafeReader.GetString(reader, "artikelname"),
                Menge = SafeReader.GetInt(reader, "menge"),
                Einzelpreis = SafeReader.GetDecimal(reader, "preis"),
                Gesamtpreis = SafeReader.GetDecimal(reader, "gesamtpreis"),
                ImageUrl = string.IsNullOrWhiteSpace(bildname) ? $"{ImageBaseUrl}noimage.jpg" : $"{ImageBaseUrl}{bildname}"
            });
        }

        return result;
    }
}
