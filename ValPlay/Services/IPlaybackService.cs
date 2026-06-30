using ValPlay.Models;

namespace ValPlay.Services;

public interface IPlaybackService
{
    MediaItem? CurrentMedia { get; }
    IReadOnlyList<MediaItem> Playlist { get; }
    int CurrentIndex { get; }
    bool IsPlaying { get; }
    bool ShuffleEnabled { get; }
    RepeatMode RepeatMode { get; }
    TimeSpan Position { get; set; }
    TimeSpan Duration { get; set; }

    event EventHandler? StateChanged;
    event EventHandler<MediaItem?>? MediaChanged;

    void SetPlaylist(IReadOnlyList<MediaItem> items, int startIndex = 0);
    void Play(MediaItem item);
    void Play();
    void Pause();
    void TogglePlayPause();
    void Stop();
    void Next();
    void Previous();
    void ToggleShuffle();
    void CycleRepeatMode();
    void OnMediaEnded();
    void UpdatePosition(TimeSpan position, TimeSpan duration);
    void RestoreLastSession();
}
