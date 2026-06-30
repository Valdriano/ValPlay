using ValPlay.Models;

namespace ValPlay.Services;

public interface IMediaLibraryService
{
    IReadOnlyList<MediaItem> Items { get; }
    bool IsScanning { get; }
    string? LastScanPath { get; }
    event EventHandler? LibraryChanged;

    Task ScanAsync(string? rootPath = null, CancellationToken cancellationToken = default);
    Task<bool> RequestPermissionsAsync();
    IReadOnlyList<string> GetDefaultScanRoots();

    IReadOnlyList<MediaFolder> GetSubfolders(string parentPath);
    IReadOnlyList<MediaItem> GetItemsInFolder(string folderPath, bool recursive = false);
}
