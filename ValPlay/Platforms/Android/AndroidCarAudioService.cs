using Android.Media;
using ValPlay.Services;

namespace ValPlay.Platforms.Android;

/// <summary>
/// Gerencia foco de áudio conforme especificação VW Play (USAGE_MEDIA / AOSP).
/// </summary>
public sealed class AndroidCarAudioService : ICarAudioService
{
    private readonly AudioManager _audioManager;
    private readonly FocusChangeListener _focusListener;
    private AudioFocusRequestClass? _focusRequest;
    private bool _hasFocus;

    public AndroidCarAudioService()
    {
        var context = global::Android.App.Application.Context;
        _audioManager = (AudioManager)context.GetSystemService(global::Android.Content.Context.AudioService)!;
        _focusListener = new FocusChangeListener(change => FocusChanged?.Invoke(this, change));
    }

    public event EventHandler<CarAudioFocusChange>? FocusChanged;

    public bool RequestMediaFocus()
    {
        if (_hasFocus)
            return true;

        var audioAttributes = new AudioAttributes.Builder()
            .SetUsage(AudioUsageKind.Media)!
            .Build();

        _focusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)!
            .SetAudioAttributes(audioAttributes)!
            .SetOnAudioFocusChangeListener(_focusListener)!
            .Build();

        var status = _audioManager.RequestAudioFocus(_focusRequest!);
        _hasFocus = status == AudioFocusRequest.Granted;
        return _hasFocus;
    }

    public void AbandonMediaFocus()
    {
        if (_focusRequest is null || !_hasFocus)
            return;

        _audioManager.AbandonAudioFocusRequest(_focusRequest);
        _hasFocus = false;
    }

    private sealed class FocusChangeListener : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener
    {
        private readonly Action<CarAudioFocusChange> _onChange;

        public FocusChangeListener(Action<CarAudioFocusChange> onChange) => _onChange = onChange;

        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            var mapped = focusChange switch
            {
                AudioFocus.Gain => CarAudioFocusChange.Gain,
                AudioFocus.Loss => CarAudioFocusChange.Loss,
                AudioFocus.LossTransient => CarAudioFocusChange.LossTransient,
                AudioFocus.LossTransientCanDuck => CarAudioFocusChange.LossTransientCanDuck,
                _ => CarAudioFocusChange.Loss
            };

            _onChange(mapped);
        }
    }
}
