namespace DraftAnalyzer.Core.Models;

public sealed record TurnSnapshot(
    int Number,
    BattleState StartState,
    IReadOnlyList<GameEvent> Events)
{
    public PlayerState? P1a => StartState.P1a;
    public PlayerState? P1b => StartState.P1b;
    public PlayerState? P2a => StartState.P2a;
    public PlayerState? P2b => StartState.P2b;
}
