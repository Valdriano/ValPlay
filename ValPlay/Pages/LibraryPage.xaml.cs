using ValPlay.Models;
using ValPlay.ViewModels;

namespace ValPlay.Pages;

public partial class LibraryPage : ContentPage
{
    private readonly LibraryViewModel _viewModel;

    public LibraryPage(LibraryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.Items.Count == 0 && !_viewModel.IsScanning)
            await _viewModel.ScanCommand.ExecuteAsync(null);
    }

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is MediaItem item)
            await _viewModel.PlayItemCommand.ExecuteAsync(item);
    }
}
