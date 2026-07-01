namespace ValPlay.Models;

public sealed class AppSettings
{
    public bool ShuffleEnabled { get; set; }
    public RepeatMode RepeatMode { get; set; } = RepeatMode.Off;
    public string? LastMediaPath { get; set; }
    public double LastPositionSeconds { get; set; }
    public bool ResumePlaybackOnStart { get; set; } = true;
    public string? LastScanRootPath { get; set; }
    public bool ShowVideoOnly { get; set; }
    public bool ShowAudioOnly { get; set; }
    public string Language { get; set; } = "pt";
}
