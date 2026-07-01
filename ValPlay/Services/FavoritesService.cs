using System.Text.Json;
using ValPlay.Models;

namespace ValPlay.Services;

public sealed class FavoritesService : IFavoritesService
{
    private const string FavoritesKey = "valplay_favorites";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly List<FavoriteEntry> _items = [];

    public FavoritesService()
    {
        Load();
    }

    public event EventHandler? FavoritesChanged;

    public IReadOnlyList<FavoriteEntry> Items => _items;

    public bool IsFavorite(string path) =>
        _items.Any(item => item.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

    public void Add(MediaItem media)
    {
        if (IsFavorite(media.Path))
            return;

        _items.Insert(0, new FavoriteEntry
        {
            Path = media.Path,
            Title = media.DisplayTitle,
            Artist = media.Artist,
            Album = media.Album,
            Type = media.Type,
            AddedAtUtc = DateTime.UtcNow
        });

        Save();
        FavoritesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(string path)
    {
        var removed = _items.RemoveAll(item =>
            item.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

        if (removed == 0)
            return;

        Save();
        FavoritesChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool Toggle(MediaItem media)
    {
        if (IsFavorite(media.Path))
        {
            Remove(media.Path);
            return false;
        }

        Add(media);
        return true;
    }

    public IReadOnlyList<MediaItem> GetPlayableItems() =>
        _items
            .Where(item => File.Exists(item.Path))
            .Select(item => item.ToMediaItem())
            .ToList();

    private void Load()
    {
        _items.Clear();
        var json = Preferences.Default.Get(FavoritesKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            var items = JsonSerializer.Deserialize<List<FavoriteEntry>>(json, JsonOptions);
            if (items is not null)
                _items.AddRange(items);
        }
        catch
        {
            // ignored
        }
    }

    private void Save() =>
        Preferences.Default.Set(FavoritesKey, JsonSerializer.Serialize(_items, JsonOptions));
}
