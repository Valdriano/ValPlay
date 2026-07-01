namespace ValPlay.Models;

public sealed class FavoriteEntry
{
    public required string Path { get; init; }
    public required string Title { get; init; }
    public string? Artist { get; init; }
    public string? Album { get; init; }
    public required MediaType Type { get; init; }
    public DateTime AddedAtUtc { get; init; }

    public MediaItem ToMediaItem() => new()
    {
        Path = Path,
        FileName = System.IO.Path.GetFileName(Path),
        Title = Title,
        Type = Type,
        Artist = Artist,
        Album = Album
    };
}
