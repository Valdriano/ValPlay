namespace ValPlay.Models;

public sealed class VisualizationModeOption
{
    public required VisualizationMode Mode { get; init; }
    public required string Label { get; init; }

    public override string ToString() => Label;
}
