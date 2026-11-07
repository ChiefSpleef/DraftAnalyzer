namespace DraftAnalyzer.Core.Metadata;

public sealed class LeagueInfo
{
    public required string Name { get; init; }
    public required List<TeamInfo> Teams { get; init; }
    public Dictionary<string, string> Aliases { get; init; } = new();
}
