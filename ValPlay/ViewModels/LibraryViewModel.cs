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
            NotifyQuickPlayState();
            NotifySelectionState();
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
    public string RefreshLabel => $"🔄 {_localization.GetString("Library_Refresh")}";
    public string EmptyHint => _localization.GetString("Library_EmptyHint");
    public string PlayAllAudioLabel => $"🎵 {_localization.GetString("Library_PlayAllAudio")}";
    public string PlayAllVideoLabel => $"🎬 {_localization.GetString("Library_PlayAllVideo")}";
    public string PlayAllMediaLabel => $"▶️ {_localization.GetString("Library_PlayAllMedia")}";
    public string PlayFolderIcon => "📂▶";
    public string PlayItemIcon => "▶";
    public string BackLabel => "◀";
    public string SelectModeLabel => IsSelectionMode
        ? $"✕ {_localization.GetString("Library_SelectModeOff")}"
        : $"☑ {_localization.GetString("Library_SelectMode")}";
    public string PlaySelectedLabel => $"▶ {_localization.GetString("Library_PlaySelected")}";
    public string SelectAllLabel => $"☑ {_localization.GetString("Library_SelectAll")}";
    public string ClearSelectionLabel => $"✕ {_localization.GetString("Library_ClearSelection")}";

    public bool CanPlayAllAudio => _mediaLibraryService.Items.Any(item => item.Type == MediaType.Audio);
    public bool CanPlayAllVideo => _mediaLibraryService.Items.Any(item => item.Type == MediaType.Video);
    public bool CanPlayAllMedia => _mediaLibraryService.Items.Count > 0;
    public bool CanPlaySelected => SelectedCount > 0;
    public bool HasRows => Rows.Count > 0;

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

    [ObservableProperty]
    private bool _isSelectionMode;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private string _selectionStatus = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        if (IsSelectionMode)
            ExitSelectionMode();

        RefreshView();
    }

    partial void OnBrowsePathChanged(string? value) => UpdateNavigationState();

    partial void OnIsSelectionModeChanged(bool value)
    {
        OnPropertyChanged(nameof(SelectModeLabel));
        NotifySelectionState();
    }

    partial void OnSelectedCountChanged(int value) => NotifySelectionState();

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
            ExitSelectionMode();
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
    private void ToggleSelectionMode()
    {
        if (IsSelectionMode)
            ExitSelectionMode();
        else
            IsSelectionMode = true;
    }

    [RelayCommand]
    private void ToggleRowSelection(LibraryRow row)
    {
        row.IsSelected = !row.IsSelected;
        UpdateSelectedCount();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var row in Rows)
            row.IsSelected = true;

        UpdateSelectedCount();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var row in Rows)
            row.IsSelected = false;

        UpdateSelectedCount();
    }

    [RelayCommand(CanExecute = nameof(CanPlaySelected))]
    private async Task PlaySelectedAsync()
    {
        var playlist = BuildPlaylistFromSelection();
        if (playlist.Count == 0)
            return;

        ExitSelectionMode();
        await PlayPlaylistAsync(playlist);
    }

    [RelayCommand]
    private void OpenFolder(LibraryRow row)
    {
        if (row.Kind != LibraryEntryKind.Folder)
            return;

        if (IsSelectionMode)
        {
            ToggleRowSelection(row);
            return;
        }

        BrowsePath = row.Path;
        SearchText = string.Empty;
        RefreshView();
    }

    [RelayCommand]
    private async Task OpenOrPlayAsync(LibraryRow row)
    {
        if (IsSelectionMode)
        {
            ToggleRowSelection(row);
            return;
        }

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

        if (IsSelectionMode)
            ExitSelectionMode();

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

        await PlayPlaylistAsync(playlist);
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

    [RelayCommand(CanExecute = nameof(CanPlayAllAudio))]
    private async Task PlayAllAudioAsync() =>
        await PlayPlaylistAsync(_mediaLibraryService.Items.Where(item => item.Type == MediaType.Audio));

    [RelayCommand(CanExecute = nameof(CanPlayAllVideo))]
    private async Task PlayAllVideoAsync() =>
        await PlayPlaylistAsync(_mediaLibraryService.Items.Where(item => item.Type == MediaType.Video));

    [RelayCommand(CanExecute = nameof(CanPlayAllMedia))]
    private async Task PlayAllMediaAsync() =>
        await PlayPlaylistAsync(_mediaLibraryService.Items);

    private List<MediaItem> BuildPlaylistFromSelection()
    {
        var playlist = new List<MediaItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in Rows.Where(r => r.IsSelected))
        {
            IEnumerable<MediaItem> items = row.Kind == LibraryEntryKind.Folder
                ? _mediaLibraryService.GetItemsInFolder(row.Path, recursive: true)
                : row.Media is not null ? [row.Media] : [];

            foreach (var item in items)
            {
                if (seen.Add(item.Path))
                    playlist.Add(item);
            }
        }

        return playlist;
    }

    private async Task PlayPlaylistAsync(IEnumerable<MediaItem> items)
    {
        var playlist = items.ToList();
        if (playlist.Count == 0)
            return;

        _playbackService.SetPlaylist(playlist, 0);
        _playbackService.Play(playlist[0]);
        await Shell.Current.GoToAsync("//PlayerPage");
    }

    private void ExitSelectionMode()
    {
        IsSelectionMode = false;
        foreach (var row in Rows)
            row.IsSelected = false;

        SelectedCount = 0;
        SelectionStatus = string.Empty;
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = Rows.Count(r => r.IsSelected);
        SelectionStatus = SelectedCount > 0
            ? _localization.GetString("Library_SelectionCount", SelectedCount)
            : string.Empty;
    }

    private void NotifyQuickPlayState()
    {
        OnPropertyChanged(nameof(CanPlayAllAudio));
        OnPropertyChanged(nameof(CanPlayAllVideo));
        OnPropertyChanged(nameof(CanPlayAllMedia));
        PlayAllAudioCommand.NotifyCanExecuteChanged();
        PlayAllVideoCommand.NotifyCanExecuteChanged();
        PlayAllMediaCommand.NotifyCanExecuteChanged();
    }

    private void NotifySelectionState()
    {
        OnPropertyChanged(nameof(CanPlaySelected));
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(SelectModeLabel));
        OnPropertyChanged(nameof(PlaySelectedLabel));
        OnPropertyChanged(nameof(SelectAllLabel));
        OnPropertyChanged(nameof(ClearSelectionLabel));
        PlaySelectedCommand.NotifyCanExecuteChanged();
    }

    private void RefreshView()
    {
        IsScanning = _mediaLibraryService.IsScanning;
        var wasSelectionMode = IsSelectionMode;
        Rows.Clear();
        NotifyQuickPlayState();
        NotifySelectionState();

        if (wasSelectionMode)
        {
            SelectedCount = 0;
            SelectionStatus = string.Empty;
        }

        IsSearchMode = !string.IsNullOrWhiteSpace(SearchText);

        if (IsSearchMode)
        {
            var query = _mediaLibraryService.Items.AsEnumerable();
            query = query.Where(item =>
                item.DisplayTitle.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                item.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var item in query)
            AddRow(LibraryRow.FromMedia(item, _localization));

            if (!IsScanning)
                StatusMessage = Rows.Count == 0
                    ? _localization.GetString("Library_Status_NoSearchResults")
                    : _localization.GetString("Library_Status_SearchResults", Rows.Count);

            NotifySelectionState();
            return;
        }

        var currentPath = BrowsePath ?? GetBrowseRoot();
        if (string.IsNullOrWhiteSpace(BrowsePath) && _mediaLibraryService.Items.Count > 0)
            BrowsePath = currentPath;

        foreach (var folder in _mediaLibraryService.GetSubfolders(currentPath))
            AddRow(LibraryRow.FromFolder(folder, _localization));

        foreach (var item in _mediaLibraryService.GetItemsInFolder(currentPath))
            AddRow(LibraryRow.FromMedia(item, _localization));

        UpdateNavigationState();

        if (!IsScanning)
        {
            var folderCount = Rows.Count(r => r.Kind == LibraryEntryKind.Folder);
            var fileCount = Rows.Count(r => r.Kind == LibraryEntryKind.Media);
            StatusMessage = Rows.Count == 0
                ? _localization.GetString("Library_Status_EmptyFolder")
                : _localization.GetString("Library_Status_FolderSummary", folderCount, fileCount);
        }

        NotifySelectionState();
    }

    private void AddRow(LibraryRow row)
    {
        row.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LibraryRow.IsSelected))
                UpdateSelectedCount();
        };

        Rows.Add(row);
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
