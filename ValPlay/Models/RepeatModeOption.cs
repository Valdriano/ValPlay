using ValPlay.Services;

namespace ValPlay.Models;

public sealed class RepeatModeOption
{
    public required RepeatMode Mode { get; init; }
    public required string Label { get; init; }

    public override string ToString() => Label;
}
