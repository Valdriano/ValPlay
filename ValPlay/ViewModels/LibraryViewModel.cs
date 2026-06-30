using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValPlay.Models;
using ValPlay.Services;

namespace ValPlay.ViewModels;

public partial class LibraryViewModel : ObservableObject
{
    private readonly IMediaLibraryService _mediaLibraryService;
    private readonly IPlaybackService _playbackService;
    private readonly ISettingsService _settingsService;

    public LibraryViewModel(
        IMediaLibraryService mediaLibraryService,
        IPlaybackService playbackService,
        ISettingsService settingsService)
    {
        _mediaLibraryService = mediaLibraryService;
        _playbackService = playbackService;
        _settingsService = settingsService;

        _mediaLibraryService.LibraryChanged += (_, _) => RefreshItems();

        ScanRoots = new ObservableCollection<string>(_mediaLibraryService.GetDefaultScanRoots());
        if (ScanRoots.Count > 0)
            SelectedScanRoot = _settingsService.Current.LastScanRootPath ?? ScanRoots[0];

        RefreshItems();
    }

    public ObservableCollection<MediaItem> Items { get; } = [];

    public ObservableCollection<string> ScanRoots { get; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusMessage = "Pronto para buscar mídias";

    [ObservableProperty]
    private string? _selectedScanRoot;

    [ObservableProperty]
    private MediaItem? _selectedItem;

    partial void OnSearchTextChanged(string value) => RefreshItems();

    [RelayCommand]
    private async Task ScanAsync()
    {
        IsScanning = true;
        StatusMessage = "Buscando arquivos...";

        try
        {
            var granted = await _mediaLibraryService.RequestPermissionsAsync();
            if (!granted)
            {
                StatusMessage = "Permissão de armazenamento negada";
                return;
            }

            await _mediaLibraryService.ScanAsync(SelectedScanRoot);
            RefreshItems();
            StatusMessage = $"{Items.Count} arquivo(s) encontrado(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao buscar: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task PlaySelectedAsync()
    {
        if (SelectedItem is null)
            return;

        _playbackService.SetPlaylist(Items.ToList(), Items.IndexOf(SelectedItem));
        _playbackService.Play(SelectedItem);
        await Shell.Current.GoToAsync("//PlayerPage");
    }

    [RelayCommand]
    private async Task PlayItemAsync(MediaItem item)
    {
        SelectedItem = item;
        _playbackService.SetPlaylist(Items.ToList(), Items.IndexOf(item));
        _playbackService.Play(item);
        await Shell.Current.GoToAsync("//PlayerPage");
    }

    private void RefreshItems()
    {
        IsScanning = _mediaLibraryService.IsScanning;
        Items.Clear();

        var query = _mediaLibraryService.Items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(item =>
                item.DisplayTitle.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                item.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
            Items.Add(item);

        if (!IsScanning)
            StatusMessage = Items.Count == 0
                ? "Nenhuma mídia encontrada. Toque em Atualizar."
                : $"{Items.Count} arquivo(s)";
    }
}
