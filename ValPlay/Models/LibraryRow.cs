namespace ValPlay.Models;

public sealed class LibraryRow
{
    public required LibraryEntryKind Kind { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string Path { get; init; }
    public MediaItem? Media { get; init; }

    public static LibraryRow FromFolder(MediaFolder folder) => new()
    {
        Kind = LibraryEntryKind.Folder,
        Title = folder.Name,
        Subtitle = $"{folder.ItemCount} arquivo(s)",
        Path = folder.Path
    };

    public static LibraryRow FromMedia(MediaItem media) => new()
    {
        Kind = LibraryEntryKind.Media,
        Title = media.DisplayTitle,
        Subtitle = media.DisplaySubtitle,
        Path = media.Path,
        Media = media
    };
}
