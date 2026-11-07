namespace DraftAnalyzer.Core.Metadata;

public sealed class CoachInfo
{
    public required string Name { get; init; }
    public List<string>? ReplayNames { get; init; }
}
