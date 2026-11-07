namespace DraftAnalyzer.Core.Models;

public sealed record BattleState(
    PlayerState? P1a,
    PlayerState? P1b,
    PlayerState? P2a,
    PlayerState? P2b,
    IReadOnlyList<FieldEffect> FieldEffects);
