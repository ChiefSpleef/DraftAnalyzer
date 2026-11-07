using DraftAnalyzer.Core.Metadata;
using DraftAnalyzer.Core.Models;
using DraftAnalyzer.Core.Parsing;

CliOptions options;
try
{
    options = CliOptions.Parse(args);
}
catch (ArgumentException error)
{
    Console.Error.WriteLine(error.Message);
    Console.Error.WriteLine(
        "Usage: DraftAnalyzer.Cli [--events] [--league <file> --week <number> " +
        "[--transactions <directory>] [--game <number>]] <replay file>");
    return 2;
}

try
{
    using var reader = File.OpenText(options.ReplayFile);
    BattleReplay replay = options.LeagueFile is null
        ? ReplayParser.Parse(reader)
        : ReplayParser.Parse(reader,
            LeagueLoader.Load(options.LeagueFile, options.TransactionsDirectory),
            options.Week, options.GameNumber);

    Console.WriteLine($"{replay.Player1 ?? "Player 1"} vs. {replay.Player2 ?? "Player 2"}");
    if (replay.Game is not null)
        Console.WriteLine(replay.Game);
    PrintRoster("p1", replay.Roster1);
    PrintRoster("p2", replay.Roster2);
    if (options.ShowEvents && replay.SetupEvents.Count > 0)
    {
        Console.WriteLine("Before turn 1");
        PrintEvents(replay.SetupEvents);
    }

    Console.WriteLine("Turn | p1a | p1b | p2a | p2b");
    foreach (TurnSnapshot turn in replay.Turns)
    {
        Console.WriteLine(
            $"{turn.Number} | {Describe(turn.P1a)} | {Describe(turn.P1b)} | " +
            $"{Describe(turn.P2a)} | {Describe(turn.P2b)}");
        if (options.ShowEvents)
            PrintEvents(turn.Events);
    }

    return 0;
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException or FormatException
    or ArgumentException or InvalidOperationException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

static void PrintRoster(string side, GameRoster? roster)
{
    if (roster is not null)
        Console.WriteLine(
            $"{side} {roster.Team.Code} / {roster.Team.Name} / Coach {roster.Team.Coach.Name}: " +
            string.Join(", ", roster.Players.Select(player => player.Species)));
}

static void PrintEvents(IReadOnlyList<GameEvent> events)
{
    foreach (GameEvent gameEvent in events)
    {
        Console.WriteLine($"  {gameEvent.ReplayLine,4} |{gameEvent.Command}|{string.Join('|', gameEvent.Arguments)}");
        if (ReferenceEquals(gameEvent.Before, gameEvent.After))
            continue;

        BattleState state = gameEvent.After;
        if (gameEvent.Command is "-damage" or "-heal" or "-sethp")
        {
            BattleState before = gameEvent.Before;
            PrintHpChange("p1a", before.P1a?.Hp, state.P1a?.Hp);
            PrintHpChange("p1b", before.P1b?.Hp, state.P1b?.Hp);
            PrintHpChange("p2a", before.P2a?.Hp, state.P2a?.Hp);
            PrintHpChange("p2b", before.P2b?.Hp, state.P2b?.Hp);
            continue;
        }

        var effects = state.FieldEffects.Select(effect =>
            $"{effect.Name}{(effect.Side is null ? "" : $" ({effect.Side})")}" +
            $"{(effect.Layers > 1 ? $" x{effect.Layers}" : "")}");
        Console.WriteLine(
            $"       {Describe(state.P1a)} | {Describe(state.P1b)} | " +
            $"{Describe(state.P2a)} | {Describe(state.P2b)} | Field: [{string.Join(", ", effects)}]");
    }
}

static void PrintHpChange(string slot, double? before, double? after)
{
    if (before != after)
        Console.WriteLine($"       {slot} HP: {DescribeHp(before)} -> {DescribeHp(after)}");
}

static string DescribeHp(double? hp) => hp is double value ? $"{value:0.##}%" : "?";

static string Describe(PlayerState? pokemon)
{
    if (pokemon is null)
        return "-";

    string name = pokemon.Nickname == pokemon.Species || pokemon.Nickname.Length == 0
        ? pokemon.Species
        : $"{pokemon.Species} ({pokemon.Nickname})";
    return pokemon.Hp is null ? name : $"{name} [{DescribeHp(pokemon.Hp)}]";
}
