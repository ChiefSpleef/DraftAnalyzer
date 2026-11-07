namespace DraftAnalyzer.Core.Models;

public sealed record GameKey
{
    public GameKey(int week, string team1, string team2, int number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(team1);
        ArgumentException.ThrowIfNullOrWhiteSpace(team2);
        if (week < 1 || number < 1)
            throw new ArgumentException("Week and game number must be positive.");
        if (team1.Equals(team2, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A game requires two different teams.");

        Week = week;
        Number = number;
        // Replay sides may reverse between games without changing the matchup identity.
        string[] codes = [team1.ToUpperInvariant(), team2.ToUpperInvariant()];
        Array.Sort(codes, StringComparer.Ordinal);
        Team1 = codes[0];
        Team2 = codes[1];
    }

    public int Week { get; }
    public string Team1 { get; }
    public string Team2 { get; }
    public int Number { get; }
    public override string ToString() => $"W{Week}/{Team1}-{Team2}/G{Number}";
}
