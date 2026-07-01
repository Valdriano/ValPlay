#if ANDROID
using Android.Media.Audiofx;
using Android.Util;
using ValPlay.Services;

namespace ValPlay.Platforms.Android;

public sealed class AndroidAudioVisualizerService : IAudioVisualizerService
{
    private const string Tag = "ValPlayVisualizer";
    private const int BandCount = 22;
    private const int CaptureRateMilliHz = 20_000;
    private const float MinDb = -60f;
    private const float MaxDb = 0f;
    private const float MinHz = 50f;
    private const float MaxHz = 14_000f;

    private readonly float[] _fftBands = new float[BandCount];
    private readonly float[] _waveBands = new float[BandCount];
    private readonly float[] _combined = new float[BandCount];
    private readonly float[] _smoothed = new float[BandCount];
    private Visualizer? _visualizer;
    private int _attachedSessionId = -1;
    private int _silentFrames;
    private readonly SemaphoreSlim _attachLock = new(1, 1);
    private int _pendingSessionId = -1;

    public event EventHandler<float[]>? BandsUpdated;

    public void AttachToSession(int audioSessionId)
    {
        _pendingSessionId = audioSessionId;

        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() => AttachToSession(audioSessionId));
            return;
        }

        _ = AttachInternalAsync();
    }

    private async Task AttachInternalAsync()
    {
        var audioSessionId = _pendingSessionId;
        await _attachLock.WaitAsync();
        try
        {
            if (!await AndroidVisualizerPermissionHelper.EnsureGrantedAsync())
            {
                Log.Warn(Tag, "RECORD_AUDIO not granted; visualizer disabled.");
                return;
            }

            var playerSession = audioSessionId > 0 ? audioSessionId : 0;
            if (_visualizer is not null && _attachedSessionId == playerSession)
                return;

            TryAttach(playerSession);

            if (_visualizer is null && playerSession != 0)
                TryAttach(0);
        }
        finally
        {
            _attachLock.Release();
        }
    }

    private void TryAttach(int targetSession)
    {
        Detach();

        try
        {
            var captureSize = GetPreferredCaptureSize();
            _visualizer = new Visualizer(targetSession);
            _visualizer.SetCaptureSize(captureSize);
            _visualizer.SetDataCaptureListener(
                new AudioCaptureListener(OnWaveformCaptured, OnFftCaptured),
                CaptureRateMilliHz,
                true,
                true);
            _visualizer.SetEnabled(true);
            _attachedSessionId = targetSession;
            _silentFrames = 0;
            Log.Debug(Tag, $"Visualizer attached to session {targetSession}, captureSize={captureSize}.");
        }
        catch (Exception ex)
        {
            Log.Error(Tag, $"Visualizer attach failed for session {targetSession}: {ex.Message}");
            Detach();
        }
    }

    private static int GetPreferredCaptureSize()
    {
        var range = Visualizer.GetCaptureSizeRange();
        if (range is { Length: >= 2 })
        {
            for (var size = range[1]; size >= range[0]; size -= 128)
            {
                if (size is 512 or 1024 or 2048 or 4096)
                    return size;
            }

            return range[1];
        }

        return 1024;
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
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Visualizer detach: {ex.Message}");
        }
        finally
        {
            _visualizer = null;
            _attachedSessionId = -1;
            _silentFrames = 0;
            Array.Clear(_smoothed);
            Array.Clear(_fftBands);
            Array.Clear(_waveBands);
        }
    }

    private void OnWaveformCaptured(byte[] waveform)
    {
        ParseWaveformBands(waveform, _waveBands);
        PublishBands();
    }

    private void OnFftCaptured(byte[] fft, int samplingRate)
    {
        if (samplingRate <= 0)
            samplingRate = 44_100;

        MapFrequencyBands(fft, samplingRate, _fftBands);
        PublishBands();
    }

    private void PublishBands()
    {
        var peak = 0f;
        for (var i = 0; i < BandCount; i++)
        {
            _combined[i] = MathF.Max(_fftBands[i], _waveBands[i]);
            peak = MathF.Max(peak, _combined[i]);
        }

        if (peak < 0.02f)
        {
            _silentFrames++;
            if (_silentFrames > 40 && _attachedSessionId != 0)
            {
                TryAttach(0);
                return;
            }
        }
        else
        {
            _silentFrames = 0;
        }

        SmoothBands(_combined, _smoothed);
        BandsUpdated?.Invoke(this, _smoothed);
    }

    private static void ParseWaveformBands(byte[] waveform, float[] bands)
    {
        if (waveform.Length < bands.Length)
            return;

        for (var band = 0; band < bands.Length; band++)
        {
            var t0 = band / (float)bands.Length;
            var t1 = (band + 1) / (float)bands.Length;
            var start = (int)(waveform.Length * t0 * t0);
            var end = (int)(waveform.Length * t1 * t1);
            if (end <= start)
                end = Math.Min(waveform.Length, start + 1);

            var sum = 0f;
            for (var i = start; i < end; i++)
            {
                var sample = (sbyte)waveform[i] / 128f;
                sum += sample * sample;
            }

            var rms = MathF.Sqrt(sum / Math.Max(1, end - start));
            bands[band] = Math.Clamp(rms * 3.2f, 0f, 1f);
        }
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

        var realSigned = (sbyte)fft[index];
        var imagSigned = (sbyte)fft[index + 1];
        var signedMag = MathF.Sqrt(realSigned * realSigned + imagSigned * imagSigned);

        var realUnsigned = fft[index] - 128;
        var imagUnsigned = fft[index + 1] - 128;
        var unsignedMag = MathF.Sqrt(realUnsigned * realUnsigned + imagUnsigned * imagUnsigned);

        return MathF.Max(signedMag, unsignedMag);
    }

    private static float MagnitudeToLevel(float magnitude)
    {
        var db = 20f * MathF.Log10(MathF.Max(magnitude, 0.5f) / 64f);
        return Math.Clamp((db - MinDb) / (MaxDb - MinDb), 0f, 1f);
    }

    private static void SmoothBands(float[] source, float[] destination)
    {
        for (var i = 0; i < destination.Length; i++)
        {
            var target = i < source.Length ? source[i] : 0f;
            var attack = i < 7 ? 0.62f : 0.5f;
            var release = i < 7 ? 0.16f : 0.12f;
            var coeff = target > destination[i] ? attack : release;
            destination[i] += (target - destination[i]) * coeff;
        }
    }

    private sealed class AudioCaptureListener(
        Action<byte[]> onWaveform,
        Action<byte[], int> onFft) : Java.Lang.Object, Visualizer.IOnDataCaptureListener
    {
        public void OnFftDataCapture(Visualizer? visualizer, byte[]? fft, int samplingRate)
        {
            if (fft is { Length: > 2 })
                onFft(fft, samplingRate);
        }

        public void OnWaveFormDataCapture(Visualizer? visualizer, byte[]? waveform, int samplingRate)
        {
            if (waveform is { Length: > 0 })
                onWaveform(waveform);
        }
    }
}
#endif
