using System.Text.Json;
using System.Text.Json.Serialization;
using DraftAnalyzer.Core.Models;

namespace DraftAnalyzer.Core.Metadata;

public static class LeagueLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static League Load(string leagueFile, string? transactionsDirectory = null)
    {
        using var reader = File.OpenText(leagueFile);
        League league = Load(reader);
        if (transactionsDirectory is not null)
        {
            var batches = new List<WeeklyTransactions>();
            foreach (string path in Directory.EnumerateFiles(transactionsDirectory, "*.json"))
            {
                using var transactionReader = File.OpenText(path);
                batches.Add(ReadTransactions(transactionReader));
            }

            foreach (WeeklyTransactions batch in batches.OrderBy(batch => batch.Week))
                league.ApplyTransactions(batch);
        }

        return league;
    }

    public static League Load(TextReader reader) => new(Read<LeagueInfo>(reader));

    public static WeeklyTransactions ReadTransactions(TextReader reader) => Read<WeeklyTransactions>(reader);

    private static T Read<T>(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        try
        {
            return JsonSerializer.Deserialize<T>(reader.ReadToEnd(), Options)
                ?? throw new FormatException("The metadata file cannot be null.");
        }
        catch (JsonException error)
        {
            throw new FormatException($"Invalid metadata. {error.Message}", error);
        }
    }
}
