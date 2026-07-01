#if ANDROID
using Android;
using Android.Content.PM;
using AndroidX.Core.Content;

namespace ValPlay.Platforms.Android;

internal static class AndroidVisualizerPermissionHelper
{
    public static async Task<bool> EnsureGrantedAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status == PermissionStatus.Granted)
            return true;

        status = await Permissions.RequestAsync<Permissions.Microphone>();
        if (status == PermissionStatus.Granted)
            return true;

        var activity = Platform.CurrentActivity;
        if (activity is null)
            return false;

        return ContextCompat.CheckSelfPermission(activity, Manifest.Permission.RecordAudio)
            == Permission.Granted;
    }
}
#endif
