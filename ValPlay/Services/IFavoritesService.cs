using ValPlay.Models;

namespace ValPlay.Services;

public interface IFavoritesService
{
    event EventHandler? FavoritesChanged;

    IReadOnlyList<FavoriteEntry> Items { get; }

    bool IsFavorite(string path);
    void Add(MediaItem media);
    void Remove(string path);
    bool Toggle(MediaItem media);
    IReadOnlyList<MediaItem> GetPlayableItems();
    int PruneMissingFiles();
}
