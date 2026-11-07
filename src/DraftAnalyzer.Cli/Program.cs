using DraftAnalyzer.Core.Models;
using DraftAnalyzer.Core.Parsing;

bool showEvents = args.Length == 2 && args[0] == "--events";
if (args.Length != 1 && !showEvents)
{
    Console.Error.WriteLine("Usage: DraftAnalyzer.Cli [--events] <replay file>");
    return 2;
}

try
{
    using var reader = File.OpenText(args[^1]);
    BattleReplay replay = ReplayParser.Parse(reader);

    Console.WriteLine($"{replay.Player1 ?? "Player 1"} vs. {replay.Player2 ?? "Player 2"}");
    if (showEvents && replay.SetupEvents.Count > 0)
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
        if (showEvents)
            PrintEvents(turn.Events);
    }

    return 0;
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException or FormatException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

static void PrintEvents(IReadOnlyList<GameEvent> events)
{
    foreach (GameEvent gameEvent in events)
    {
        Console.WriteLine($"  {gameEvent.ReplayLine,4} |{gameEvent.Command}|{string.Join('|', gameEvent.Arguments)}");
        if (ReferenceEquals(gameEvent.Before, gameEvent.After))
            continue;

        BattleState state = gameEvent.After;
        var effects = state.FieldEffects.Select(effect =>
            $"{effect.Name}{(effect.Side is null ? "" : $" ({effect.Side})")}" +
            $"{(effect.Layers > 1 ? $" x{effect.Layers}" : "")}");
        Console.WriteLine(
            $"       {Describe(state.P1a)} | {Describe(state.P1b)} | " +
            $"{Describe(state.P2a)} | {Describe(state.P2b)} | Field: [{string.Join(", ", effects)}]");
    }
}

static string Describe(ActivePokemon? pokemon)
{
    if (pokemon is null)
        return "-";

    return pokemon.Nickname == pokemon.Species || pokemon.Nickname.Length == 0
        ? pokemon.Species
        : $"{pokemon.Species} ({pokemon.Nickname})";
}
