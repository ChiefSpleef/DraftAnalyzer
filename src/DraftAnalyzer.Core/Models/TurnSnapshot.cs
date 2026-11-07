namespace DraftAnalyzer.Core.Models;

public sealed record TurnSnapshot(
    int Number,
    BattleState StartState,
    IReadOnlyList<GameEvent> Events)
{
    public ActivePokemon? P1a => StartState.P1a;
    public ActivePokemon? P1b => StartState.P1b;
    public ActivePokemon? P2a => StartState.P2a;
    public ActivePokemon? P2b => StartState.P2b;
}
