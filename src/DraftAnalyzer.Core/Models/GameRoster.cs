namespace DraftAnalyzer.Core.Models;

public sealed class GameRoster
{
    internal GameRoster(GameKey game, Team team, IEnumerable<Player> players)
    {
        Game = game;
        Team = team;
        Players = Array.AsReadOnly(players.ToArray());
    }

    public GameKey Game { get; }
    public Team Team { get; }
    public IReadOnlyList<Player> Players { get; }
}
