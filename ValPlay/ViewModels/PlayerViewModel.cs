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
    private readonly IMediaMetadataService _metadataService;
    private CancellationTokenSource? _albumArtCts;

    public PlayerViewModel(
        IPlaybackService playbackService,
        ISettingsService settingsService,
        IMediaMetadataService metadataService)
    {
        _playbackService = playbackService;
        _settingsService = settingsService;
        _metadataService = metadataService;

        _playbackService.StateChanged += (_, _) => RefreshFromService();
        _playbackService.MediaChanged += (_, media) => OnMediaChanged(media);

        RefreshFromService();
    }

    [ObservableProperty]
    private string _title = "Nenhuma mídia";

    [ObservableProperty]
    private string _subtitle = "Selecione um arquivo na biblioteca";

    [ObservableProperty]
    private string _artist = "—";

    [ObservableProperty]
    private string _album = "—";

    [ObservableProperty]
    private string _year = "—";

    [ObservableProperty]
    private string _yearLabel = "Ano: —";

    [ObservableProperty]
    private string _durationLabel = "Duração: 00:00";

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
    private TimeSpan _trackDuration;

    [ObservableProperty]
    private double _positionSeconds;

    [ObservableProperty]
    private bool _isVideo;

    [ObservableProperty]
    private bool _isFullscreen;

    public bool ShowPlayerChrome => !IsFullscreen;

    [ObservableProperty]
    private bool _hasMedia;

    [ObservableProperty]
    private string? _mediaPath;

    [ObservableProperty]
    private ImageSource? _albumArt;

    public bool HasAlbumArt => AlbumArt is not null;

    public bool IsAudio => HasMedia && !IsVideo;

    public bool ShowAudioProgress => IsAudio && HasMedia && ShowPlayerChrome;

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

    [RelayCommand]
    private void ToggleFullscreen()
    {
        if (!IsVideo)
            return;

        IsFullscreen = !IsFullscreen;
    }

    public void ExitFullscreen()
    {
        if (IsFullscreen)
            IsFullscreen = false;
    }

    partial void OnIsFullscreenChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowPlayerChrome));
        OnPropertyChanged(nameof(ShowAudioProgress));
    }

    partial void OnIsVideoChanged(bool value)
    {
        if (!value)
            ExitFullscreen();

        OnPropertyChanged(nameof(IsAudio));
        OnPropertyChanged(nameof(ShowAudioProgress));
    }

    partial void OnHasMediaChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAudio));
        OnPropertyChanged(nameof(ShowAudioProgress));
    }

    partial void OnAlbumArtChanged(ImageSource? value) =>
        OnPropertyChanged(nameof(HasAlbumArt));

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
        _albumArtCts?.Cancel();
        _albumArtCts = new CancellationTokenSource();
        var token = _albumArtCts.Token;

        if (media is null)
        {
            HasMedia = false;
            MediaPath = null;
            Title = "Nenhuma mídia";
            Subtitle = "Selecione um arquivo na biblioteca";
            Artist = "—";
            Album = "—";
            Year = "—";
            YearLabel = "Ano: —";
            DurationLabel = "Duração: 00:00";
            TrackDuration = TimeSpan.Zero;
            AlbumArt = null;
            IsVideo = false;
            ExitFullscreen();
            return;
        }

        HasMedia = true;
        ApplyMediaMetadata(media);
        IsVideo = media.Type == MediaType.Video;
        MediaPath = media.Path;
        AlbumArt = null;

        if (!IsVideo)
            _ = LoadAlbumArtAsync(media.Path, token);

        var settings = _settingsService.Current;
        ShouldResumeFromSavedPosition = settings.ResumePlaybackOnStart &&
                                        settings.LastMediaPath == media.Path &&
                                        settings.LastPositionSeconds > 1;
        SavedPositionSeconds = settings.LastPositionSeconds;
    }

    private async Task LoadAlbumArtAsync(string path, CancellationToken cancellationToken)
    {
        var art = await _metadataService.LoadAlbumArtAsync(path, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return;

        MainThread.BeginInvokeOnMainThread(() => AlbumArt = art);
    }

    private void ApplyMediaMetadata(MediaItem media)
    {
        Title = media.DisplayTitle;
        Subtitle = media.DisplaySubtitle;
        Artist = string.IsNullOrWhiteSpace(media.Artist) ? "Artista desconhecido" : media.Artist;
        Album = string.IsNullOrWhiteSpace(media.Album) ? "Álbum desconhecido" : media.Album;
        Year = media.Year is > 0 ? media.Year.Value.ToString() : "—";
        YearLabel = media.Year is > 0 ? $"Ano: {media.Year.Value}" : "Ano: —";
        TrackDuration = media.Duration ?? TimeSpan.Zero;
        DurationLabel = $"Duração: {FormatDuration(TrackDuration)}";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return duration.ToString(@"h\:mm\:ss");

        return duration.ToString(@"m\:ss");
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
            ApplyMediaMetadata(media);
            IsVideo = media.Type == MediaType.Video;
        }
    }

    public bool ShouldResumeFromSavedPosition { get; private set; }
    public double SavedPositionSeconds { get; private set; }
}
