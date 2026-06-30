using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ValPlay.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    private const int SecretTapCount = 5;
    private const int TapResetMilliseconds = 2500;
    private const string SecretUrl = "https://www.vw.com.br";

    private int _logoTapCount;
    private DateTime _lastLogoTap;

    public AboutViewModel()
    {
        AppVersion = AppInfo.Current.VersionString;
        BuildLabel = GetBuildDateLabel();
    }

    [ObservableProperty]
    private string _appVersion = "1.0";

    [ObservableProperty]
    private string _buildLabel = string.Empty;

    [ObservableProperty]
    private string _appName = "ValPlay";

    [ObservableProperty]
    private string _platformLabel = "VW Play";

    [RelayCommand]
    private async Task OnLogoTappedAsync()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastLogoTap).TotalMilliseconds > TapResetMilliseconds)
            _logoTapCount = 0;

        _lastLogoTap = now;
        _logoTapCount++;

        if (_logoTapCount < SecretTapCount)
            return;

        _logoTapCount = 0;

        try
        {
            await Launcher.Default.OpenAsync(new Uri(SecretUrl));
        }
        catch
        {
            // Navegador indisponível no dispositivo.
        }
    }

    private static string GetBuildDateLabel()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var dllPath = Path.Combine(baseDir, "ValPlay.dll");

            if (File.Exists(dllPath))
                return File.GetLastWriteTime(dllPath).ToString("dd/MM/yyyy HH:mm");
        }
        catch
        {
            // ignored
        }

        return DateTime.Now.ToString("dd/MM/yyyy");
    }
}
