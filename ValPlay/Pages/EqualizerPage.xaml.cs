using ValPlay.ViewModels;

namespace ValPlay.Pages;

public partial class EqualizerPage : ContentPage
{
    public EqualizerPage(EqualizerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
