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
    private const float MinHz = 50f;
    private const float MaxHz = 14_000f;

    private readonly float[] _fftBands = new float[BandCount];
    private readonly float[] _smoothed = new float[BandCount];
    private readonly float[] _publishBuffer = new float[BandCount];
    private Visualizer? _visualizer;
    private int _attachedSessionId = -1;
    private int _silentFrames;
    private float _peakEnvelope = 0.12f;
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
            _visualizer.SetScalingMode(VisualizerScalingMode.Normalized);
            _visualizer.SetDataCaptureListener(
                new AudioCaptureListener(OnFftCaptured),
                CaptureRateMilliHz,
                false,
                true);
            _visualizer.SetEnabled(true);
            _attachedSessionId = targetSession;
            _silentFrames = 0;
            _peakEnvelope = 0.12f;
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
            return range[1];

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
            _peakEnvelope = 0.12f;
            Array.Clear(_smoothed);
            Array.Clear(_fftBands);
        }
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
        var framePeak = 0.001f;
        for (var i = 0; i < BandCount; i++)
            framePeak = MathF.Max(framePeak, _fftBands[i]);

        if (framePeak < 0.01f)
        {
            _silentFrames++;
            if (_silentFrames > 40 && _attachedSessionId != 0)
            {
                TryAttach(0);
                return;
            }

            for (var i = 0; i < BandCount; i++)
                _fftBands[i] *= 0.85f;
        }
        else
        {
            _silentFrames = 0;
        }

        if (framePeak > _peakEnvelope)
            _peakEnvelope += (framePeak - _peakEnvelope) * 0.45f;
        else
            _peakEnvelope += (framePeak - _peakEnvelope) * 0.08f;

        var gain = MathF.Max(_peakEnvelope, 0.06f);

        for (var i = 0; i < BandCount; i++)
        {
            var normalized = Math.Clamp(_fftBands[i] / gain, 0f, 1f);
            normalized = MathF.Pow(normalized, 0.82f);
            _publishBuffer[i] = normalized;
        }

        SmoothBands(_publishBuffer, _smoothed);

        var output = new float[BandCount];
        Array.Copy(_smoothed, output, BandCount);
        BandsUpdated?.Invoke(this, output);
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

            var sum = 0f;
            for (var bin = binStart; bin <= binEnd; bin++)
                sum += GetBinMagnitude(fft, bin);

            bands[band] = sum / (binEnd - binStart + 1);
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
            return MathF.Abs(fft[0] - 128) / 128f;

        var nyquistBin = fft.Length / 2;
        if (bin >= nyquistBin)
            return MathF.Abs(fft[1] - 128) / 128f;

        var index = bin * 2;
        if (index + 1 >= fft.Length)
            return 0f;

        var real = (fft[index] - 128) / 128f;
        var imag = (fft[index + 1] - 128) / 128f;
        return MathF.Sqrt(real * real + imag * imag);
    }

    private static void SmoothBands(float[] source, float[] destination)
    {
        for (var i = 0; i < destination.Length; i++)
        {
            var target = i < source.Length ? source[i] : 0f;
            var attack = i < 7 ? 0.55f : 0.42f;
            var release = i < 7 ? 0.14f : 0.1f;
            var coeff = target > destination[i] ? attack : release;
            destination[i] += (target - destination[i]) * coeff;
        }
    }

    private sealed class AudioCaptureListener(Action<byte[], int> onFft)
        : Java.Lang.Object, Visualizer.IOnDataCaptureListener
    {
        public void OnFftDataCapture(Visualizer? visualizer, byte[]? fft, int samplingRate)
        {
            if (fft is { Length: > 2 })
                onFft(fft, samplingRate);
        }

        public void OnWaveFormDataCapture(Visualizer? visualizer, byte[]? waveform, int samplingRate)
        {
        }
    }
}
#endif
