using System.Text;
using System.Text.RegularExpressions;
using SchnullerkettchenMobile.Data;

namespace SchnullerkettchenMobile.Util;

// SEO-Slug-Erzeugung/-Eindeutigkeit, spiegelt die Logik der Desktop-App (Artikel bearbeiten,
// SEO-URL-Feld): Umlaute transliterieren, alles Nicht-Alphanumerische zu "-", Duplikate gegen
// ANDERE Artikel per angehängtem Zähler ("-2", "-3", ...) auflösen.
public static class SeoUrlHelper
{
    public static string Build(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string text = input.Trim().ToLowerInvariant();

        text = text
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");

        StringBuilder normalized = new();
        foreach (char c in text.Normalize(System.Text.NormalizationForm.FormD))
        {
            System.Globalization.UnicodeCategory category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                normalized.Append(c);
            }
        }

        string slug = Regex.Replace(normalized.ToString(), @"[^a-z0-9]+", "-");
        slug = slug.Trim('-');
        slug = Regex.Replace(slug, "-{2,}", "-");

        return slug;
    }

    // Hängt bei Kollision mit einem ANDEREN Artikel "-2", "-3", ... an, bis die URL frei ist -
    // wie am Desktop.
    public static async Task<string> EnsureUniqueAsync(ArticlesRepository repository, string baseSlug, string excludeId)
    {
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            return baseSlug;
        }

        string kandidat = baseSlug;
        int zaehler = 2;

        while (await repository.SeoUrlExistsAsync(kandidat, excludeId))
        {
            kandidat = $"{baseSlug}-{zaehler}";
            zaehler++;
        }

        return kandidat;
    }
}
