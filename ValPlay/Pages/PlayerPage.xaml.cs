using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using ValPlay.Controls;
using ValPlay.Helpers;
using ValPlay.Services;
using ValPlay.ViewModels;
#if ANDROID
using ValPlay.Platforms.Android;
#endif

namespace ValPlay.Pages;

public partial class PlayerPage : ContentPage
{
    private readonly PlayerViewModel _viewModel;
    private readonly ICarAudioService _carAudio;
    private readonly ICarNotificationService _carNotification;
    private readonly IAudioEqualizerService _equalizer;
    private readonly IAudioVisualizerService _visualizer;
    private bool _isSeeking;
    private bool _shouldResumeAfterFocusGain;
    private CancellationTokenSource? _attachEffectsCts;

    public PlayerPage(
        PlayerViewModel viewModel,
        ICarAudioService carAudio,
        ICarNotificationService carNotification,
        IAudioEqualizerService equalizer,
        IAudioVisualizerService visualizer)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _carAudio = carAudio;
        _carNotification = carNotification;
        _equalizer = equalizer;
        _visualizer = visualizer;
        BindingContext = _viewModel;

        _carAudio.FocusChanged += OnAudioFocusChanged;
        _visualizer.BandsUpdated += OnVisualizerBandsUpdated;

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(PlayerViewModel.MediaPath) or nameof(PlayerViewModel.IsPlaying))
                SyncMediaElement();

            if (e.PropertyName is nameof(PlayerViewModel.IsFullscreen) or nameof(PlayerViewModel.IsVisualizationFullscreen))
                ApplyChromeMode();
        };

        MediaPlayer.MediaOpened += OnMediaOpened;
        MediaPlayer.MediaEnded += OnMediaEnded;
        MediaPlayer.PositionChanged += OnPositionChanged;
        MediaPlayer.StateChanged += OnMediaStateChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ReloadSettings();
        SyncMediaElement();
        ApplyChromeMode();
        _ = AttachAudioEffectsWithRetryAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _attachEffectsCts?.Cancel();
        _viewModel.ExitVisualizationFullscreen();
        if (_viewModel.IsVideo)
            _viewModel.ExitFullscreen();

        ApplyChromeMode();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsVisualizationFullscreen)
        {
            _viewModel.ExitVisualizationFullscreen();
            return true;
        }

        if (_viewModel.IsFullscreen)
        {
            _viewModel.ExitFullscreen();
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private void ApplyChromeMode()
    {
        var immersive = _viewModel.IsFullscreen || _viewModel.IsVisualizationFullscreen;
        Shell.SetTabBarIsVisible(this, !immersive);

        if (SafeArea is VwSafeAreaLayout safeArea)
        {
            safeArea.MaximumWidthRequest = immersive ? double.PositiveInfinity : VwPlayConstants.AppAreaWidth;
            safeArea.MaximumHeightRequest = immersive ? double.PositiveInfinity : VwPlayConstants.AppAreaHeight;
        }

        PlayerLayout.Padding = immersive ? new Thickness(0) : new Thickness(16, 12);
        BackgroundColor = immersive
            ? Colors.Black
            : (Color)Application.Current!.Resources["AppBackground"]!;

        if (_viewModel.IsVideo)
            MediaPlayer.Aspect = _viewModel.IsFullscreen ? Aspect.AspectFill : Aspect.AspectFit;

        FullscreenButton.Margin = _viewModel.IsFullscreen ? new Thickness(16) : new Thickness(8);
    }

    private void OnVisualizerBandsUpdated(object? sender, float[] bands)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _viewModel.UpdateAudioBands(bands);
        });
    }

    private void OnAudioFocusChanged(object? sender, CarAudioFocusChange change)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (change)
            {
                case CarAudioFocusChange.Loss:
                case CarAudioFocusChange.LossTransient:
                    if (MediaPlayer.CurrentState == MediaElementState.Playing)
                    {
                        _shouldResumeAfterFocusGain = change == CarAudioFocusChange.LossTransient;
                        MediaPlayer.Pause();
                    }
                    break;

                case CarAudioFocusChange.Gain when _shouldResumeAfterFocusGain:
                    _shouldResumeAfterFocusGain = false;
                    if (_viewModel.HasMedia)
                        MediaPlayer.Play();
                    break;
            }
        });
    }

    private void SyncMediaElement()
    {
        if (string.IsNullOrWhiteSpace(_viewModel.MediaPath))
            return;

        var source = MediaSource.FromFile(_viewModel.MediaPath);
        if (MediaPlayer.Source?.ToString() != source.ToString())
            MediaPlayer.Source = source;

        if (_viewModel.IsPlaying && MediaPlayer.CurrentState != MediaElementState.Playing)
            TryPlay();
        else if (!_viewModel.IsPlaying && MediaPlayer.CurrentState == MediaElementState.Playing)
            MediaPlayer.Pause();
    }

    private void TryPlay()
    {
        if (!_carAudio.RequestMediaFocus())
            return;

        MediaPlayer.Play();
        _carNotification.ShowPlaybackNotification(_viewModel.Title);
    }

    private void OnMediaOpened(object? sender, EventArgs e)
    {
        _ = AttachAudioEffectsWithRetryAsync();

        if (_viewModel.ShouldResumeFromSavedPosition)
            MediaPlayer.SeekTo(TimeSpan.FromSeconds(_viewModel.SavedPositionSeconds));

        if (_viewModel.IsPlaying)
            TryPlay();
    }

    private async Task AttachAudioEffectsWithRetryAsync()
    {
#if ANDROID
        _attachEffectsCts?.Cancel();
        _attachEffectsCts = new CancellationTokenSource();
        var token = _attachEffectsCts.Token;

        if (!await AndroidVisualizerPermissionHelper.EnsureGrantedAsync())
            return;

        for (var attempt = 0; attempt < 15 && !token.IsCancellationRequested; attempt++)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var sessionId = AndroidMediaSessionHelper.GetAudioSessionId(MediaPlayer);
                _equalizer.AttachToSession(sessionId);
                _visualizer.AttachToSession(sessionId);
            });

            var sessionId = AndroidMediaSessionHelper.GetAudioSessionId(MediaPlayer);
            if (sessionId > 0 || attempt >= 6)
                break;

            await Task.Delay(150, token);
        }
#endif
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        _viewModel.OnMediaFinished();
        SyncMediaElement();
    }

    private void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e)
    {
        if (_isSeeking)
            return;

        _viewModel.ReportPlaybackPosition(e.Position, MediaPlayer.Duration);
    }

    private void OnMediaStateChanged(object? sender, MediaStateChangedEventArgs e)
    {
        if (e.NewState == MediaElementState.Playing)
        {
            _ = AttachAudioEffectsWithRetryAsync();
            _viewModel.OnPlaybackStarted();
            _carNotification.ShowPlaybackNotification(_viewModel.Title);
        }
        else if (e.NewState is MediaElementState.Paused or MediaElementState.Stopped)
        {
            _viewModel.OnPlaybackPaused();

            if (e.NewState == MediaElementState.Stopped)
            {
                _carAudio.AbandonMediaFocus();
                _carNotification.HidePlaybackNotification();
                _visualizer.Detach();
            }
        }
    }

    private void OnSeekStarted(object? sender, EventArgs e) => _isSeeking = true;

    private void OnSeekCompleted(object? sender, EventArgs e)
    {
        _isSeeking = false;
        var seconds = _viewModel.PositionSeconds;
        _viewModel.SeekTo(seconds);
        MediaPlayer.SeekTo(TimeSpan.FromSeconds(seconds));
    }
}
