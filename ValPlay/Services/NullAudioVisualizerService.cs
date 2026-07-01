namespace ValPlay.Services;

public sealed class NullAudioVisualizerService : IAudioVisualizerService
{
    public event EventHandler<float[]>? BandsUpdated;

    public void AttachToSession(int audioSessionId) { }

    public void Detach() { }
}
