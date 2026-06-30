using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Util;

namespace ValPlay.Platforms.Android;

/// <summary>
/// Fixa o DPI em 160 conforme especificação VW Play.
/// </summary>
public static class VwDisplayHelper
{
    public const int VwPlayDpi = 160;

    public static void ApplyFixedDpi(Activity activity)
    {
        var resources = activity.Resources;
        if (resources is null)
            return;

        var displayMetrics = resources.DisplayMetrics;
        var configuration = resources.Configuration;
        if (displayMetrics is null || configuration is null)
            return;

#pragma warning disable CS0618, CA1422
        displayMetrics.DensityDpi = (DisplayMetricsDensity)VwPlayDpi;
        configuration.DensityDpi = VwPlayDpi;
        resources.UpdateConfiguration(configuration, displayMetrics);
#pragma warning restore CS0618, CA1422
    }
}
