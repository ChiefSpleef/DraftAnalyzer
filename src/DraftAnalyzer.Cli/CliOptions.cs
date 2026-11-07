using System.Globalization;

internal sealed class CliOptions
{
    public required string ReplayFile { get; init; }
    public bool ShowEvents { get; init; }
    public string? LeagueFile { get; init; }
    public string? TransactionsDirectory { get; init; }
    public int Week { get; init; }
    public int GameNumber { get; init; } = 1;

    public static CliOptions Parse(string[] args)
    {
        string? replay = null;
        string? league = null;
        string? transactions = null;
        bool events = false;
        int week = 0;
        int game = 1;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--events":
                    events = true;
                    break;
                case "--league":
                    league = Value();
                    break;
                case "--transactions":
                    transactions = Value();
                    break;
                case "--week":
                    week = PositiveNumber();
                    break;
                case "--game":
                    game = PositiveNumber();
                    break;
                default:
                    if (args[i].StartsWith("--", StringComparison.Ordinal) || replay is not null)
                        throw new ArgumentException($"Unexpected argument '{args[i]}'.");
                    replay = args[i];
                    break;
            }

            string Value()
            {
                if (++i >= args.Length)
                    throw new ArgumentException("Missing option value.");
                return args[i];
            }

            int PositiveNumber()
            {
                if (!int.TryParse(Value(), NumberStyles.None, CultureInfo.InvariantCulture, out int number) || number < 1)
                    throw new ArgumentException("Week and game number must be positive integers.");
                return number;
            }
        }

        if (replay is null || (league is not null && week == 0) ||
            (league is null && (transactions is not null || week != 0 || game != 1)))
            throw new ArgumentException("Provide a replay path. League mode also requires --league and --week.");

        return new CliOptions
        {
            ReplayFile = replay,
            LeagueFile = league,
            TransactionsDirectory = transactions,
            ShowEvents = events,
            Week = week,
            GameNumber = game
        };
    }
}
