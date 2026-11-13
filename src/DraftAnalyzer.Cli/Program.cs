using DraftAnalyzer.Core.Importing;
using DraftAnalyzer.Core.Models;

CliOptions options;
try
{
    options = CliOptions.Parse(args);
}
catch (ArgumentException error)
{
    Console.Error.WriteLine(error.Message);
    Console.Error.WriteLine(
        "Usage: DraftAnalyzer.Cli --season <directory> --league <file> [--transactions <directory>]");
    return 2;
}

try
{
    League league = SeasonImporter.Import(
        options.SeasonDirectory, options.LeagueFile, options.TransactionsDirectory);
    Console.WriteLine(league.Name);
    Console.WriteLine($"{league.Matches.Count} matches, {league.Games.Count} games");
    foreach (BattleMatch match in league.Matches.Values.OrderBy(match => match.Week)
        .ThenBy(match => match.Key.Team1).ThenBy(match => match.Key.Team2))
        Console.WriteLine($"{match.Key}  {match.Wins[match.Key.Team1]}-{match.Wins[match.Key.Team2]}");
    return 0;
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException or FormatException
    or ArgumentException or InvalidOperationException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
