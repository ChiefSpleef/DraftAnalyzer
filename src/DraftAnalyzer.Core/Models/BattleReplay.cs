namespace DraftAnalyzer.Core.Models;

public sealed record BattleReplay(
    GameKey Game,
    GameRoster Roster1,
    GameRoster Roster2,
    IReadOnlyList<TurnSnapshot> Turns,
    Team? Winner)
{
    public IReadOnlyList<GameEvent> SetupEvents { get; init; } = Array.Empty<GameEvent>();
}
