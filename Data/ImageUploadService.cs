using FluentFTP;
using MySqlConnector;
using SchnullerkettchenMobile.Util;

namespace SchnullerkettchenMobile.Data;

// Bild-Upload/-Ersetzen/-Löschen für Artikelbilder (tbl_images), analog zur Desktop-Logik in
// Programme/Artikel/ImageUpload.cs + FTPUpload/UploadFiles.cs + SchnullerkettchenLibary
// Datenbank/Produkte/CreateProduct.cs (UpdateImage). Mobil wird statt der synchronen FtpClient-
// Klasse die asynchrone AsyncFtpClient-API von FluentFTP verwendet.
public class ImageUploadService
{
    // Neues Bild: 3 Größen erzeugen, hochladen, DB-Zeile in tbl_images anlegen.
    // Dateiname folgt dem Desktop-Schema "{parentSku}_{position}.jpg" (ImageConverterSingle).
    public async Task<bool> UploadNewImageAsync(string parentSku, string artikelname, byte[] originalBytes, int position)
    {
        string dateiname = $"{parentSku}_{position}.jpg";

        try
        {
            var (bild250, bild500, bild1200) = ImageResizer.ResizeToThreeSizes(originalBytes);
            await UploadAlleGroessenAsync(dateiname, bild250, bild500, bild1200);

            return await InsertImageRowAsync(parentSku, parentSku, dateiname, position, artikelname);
        }
        catch
        {
            return false;
        }
    }

    // Bestehendes Bild ersetzen: gleicher Dateiname wird überschrieben, keine DB-Änderung nötig
    // (wie am Desktop - der Dateiname bleibt identisch, tbl_images.image ändert sich nicht).
    public async Task<bool> ReplaceImageAsync(string imagename, byte[] originalBytes)
    {
        try
        {
            var (bild250, bild500, bild1200) = ImageResizer.ResizeToThreeSizes(originalBytes);
            await UploadAlleGroessenAsync(imagename, bild250, bild500, bild1200);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Bild komplett löschen: FTP-Dateien in allen 3 Größen entfernen UND (anders als am Desktop,
    // wo das fehlt) die zugehörige Zeile in tbl_images löschen.
    public async Task<bool> DeleteImageAsync(string parentSku, string imagename)
    {
        try
        {
            AsyncFtpClient client = NeuerClient();
            try
            {
                await client.AutoConnect();

                foreach (int groesse in new[] { ImageResizer.SizeThumbnail, ImageResizer.SizeMedium, ImageResizer.SizeFull })
                {
                    string remotePfad = RemotePfad(groesse, imagename);
                    if (await client.FileExists(remotePfad))
                    {
                        await client.DeleteFile(remotePfad);
                    }
                }
            }
            finally
            {
                await client.Disconnect();
            }

            await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
            await connection.OpenAsync();

            await using MySqlCommand command = new(
                "DELETE FROM tbl_images WHERE parent_sku = @parentSku AND image = @image", connection);
            command.Parameters.AddWithValue("@parentSku", parentSku);
            command.Parameters.AddWithValue("@image", imagename);
            await command.ExecuteNonQueryAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task UploadAlleGroessenAsync(string dateiname, byte[] bild250, byte[] bild500, byte[] bild1200)
    {
        AsyncFtpClient client = NeuerClient();
        try
        {
            await client.AutoConnect();

            await client.UploadBytes(bild250, RemotePfad(ImageResizer.SizeThumbnail, dateiname), FtpRemoteExists.Overwrite, createRemoteDir: true);
            await client.UploadBytes(bild500, RemotePfad(ImageResizer.SizeMedium, dateiname), FtpRemoteExists.Overwrite, createRemoteDir: true);
            await client.UploadBytes(bild1200, RemotePfad(ImageResizer.SizeFull, dateiname), FtpRemoteExists.Overwrite, createRemoteDir: true);
        }
        finally
        {
            await client.Disconnect();
        }
    }

    private static async Task<bool> InsertImageRowAsync(string parentSku, string sku, string imagename, int position, string artikelname)
    {
        try
        {
            await using MySqlConnection connection = new(DatabaseConfig.ConnectionString);
            await connection.OpenAsync();

            await using MySqlCommand command = new(
                "INSERT INTO tbl_images (parent_sku, sku, image, position, alt_tag, teaserbild, allgemein) " +
                "VALUES (@ParentSKU, @SKU, @Image, @Position, @AltTag, @Teaserbild, @Allgemein)", connection);
            command.Parameters.AddWithValue("@ParentSKU", parentSku);
            command.Parameters.AddWithValue("@SKU", sku);
            command.Parameters.AddWithValue("@Image", imagename);
            command.Parameters.AddWithValue("@Position", position);
            command.Parameters.AddWithValue("@AltTag", $"{artikelname} Bild {imagename}");
            command.Parameters.AddWithValue("@Teaserbild", position == 1 ? 1 : 0);
            command.Parameters.AddWithValue("@Allgemein", 0);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Bildgröße 1200 landet wie am Desktop im Ordner "images_1000" (historisch gewachsener Name,
    // siehe DBConnection.GetImage / ArticlesRepository.GetImagesAsync).
    private static string RemotePfad(int groesse, string dateiname)
    {
        string ordner = groesse == ImageResizer.SizeFull ? "images_1000" : $"images_{groesse}";
        return $"{FtpConfig.RemoteBasePath}/{ordner}/{dateiname}";
    }

    private static AsyncFtpClient NeuerClient() => new(FtpConfig.Host, FtpConfig.Username, FtpConfig.Password);
}
