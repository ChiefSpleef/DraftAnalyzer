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
    public IReadOnlyDictionary<MatchKey, BattleMatch> Matches => league.Matches
        .Where(entry => entry.Key.Team1.Equals(Code, StringComparison.OrdinalIgnoreCase) ||
                        entry.Key.Team2.Equals(Code, StringComparison.OrdinalIgnoreCase))
        .ToDictionary(entry => entry.Key, entry => entry.Value);

    public IReadOnlyList<Player> GetRoster(int week) => league.GetRoster(Code, week);

    public IReadOnlyDictionary<int, IReadOnlyList<Player>> GetRosters(int firstWeek, int lastWeek) =>
        league.GetRosters(Code, firstWeek, lastWeek);

    public IReadOnlyList<BattleReplay> GetGames(int week) => GetGames(week, week);

    public IReadOnlyList<BattleReplay> GetGames(int firstWeek, int lastWeek) =>
        Array.AsReadOnly(league.GetGames(firstWeek, lastWeek)
            .Where(game => game.Roster1.Team == this || game.Roster2.Team == this).ToArray());

    public IReadOnlyList<BattleMatch> GetMatches(int week) => GetMatches(week, week);

    public IReadOnlyList<BattleMatch> GetMatches(int firstWeek, int lastWeek) =>
        Array.AsReadOnly(league.GetMatches(firstWeek, lastWeek)
            .Where(match => match.Key.Team1.Equals(Code, StringComparison.OrdinalIgnoreCase) ||
                            match.Key.Team2.Equals(Code, StringComparison.OrdinalIgnoreCase)).ToArray());
}
