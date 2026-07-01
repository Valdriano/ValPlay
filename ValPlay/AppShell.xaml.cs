using ValPlay.Pages;
using ValPlay.Services;

namespace ValPlay;

public partial class AppShell : Shell
{
    private readonly ILocalizationService _localization;
    private readonly ShellContent _libraryTab;
    private readonly ShellContent _favoritesTab;
    private readonly ShellContent _playerTab;
    private readonly ShellContent _settingsTab;
    private readonly ShellContent _aboutTab;
    private bool _splashShown;

    public AppShell(
        LibraryPage libraryPage,
        FavoritesPage favoritesPage,
        PlayerPage playerPage,
        SettingsPage settingsPage,
        AboutPage aboutPage,
        AppBootstrapper bootstrapper,
        ILocalizationService localization)
    {
        InitializeComponent();
        FlyoutBehavior = FlyoutBehavior.Disabled;
        _localization = localization;

        _libraryTab = new ShellContent
        {
            Title = _localization.GetString("Tab_Library"),
            Icon = "tab_library",
            Content = libraryPage,
            Route = "LibraryPage"
        };

        _favoritesTab = new ShellContent
        {
            Title = _localization.GetString("Tab_Favorites"),
            Icon = "tab_favorites",
            Content = favoritesPage,
            Route = "FavoritesPage"
        };

        _playerTab = new ShellContent
        {
            Title = _localization.GetString("Tab_Player"),
            Icon = "tab_player",
            Content = playerPage,
            Route = "PlayerPage"
        };

        _settingsTab = new ShellContent
        {
            Title = _localization.GetString("Tab_Settings"),
            Icon = "tab_settings",
            Content = settingsPage,
            Route = "SettingsPage"
        };

        _aboutTab = new ShellContent
        {
            Title = _localization.GetString("Tab_About"),
            Icon = "tab_about",
            Content = aboutPage,
            Route = "AboutPage"
        };

        MainTabBar.Items.Add(_libraryTab);
        MainTabBar.Items.Add(_favoritesTab);
        MainTabBar.Items.Add(_playerTab);
        MainTabBar.Items.Add(_settingsTab);
        MainTabBar.Items.Add(_aboutTab);

        _localization.LanguageChanged += (_, _) => UpdateTabTitles();
        Loaded += OnShellLoaded;

        _ = InitializeAppAsync(bootstrapper);
    }

    private async void OnShellLoaded(object? sender, EventArgs e)
    {
        if (_splashShown)
            return;

        _splashShown = true;
        Loaded -= OnShellLoaded;

        await Task.Delay(100);

        try
        {
            await Navigation.PushModalAsync(new SplashPage(), false);
        }
        catch
        {
            // Splash opcional — não impede o uso do app.
        }
    }

    private void UpdateTabTitles()
    {
        _libraryTab.Title = _localization.GetString("Tab_Library");
        _favoritesTab.Title = _localization.GetString("Tab_Favorites");
        _playerTab.Title = _localization.GetString("Tab_Player");
        _settingsTab.Title = _localization.GetString("Tab_Settings");
        _aboutTab.Title = _localization.GetString("Tab_About");
    }

    private static async Task InitializeAppAsync(AppBootstrapper bootstrapper)
    {
        try
        {
            await bootstrapper.InitializeAsync();
        }
        catch
        {
            // Falha silenciosa na inicialização para não bloquear a UI.
        }
    }
}
