using Microsoft.Extensions.DependencyInjection;
using ValPlay.Services;

namespace ValPlay.Pages;

public partial class SplashPage : ContentPage
{
    private readonly IServiceProvider _services;
    private readonly AppBootstrapper _bootstrapper;
    private bool _animationStarted;

    public SplashPage(IServiceProvider services, AppBootstrapper bootstrapper)
    {
        _services = services;
        _bootstrapper = bootstrapper;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_animationStarted)
            return;

        _animationStarted = true;
        _ = RunSplashAsync();
    }

    private async Task RunSplashAsync()
    {
        _ = InitializeAppInBackgroundAsync();

        var fadeIn = new Animation(v => LogoImage.Opacity = v, 0, 1);
        var scaleIn = new Animation(v => LogoImage.Scale = v, 0.6, 1);
        var ringFade = new Animation(v => GlowRing.Opacity = v, 0, 1);
        var ringScale = new Animation(v => GlowRing.Scale = v, 0.8, 1);
        var titleFade = new Animation(v => AppNameLabel.Opacity = v, 0, 1);

        var intro = new Animation();
        intro.Add(0, 1, fadeIn);
        intro.Add(0, 1, scaleIn);
        intro.Add(0.2, 1, ringFade);
        intro.Add(0.2, 1, ringScale);
        intro.Add(0.5, 1, titleFade);

        var tcs = new TaskCompletionSource();
        intro.Commit(this, "SplashIntro", length: 900, easing: Easing.CubicOut, finished: (_, _) => tcs.TrySetResult());
        await tcs.Task;

        await LogoImage.ScaleToAsync(1.06, 500, Easing.SinInOut);
        await LogoImage.ScaleToAsync(1, 500, Easing.SinInOut);

        var pulse = new Animation(v => GlowRing.Scale = v, 1, 1.12);
        var pulseBack = new Animation(v => GlowRing.Scale = v, 1.12, 1);
        var pulseAnim = new Animation();
        pulseAnim.Add(0, 0.5, pulse);
        pulseAnim.Add(0.5, 1, pulseBack);

        var pulseTcs = new TaskCompletionSource();
        pulseAnim.Commit(this, "SplashPulse", length: 700, easing: Easing.SinInOut, finished: (_, _) => pulseTcs.TrySetResult());
        await pulseTcs.Task;

        await Task.Delay(250);

        var fadeOutTcs = new TaskCompletionSource();
        new Animation(v => Opacity = v, 1, 0)
            .Commit(this, "SplashOut", length: 300, easing: Easing.CubicIn, finished: (_, _) => fadeOutTcs.TrySetResult());
        await fadeOutTcs.Task;

        await NavigateToShellAsync();
    }

    private async Task InitializeAppInBackgroundAsync()
    {
        try
        {
            await _bootstrapper.InitializeAsync();
        }
        catch
        {
            // Falha silenciosa na inicialização para não bloquear a UI.
        }
    }

    private async Task NavigateToShellAsync()
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is null)
            return;

        var shell = _services.GetRequiredService<AppShell>();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            window.Page = shell;
        });
    }
}
