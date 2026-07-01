using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ValPlay.Helpers;
using ValPlay.Models;
using ValPlay.Pages;
using ValPlay.Services;

namespace ValPlay.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    private readonly IPlaybackService _playbackService;
    private readonly ISettingsService _settingsService;
    private readonly IMediaMetadataService _metadataService;
    private readonly ILocalizationService _localization;
    private readonly IFavoritesService _favoritesService;
    private readonly IServiceProvider _services;
    private CancellationTokenSource? _albumArtCts;

    public PlayerViewModel(
        IPlaybackService playbackService,
        ISettingsService settingsService,
        IMediaMetadataService metadataService,
        ILocalizationService localization,
        IFavoritesService favoritesService,
        IServiceProvider services)
    {
        _playbackService = playbackService;
        _settingsService = settingsService;
        _metadataService = metadataService;
        _localization = localization;
        _favoritesService = favoritesService;
        _services = services;

        _playbackService.StateChanged += (_, _) => RefreshFromService();
        _playbackService.MediaChanged += (_, media) => OnMediaChanged(media);
        _favoritesService.FavoritesChanged += (_, _) => UpdateFavoriteState();
        _localization.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(string.Empty);
            RefreshFromService();
            if (!HasMedia)
                ApplyEmptyState();
        };

        ApplyEmptyState();
        RefreshFromService();

        VisualizationMode = _settingsService.Current.VisualizationMode;
    }

    public string PageTitle => _localization.GetString("Player_Title");
    public string FavoriteIcon => IsFavorite ? "♥" : "♡";
    public string EqualizerIcon => "🎚";
    public string VisualizationFullscreenIcon => IsVisualizationFullscreen ? "✕" : "⛶";
    public bool ShowEqualizer => HasMedia && IsAudio;
    public bool ShowVisualizationToggle => IsAudio && ShowPlayerChrome;
    public bool ShowVisualization => IsAudio && VisualizationMode != VisualizationMode.Off;
    public bool ShowVisualizationExpand => ShowVisualization && !IsVisualizationFullscreen && ShowPlayerChrome;
    public bool ShowAlbumArtImage => IsAudio && HasAlbumArt && VisualizationMode == VisualizationMode.Off && !IsVisualizationFullscreen;
    public bool ShowAlbumArtPlaceholder => IsAudio && !HasAlbumArt && VisualizationMode == VisualizationMode.Off && !IsVisualizationFullscreen;

    public string VisualizationIcon => VisualizationMode switch
    {
        VisualizationMode.Bars => "▮",
        VisualizationMode.Waves => "∿",
        VisualizationMode.Orbs => "◎",
        _ => "◉"
    };

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    [ObservableProperty]
    private string _artist = "—";

    [ObservableProperty]
    private string _album = "—";

    [ObservableProperty]
    private string _year = "—";

    [ObservableProperty]
    private string _yearLabel = string.Empty;

    [ObservableProperty]
    private string _durationLabel = string.Empty;

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

    [ObservableProperty]
    private bool _isVisualizationFullscreen;

    public bool ShowPlayerChrome => !IsFullscreen && !IsVisualizationFullscreen;

    [ObservableProperty]
    private bool _hasMedia;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private VisualizationMode _visualizationMode;

    private float[] _audioBands = new float[22];

    public float[] AudioBands => _audioBands;

    [ObservableProperty]
    private int _audioBandsVersion;

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
    private void ToggleFavorite()
    {
        if (_playbackService.CurrentMedia is not { } media)
            return;

        IsFavorite = _favoritesService.Toggle(media);
    }

    [RelayCommand]
    private async Task OpenEqualizerAsync()
    {
        var page = _services.GetRequiredService<EqualizerPage>();
        await Shell.Current.Navigation.PushModalAsync(page);
    }

    [RelayCommand]
    private void CycleVisualization()
    {
        VisualizationMode = VisualizationMode switch
        {
            VisualizationMode.Off => VisualizationMode.Bars,
            VisualizationMode.Bars => VisualizationMode.Waves,
            VisualizationMode.Waves => VisualizationMode.Orbs,
            _ => VisualizationMode.Off
        };

        _settingsService.Update(settings => settings.VisualizationMode = VisualizationMode);
        NotifyVisualizationProperties();
    }

    [RelayCommand]
    private void ToggleFullscreen()
    {
        if (!IsVideo)
            return;

        IsFullscreen = !IsFullscreen;
    }

    [RelayCommand]
    private void ToggleVisualizationFullscreen()
    {
        if (!ShowVisualization && !IsVisualizationFullscreen)
            return;

        IsVisualizationFullscreen = !IsVisualizationFullscreen;
    }

    public void UpdateAudioBands(float[] bands)
    {
        if (bands.Length == 0)
            return;

        if (_audioBands.Length != bands.Length)
            _audioBands = new float[bands.Length];

        Array.Copy(bands, _audioBands, bands.Length);
        AudioBandsVersion++;
        OnPropertyChanged(nameof(AudioBands));
    }

    public void ExitVisualizationFullscreen()
    {
        if (IsVisualizationFullscreen)
            IsVisualizationFullscreen = false;
    }

    public void ExitFullscreen()
    {
        if (IsFullscreen)
            IsFullscreen = false;
    }

    public void ReloadSettings()
    {
        VisualizationMode = _settingsService.Current.VisualizationMode;
        NotifyVisualizationProperties();
    }

    partial void OnIsFullscreenChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowPlayerChrome));
        OnPropertyChanged(nameof(ShowAudioProgress));
        NotifyVisualizationProperties();
    }

    partial void OnIsVisualizationFullscreenChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowPlayerChrome));
        OnPropertyChanged(nameof(ShowAudioProgress));
        OnPropertyChanged(nameof(VisualizationFullscreenIcon));
        NotifyVisualizationProperties();
    }

    partial void OnVisualizationModeChanged(VisualizationMode value) => NotifyVisualizationProperties();

    partial void OnIsPlayingChanged(bool value) => OnPropertyChanged(nameof(ShowVisualization));

    partial void OnIsVideoChanged(bool value)
    {
        if (!value)
            ExitFullscreen();

        OnPropertyChanged(nameof(IsAudio));
        OnPropertyChanged(nameof(ShowAudioProgress));
        OnPropertyChanged(nameof(ShowEqualizer));
        NotifyVisualizationProperties();
    }

    partial void OnHasMediaChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAudio));
        OnPropertyChanged(nameof(ShowAudioProgress));
        OnPropertyChanged(nameof(ShowEqualizer));
        NotifyVisualizationProperties();
    }

    partial void OnIsFavoriteChanged(bool value) => OnPropertyChanged(nameof(FavoriteIcon));

    partial void OnAlbumArtChanged(ImageSource? value)
    {
        OnPropertyChanged(nameof(HasAlbumArt));
        OnPropertyChanged(nameof(ShowAlbumArtImage));
        OnPropertyChanged(nameof(ShowAlbumArtPlaceholder));
    }

    private void NotifyVisualizationProperties()
    {
        OnPropertyChanged(nameof(ShowVisualization));
        OnPropertyChanged(nameof(ShowVisualizationToggle));
        OnPropertyChanged(nameof(ShowVisualizationExpand));
        OnPropertyChanged(nameof(ShowAlbumArtImage));
        OnPropertyChanged(nameof(ShowAlbumArtPlaceholder));
        OnPropertyChanged(nameof(VisualizationIcon));
    }

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
            ApplyEmptyState();
            AlbumArt = null;
            IsFavorite = false;
            IsVideo = false;
            ExitFullscreen();
            return;
        }

        HasMedia = true;
        ApplyMediaMetadata(media);
        IsVideo = media.Type == MediaType.Video;
        MediaPath = media.Path;
        AlbumArt = null;
        UpdateFavoriteState();

        if (!IsVideo)
            _ = LoadAlbumArtAsync(media.Path, token);

        var settings = _settingsService.Current;
        ShouldResumeFromSavedPosition = settings.ResumePlaybackOnStart &&
                                        settings.LastMediaPath == media.Path &&
                                        settings.LastPositionSeconds > 1;
        SavedPositionSeconds = settings.LastPositionSeconds;
    }

    private void ApplyEmptyState()
    {
        Title = _localization.GetString("Player_NoMedia");
        Subtitle = _localization.GetString("Player_SelectFile");
        Artist = "—";
        Album = "—";
        Year = "—";
        YearLabel = _localization.GetString("Player_YearEmpty");
        TrackDuration = TimeSpan.Zero;
        DurationLabel = _localization.GetString("Player_DurationFormat", FormatDuration(TimeSpan.Zero));
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
        Subtitle = media.GetLocalizedSubtitle(_localization);
        Artist = string.IsNullOrWhiteSpace(media.Artist)
            ? _localization.GetString("Player_UnknownArtist")
            : media.Artist;
        Album = string.IsNullOrWhiteSpace(media.Album)
            ? _localization.GetString("Player_UnknownAlbum")
            : media.Album;
        Year = media.Year is > 0 ? media.Year.Value.ToString() : "—";
        YearLabel = media.Year is > 0
            ? _localization.GetString("Player_YearFormat", media.Year.Value)
            : _localization.GetString("Player_YearEmpty");
        TrackDuration = media.Duration ?? TimeSpan.Zero;
        DurationLabel = _localization.GetString("Player_DurationFormat", FormatDuration(TrackDuration));
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return duration.ToString(@"h\:mm\:ss", _localization.CurrentCulture);

        return duration.ToString(@"m\:ss", _localization.CurrentCulture);
    }

    private void UpdateFavoriteState()
    {
        var path = _playbackService.CurrentMedia?.Path ?? MediaPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            IsFavorite = false;
            return;
        }

        if (!File.Exists(path))
        {
            _favoritesService.Remove(path);
            IsFavorite = false;
            return;
        }

        IsFavorite = _favoritesService.IsFavorite(path);
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
            UpdateFavoriteState();
        }
        else
        {
            ApplyEmptyState();
        }
    }

    public bool ShouldResumeFromSavedPosition { get; private set; }
    public double SavedPositionSeconds { get; private set; }
}
