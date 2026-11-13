using System.Globalization;
using System.Text.RegularExpressions;
using DraftAnalyzer.Core.Models;

namespace DraftAnalyzer.Core.Parsing;

internal static class ReplayParser
{
    internal static BattleReplay Parse(TextReader reader, League league, int week)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var turns = new List<TurnSnapshot>();
        var setup = new List<GameEvent>();
        var events = setup;
        var board = new PlayerState?[4];
        var state = new BattleState(null, null, null, null, Array.Empty<FieldEffect>());
        BattleState turnStart = state;
        int? turnNumber = null;
        string? player1 = null;
        string? player2 = null;
        var preview = new List<(string Side, string Species)>();
        GameKey? game = null;
        GameRoster? roster1 = null;
        GameRoster? roster2 = null;
        int? replayGameNumber = null;
        Team? winner = null;
        bool finished = false;
        bool started = false;
        int lineNumber = 0;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (!line.StartsWith('|'))
                continue;

            string[] fields = line.Split('|');
            string command = fields[1];
            if (!started)
            {
                switch (command)
                {
                    case "gametype":
                        if (Field(2) is not ("singles" or "doubles"))
                            throw Error("Expected a singles or doubles replay.");
                        break;
                    case "player":
                        if (Field(2) == "p1")
                            player1 = Field(3);
                        else if (Field(2) == "p2")
                            player2 = Field(3);
                        break;
                    case "uhtml":
                    case "uhtmlchange":
                        if (fields.Length > 3 && fields[2] == "bestof")
                        {
                            var numberMatch = Regex.Match(fields[3], @"<strong>Game\s+(\d+)");
                            if (numberMatch.Success)
                                replayGameNumber = int.Parse(numberMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                        }
                        break;
                    case "clearpoke":
                        preview.Clear();
                        break;
                    case "poke":
                        preview.Add((Field(2), ReadSpecies()));
                        break;
                    case "start":
                        Team team1 = league.GetTeamByReplayName(player1 ?? throw Error("Missing p1 coach name."));
                        Team team2 = league.GetTeamByReplayName(player2 ?? throw Error("Missing p2 coach name."));
                        game = new GameKey(week, team1.Code, team2.Code,
                            replayGameNumber ?? throw Error("Missing best-of-three game number."));
                        roster1 = league.CreateGameRoster(game, team1);
                        roster2 = league.CreateGameRoster(game, team2);
                        foreach (var entry in preview)
                        {
                            GameRoster roster = entry.Side switch
                            {
                                "p1" => roster1,
                                "p2" => roster2,
                                _ => throw Error($"Unknown preview side '{entry.Side}'.")
                            };
                            league.ResolvePlayer(roster, entry.Species);
                        }
                        started = true;
                        break;
                }

                continue;
            }

            if (command == "turn")
            {
                if (!int.TryParse(Field(2), NumberStyles.None, CultureInfo.InvariantCulture,
                        out int number) || number <= 0)
                    throw Error("Invalid turn number.");

                FinishTurn();
                turnNumber = number;
                turnStart = state;
                events = new List<GameEvent>();
                continue;
            }

            if (!IsBattleEvent(command))
                continue;

            string[] arguments = fields[2..];
            var effects = state.FieldEffects;
            switch (command)
            {
                case "switch":
                case "drag":
                case "replace":
                    string pokemon = Field(2);
                    int slot = GetSlot(pokemon);
                    string nickname = pokemon[(pokemon.IndexOf(':') + 1)..].Trim();
                    string species = ReadSpecies();
                    board[slot] = new PlayerState(
                        league.ResolvePlayer(slot < 2 ? roster1! : roster2!, species),
                        nickname, species, ReadHp(Field(4)));
                    break;
                case "-damage":
                case "-heal":
                case "-sethp":
                    ApplyHp(2);
                    // Some logs include a second Pokémon/HP pair on the same sethp event.
                    if (command == "-sethp" && fields.Length > 4 &&
                        fields[4].Length > 0 && !fields[4].StartsWith('['))
                    {
                        ApplyHp(4);
                    }
                    break;
                case "faint":
                    board[GetSlot(Field(2))] = null;
                    break;
                case "swap":
                    int from = GetSlot(Field(2));
                    int position = Field(3) switch
                    {
                        "0" => 0,
                        "1" => 1,
                        _ => throw Error("Expected swap position 0 or 1.")
                    };
                    int to = (from / 2 * 2) + position;
                    (board[from], board[to]) = (board[to], board[from]);
                    break;
                case "detailschange":
                case "-formechange":
                    string changed = Field(2);
                    if (IsBenched(changed))
                        break; // The replay can report a form change for a benched Pokémon.

                    int changedSlot = GetSlot(changed);
                    if (board[changedSlot] is { } active)
                        board[changedSlot] = active with { Species = ReadSpecies() };
                    break;
                case "-weather":
                case "-fieldstart":
                case "-fieldend":
                    _ = Field(2);
                    effects = FieldEffectTracker.Apply(effects, command, arguments);
                    break;
                case "-sidestart":
                case "-sideend":
                    _ = Field(2);
                    _ = Field(3);
                    effects = FieldEffectTracker.Apply(effects, command, arguments);
                    break;
                case "-swapsideconditions":
                    effects = FieldEffectTracker.Apply(effects, command, arguments);
                    break;
            }

            BattleState before = state;
            if (state.P1a != board[0] || state.P1b != board[1] ||
                state.P2a != board[2] || state.P2b != board[3] || !ReferenceEquals(state.FieldEffects, effects))
            {
                state = new BattleState(board[0], board[1], board[2], board[3], effects);
            }

            events.Add(new GameEvent(command, Array.AsReadOnly(arguments), lineNumber, before, state));
            if (command is "win" or "tie")
            {
                if (command == "win")
                {
                    string name = Field(2);
                    winner = name.Equals(player1, StringComparison.OrdinalIgnoreCase) ? roster1!.Team :
                        name.Equals(player2, StringComparison.OrdinalIgnoreCase) ? roster2!.Team :
                        throw Error($"Winner '{name}' is not a replay participant.");
                }
                finished = true;
                break;
            }

            string Field(int index)
            {
                if (index >= fields.Length || fields[index].Length == 0)
                    throw Error($"Missing field {index} for '{command}'.");

                return fields[index];
            }

            string ReadSpecies() => Field(3).Split(',', 2)[0].Trim();

            void ApplyHp(int targetIndex)
            {
                string target = Field(targetIndex);
                if (IsBenched(target))
                    return;

                int targetSlot = GetSlot(target);
                if (board[targetSlot] is { } active)
                    board[targetSlot] = active with { Hp = ReadHp(Field(targetIndex + 1)) };
            }
        }

        FinishTurn();
        if (turns.Count == 0)
            throw new FormatException("No turn markers found after the battle start.");
        if (!finished)
            throw Error("The replay has no final result.");

        return new BattleReplay(game!, roster1!, roster2!, turns.AsReadOnly(), winner)
        {
            SetupEvents = setup.AsReadOnly()
        };

        void FinishTurn()
        {
            if (turnNumber is int number)
                turns.Add(new TurnSnapshot(number, turnStart, events.AsReadOnly()));
        }

        FormatException Error(string message) => new($"Replay line {lineNumber}. {message}");

        double ReadHp(string condition)
        {
            try
            {
                return HpPercentageParser.Parse(condition);
            }
            catch (FormatException error)
            {
                throw Error(error.Message);
            }
        }

        int GetSlot(string pokemon)
        {
            int separator = pokemon.IndexOf(':');
            return separator == 3 ? pokemon[..separator] switch
            {
                "p1a" => 0,
                "p1b" => 1,
                "p2a" => 2,
                "p2b" => 3,
                _ => throw Error($"Unknown active slot in '{pokemon}'.")
            } : throw Error($"Invalid active Pokémon field '{pokemon}'.");
        }
    }

    private static bool IsBenched(string pokemon) =>
        pokemon.StartsWith("p1:", StringComparison.Ordinal) ||
        pokemon.StartsWith("p2:", StringComparison.Ordinal);

    private static bool IsBattleEvent(string command) =>
        command.StartsWith('-') ||
        command is "switch" or "drag" or "replace" or "faint" or "swap" or "detailschange"
            or "move" or "cant" or "upkeep" or "win" or "tie";
}
