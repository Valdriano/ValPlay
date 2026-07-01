#if ANDROID
using AndroidEqualizer = Android.Media.Audiofx.Equalizer;

namespace ValPlay.Platforms.Android;

public static class AndroidMediaSessionHelper
{
    public static int GetAudioSessionId(CommunityToolkit.Maui.Views.MediaElement mediaElement)
    {
        try
        {
            var platformView = mediaElement.Handler?.PlatformView;
            if (platformView is null)
                return 0;

            var playerProperty = platformView.GetType().GetProperty("Player");
            var player = playerProperty?.GetValue(platformView);
            if (player is null)
                return 0;

            var sessionProperty = player.GetType().GetProperty("AudioSessionId");
            if (sessionProperty?.GetValue(player) is int sessionId)
                return sessionId;
        }
        catch
        {
            // ignored
        }

        return 0;
    }
}
#endif
