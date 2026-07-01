using ValPlay.Models;

namespace ValPlay.Services;

public interface IAudioEqualizerService
{
    EqualizerPreset CurrentPreset { get; }
    IReadOnlyList<EqualizerPreset> AvailablePresets { get; }

    void AttachToSession(int audioSessionId);
    void Detach();
    void ApplyPreset(EqualizerPreset preset);
    string GetPresetLabel(EqualizerPreset preset);
}
