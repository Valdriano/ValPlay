namespace ValPlay.Services;

public interface IAudioVisualizerService
{
    event EventHandler<float[]>? BandsUpdated;

    void AttachToSession(int audioSessionId);
    void Detach();
}
