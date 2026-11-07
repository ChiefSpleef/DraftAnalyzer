namespace DraftAnalyzer.Core.Models;

public sealed record BattleReplay(
    string? Player1,
    string? Player2,
    IReadOnlyList<TurnSnapshot> Turns)
{
    public GameKey? Game { get; init; }
    public GameRoster? Roster1 { get; init; }
    public GameRoster? Roster2 { get; init; }
    public IReadOnlyList<GameEvent> SetupEvents { get; init; } = Array.Empty<GameEvent>();
}
