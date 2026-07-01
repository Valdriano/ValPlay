using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValPlay.Helpers;
using ValPlay.Models;
using ValPlay.Services;

namespace ValPlay.ViewModels;

public partial class LibraryViewModel : ObservableObject
{
    private readonly IMediaLibraryService _mediaLibraryService;
    private readonly IPlaybackService _playbackService;
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localization;

    public LibraryViewModel(
        IMediaLibraryService mediaLibraryService,
        IPlaybackService playbackService,
        ISettingsService settingsService,
        ILocalizationService localization)
    {
        _mediaLibraryService = mediaLibraryService;
        _playbackService = playbackService;
        _settingsService = settingsService;
        _localization = localization;

        _mediaLibraryService.LibraryChanged += (_, _) => RefreshView();
        _localization.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(string.Empty);
            RefreshView();
        };

        ScanRoots = new ObservableCollection<string>(_mediaLibraryService.GetDefaultScanRoots());
        if (ScanRoots.Count > 0)
            SelectedScanRoot = _settingsService.Current.LastScanRootPath ?? ScanRoots[0];

        StatusMessage = _localization.GetString("Library_Status_Ready");
        RefreshView();
    }

    public ObservableCollection<LibraryRow> Rows { get; } = [];

    public ObservableCollection<string> ScanRoots { get; }

    public string PageTitle => _localization.GetString("Library_Title");
    public string SearchPlaceholder => _localization.GetString("Library_SearchPlaceholder");
    public string ScanFolderTitle => _localization.GetString("Library_ScanFolderTitle");
    public string RefreshLabel => _localization.GetString("Library_Refresh");
    public string EmptyHint => _localization.GetString("Library_EmptyHint");

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusMessage;

    [ObservableProperty]
    private string? _selectedScanRoot;

    [ObservableProperty]
    private string? _browsePath;

    [ObservableProperty]
    private string _currentFolderLabel = "/";

    [ObservableProperty]
    private bool _isSearchMode;

    [ObservableProperty]
    private bool _canGoBack;

    partial void OnSearchTextChanged(string value) => RefreshView();

    partial void OnBrowsePathChanged(string? value) => UpdateNavigationState();

    [RelayCommand]
    private async Task ScanAsync()
    {
        IsScanning = true;
        StatusMessage = _localization.GetString("Library_Status_Scanning");

        try
        {
            var granted = await _mediaLibraryService.RequestPermissionsAsync();
            if (!granted)
            {
                StatusMessage = _localization.GetString("Library_Status_PermissionDenied");
                return;
            }

            await _mediaLibraryService.ScanAsync(SelectedScanRoot);
            BrowsePath = _mediaLibraryService.LastScanPath ?? SelectedScanRoot;
            RefreshView();
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.GetString("Library_Status_ScanError", ex.Message);
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void OpenFolder(LibraryRow row)
    {
        if (row.Kind != LibraryEntryKind.Folder)
            return;

        BrowsePath = row.Path;
        SearchText = string.Empty;
        RefreshView();
    }

    [RelayCommand]
    private async Task OpenOrPlayAsync(LibraryRow row)
    {
        if (row.Kind == LibraryEntryKind.Folder)
            OpenFolder(row);
        else
            await PlayItemAsync(row);
    }

    [RelayCommand]
    private void GoBack()
    {
        if (string.IsNullOrWhiteSpace(BrowsePath))
            return;

        var parent = Path.GetDirectoryName(BrowsePath);
        var root = GetBrowseRoot();

        if (string.IsNullOrWhiteSpace(parent) ||
            parent.Length < root.Length ||
            !parent.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            BrowsePath = root;
        }
        else
        {
            BrowsePath = parent;
        }

        RefreshView();
    }

    [RelayCommand]
    private async Task PlayFolderAsync(LibraryRow row)
    {
        if (row.Kind != LibraryEntryKind.Folder)
            return;

        var playlist = _mediaLibraryService.GetItemsInFolder(row.Path, recursive: true);
        if (playlist.Count == 0)
            return;

        _playbackService.SetPlaylist(playlist, 0);
        _playbackService.Play(playlist[0]);
        await Shell.Current.GoToAsync("//PlayerPage");
    }

    [RelayCommand]
    private async Task PlayItemAsync(LibraryRow row)
    {
        if (row.Kind != LibraryEntryKind.Media || row.Media is null)
            return;

        var playlist = IsSearchMode
            ? Rows.Where(r => r.Kind == LibraryEntryKind.Media && r.Media is not null)
                .Select(r => r.Media!)
                .ToList()
            : _mediaLibraryService.GetItemsInFolder(BrowsePath ?? GetBrowseRoot(), recursive: false).ToList();

        var index = playlist.FindIndex(m => m.Path == row.Media.Path);
        if (index < 0)
        {
            playlist = [row.Media];
            index = 0;
        }

        _playbackService.SetPlaylist(playlist, index);
        _playbackService.Play(row.Media);
        await Shell.Current.GoToAsync("//PlayerPage");
    }

    private void RefreshView()
    {
        IsScanning = _mediaLibraryService.IsScanning;
        Rows.Clear();

        IsSearchMode = !string.IsNullOrWhiteSpace(SearchText);

        if (IsSearchMode)
        {
            var query = _mediaLibraryService.Items.AsEnumerable();
            query = query.Where(item =>
                item.DisplayTitle.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                item.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var item in query)
                Rows.Add(LibraryRow.FromMedia(item, _localization));

            if (!IsScanning)
                StatusMessage = Rows.Count == 0
                    ? _localization.GetString("Library_Status_NoSearchResults")
                    : _localization.GetString("Library_Status_SearchResults", Rows.Count);
            return;
        }

        var currentPath = BrowsePath ?? GetBrowseRoot();
        if (string.IsNullOrWhiteSpace(BrowsePath) && _mediaLibraryService.Items.Count > 0)
            BrowsePath = currentPath;

        foreach (var folder in _mediaLibraryService.GetSubfolders(currentPath))
            Rows.Add(LibraryRow.FromFolder(folder, _localization));

        foreach (var item in _mediaLibraryService.GetItemsInFolder(currentPath))
            Rows.Add(LibraryRow.FromMedia(item, _localization));

        UpdateNavigationState();

        if (!IsScanning)
        {
            var folderCount = Rows.Count(r => r.Kind == LibraryEntryKind.Folder);
            var fileCount = Rows.Count(r => r.Kind == LibraryEntryKind.Media);
            StatusMessage = Rows.Count == 0
                ? _localization.GetString("Library_Status_EmptyFolder")
                : _localization.GetString("Library_Status_FolderSummary", folderCount, fileCount);
        }
    }

    private void UpdateNavigationState()
    {
        var root = GetBrowseRoot();
        var path = BrowsePath ?? root;

        CurrentFolderLabel = string.IsNullOrWhiteSpace(path)
            ? "/"
            : Path.GetFileName(path) ?? path;

        CanGoBack = !string.IsNullOrWhiteSpace(path) &&
                    !path.Equals(root, StringComparison.OrdinalIgnoreCase);
    }

    private string GetBrowseRoot() =>
        _mediaLibraryService.LastScanPath ?? SelectedScanRoot ?? string.Empty;
}
