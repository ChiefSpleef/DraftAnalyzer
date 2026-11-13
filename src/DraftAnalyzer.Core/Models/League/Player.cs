namespace DraftAnalyzer.Core.Models;

public sealed class Player
{
    private readonly League league;

    internal Player(League league, string species)
    {
        this.league = league;
        Species = species;
    }

    public string Species { get; }
    public IReadOnlyDictionary<GameKey, GameRoster> Rosters => league.GetPlayerRosters(this);

    public Team? GetTeam(GameKey game) => Rosters.GetValueOrDefault(game)?.Team;
    public Team? GetTeam(int week) => league.GetPlayerTeam(Species, week);

    public IReadOnlyDictionary<int, Team?> GetTeams(int firstWeek, int lastWeek) =>
        league.GetPlayerTeams(Species, firstWeek, lastWeek);

    public IReadOnlyList<BattleReplay> GetGames(int week) => GetGames(week, week);

    public IReadOnlyList<BattleReplay> GetGames(int firstWeek, int lastWeek) =>
        Array.AsReadOnly(league.GetGames(firstWeek, lastWeek)
            .Where(game => game.Roster1.Players.Contains(this) || game.Roster2.Players.Contains(this)).ToArray());

    public IReadOnlyList<BattleMatch> GetMatches(int week) => GetMatches(week, week);

    public IReadOnlyList<BattleMatch> GetMatches(int firstWeek, int lastWeek) =>
        Array.AsReadOnly(league.GetMatches(firstWeek, lastWeek)
            .Where(match => match.Games.Any(game =>
                game.Roster1.Players.Contains(this) || game.Roster2.Players.Contains(this))).ToArray());
}
