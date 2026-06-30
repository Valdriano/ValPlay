namespace ValPlay.Helpers;

public static class MediaTagReader
{
    public static (string? Artist, string? Album, int? Year, TimeSpan? Duration) ReadAudioTags(string filePath)
    {
        if (!MediaFormatHelper.IsAudio(filePath) || !File.Exists(filePath))
            return (null, null, null, null);

        try
        {
            using var file = TagLib.File.Create(filePath);
            var tag = file.Tag;

            var artist = FirstNonEmpty(tag.FirstPerformer, tag.FirstAlbumArtist, tag.AlbumArtists.FirstOrDefault());
            var album = string.IsNullOrWhiteSpace(tag.Album) ? null : tag.Album.Trim();
            int? year = tag.Year > 0 ? (int)tag.Year : null;
            var duration = file.Properties.Duration > TimeSpan.Zero ? file.Properties.Duration : (TimeSpan?)null;

            return (artist, album, year, duration);
        }
        catch
        {
            return (null, null, null, null);
        }
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
}
