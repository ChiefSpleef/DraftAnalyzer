namespace DraftAnalyzer.Core.Metadata;

public sealed class RosterTransaction
{
    public required string Player { get; init; }
    public string? FromTeam { get; init; }
    public string? ToTeam { get; init; }
}
