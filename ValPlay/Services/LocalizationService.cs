using System.Globalization;
using ValPlay.Models;
using ValPlay.Resources.Strings;

namespace ValPlay.Services;

public sealed class LocalizationService : ILocalizationService
{
    private static readonly LanguageOption[] Languages =
    [
        new() { Code = "pt", DisplayName = "Português" },
        new() { Code = "en", DisplayName = "English" },
        new() { Code = "es", DisplayName = "Español" }
    ];

    private readonly ISettingsService _settingsService;
    private CultureInfo _currentCulture = new("pt");

    public LocalizationService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        ApplySavedLanguage();
    }

    public CultureInfo CurrentCulture => _currentCulture;

    public string CurrentLanguageCode => _currentCulture.TwoLetterISOLanguageName;

    public IReadOnlyList<LanguageOption> AvailableLanguages => Languages;

    public event EventHandler? LanguageChanged;

    public string GetString(string key)
    {
        var value = AppResources.Get(key, _currentCulture);
        if (!string.IsNullOrEmpty(value))
            return value;

        return AppResources.Get(key, CultureInfo.InvariantCulture) ?? key;
    }

    public string GetString(string key, params object[] args)
    {
        var template = GetString(key);
        return args.Length == 0 ? template : string.Format(_currentCulture, template, args);
    }

    public void SetLanguage(string languageCode)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        if (normalized.Equals(CurrentLanguageCode, StringComparison.OrdinalIgnoreCase))
            return;

        _currentCulture = CreateCulture(normalized);
        ApplyCulture(_currentCulture);

        _settingsService.Update(settings => settings.Language = normalized);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplySavedLanguage()
    {
        var saved = _settingsService.Current.Language;
        var normalized = NormalizeLanguageCode(saved);
        _currentCulture = CreateCulture(normalized);
        ApplyCulture(_currentCulture);
    }

    public string GetRepeatModeLabel(RepeatMode mode) => mode switch
    {
        RepeatMode.One => GetString("Repeat_One"),
        RepeatMode.All => GetString("Repeat_All"),
        _ => GetString("Repeat_Off")
    };

    public string GetVisualizationModeLabel(VisualizationMode mode) => mode switch
    {
        VisualizationMode.Bars => GetString("Viz_Bars"),
        VisualizationMode.Waves => GetString("Viz_Waves"),
        VisualizationMode.Orbs => GetString("Viz_Orbs"),
        _ => GetString("Viz_Off")
    };

    private static string NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return "pt";

        var code = languageCode.Trim().ToLowerInvariant();
        return Languages.Any(language => language.Code == code) ? code : "pt";
    }

    private static CultureInfo CreateCulture(string languageCode) =>
        languageCode switch
        {
            "en" => new CultureInfo("en"),
            "es" => new CultureInfo("es"),
            _ => new CultureInfo("pt")
        };

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
