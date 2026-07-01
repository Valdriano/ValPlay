#if ANDROID
using AndroidX.Media3.Common;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Views;

namespace ValPlay.Platforms.Android;

public static class AndroidMediaSessionHelper
{
    public static int GetAudioSessionId(MediaElement mediaElement)
    {
        try
        {
            var platformView = mediaElement.Handler?.PlatformView;
            if (platformView is null)
                return 0;

            if (platformView is PlayerView playerView)
                return ReadSessionId(playerView.Player);

            var playerViewProperty = platformView.GetType().GetProperty("PlayerView");
            if (playerViewProperty?.GetValue(platformView) is PlayerView nestedPlayerView)
                return ReadSessionId(nestedPlayerView.Player);

            var playerProperty = platformView.GetType().GetProperty("Player");
            if (playerProperty?.GetValue(platformView) is IPlayer player)
                return ReadSessionId(player);

            foreach (var property in platformView.GetType().GetProperties())
            {
                if (property.GetValue(platformView) is not IPlayer nestedPlayer)
                    continue;

                var sessionId = ReadSessionId(nestedPlayer);
                if (sessionId > 0)
                    return sessionId;
            }
        }
        catch
        {
            // ignored
        }

        return 0;
    }

    private static int ReadSessionId(IPlayer? player)
    {
        if (player is null)
            return 0;

        var property = player.GetType().GetProperty("AudioSessionId");
        if (property?.GetValue(player) is int sessionId && sessionId > 0)
            return sessionId;

        var method = player.GetType().GetMethod("GetAudioSessionId", Type.EmptyTypes)
            ?? player.GetType().GetMethod("getAudioSessionId", Type.EmptyTypes);
        if (method?.Invoke(player, null) is int methodSessionId && methodSessionId > 0)
            return methodSessionId;

        return 0;
    }
}
#endif
