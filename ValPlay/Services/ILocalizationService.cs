using System.Globalization;
using ValPlay.Models;

namespace ValPlay.Services;

public interface ILocalizationService
{
    CultureInfo CurrentCulture { get; }
    string CurrentLanguageCode { get; }
    IReadOnlyList<LanguageOption> AvailableLanguages { get; }

    event EventHandler? LanguageChanged;

    string GetString(string key);
    string GetString(string key, params object[] args);
    void SetLanguage(string languageCode);
    void ApplySavedLanguage();
    string GetRepeatModeLabel(RepeatMode mode);
}
