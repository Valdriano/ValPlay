using ValPlay.Pages;
using ValPlay.Services;

namespace ValPlay;

public partial class AppShell : Shell
{
    public AppShell(
        LibraryPage libraryPage,
        PlayerPage playerPage,
        SettingsPage settingsPage,
        AboutPage aboutPage,
        AppBootstrapper bootstrapper)
    {
        InitializeComponent();

        MainTabBar.Items.Add(new ShellContent
        {
            Title = "Biblioteca",
            Content = libraryPage,
            Route = "LibraryPage"
        });

        MainTabBar.Items.Add(new ShellContent
        {
            Title = "Player",
            Content = playerPage,
            Route = "PlayerPage"
        });

        MainTabBar.Items.Add(new ShellContent
        {
            Title = "Ajustes",
            Content = settingsPage,
            Route = "SettingsPage"
        });

        MainTabBar.Items.Add(new ShellContent
        {
            Title = "Sobre",
            Content = aboutPage,
            Route = "AboutPage"
        });

        _ = InitializeAppAsync(bootstrapper);
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
