#if ANDROID
using Android.Media.Audiofx;
using ValPlay.Services;

namespace ValPlay.Platforms.Android;

public sealed class AndroidAudioVisualizerService : IAudioVisualizerService
{
    private const int BandCount = 22;
    private const int CaptureRateHz = 12_000;
    private const float MinDb = -48f;
    private const float MaxDb = 0f;
    private const float Attack = 0.28f;
    private const float Release = 0.07f;

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
                new FftCaptureListener(OnFftCaptured),
                CaptureRateHz,
                false,
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

    private void OnFftCaptured(byte[] fft)
    {
        ParseFftLogBands(fft, _bands);
        SmoothBands(_bands, _smoothed);
        BandsUpdated?.Invoke(this, _smoothed);
    }

    private static void ParseFftLogBands(byte[] fft, float[] bands)
    {
        var usablePairs = (fft.Length - 2) / 2;
        if (usablePairs <= 1)
            return;

        for (var band = 0; band < bands.Length; band++)
        {
            var startPair = LogBinIndex(band, bands.Length, usablePairs);
            var endPair = LogBinIndex(band + 1, bands.Length, usablePairs);
            if (endPair <= startPair)
                endPair = startPair + 1;

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

            var average = count > 0 ? sum / count : 0f;
            var db = AmplitudeToDb(average);
            bands[band] = DbToNormalized(db);
        }
    }

    private static int LogBinIndex(int band, int bandCount, int fftBins)
    {
        var ratio = band / (double)bandCount;
        var value = Math.Pow(fftBins, ratio);
        return Math.Clamp((int)value - 1, 0, fftBins - 1);
    }

    private static float AmplitudeToDb(float amplitude)
    {
        const float minAmp = 2f;
        var amp = MathF.Max(amplitude, minAmp);
        return 20f * MathF.Log10(amp / 128f);
    }

    private static float DbToNormalized(float db) =>
        Math.Clamp((db - MinDb) / (MaxDb - MinDb), 0f, 1f);

    private static void SmoothBands(float[] source, float[] destination)
    {
        for (var i = 0; i < destination.Length; i++)
        {
            var target = i < source.Length ? source[i] : 0f;
            var coeff = target > destination[i] ? Attack : Release;
            destination[i] += (target - destination[i]) * coeff;
        }
    }

    private sealed class FftCaptureListener(Action<byte[]> onFft) : Java.Lang.Object, Visualizer.IOnDataCaptureListener
    {
        public void OnFftDataCapture(Visualizer? visualizer, byte[]? fft, int samplingRate)
        {
            if (fft is { Length: > 2 })
                onFft(fft);
        }

        public void OnWaveFormDataCapture(Visualizer? visualizer, byte[]? waveform, int samplingRate) { }
    }
}
#endif
