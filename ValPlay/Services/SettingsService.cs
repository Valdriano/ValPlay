using System.Text.Json;
using ValPlay.Models;

namespace ValPlay.Services;

public sealed class SettingsService : ISettingsService
{
    private const string SettingsKey = "valplay_settings";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public AppSettings Current { get; private set; }

    public SettingsService()
    {
        Current = Load();
    }

    public void Save()
    {
        Preferences.Default.Set(SettingsKey, JsonSerializer.Serialize(Current, JsonOptions));
    }

    public void Update(Action<AppSettings> update)
    {
        update(Current);
        Save();
    }

    private static AppSettings Load()
    {
        var json = Preferences.Default.Get(SettingsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return new AppSettings();

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
}
