namespace DraftAnalyzer.Core.Metadata;

public sealed class TeamInfo
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required CoachInfo Coach { get; init; }
    public required List<string> Roster { get; init; }
}
