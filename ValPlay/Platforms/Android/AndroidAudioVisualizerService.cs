#if ANDROID
using Android.Media.Audiofx;
using ValPlay.Services;

namespace ValPlay.Platforms.Android;

public sealed class AndroidAudioVisualizerService : IAudioVisualizerService
{
    private const int BandCount = 22;
    private const int CaptureRateHz = 20_000;
    private readonly float[] _bands = new float[BandCount];
    private Visualizer? _visualizer;

    public event EventHandler<float[]>? BandsUpdated;

    public void AttachToSession(int audioSessionId)
    {
        Detach();
        if (audioSessionId <= 0)
            return;

        try
        {
            var range = Visualizer.GetCaptureSizeRange()!;
            var captureSize = range[1];
            _visualizer = new Visualizer(audioSessionId);
            _visualizer.SetCaptureSize(captureSize);
            _visualizer.SetDataCaptureListener(
                new FftCaptureListener(OnFftCaptured),
                CaptureRateHz,
                false,
                true);
            _visualizer.SetEnabled(true);
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
        }
    }

    private void OnFftCaptured(byte[] fft)
    {
        ParseFft(fft, _bands);
        BandsUpdated?.Invoke(this, _bands);
    }

    private static void ParseFft(byte[] fft, float[] bands)
    {
        var half = (fft.Length - 2) / 2;
        if (half <= 0)
            return;

        for (var band = 0; band < bands.Length; band++)
        {
            var start = 2 + band * half / bands.Length * 2;
            var end = 2 + (band + 1) * half / bands.Length * 2;
            var sum = 0f;
            var count = 0;

            for (var i = start; i < end && i + 1 < fft.Length; i += 2)
            {
                var magnitude = MathF.Sqrt(fft[i] * fft[i] + fft[i + 1] * fft[i + 1]);
                sum += magnitude;
                count++;
            }

            var average = count > 0 ? sum / count : 0f;
            bands[band] = Math.Clamp(average / 96f, 0.05f, 1f);
        }
    }

    private sealed class FftCaptureListener(Action<byte[]> callback) : Java.Lang.Object, Visualizer.IOnDataCaptureListener
    {
        public void OnFftDataCapture(Visualizer? visualizer, byte[]? fft, int samplingRate)
        {
            if (fft is { Length: > 2 })
                callback(fft);
        }

        public void OnWaveFormDataCapture(Visualizer? visualizer, byte[]? waveform, int samplingRate) { }
    }
}
#endif
