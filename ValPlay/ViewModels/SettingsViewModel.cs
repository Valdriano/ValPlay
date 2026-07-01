using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValPlay.Models;
using ValPlay.Services;

namespace ValPlay.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IPlaybackService _playbackService;
    private readonly ILocalizationService _localization;
    private bool _isLoading;

    public SettingsViewModel(
        ISettingsService settingsService,
        IPlaybackService playbackService,
        ILocalizationService localization)
    {
        _settingsService = settingsService;
        _playbackService = playbackService;
        _localization = localization;

        _localization.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(string.Empty);
            RefreshRepeatModeOptions();
            RefreshVisualizationModeOptions();
        };

        Languages = new ObservableCollection<LanguageOption>(_localization.AvailableLanguages);
        RepeatModeOptions = new ObservableCollection<RepeatModeOption>();
        VisualizationModeOptions = new ObservableCollection<VisualizationModeOption>();

        _isLoading = true;
        LoadFromSettings();
        RefreshRepeatModeOptions();
        RefreshVisualizationModeOptions();
        _isLoading = false;
    }

    public ObservableCollection<LanguageOption> Languages { get; }

    public ObservableCollection<RepeatModeOption> RepeatModeOptions { get; }

    public ObservableCollection<VisualizationModeOption> VisualizationModeOptions { get; }

    public string PageTitle => _localization.GetString("Settings_Title");
    public string PlaybackTitle => _localization.GetString("Settings_Playback");
    public string ShuffleTitle => _localization.GetString("Settings_Shuffle");
    public string ShuffleHint => _localization.GetString("Settings_ShuffleHint");
    public string RepeatTitle => _localization.GetString("Settings_Repeat");
    public string ResumeTitle => _localization.GetString("Settings_Resume");
    public string ResumeHint => _localization.GetString("Settings_ResumeHint");
    public string ClearSessionLabel => _localization.GetString("Settings_ClearSession");
    public string LanguageTitle => _localization.GetString("Settings_Language");
    public string VisualizationTitle => _localization.GetString("Viz_Title");
    public string VisualizationHint => _localization.GetString("Viz_Hint");

    [ObservableProperty]
    private bool _shuffleEnabled;

    [ObservableProperty]
    private RepeatMode _repeatMode;

    [ObservableProperty]
    private RepeatModeOption? _selectedRepeatMode;

    [ObservableProperty]
    private VisualizationModeOption? _selectedVisualizationMode;

    [ObservableProperty]
    private VisualizationMode _visualizationMode;

    [ObservableProperty]
    private bool _resumePlaybackOnStart = true;

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    [ObservableProperty]
    private string _appVersion = AppInfo.Current.VersionString;

    partial void OnShuffleEnabledChanged(bool value)
    {
        if (_isLoading)
            return;

        if (_playbackService.ShuffleEnabled != value)
            _playbackService.ToggleShuffle();

        _settingsService.Update(s => s.ShuffleEnabled = value);
    }

    partial void OnSelectedRepeatModeChanged(RepeatModeOption? value)
    {
        if (_isLoading || value is null)
            return;

        RepeatMode = value.Mode;
    }

    partial void OnRepeatModeChanged(RepeatMode value)
    {
        if (_isLoading)
            return;

        _settingsService.Update(s => s.RepeatMode = value);

        while (_playbackService.RepeatMode != value)
            _playbackService.CycleRepeatMode();

        SyncSelectedRepeatMode(value);
    }

    partial void OnResumePlaybackOnStartChanged(bool value)
    {
        if (_isLoading)
            return;

        _settingsService.Update(s => s.ResumePlaybackOnStart = value);
    }

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (_isLoading || value is null)
            return;

        _localization.SetLanguage(value.Code);
    }

    partial void OnSelectedVisualizationModeChanged(VisualizationModeOption? value)
    {
        if (_isLoading || value is null)
            return;

        VisualizationMode = value.Mode;
    }

    partial void OnVisualizationModeChanged(VisualizationMode value)
    {
        if (_isLoading)
            return;

        _settingsService.Update(settings => settings.VisualizationMode = value);
        SyncSelectedVisualizationMode(value);
    }

    [RelayCommand]
    private void ClearSavedSession()
    {
        _settingsService.Update(s =>
        {
            s.LastMediaPath = null;
            s.LastPositionSeconds = 0;
        });
    }

    private void LoadFromSettings()
    {
        var settings = _settingsService.Current;
        ShuffleEnabled = settings.ShuffleEnabled;
        RepeatMode = settings.RepeatMode;
        VisualizationMode = settings.VisualizationMode;
        ResumePlaybackOnStart = settings.ResumePlaybackOnStart;

        SelectedLanguage = Languages.FirstOrDefault(language =>
            language.Code.Equals(_localization.CurrentLanguageCode, StringComparison.OrdinalIgnoreCase))
            ?? Languages.First();
    }

    private void RefreshRepeatModeOptions()
    {
        var currentMode = RepeatMode;
        RepeatModeOptions.Clear();
        RepeatModeOptions.Add(new RepeatModeOption
        {
            Mode = RepeatMode.Off,
            Label = _localization.GetRepeatModeLabel(RepeatMode.Off)
        });
        RepeatModeOptions.Add(new RepeatModeOption
        {
            Mode = RepeatMode.All,
            Label = _localization.GetRepeatModeLabel(RepeatMode.All)
        });
        RepeatModeOptions.Add(new RepeatModeOption
        {
            Mode = RepeatMode.One,
            Label = _localization.GetRepeatModeLabel(RepeatMode.One)
        });

        SyncSelectedRepeatMode(currentMode);
    }

    private void RefreshVisualizationModeOptions()
    {
        var currentMode = VisualizationMode;
        VisualizationModeOptions.Clear();
        VisualizationModeOptions.Add(new VisualizationModeOption
        {
            Mode = VisualizationMode.Off,
            Label = _localization.GetVisualizationModeLabel(VisualizationMode.Off)
        });
        VisualizationModeOptions.Add(new VisualizationModeOption
        {
            Mode = VisualizationMode.Bars,
            Label = _localization.GetVisualizationModeLabel(VisualizationMode.Bars)
        });
        VisualizationModeOptions.Add(new VisualizationModeOption
        {
            Mode = VisualizationMode.Waves,
            Label = _localization.GetVisualizationModeLabel(VisualizationMode.Waves)
        });
        VisualizationModeOptions.Add(new VisualizationModeOption
        {
            Mode = VisualizationMode.Orbs,
            Label = _localization.GetVisualizationModeLabel(VisualizationMode.Orbs)
        });

        SyncSelectedVisualizationMode(currentMode);
    }

    private void SyncSelectedVisualizationMode(VisualizationMode mode)
    {
        SelectedVisualizationMode = VisualizationModeOptions.FirstOrDefault(option => option.Mode == mode)
            ?? VisualizationModeOptions.FirstOrDefault();
    }

    private void SyncSelectedRepeatMode(RepeatMode mode)
    {
        SelectedRepeatMode = RepeatModeOptions.FirstOrDefault(option => option.Mode == mode)
            ?? RepeatModeOptions.FirstOrDefault();
    }
}
