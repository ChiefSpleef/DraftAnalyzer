namespace DraftAnalyzer.Core.Models;

public sealed record LeagueWeek(
    int Number,
    IReadOnlyDictionary<string, IReadOnlyList<Player>> Rosters,
    IReadOnlyList<BattleReplay> Games,
    IReadOnlyList<BattleMatch> Matches);
