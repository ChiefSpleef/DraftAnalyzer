using System.Globalization;
using System.Text.RegularExpressions;
using DraftAnalyzer.Core.Metadata;
using DraftAnalyzer.Core.Models;
using DraftAnalyzer.Core.Parsing;

namespace DraftAnalyzer.Core.Importing;

public static class SeasonImporter
{
    public static League Import(string replayDirectory, string leagueFile, string? transactionsDirectory = null)
    {
        League league = LeagueLoader.Load(leagueFile);
        var weeks = new SortedDictionary<int, string>();
        foreach (string folder in Directory.EnumerateDirectories(replayDirectory))
        {
            var weekMatch = Regex.Match(Path.GetFileName(folder), @"^w(\d+)$", RegexOptions.IgnoreCase);
            if (!weekMatch.Success)
                continue;
            int week = int.Parse(weekMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            if (week < 1 || !weeks.TryAdd(week, folder))
                throw new FormatException($"Invalid or repeated week folder '{folder}'.");
        }
        if (weeks.Count == 0)
            throw new FormatException("No w1, w2, ... week folders found.");

        var transactions = new SortedDictionary<int, WeeklyTransactions>();
        if (transactionsDirectory is not null)
        {
            foreach (string file in Directory.EnumerateFiles(transactionsDirectory, "*.json"))
            {
                using var reader = File.OpenText(file);
                WeeklyTransactions batch = LeagueLoader.ReadTransactions(reader);
                if (!transactions.TryAdd(batch.Week, batch))
                    throw new FormatException($"Repeated transaction week {batch.Week}.");
            }
        }

        foreach (int week in weeks.Keys.Union(transactions.Keys).Order())
        {
            if (transactions.TryGetValue(week, out WeeklyTransactions? batch))
            {
                try
                {
                    league.ApplyTransactions(batch);
                }
                catch (Exception error) when (IsImportError(error))
                {
                    throw new FormatException($"Week {week} transactions: {error.Message}", error);
                }
            }
            if (!weeks.TryGetValue(week, out string? weekFolder))
                continue;

            foreach (string matchFolder in Directory.EnumerateDirectories(weekFolder).Order(StringComparer.Ordinal))
            {
                try
                {
                    var games = new List<BattleReplay>();
                    foreach (string file in Directory.EnumerateFiles(matchFolder)
                        .Where(file => Path.GetExtension(file).ToLowerInvariant() is ".html" or ".log" or ".txt"))
                    {
                        using var reader = File.OpenText(file);
                        try
                        {
                            games.Add(ReplayParser.Parse(reader, league, week));
                        }
                        catch (Exception error) when (IsImportError(error))
                        {
                            throw new FormatException($"{Path.GetFileName(file)}: {error.Message}", error);
                        }
                    }

                    league.RegisterMatch(new BattleMatch(games));
                }
                catch (Exception error) when (IsImportError(error))
                {
                    throw new FormatException($"{matchFolder}: {error.Message}", error);
                }
            }
        }

        return league;
    }

    private static bool IsImportError(Exception error) => error is
        FormatException or InvalidOperationException or ArgumentException or IOException or UnauthorizedAccessException;
}
