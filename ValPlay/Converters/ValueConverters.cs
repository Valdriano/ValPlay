using System.Globalization;
using ValPlay.Models;

namespace ValPlay.Converters;

public sealed class TimeSpanToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TimeSpan timeSpan)
            return "0:00";

        return timeSpan.TotalHours >= 1
            ? timeSpan.ToString(@"h\:mm\:ss", culture)
            : timeSpan.ToString(@"m\:ss", culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToPlayPauseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "⏸" : "▶";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class RepeatModeToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            RepeatMode.One => "🔂 1",
            RepeatMode.All => "🔁",
            _ => "➡"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ShuffleToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enabled = value is true;
        if (Application.Current?.Resources.TryGetValue(enabled ? "Accent" : "TextMuted", out var color) == true)
            return color;

        return enabled ? Colors.Cyan : Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

public sealed class MediaTypeToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is MediaType.Video ? "🎬" : "🎵";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class FavoriteColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value is true ? "Accent" : "TextMuted";
        if (Application.Current?.Resources.TryGetValue(key, out var color) == true)
            return color;

        return value is true ? Color.FromArgb("#00B4D8") : Color.FromArgb("#8B949E");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
