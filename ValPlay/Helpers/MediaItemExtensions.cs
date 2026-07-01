using ValPlay.Models;
using ValPlay.Services;

namespace ValPlay.Helpers;

public static class MediaItemExtensions
{
    public static string GetLocalizedSubtitle(this MediaItem media, ILocalizationService localization) =>
        media.Type == MediaType.Video
            ? localization.GetString("MediaType_Video")
            : string.IsNullOrWhiteSpace(media.Artist)
                ? localization.GetString("MediaType_Audio")
                : media.Artist;
}
