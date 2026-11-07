namespace DraftAnalyzer.Core.Models;

public sealed class Team
{
    private readonly League league;

    internal Team(League league, string code, string name, Coach coach)
    {
        this.league = league;
        Code = code;
        Name = name;
        Coach = coach;
    }

    public string Code { get; }
    public string Name { get; }
    public Coach Coach { get; }
    public IReadOnlyDictionary<GameKey, GameRoster> Rosters => league.GetTeamRosters(this);
}
