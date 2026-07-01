using Microsoft.Extensions.DependencyInjection;

namespace ValPlay;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var services = activationState?.Context?.Services
            ?? IPlatformApplication.Current?.Services
            ?? throw new InvalidOperationException("Serviços do aplicativo indisponíveis.");

        var shell = services.GetRequiredService<AppShell>();
        return new Window(shell);
    }
}
