using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValPlay.Models;
using ValPlay.Services;

namespace ValPlay.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    private readonly IPlaybackService _playbackService;
    private readonly ISettingsService _settingsService;

    public PlayerViewModel(IPlaybackService playbackService, ISettingsService settingsService)
    {
        _playbackService = playbackService;
        _settingsService = settingsService;

        _playbackService.StateChanged += (_, _) => RefreshFromService();
        _playbackService.MediaChanged += (_, media) => OnMediaChanged(media);

        RefreshFromService();
    }

    [ObservableProperty]
    private string _title = "Nenhuma mídia";

    [ObservableProperty]
    private string _subtitle = "Selecione um arquivo na biblioteca";

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _shuffleEnabled;

    [ObservableProperty]
    private RepeatMode _repeatMode;

    [ObservableProperty]
    private TimeSpan _position;

    [ObservableProperty]
    private TimeSpan _duration;

    [ObservableProperty]
    private double _positionSeconds;

    [ObservableProperty]
    private bool _isVideo;

    [ObservableProperty]
    private bool _hasMedia;

    [ObservableProperty]
    private string? _mediaPath;

    public bool ShouldResumeFromSavedPosition { get; private set; }
    public double SavedPositionSeconds { get; private set; }

    [RelayCommand]
    private void TogglePlayPause() => _playbackService.TogglePlayPause();

    [RelayCommand]
    private void Next() => _playbackService.Next();

    [RelayCommand]
    private void Previous() => _playbackService.Previous();

    [RelayCommand]
    private void ToggleShuffle() => _playbackService.ToggleShuffle();

    [RelayCommand]
    private void CycleRepeat() => _playbackService.CycleRepeatMode();

    public void OnPlaybackStarted()
    {
        _playbackService.Play();
        RefreshFromService();
    }

    public void OnPlaybackPaused()
    {
        _playbackService.Pause();
        RefreshFromService();
    }

    public void ReportPlaybackPosition(TimeSpan position, TimeSpan duration)
    {
        _playbackService.UpdatePosition(position, duration);
        Duration = duration;
        PositionSeconds = position.TotalSeconds;

        if (Position != position)
            Position = position;
    }

    public void OnMediaFinished()
    {
        _playbackService.OnMediaEnded();
        RefreshFromService();
    }

    public void SeekTo(double seconds)
    {
        PositionSeconds = seconds;
        Position = TimeSpan.FromSeconds(seconds);
    }

    private void OnMediaChanged(MediaItem? media)
    {
        if (media is null)
        {
            HasMedia = false;
            MediaPath = null;
            Title = "Nenhuma mídia";
            Subtitle = "Selecione um arquivo na biblioteca";
            IsVideo = false;
            return;
        }

        HasMedia = true;
        Title = media.DisplayTitle;
        Subtitle = media.DisplaySubtitle;
        IsVideo = media.Type == MediaType.Video;
        MediaPath = media.Path;

        var settings = _settingsService.Current;
        ShouldResumeFromSavedPosition = settings.ResumePlaybackOnStart &&
                                        settings.LastMediaPath == media.Path &&
                                        settings.LastPositionSeconds > 1;
        SavedPositionSeconds = settings.LastPositionSeconds;
    }

    private void RefreshFromService()
    {
        IsPlaying = _playbackService.IsPlaying;
        ShuffleEnabled = _playbackService.ShuffleEnabled;
        RepeatMode = _playbackService.RepeatMode;
        Position = _playbackService.Position;
        Duration = _playbackService.Duration;
        PositionSeconds = _playbackService.Position.TotalSeconds;
        HasMedia = _playbackService.CurrentMedia is not null;

        if (_playbackService.CurrentMedia is { } media)
        {
            Title = media.DisplayTitle;
            Subtitle = media.DisplaySubtitle;
            IsVideo = media.Type == MediaType.Video;
        }
    }
}
