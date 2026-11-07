namespace DraftAnalyzer.Core.Models;

public sealed record BattleState(
    ActivePokemon? P1a,
    ActivePokemon? P1b,
    ActivePokemon? P2a,
    ActivePokemon? P2b,
    IReadOnlyList<FieldEffect> FieldEffects);
