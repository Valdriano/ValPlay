using Android.App;
using AndroidX.Core.App;
using ValPlay.Services;

namespace ValPlay.Platforms.Android;

/// <summary>
/// Notificação de reprodução conforme VW Play (sem ações contextuais).
/// </summary>
public sealed class AndroidCarNotificationService : ICarNotificationService
{
    private const string ChannelId = "valplay_playback";
    private const int NotificationId = 1001;

    public void ShowPlaybackNotification(string title)
    {
        var context = global::Android.App.Application.Context;
        var notificationManager = NotificationManagerCompat.From(context);
        if (notificationManager is null)
            return;

        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(
                ChannelId,
                "Reprodução de mídia",
                NotificationImportance.Low)
            {
                Description = "ValPlay reprodução em andamento"
            };

            notificationManager.CreateNotificationChannel(channel);
        }

        var builder = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(global::Android.Resource.Drawable.IcMediaPlay)
            .SetContentTitle(title)
            .SetContentText("N/A")
            .SetOngoing(true)
            .SetOnlyAlertOnce(true);

        notificationManager.Notify(NotificationId, builder.Build());
    }

    public void HidePlaybackNotification()
    {
        var context = global::Android.App.Application.Context;
        NotificationManagerCompat.From(context)?.Cancel(NotificationId);
    }
}
