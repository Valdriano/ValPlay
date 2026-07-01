#if ANDROID
using AndroidEqualizer = Android.Media.Audiofx.Equalizer;
using ValPlay.Models;

namespace ValPlay.Platforms.Android;

public sealed class AndroidAudioEqualizerService : Services.IAudioEqualizerService
{
    private readonly Services.ISettingsService _settingsService;
    private readonly Services.ILocalizationService _localization;
    private AndroidEqualizer? _equalizer;
    private int _sessionId;

    public AndroidAudioEqualizerService(
        Services.ISettingsService settingsService,
        Services.ILocalizationService localization)
    {
        _settingsService = settingsService;
        _localization = localization;
        CurrentPreset = _settingsService.Current.EqualizerPreset;
    }

    public EqualizerPreset CurrentPreset { get; private set; }

    public IReadOnlyList<EqualizerPreset> AvailablePresets { get; } =
    [
        EqualizerPreset.Off,
        EqualizerPreset.Normal,
        EqualizerPreset.Bass,
        EqualizerPreset.Treble,
        EqualizerPreset.Vocal,
        EqualizerPreset.Rock,
        EqualizerPreset.Pop
    ];

    public void AttachToSession(int audioSessionId)
    {
        if (audioSessionId <= 0)
            return;

        if (_equalizer is not null && _sessionId == audioSessionId)
            return;

        Detach();
        _sessionId = audioSessionId;

        try
        {
            _equalizer = new AndroidEqualizer(0, audioSessionId > 0 ? audioSessionId : 0);
            _equalizer.SetEnabled(true);
            ApplyPreset(_settingsService.Current.EqualizerPreset);
        }
        catch
        {
            Detach();
        }
    }

    public void Detach()
    {
        try
        {
            _equalizer?.Release();
        }
        catch
        {
            // ignored
        }
        finally
        {
            _equalizer = null;
            _sessionId = 0;
        }
    }

    public void ApplyPreset(EqualizerPreset preset)
    {
        CurrentPreset = preset;
        _settingsService.Update(settings => settings.EqualizerPreset = preset);

        if (_equalizer is null)
            return;

        if (preset == EqualizerPreset.Off)
        {
            _equalizer.SetEnabled(false);
            return;
        }

        _equalizer.SetEnabled(true);
        var bands = _equalizer.NumberOfBands;
        var levels = GetPresetLevels(preset, bands);

        for (short i = 0; i < bands; i++)
        {
            var level = i < levels.Length ? levels[i] : (short)0;
            _equalizer.SetBandLevel(i, level);
        }
    }

    public string GetPresetLabel(EqualizerPreset preset) => preset switch
    {
        EqualizerPreset.Off => _localization.GetString("Eq_Off"),
        EqualizerPreset.Normal => _localization.GetString("Eq_Normal"),
        EqualizerPreset.Bass => _localization.GetString("Eq_Bass"),
        EqualizerPreset.Treble => _localization.GetString("Eq_Treble"),
        EqualizerPreset.Vocal => _localization.GetString("Eq_Vocal"),
        EqualizerPreset.Rock => _localization.GetString("Eq_Rock"),
        EqualizerPreset.Pop => _localization.GetString("Eq_Pop"),
        _ => preset.ToString()
    };

    private static short[] GetPresetLevels(EqualizerPreset preset, short bandCount)
    {
        var factors = preset switch
        {
            EqualizerPreset.Normal => new[] { 0f, 0f, 0f, 0f, 0f },
            EqualizerPreset.Bass => new[] { 1f, 0.7f, 0.2f, 0f, 0f },
            EqualizerPreset.Treble => new[] { 0f, 0f, 0.2f, 0.7f, 1f },
            EqualizerPreset.Vocal => new[] { -0.2f, 0.3f, 0.8f, 0.4f, 0f },
            EqualizerPreset.Rock => new[] { 0.8f, 0.4f, 0.1f, 0.5f, 0.7f },
            EqualizerPreset.Pop => new[] { 0.3f, 0.5f, 0.4f, 0.6f, 0.5f },
            _ => new[] { 0f, 0f, 0f, 0f, 0f }
        };

        var result = new short[bandCount];
        for (short i = 0; i < bandCount; i++)
        {
            var factorIndex = bandCount == 1
                ? 0
                : (int)Math.Round(i / (double)(bandCount - 1) * (factors.Length - 1));
            var factor = factors[Math.Clamp(factorIndex, 0, factors.Length - 1)];
            result[i] = (short)(factor * 800);
        }

        return result;
    }
}
#endif
