namespace ValPlay.Services;

public enum CarAudioFocusChange
{
    Gain,
    Loss,
    LossTransient,
    LossTransientCanDuck
}

public interface ICarAudioService
{
    event EventHandler<CarAudioFocusChange>? FocusChanged;

    bool RequestMediaFocus();
    void AbandonMediaFocus();
}

public interface ICarNotificationService
{
    void ShowPlaybackNotification(string title);
    void HidePlaybackNotification();
}
