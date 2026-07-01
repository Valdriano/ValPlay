#if ANDROID
using Android.Media.Audiofx;
using ValPlay.Services;

namespace ValPlay.Platforms.Android;

public sealed class AndroidAudioVisualizerService : IAudioVisualizerService
{
    private const int BandCount = 22;
    private const int CaptureRateHz = 20_000;
    private readonly float[] _bands = new float[BandCount];
    private readonly float[] _smoothed = new float[BandCount];
    private Visualizer? _visualizer;
    private int _attachedSessionId = -1;

    public event EventHandler<float[]>? BandsUpdated;

    public void AttachToSession(int audioSessionId)
    {
        var targetSession = audioSessionId > 0 ? audioSessionId : 0;
        if (_visualizer is not null && _attachedSessionId == targetSession)
            return;

        Detach();

        try
        {
            var range = Visualizer.GetCaptureSizeRange()!;
            var captureSize = range[1];
            _visualizer = new Visualizer(targetSession);
            _visualizer.SetCaptureSize(captureSize);
            _visualizer.SetDataCaptureListener(
                new AudioCaptureListener(OnWaveformCaptured, OnFftCaptured),
                CaptureRateHz,
                true,
                true);
            _visualizer.SetEnabled(true);
            _attachedSessionId = targetSession;
        }
        catch
        {
            Detach();
        }
    }

    public void Detach()
    {
        if (_visualizer is null)
            return;

        try
        {
            _visualizer.SetEnabled(false);
            _visualizer.SetDataCaptureListener(null!, 0, false, false);
            _visualizer.Release();
        }
        catch
        {
            // ignored
        }
        finally
        {
            _visualizer = null;
            _attachedSessionId = -1;
            Array.Clear(_smoothed);
        }
    }

    private void OnWaveformCaptured(byte[] waveform)
    {
        ParseWaveform(waveform, _bands);
        PublishBands();
    }

    private void OnFftCaptured(byte[] fft)
    {
        ParseFft(fft, _bands);
        PublishBands();
    }

    private void PublishBands()
    {
        const float smoothing = 0.35f;
        for (var i = 0; i < _smoothed.Length; i++)
            _smoothed[i] = _smoothed[i] * (1f - smoothing) + _bands[i] * smoothing;

        NormalizeBands(_smoothed);
        BandsUpdated?.Invoke(this, _smoothed);
    }

    private static void ParseWaveform(byte[] waveform, float[] bands)
    {
        if (waveform.Length < bands.Length)
            return;

        var chunk = Math.Max(1, waveform.Length / bands.Length);

        for (var band = 0; band < bands.Length; band++)
        {
            var sum = 0f;
            var start = band * chunk;
            var end = Math.Min(waveform.Length, start + chunk);

            for (var i = start; i < end; i++)
            {
                var sample = (sbyte)waveform[i] / 128f;
                sum += MathF.Abs(sample);
            }

            bands[band] = sum / Math.Max(1, end - start);
        }
    }

    private static void ParseFft(byte[] fft, float[] bands)
    {
        var usablePairs = (fft.Length - 2) / 2;
        if (usablePairs <= 0)
            return;

        for (var band = 0; band < bands.Length; band++)
        {
            var startPair = band * usablePairs / bands.Length;
            var endPair = (band + 1) * usablePairs / bands.Length;
            var sum = 0f;
            var count = 0;

            for (var pair = startPair; pair < endPair; pair++)
            {
                var index = 2 + pair * 2;
                if (index + 1 >= fft.Length)
                    break;

                var real = (sbyte)fft[index];
                var imag = (sbyte)fft[index + 1];
                sum += MathF.Sqrt(real * real + imag * imag);
                count++;
            }

            bands[band] = count > 0 ? sum / count / 128f : 0f;
        }
    }

    private static void NormalizeBands(float[] bands)
    {
        var peak = 0f;
        for (var i = 0; i < bands.Length; i++)
            peak = MathF.Max(peak, bands[i]);

        var floor = MathF.Max(peak * 0.18f, 0.04f);
        for (var i = 0; i < bands.Length; i++)
            bands[i] = Math.Clamp((bands[i] - floor) / MathF.Max(peak - floor, 0.001f), 0.05f, 1f);
    }

    private sealed class AudioCaptureListener(
        Action<byte[]> onWaveform,
        Action<byte[]> onFft) : Java.Lang.Object, Visualizer.IOnDataCaptureListener
    {
        public void OnFftDataCapture(Visualizer? visualizer, byte[]? fft, int samplingRate)
        {
            if (fft is { Length: > 2 })
                onFft(fft);
        }

        public void OnWaveFormDataCapture(Visualizer? visualizer, byte[]? waveform, int samplingRate)
        {
            if (waveform is { Length: > 0 })
                onWaveform(waveform);
        }
    }
}
#endif
