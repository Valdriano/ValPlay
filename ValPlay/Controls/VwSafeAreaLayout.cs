using ValPlay.Helpers;

namespace ValPlay.Controls;

/// <summary>
/// Limita o conteúdo à área útil do VW Play (1332 x 636 px @ 160 DPI).
/// </summary>
public class VwSafeAreaLayout : ContentView
{
    public VwSafeAreaLayout()
    {
        MaximumWidthRequest = VwPlayConstants.AppAreaWidth;
        MaximumHeightRequest = VwPlayConstants.AppAreaHeight;
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
    }
}
