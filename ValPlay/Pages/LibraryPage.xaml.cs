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

        if (_viewModel.Rows.Count == 0 && !_viewModel.IsScanning)
            await _viewModel.ScanCommand.ExecuteAsync(null);
    }
}
