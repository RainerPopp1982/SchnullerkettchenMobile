using System.Globalization;
using MySqlConnector;

namespace SchnullerkettchenMobile.Util;

// MySqlConnector ist bei impliziten Typkonvertierungen strenger als der Desktop-Treiber
// (MySql.Data): reader.GetInt32(...) auf einer Spalte, die tatsächlich als VARCHAR/TINYINT/
// DECIMAL etc. gespeichert ist (oder umgekehrt), kann eine MySqlConversionException auslösen,
// obwohl der Wert inhaltlich problemlos in den Zieltyp passt. Diese Helfer lesen den Rohwert
// und konvertieren tolerant, statt sich auf den exakt erwarteten Spaltentyp zu verlassen.
public static class SafeReader
{
    public static bool IsNull(MySqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column));

    public static string GetString(MySqlDataReader reader, string column, string fallback = "")
    {
        int ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal))
        {
            return fallback;
        }

        object value = reader.GetValue(ordinal);
        return value.ToString() ?? fallback;
    }

    public static int GetInt(MySqlDataReader reader, string column, int fallback = 0)
    {
        int ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal))
        {
            return fallback;
        }

        object value = reader.GetValue(ordinal);

        return value switch
        {
            int i => i,
            long l => (int)l,
            short s => s,
            sbyte sb => sb,
            byte b => b,
            bool bo => bo ? 1 : 0,
            decimal d => (int)d,
            double db => (int)db,
            float f => (int)f,
            string str => int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out int parsed) ? parsed : fallback,
            _ => fallback
        };
    }

    public static decimal GetDecimal(MySqlDataReader reader, string column, decimal fallback = 0m)
    {
        int ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal))
        {
            return fallback;
        }

        object value = reader.GetValue(ordinal);

        return value switch
        {
            decimal d => d,
            double db => (decimal)db,
            float f => (decimal)f,
            int i => i,
            long l => l,
            short s => s,
            string str => decimal.TryParse(str.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed) ? parsed : fallback,
            _ => fallback
        };
    }

    public static bool GetBool(MySqlDataReader reader, string column, bool fallback = false) =>
        GetInt(reader, column, fallback ? 1 : 0) != 0;

    public static DateTime? GetDateTimeOrNull(MySqlDataReader reader, string column)
    {
        int ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        object value = reader.GetValue(ordinal);

        return value switch
        {
            DateTime dt => dt,
            string str => DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed) ? parsed : null,
            _ => null
        };
    }
}
