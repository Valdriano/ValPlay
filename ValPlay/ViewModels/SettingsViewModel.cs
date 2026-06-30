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

    public SettingsViewModel(ISettingsService settingsService, IPlaybackService playbackService)
    {
        _settingsService = settingsService;
        _playbackService = playbackService;
        LoadFromSettings();
    }

    public ObservableCollection<RepeatMode> RepeatModes { get; } =
    [
        RepeatMode.Off,
        RepeatMode.All,
        RepeatMode.One
    ];

    [ObservableProperty]
    private bool _shuffleEnabled;

    [ObservableProperty]
    private RepeatMode _repeatMode;

    [ObservableProperty]
    private bool _resumePlaybackOnStart = true;

    [ObservableProperty]
    private string _appVersion = AppInfo.Current.VersionString;

    partial void OnShuffleEnabledChanged(bool value)
    {
        if (_playbackService.ShuffleEnabled != value)
            _playbackService.ToggleShuffle();

        _settingsService.Update(s => s.ShuffleEnabled = value);
    }

    partial void OnRepeatModeChanged(RepeatMode value)
    {
        _settingsService.Update(s => s.RepeatMode = value);

        while (_playbackService.RepeatMode != value)
            _playbackService.CycleRepeatMode();
    }

    partial void OnResumePlaybackOnStartChanged(bool value)
    {
        _settingsService.Update(s => s.ResumePlaybackOnStart = value);
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
        ResumePlaybackOnStart = settings.ResumePlaybackOnStart;
    }
}
