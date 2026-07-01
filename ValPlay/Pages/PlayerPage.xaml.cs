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
    private bool _isSeeking;
    private bool _shouldResumeAfterFocusGain;

    public PlayerPage(
        PlayerViewModel viewModel,
        ICarAudioService carAudio,
        ICarNotificationService carNotification,
        IAudioEqualizerService equalizer)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _carAudio = carAudio;
        _carNotification = carNotification;
        _equalizer = equalizer;
        BindingContext = _viewModel;

        _carAudio.FocusChanged += OnAudioFocusChanged;

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(PlayerViewModel.MediaPath) or nameof(PlayerViewModel.IsPlaying))
                SyncMediaElement();

            if (e.PropertyName == nameof(PlayerViewModel.IsFullscreen))
                ApplyFullscreenMode(_viewModel.IsFullscreen);
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
        ApplyFullscreenMode(_viewModel.IsFullscreen);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.ExitFullscreen();
        ApplyFullscreenMode(false);
        _equalizer.Detach();

        if (MediaPlayer.CurrentState == MediaElementState.Playing)
            MediaPlayer.Pause();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsFullscreen)
        {
            _viewModel.ExitFullscreen();
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private void ApplyFullscreenMode(bool isFullscreen)
    {
        Shell.SetTabBarIsVisible(this, !isFullscreen);

        if (SafeArea is VwSafeAreaLayout safeArea)
        {
            safeArea.MaximumWidthRequest = isFullscreen ? double.PositiveInfinity : VwPlayConstants.AppAreaWidth;
            safeArea.MaximumHeightRequest = isFullscreen ? double.PositiveInfinity : VwPlayConstants.AppAreaHeight;
        }

        PlayerLayout.Padding = isFullscreen ? new Thickness(0) : new Thickness(16, 12);
        PlayerLayout.RowSpacing = isFullscreen ? 0 : 0;
        BackgroundColor = isFullscreen ? Colors.Black : (Color)Application.Current!.Resources["AppBackground"]!;

        MediaPlayer.Aspect = isFullscreen ? Aspect.AspectFill : Aspect.AspectFit;
        FullscreenButton.Margin = isFullscreen ? new Thickness(16) : new Thickness(8);
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
        AttachEqualizer();

        if (_viewModel.ShouldResumeFromSavedPosition)
            MediaPlayer.SeekTo(TimeSpan.FromSeconds(_viewModel.SavedPositionSeconds));

        if (_viewModel.IsPlaying)
            TryPlay();
    }

    private void AttachEqualizer()
    {
#if ANDROID
        var sessionId = AndroidMediaSessionHelper.GetAudioSessionId(MediaPlayer);
        if (sessionId > 0)
            _equalizer.AttachToSession(sessionId);
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
            AttachEqualizer();
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
