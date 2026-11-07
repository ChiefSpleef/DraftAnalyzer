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
}
