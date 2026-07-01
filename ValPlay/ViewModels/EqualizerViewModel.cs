using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValPlay.Models;
using ValPlay.Services;

namespace ValPlay.ViewModels;

public partial class EqualizerViewModel : ObservableObject
{
    private readonly IAudioEqualizerService _equalizerService;
    private readonly ILocalizationService _localization;

    public EqualizerViewModel(IAudioEqualizerService equalizerService, ILocalizationService localization)
    {
        _equalizerService = equalizerService;
        _localization = localization;

        _localization.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(string.Empty);
            BuildPresets();
        };

        BuildPresets();
        SelectedPreset = Presets.FirstOrDefault(p => p.Preset == _equalizerService.CurrentPreset)
            ?? Presets.FirstOrDefault();
    }

    public ObservableCollection<EqualizerPresetOption> Presets { get; } = [];

    public string PageTitle => _localization.GetString("Eq_Title");
    public string Description => _localization.GetString("Eq_Description");
    public string CloseLabel => _localization.GetString("Eq_Close");

    [ObservableProperty]
    private EqualizerPresetOption? _selectedPreset;

    partial void OnSelectedPresetChanged(EqualizerPresetOption? value)
    {
        if (value is null)
            return;

        _equalizerService.ApplyPreset(value.Preset);
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        if (Shell.Current.Navigation.ModalStack.Count > 0)
            await Shell.Current.Navigation.PopModalAsync();
    }

    private void BuildPresets()
    {
        var selected = SelectedPreset?.Preset ?? _equalizerService.CurrentPreset;
        Presets.Clear();

        foreach (var preset in _equalizerService.AvailablePresets)
        {
            Presets.Add(new EqualizerPresetOption
            {
                Preset = preset,
                Label = _equalizerService.GetPresetLabel(preset)
            });
        }

        SelectedPreset = Presets.FirstOrDefault(p => p.Preset == selected) ?? Presets.FirstOrDefault();
    }
}

public sealed class EqualizerPresetOption
{
    public required EqualizerPreset Preset { get; init; }
    public required string Label { get; init; }

    public override string ToString() => Label;
}
