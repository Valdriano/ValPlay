namespace ValPlay.Helpers;

public static class MediaFormatHelper
{
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".wav", ".aac", ".m4a", ".ogg", ".opus", ".wma", ".ape", ".alac", ".amr", ".3gp"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".mpeg", ".mpg", ".3gp"
    };

    public static bool IsAudio(string path) => HasExtension(path, AudioExtensions);

    public static bool IsVideo(string path) => HasExtension(path, VideoExtensions);

    public static bool IsMedia(string path) => IsAudio(path) || IsVideo(path);

    public static Models.MediaType GetMediaType(string path) =>
        IsVideo(path) ? Models.MediaType.Video : Models.MediaType.Audio;

    private static bool HasExtension(string path, HashSet<string> extensions)
    {
        var extension = Path.GetExtension(path);
        return !string.IsNullOrEmpty(extension) && extensions.Contains(extension);
    }
}
