namespace ValPlay.Models;

public sealed class MediaItem
{
    public required string Path { get; init; }
    public required string FileName { get; init; }
    public required string Title { get; init; }
    public required MediaType Type { get; init; }
    public string? Artist { get; init; }
    public string? Album { get; init; }
    public TimeSpan? Duration { get; init; }

    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? FileName : Title;

    public string DisplaySubtitle => Type == MediaType.Video
        ? "Vídeo"
        : string.IsNullOrWhiteSpace(Artist) ? "Áudio" : Artist;
}
