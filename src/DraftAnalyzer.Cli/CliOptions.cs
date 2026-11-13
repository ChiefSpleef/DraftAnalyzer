internal sealed record CliOptions(
    string SeasonDirectory, string LeagueFile, string? TransactionsDirectory)
{
    public static CliOptions Parse(string[] args)
    {
        string? season = null;
        string? league = null;
        string? transactions = null;
        for (int i = 0; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length)
                throw new ArgumentException($"Missing value for '{args[i]}'.");

            string value = args[i + 1];
            switch (args[i])
            {
                case "--season":
                    season = value;
                    break;
                case "--league":
                    league = value;
                    break;
                case "--transactions":
                    transactions = value;
                    break;
                default:
                    throw new ArgumentException($"Unexpected option '{args[i]}'.");
            }
        }

        if (season is null || league is null)
            throw new ArgumentException("Provide --season and --league.");

        return new CliOptions(season, league, transactions);
    }
}
