using ValPlay.Models;

namespace ValPlay.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Save();
    void Update(Action<AppSettings> update);
}
