using TagLib;

namespace ValPlay.Services;

public sealed class MediaMetadataService : IMediaMetadataService
{
    public Task<Microsoft.Maui.Controls.ImageSource?> LoadAlbumArtAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!System.IO.File.Exists(filePath))
                return null;

            try
            {
                using var file = TagLib.File.Create(filePath);
                var picture = file.Tag.Pictures.FirstOrDefault();
                if (picture?.Data?.Data is not { Length: > 0 } data)
                    return null;

                var bytes = data.ToArray();
                return Microsoft.Maui.Controls.ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch (UnsupportedFormatException)
            {
                return null;
            }
            catch (CorruptFileException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }, cancellationToken);
    }
}
