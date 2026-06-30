using ValPlay.Models;
using ValPlay.Services;

namespace ValPlay.Services;

public sealed class AppBootstrapper
{
    private readonly IMediaLibraryService _mediaLibraryService;
    private readonly IPlaybackService _playbackService;
    private readonly ISettingsService _settingsService;

    public AppBootstrapper(
        IMediaLibraryService mediaLibraryService,
        IPlaybackService playbackService,
        ISettingsService settingsService)
    {
        _mediaLibraryService = mediaLibraryService;
        _playbackService = playbackService;
        _settingsService = settingsService;
    }

    public async Task InitializeAsync()
    {
        _playbackService.RestoreLastSession();

        var settings = _settingsService.Current;
        if (string.IsNullOrWhiteSpace(settings.LastMediaPath))
            return;

        await _mediaLibraryService.RequestPermissionsAsync();

        var scanRoot = settings.LastScanRootPath;
        if (string.IsNullOrWhiteSpace(scanRoot))
            scanRoot = _mediaLibraryService.GetDefaultScanRoots().FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(scanRoot))
            await _mediaLibraryService.ScanAsync(scanRoot);

        var items = _mediaLibraryService.Items;
        if (items.Count == 0)
            return;

        var lastIndex = items.ToList().FindIndex(i => i.Path == settings.LastMediaPath);
        if (lastIndex < 0)
            return;

        _playbackService.SetPlaylist(items, lastIndex);

        if (settings.ResumePlaybackOnStart)
            _playbackService.Play();
        else
            _playbackService.Pause();
    }
}
