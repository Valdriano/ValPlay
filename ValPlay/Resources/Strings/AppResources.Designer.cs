using System.Globalization;
using System.Resources;

#nullable enable

namespace ValPlay.Resources.Strings;

public static class AppResources
{
    private static readonly ResourceManager ResourceManager = new(
        "ValPlay.Resources.Strings.AppResources",
        typeof(AppResources).Assembly);

    public static string? Get(string name, CultureInfo? culture = null) =>
        ResourceManager.GetString(name, culture);
}
