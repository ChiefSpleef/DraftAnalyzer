using System.Collections.ObjectModel;

namespace DraftAnalyzer.Core.Models;

public sealed class BattleMatch
{
    internal BattleMatch(IEnumerable<BattleReplay> games)
    {
        BattleReplay[] ordered = games.OrderBy(game => game.Game.Number).ToArray();
        if (ordered.Length is < 2 or > 3 ||
            !ordered.Select(game => game.Game.Number).SequenceEqual(Enumerable.Range(1, ordered.Length)))
            throw new FormatException("A complete best-of-three match requires games 1 and 2, and game 3 if needed.");
        Key = ordered[0].Game.Match;
        var wins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [Key.Team1] = 0,
            [Key.Team2] = 0
        };
        foreach (BattleReplay game in ordered)
        {
            if (game.Game.Match != Key)
                throw new FormatException("A match folder contains games from different matchups.");
            if (wins.Values.Contains(2))
                throw new FormatException("A replay occurs after the match was already won.");

            if (game.Winner is { } winner)
            {
                if (!wins.ContainsKey(winner.Code))
                    throw new FormatException("A game's winner is not a participant in the match.");
                wins[winner.Code]++;
            }
        }

        Games = Array.AsReadOnly(ordered);
        Wins = new ReadOnlyDictionary<string, int>(wins);
        WinnerCode = wins.FirstOrDefault(entry => entry.Value == 2).Key
            ?? throw new FormatException("A complete match requires a team with two wins.");
    }

    public MatchKey Key { get; }
    public int Week => Key.Week;
    public IReadOnlyList<BattleReplay> Games { get; }
    public IReadOnlyDictionary<string, int> Wins { get; }
    public string WinnerCode { get; }
}
