namespace ValPlay.Services;

public interface IMediaMetadataService
{
    Task<Microsoft.Maui.Controls.ImageSource?> LoadAlbumArtAsync(string filePath, CancellationToken cancellationToken = default);
}
