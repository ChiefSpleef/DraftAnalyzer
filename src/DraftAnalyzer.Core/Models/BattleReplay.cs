namespace DraftAnalyzer.Core.Models;

public sealed record BattleReplay(
    string? Player1,
    string? Player2,
    IReadOnlyList<TurnSnapshot> Turns)
{
    public IReadOnlyList<GameEvent> SetupEvents { get; init; } = Array.Empty<GameEvent>();
}
