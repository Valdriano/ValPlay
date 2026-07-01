#if ANDROID
using Android.Media.Audiofx;
using ValPlay.Services;

namespace ValPlay.Platforms.Android;

public sealed class AndroidAudioVisualizerService : IAudioVisualizerService
{
    private const int BandCount = 22;
    private const int CaptureRateHz = 15_000;
    private const float MinDb = -54f;
    private const float MaxDb = 0f;
    private const float MinHz = 40f;
    private const float MaxHz = 16_000f;

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
            _visualizer = new Visualizer(targetSession);
            _visualizer.SetCaptureSize(range[1]);
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

    private void OnFftCaptured(byte[] fft, int samplingRate)
    {
        if (samplingRate <= 0)
            samplingRate = 44_100;

        MapFrequencyBands(fft, samplingRate, _bands);
        SmoothBands(_bands, _smoothed);
        BandsUpdated?.Invoke(this, _smoothed);
    }

    private static void MapFrequencyBands(byte[] fft, int samplingRate, float[] bands)
    {
        var binCount = fft.Length / 2;
        if (binCount < 2)
            return;

        var hzPerBin = (float)samplingRate / fft.Length;
        var maxHz = Math.Min(MaxHz, samplingRate * 0.45f);

        for (var band = 0; band < bands.Length; band++)
        {
            var hzStart = LogFrequency(band, bands.Length, MinHz, maxHz);
            var hzEnd = LogFrequency(band + 1, bands.Length, MinHz, maxHz);

            var binStart = Math.Clamp((int)(hzStart / hzPerBin), 1, binCount - 1);
            var binEnd = Math.Clamp((int)Math.Ceiling(hzEnd / hzPerBin), binStart + 1, binCount - 1);

            var peak = 0f;
            for (var bin = binStart; bin <= binEnd; bin++)
                peak = MathF.Max(peak, GetBinMagnitude(fft, bin));

            bands[band] = MagnitudeToLevel(peak);
        }
    }

    private static float LogFrequency(int bandIndex, int bandCount, float minHz, float maxHz)
    {
        var t = bandIndex / (float)bandCount;
        var logMin = MathF.Log10(minHz);
        var logMax = MathF.Log10(maxHz);
        return MathF.Pow(10f, logMin + (logMax - logMin) * t);
    }

    private static float GetBinMagnitude(byte[] fft, int bin)
    {
        if (bin <= 0)
            return MathF.Abs((sbyte)fft[0]);

        var nyquistBin = fft.Length / 2;
        if (bin >= nyquistBin)
            return MathF.Abs((sbyte)fft[1]);

        var index = bin * 2;
        if (index + 1 >= fft.Length)
            return 0f;

        var real = (sbyte)fft[index];
        var imag = (sbyte)fft[index + 1];
        return MathF.Sqrt(real * real + imag * imag);
    }

    private static float MagnitudeToLevel(float magnitude)
    {
        var db = 20f * MathF.Log10(MathF.Max(magnitude, 1f) / 128f);
        return Math.Clamp((db - MinDb) / (MaxDb - MinDb), 0f, 1f);
    }

    private static void SmoothBands(float[] source, float[] destination)
    {
        for (var i = 0; i < destination.Length; i++)
        {
            var target = i < source.Length ? source[i] : 0f;
            var attack = i < 7 ? 0.72f : i < 15 ? 0.58f : 0.48f;
            var release = i < 7 ? 0.18f : i < 15 ? 0.14f : 0.11f;
            var coeff = target > destination[i] ? attack : release;
            destination[i] += (target - destination[i]) * coeff;
        }
    }

    private sealed class FftCaptureListener(Action<byte[], int> onFft) : Java.Lang.Object, Visualizer.IOnDataCaptureListener
    {
        public void OnFftDataCapture(Visualizer? visualizer, byte[]? fft, int samplingRate)
        {
            if (fft is { Length: > 2 })
                onFft(fft, samplingRate);
        }

        public void OnWaveFormDataCapture(Visualizer? visualizer, byte[]? waveform, int samplingRate) { }
    }
}
#endif
