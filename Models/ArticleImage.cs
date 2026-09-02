namespace SchnullerkettchenMobile.Models;

// Bild aus "tbl_images" - Anzeige sowie Hinzufügen/Ersetzen/Löschen (siehe Data/ImageUploadService.cs).
public class ArticleImage
{
    public string Imagename { get; set; } = string.Empty;
    public int Position { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string FullUrl { get; set; } = string.Empty;
}
