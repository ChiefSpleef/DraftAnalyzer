namespace DraftAnalyzer.Core.Models;

public sealed record GameKey
{
    public GameKey(int week, string team1, string team2, int number)
    {
        if (number < 1)
            throw new ArgumentException("Game number must be positive.");
        Match = new MatchKey(week, team1, team2);
        Number = number;
    }

    public MatchKey Match { get; }
    public int Week => Match.Week;
    public string Team1 => Match.Team1;
    public string Team2 => Match.Team2;
    public int Number { get; }
    public override string ToString() => $"W{Week}/{Team1}-{Team2}/G{Number}";
}
