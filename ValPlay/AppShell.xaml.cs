using ValPlay.Pages;
using ValPlay.Services;

namespace ValPlay;

public partial class AppShell : Shell
{
    private readonly ILocalizationService _localization;
    private readonly ShellContent _libraryTab;
    private readonly ShellContent _playerTab;
    private readonly ShellContent _settingsTab;
    private readonly ShellContent _aboutTab;

    public AppShell(
        LibraryPage libraryPage,
        PlayerPage playerPage,
        SettingsPage settingsPage,
        AboutPage aboutPage,
        AppBootstrapper bootstrapper,
        ILocalizationService localization)
    {
        InitializeComponent();
        _localization = localization;

        _libraryTab = new ShellContent
        {
            Title = _localization.GetString("Tab_Library"),
            Content = libraryPage,
            Route = "LibraryPage"
        };

        _playerTab = new ShellContent
        {
            Title = _localization.GetString("Tab_Player"),
            Content = playerPage,
            Route = "PlayerPage"
        };

        _settingsTab = new ShellContent
        {
            Title = _localization.GetString("Tab_Settings"),
            Content = settingsPage,
            Route = "SettingsPage"
        };

        _aboutTab = new ShellContent
        {
            Title = _localization.GetString("Tab_About"),
            Content = aboutPage,
            Route = "AboutPage"
        };

        MainTabBar.Items.Add(_libraryTab);
        MainTabBar.Items.Add(_playerTab);
        MainTabBar.Items.Add(_settingsTab);
        MainTabBar.Items.Add(_aboutTab);

        _localization.LanguageChanged += (_, _) => UpdateTabTitles();

        _ = InitializeAppAsync(bootstrapper);
    }

    private void UpdateTabTitles()
    {
        _libraryTab.Title = _localization.GetString("Tab_Library");
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
