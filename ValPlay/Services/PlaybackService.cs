using ValPlay.Models;

namespace ValPlay.Services;

public sealed class PlaybackService : IPlaybackService
{
    private readonly ISettingsService _settingsService;
    private readonly Random _random = new();
    private List<MediaItem> _playlist = [];
    private List<int> _shuffleOrder = [];
    private int _shufflePointer;

    public PlaybackService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        ShuffleEnabled = settingsService.Current.ShuffleEnabled;
        RepeatMode = settingsService.Current.RepeatMode;
    }

    public MediaItem? CurrentMedia { get; private set; }
    public IReadOnlyList<MediaItem> Playlist => _playlist;
    public int CurrentIndex { get; private set; } = -1;
    public bool IsPlaying { get; private set; }
    public bool ShuffleEnabled { get; private set; }
    public RepeatMode RepeatMode { get; private set; }
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; set; }

    public event EventHandler? StateChanged;
    public event EventHandler<MediaItem?>? MediaChanged;

    public void SetPlaylist(IReadOnlyList<MediaItem> items, int startIndex = 0)
    {
        _playlist = items.ToList();
        RebuildShuffleOrder();

        if (_playlist.Count == 0)
        {
            CurrentIndex = -1;
            CurrentMedia = null;
            IsPlaying = false;
            NotifyChanged();
            MediaChanged?.Invoke(this, null);
            return;
        }

        CurrentIndex = Math.Clamp(startIndex, 0, _playlist.Count - 1);
        SetCurrentByIndex(CurrentIndex, autoPlay: false);
    }

    public void Play(MediaItem item)
    {
        var index = _playlist.FindIndex(m => m.Path == item.Path);
        if (index < 0)
        {
            _playlist = [item];
            RebuildShuffleOrder();
            index = 0;
        }

        CurrentIndex = index;
        SetCurrentByIndex(index, autoPlay: true);
    }

    public void Play()
    {
        if (CurrentMedia is null && _playlist.Count > 0)
            SetCurrentByIndex(0, autoPlay: true);
        else if (CurrentMedia is not null)
        {
            IsPlaying = true;
            NotifyChanged();
        }
    }

    public void Pause()
    {
        IsPlaying = false;
        SavePlaybackState();
        NotifyChanged();
    }

    public void TogglePlayPause()
    {
        if (IsPlaying)
            Pause();
        else
            Play();
    }

    public void Stop()
    {
        IsPlaying = false;
        Position = TimeSpan.Zero;
        SavePlaybackState();
        NotifyChanged();
    }

    public void Next()
    {
        if (_playlist.Count == 0)
            return;

        if (ShuffleEnabled)
        {
            if (_shuffleOrder.Count == 0)
                RebuildShuffleOrder();

            _shufflePointer = (_shufflePointer + 1) % _shuffleOrder.Count;
            SetCurrentByIndex(_shuffleOrder[_shufflePointer], autoPlay: true);
            return;
        }

        var nextIndex = CurrentIndex + 1;
        if (nextIndex >= _playlist.Count)
        {
            if (RepeatMode == RepeatMode.All)
                nextIndex = 0;
            else
                return;
        }

        SetCurrentByIndex(nextIndex, autoPlay: true);
    }

    public void Previous()
    {
        if (_playlist.Count == 0)
            return;

        if (Position > TimeSpan.FromSeconds(3))
        {
            Position = TimeSpan.Zero;
            NotifyChanged();
            return;
        }

        if (ShuffleEnabled)
        {
            if (_shuffleOrder.Count == 0)
                RebuildShuffleOrder();

            _shufflePointer = (_shufflePointer - 1 + _shuffleOrder.Count) % _shuffleOrder.Count;
            SetCurrentByIndex(_shuffleOrder[_shufflePointer], autoPlay: true);
            return;
        }

        var previousIndex = CurrentIndex - 1;
        if (previousIndex < 0)
        {
            if (RepeatMode == RepeatMode.All)
                previousIndex = _playlist.Count - 1;
            else
                return;
        }

        SetCurrentByIndex(previousIndex, autoPlay: true);
    }

    public void ToggleShuffle()
    {
        ShuffleEnabled = !ShuffleEnabled;
        RebuildShuffleOrder();
        _settingsService.Update(s => s.ShuffleEnabled = ShuffleEnabled);
        NotifyChanged();
    }

    public void CycleRepeatMode()
    {
        RepeatMode = RepeatMode switch
        {
            RepeatMode.Off => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            _ => RepeatMode.Off
        };

        _settingsService.Update(s => s.RepeatMode = RepeatMode);
        NotifyChanged();
    }

    public void OnMediaEnded()
    {
        if (RepeatMode == RepeatMode.One)
        {
            Position = TimeSpan.Zero;
            IsPlaying = true;
            NotifyChanged();
            MediaChanged?.Invoke(this, CurrentMedia);
            return;
        }

        Next();

        if (CurrentMedia is null || (!IsPlaying && RepeatMode == RepeatMode.Off))
            Stop();
    }

    public void UpdatePosition(TimeSpan position, TimeSpan duration)
    {
        Position = position;
        Duration = duration;

        if (duration > TimeSpan.Zero && position.TotalSeconds > 0 &&
            (int)position.TotalSeconds % 5 == 0)
        {
            SavePlaybackState();
        }
    }

    public void RestoreLastSession()
    {
        var settings = _settingsService.Current;
        ShuffleEnabled = settings.ShuffleEnabled;
        RepeatMode = settings.RepeatMode;
        NotifyChanged();
    }

    private void SetCurrentByIndex(int index, bool autoPlay)
    {
        if (index < 0 || index >= _playlist.Count)
            return;

        CurrentIndex = index;
        CurrentMedia = _playlist[index];
        Position = TimeSpan.Zero;
        Duration = TimeSpan.Zero;
        IsPlaying = autoPlay;

        if (ShuffleEnabled)
            _shufflePointer = _shuffleOrder.IndexOf(index);

        SavePlaybackState();
        NotifyChanged();
        MediaChanged?.Invoke(this, CurrentMedia);
    }

    private void RebuildShuffleOrder()
    {
        _shuffleOrder = Enumerable.Range(0, _playlist.Count).ToList();

        for (var i = _shuffleOrder.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (_shuffleOrder[i], _shuffleOrder[j]) = (_shuffleOrder[j], _shuffleOrder[i]);
        }

        if (CurrentIndex >= 0)
            _shufflePointer = Math.Max(0, _shuffleOrder.IndexOf(CurrentIndex));
        else
            _shufflePointer = 0;
    }

    private void SavePlaybackState()
    {
        _settingsService.Update(settings =>
        {
            settings.ShuffleEnabled = ShuffleEnabled;
            settings.RepeatMode = RepeatMode;
            settings.LastMediaPath = CurrentMedia?.Path;
            settings.LastPositionSeconds = Position.TotalSeconds;
        });
    }

    private void NotifyChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
