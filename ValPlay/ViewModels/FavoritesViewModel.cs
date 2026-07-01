using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValPlay.Helpers;
using ValPlay.Models;
using ValPlay.Services;

namespace ValPlay.ViewModels;

public partial class FavoritesViewModel : ObservableObject
{
    private readonly IFavoritesService _favoritesService;
    private readonly IPlaybackService _playbackService;
    private readonly ILocalizationService _localization;

    public FavoritesViewModel(
        IFavoritesService favoritesService,
        IPlaybackService playbackService,
        ILocalizationService localization)
    {
        _favoritesService = favoritesService;
        _playbackService = playbackService;
        _localization = localization;

        _favoritesService.FavoritesChanged += (_, _) => Refresh();
        _localization.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(string.Empty);
            Refresh();
        };

        Refresh();
    }

    public ObservableCollection<FavoriteRow> Items { get; } = [];

    public string PageTitle => _localization.GetString("Favorites_Title");
    public string EmptyHint => _localization.GetString("Favorites_Empty");
    public string PlayAllLabel => $"▶ {_localization.GetString("Favorites_PlayAll")}";
    public string ShuffleLabel => $"🔀 {_localization.GetString("Favorites_Shuffle")}";
    public string RemoveIcon => "✕";

    public bool HasItems => Items.Count > 0;
    public bool CanPlay => Items.Any(item => item.Exists);

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [RelayCommand(CanExecute = nameof(CanPlay))]
    private async Task PlayAllAsync()
    {
        var playlist = GetPlayableItems();
        if (playlist.Count == 0)
            return;

        await StartPlaylistAsync(playlist, 0);
    }

    [RelayCommand(CanExecute = nameof(CanPlay))]
    private async Task ShuffleAllAsync()
    {
        var playlist = GetPlayableItems();
        if (playlist.Count == 0)
            return;

        var shuffled = playlist.OrderBy(_ => Random.Shared.Next()).ToList();
        await StartPlaylistAsync(shuffled, 0);
    }

    [RelayCommand]
    private async Task PlayItemAsync(FavoriteRow row)
    {
        if (!row.Exists || row.Entry is null)
            return;

        var playlist = GetPlayableItems();
        var media = row.Entry.ToMediaItem();
        var index = playlist.FindIndex(item => item.Path == media.Path);
        if (index < 0)
            playlist = [media];
        else
            index = Math.Max(0, index);

        await StartPlaylistAsync(playlist, Math.Max(0, index));
    }

    [RelayCommand]
    private void RemoveItem(FavoriteRow row)
    {
        if (row.Entry is null)
            return;

        _favoritesService.Remove(row.Entry.Path);
    }

    public void Refresh()
    {
        Items.Clear();

        foreach (var entry in _favoritesService.Items)
        {
            Items.Add(new FavoriteRow
            {
                Entry = entry,
                Title = entry.Title,
                Subtitle = BuildSubtitle(entry),
                Exists = File.Exists(entry.Path)
            });
        }

        StatusMessage = Items.Count == 0
            ? EmptyHint
            : _localization.GetString("Favorites_Count", Items.Count);

        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(CanPlay));
        PlayAllCommand.NotifyCanExecuteChanged();
        ShuffleAllCommand.NotifyCanExecuteChanged();
    }

    private string BuildSubtitle(FavoriteEntry entry)
    {
        if (entry.Type == MediaType.Video)
            return _localization.GetString("MediaType_Video");

        return string.IsNullOrWhiteSpace(entry.Artist)
            ? _localization.GetString("MediaType_Audio")
            : entry.Artist;
    }

    private List<MediaItem> GetPlayableItems() => _favoritesService.GetPlayableItems().ToList();

    private async Task StartPlaylistAsync(IReadOnlyList<MediaItem> playlist, int startIndex)
    {
        if (playlist.Count == 0)
            return;

        _playbackService.SetPlaylist(playlist, startIndex);
        _playbackService.Play(playlist[startIndex]);
        await Shell.Current.GoToAsync("//PlayerPage");
    }
}

public sealed class FavoriteRow
{
    public required FavoriteEntry Entry { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required bool Exists { get; init; }

    public string TypeIcon => Entry.Type == MediaType.Video ? "🎬" : "🎵";
}
