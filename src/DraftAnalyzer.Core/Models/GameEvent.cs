namespace DraftAnalyzer.Core.Models;

public sealed record GameEvent(
    string Command,
    IReadOnlyList<string> Arguments,
    int ReplayLine,
    BattleState Before,
    BattleState After);
