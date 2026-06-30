using ValPlay.Helpers;
using ValPlay.Models;

namespace ValPlay.Services;

public sealed class MediaLibraryService : IMediaLibraryService
{
    private readonly ISettingsService _settingsService;
    private readonly List<MediaItem> _items = [];

    public MediaLibraryService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public IReadOnlyList<MediaItem> Items => _items;
    public bool IsScanning { get; private set; }
    public string? LastScanPath { get; private set; }

    public event EventHandler? LibraryChanged;

    public async Task<bool> RequestPermissionsAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.StorageRead>();
        return status == PermissionStatus.Granted;
    }

    public IReadOnlyList<string> GetDefaultScanRoots()
    {
        var roots = new List<string>();

#if ANDROID
        try
        {
            var external = Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath;
            if (!string.IsNullOrWhiteSpace(external))
                roots.Add(external);
        }
        catch
        {
            // ignored
        }

        try
        {
            if (Directory.Exists("/storage"))
            {
                foreach (var entry in Directory.GetDirectories("/storage"))
                {
                    if (!entry.EndsWith("emulated", StringComparison.OrdinalIgnoreCase) &&
                        !entry.EndsWith("self", StringComparison.OrdinalIgnoreCase))
                    {
                        roots.Add(entry);
                    }
                }
            }
        }
        catch
        {
            // ignored
        }
#endif

        if (roots.Count == 0)
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrWhiteSpace(documents))
                roots.Add(documents);
        }

        return roots.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public IReadOnlyList<MediaFolder> GetSubfolders(string parentPath)
    {
        var parent = NormalizePath(parentPath);
        if (!Directory.Exists(parent))
            return [];

        var folders = new List<MediaFolder>();

        try
        {
            foreach (var directory in Directory.EnumerateDirectories(parent))
            {
                if (ShouldSkipFolder(directory))
                    continue;

                var count = GetItemsInFolder(directory, recursive: true).Count;
                if (count == 0)
                    continue;

                folders.Add(new MediaFolder
                {
                    Path = directory,
                    Name = Path.GetFileName(directory) ?? directory,
                    ItemCount = count
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            // ignored
        }
        catch (DirectoryNotFoundException)
        {
            // ignored
        }

        return folders.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public IReadOnlyList<MediaItem> GetItemsInFolder(string folderPath, bool recursive = false)
    {
        var folder = NormalizePath(folderPath);

        return _items
            .Where(item =>
            {
                var directory = NormalizePath(Path.GetDirectoryName(item.Path) ?? string.Empty);
                return recursive
                    ? directory.StartsWith(folder, StringComparison.OrdinalIgnoreCase)
                    : directory.Equals(folder, StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(item => item.DisplayTitle, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task ScanAsync(string? rootPath = null, CancellationToken cancellationToken = default)
    {
        if (IsScanning)
            return;

        IsScanning = true;
        _items.Clear();
        LibraryChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            var roots = string.IsNullOrWhiteSpace(rootPath)
                ? GetDefaultScanRoots()
                : [rootPath];

            foreach (var root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Directory.Exists(root))
                    continue;

                await Task.Run(() => ScanDirectory(root, cancellationToken), cancellationToken);
                LastScanPath = root;
            }

            _items.Sort((a, b) => string.Compare(a.DisplayTitle, b.DisplayTitle, StringComparison.OrdinalIgnoreCase));

            _settingsService.Update(settings =>
            {
                settings.LastScanRootPath = LastScanPath ?? settings.LastScanRootPath;
            });
        }
        finally
        {
            IsScanning = false;
            LibraryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ScanDirectory(string path, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                cancellationToken.ThrowIfCancellationRequested();
                TryAddMediaFile(file);
            }

            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (ShouldSkipFolder(directory))
                    continue;

                ScanDirectory(directory, cancellationToken);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // ignored
        }
        catch (DirectoryNotFoundException)
        {
            // ignored
        }
    }

    private void TryAddMediaFile(string filePath)
    {
        if (!MediaFormatHelper.IsMedia(filePath))
            return;

        var fileName = Path.GetFileName(filePath);
        var title = Path.GetFileNameWithoutExtension(filePath);
        string? artist = null;
        string? album = null;
        int? year = null;
        TimeSpan? duration = null;

        if (MediaFormatHelper.IsAudio(filePath))
        {
            (artist, album, year, duration) = MediaTagReader.ReadAudioTags(filePath);
        }

        _items.Add(new MediaItem
        {
            Path = filePath,
            FileName = fileName,
            Title = title,
            Type = MediaFormatHelper.GetMediaType(filePath),
            Artist = artist,
            Album = album,
            Year = year,
            Duration = duration
        });
    }

    private static bool ShouldSkipFolder(string directory)
    {
        var name = Path.GetFileName(directory);
        return name.StartsWith('.') ||
               name.Equals("Android", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("LOST.DIR", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
