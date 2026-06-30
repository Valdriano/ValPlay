namespace ValPlay.Models;

public sealed class MediaFolder
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public int ItemCount { get; init; }
}
