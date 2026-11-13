namespace DraftAnalyzer.Core.Models;

public sealed record MatchKey
{
    public MatchKey(int week, string team1, string team2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(team1);
        ArgumentException.ThrowIfNullOrWhiteSpace(team2);
        if (week < 1)
            throw new ArgumentException("Week must be positive.");
        if (team1.Equals(team2, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A match requires two different teams.");

        Week = week;
        string[] codes = [team1.ToUpperInvariant(), team2.ToUpperInvariant()];
        Array.Sort(codes, StringComparer.Ordinal);
        Team1 = codes[0];
        Team2 = codes[1];
    }

    public int Week { get; }
    public string Team1 { get; }
    public string Team2 { get; }
    public override string ToString() => $"W{Week}/{Team1}-{Team2}";
}
