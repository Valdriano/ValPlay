using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ValPlay.Pages;
using ValPlay.Platforms.Android;
using ValPlay.Services;
using ValPlay.ViewModels;

namespace ValPlay;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: true)
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
        builder.Services.AddSingleton<IMediaLibraryService, MediaLibraryService>();
        builder.Services.AddSingleton<IMediaMetadataService, MediaMetadataService>();
        builder.Services.AddSingleton<IPlaybackService, PlaybackService>();
        builder.Services.AddSingleton<IFavoritesService, FavoritesService>();
#if ANDROID
        builder.Services.AddSingleton<IAudioEqualizerService, AndroidAudioEqualizerService>();
#endif
        builder.Services.AddSingleton<AppBootstrapper>();
        builder.Services.AddSingleton<ICarAudioService, AndroidCarAudioService>();
        builder.Services.AddSingleton<ICarNotificationService, AndroidCarNotificationService>();

        builder.Services.AddTransient<PlayerViewModel>();
        builder.Services.AddTransient<LibraryViewModel>();
        builder.Services.AddTransient<FavoritesViewModel>();
        builder.Services.AddTransient<EqualizerViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<AboutViewModel>();

        builder.Services.AddTransient<PlayerPage>();
        builder.Services.AddTransient<LibraryPage>();
        builder.Services.AddTransient<FavoritesPage>();
        builder.Services.AddTransient<EqualizerPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AboutPage>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
