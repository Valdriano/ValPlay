namespace ValPlay.Models;

public sealed class LanguageOption
{
    public required string Code { get; init; }
    public required string DisplayName { get; init; }

    public override string ToString() => DisplayName;

    public override bool Equals(object? obj) =>
        obj is LanguageOption other && Code.Equals(other.Code, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => Code.GetHashCode(StringComparison.OrdinalIgnoreCase);
}
