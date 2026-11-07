using System.Collections.ObjectModel;
using DraftAnalyzer.Core.Metadata;

namespace DraftAnalyzer.Core.Models;

public sealed class League
{
    private readonly object syncRoot = new();
    private readonly Dictionary<string, Team> teams = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Team> replayTeams = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Player> players = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase);
    private readonly SortedDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Player>>> weeklyRosters = new();
    private readonly Dictionary<GameKey, BattleReplay> games = new();

    internal League(LeagueInfo info)
    {
        Name = Text(info.Name, "league name");
        if (info.Teams is null || info.Teams.Count < 2)
            throw new FormatException("A league requires at least two teams.");

        var startingRosters = new Dictionary<string, List<Player>>(StringComparer.OrdinalIgnoreCase);
        foreach (TeamInfo definition in info.Teams)
        {
            if (definition?.Coach is null || definition.Roster is null)
                throw new FormatException("Each team requires a coach and a roster.");

            string code = Text(definition.Code, "team code");
            var coach = new Coach(Text(definition.Coach.Name, "coach name"), definition.Coach.ReplayNames);
            var team = new Team(this, code, Text(definition.Name, "team name"), coach);
            if (!teams.TryAdd(code, team))
                throw new FormatException($"Duplicate team code '{code}'.");

            foreach (string replayName in coach.ReplayNames)
            {
                string handle = Text(replayName, "coach replay name");
                if (replayTeams.TryGetValue(handle, out Team? previous) && previous != team)
                    throw new FormatException($"Replay name '{handle}' belongs to multiple teams.");
                replayTeams[handle] = team;
            }

            var roster = new List<Player>();
            foreach (string label in definition.Roster)
            {
                string species = Text(label, "roster species");
                var player = new Player(this, species);
                if (!players.TryAdd(species, player))
                    throw new FormatException($"'{species}' appears more than once in the league rosters.");
                roster.Add(player);
            }

            startingRosters.Add(code, roster);
        }

        ValidateSizes(startingRosters);
        weeklyRosters.Add(0, Freeze(startingRosters));
        if (info.Aliases is null)
            throw new FormatException("Aliases must be an object.");
        foreach ((string label, string canonical) in info.Aliases)
        {
            string alias = Text(label, "species alias");
            string species = Text(canonical, "alias target");
            if (!players.ContainsKey(species) ||
                (players.ContainsKey(alias) && !alias.Equals(species, StringComparison.OrdinalIgnoreCase)) ||
                !aliases.TryAdd(alias, species))
            {
                throw new FormatException($"Invalid or ambiguous species alias '{alias}' -> '{species}'.");
            }
        }
    }

    public string Name { get; }
    public IReadOnlyDictionary<string, Team> Teams => new ReadOnlyDictionary<string, Team>(teams);

    public IReadOnlyDictionary<string, Player> Players
    {
        get
        {
            lock (syncRoot)
                return new ReadOnlyDictionary<string, Player>(
                    new Dictionary<string, Player>(players, StringComparer.OrdinalIgnoreCase));
        }
    }

    public IReadOnlyDictionary<GameKey, BattleReplay> Games
    {
        get
        {
            lock (syncRoot)
                return new ReadOnlyDictionary<GameKey, BattleReplay>(new Dictionary<GameKey, BattleReplay>(games));
        }
    }

    public IReadOnlyList<Player> GetRoster(string teamCode, int week)
    {
        if (week < 1)
            throw new ArgumentOutOfRangeException(nameof(week), "Week must be positive.");
        lock (syncRoot)
        {
            Team team = GetTeam(teamCode);
            return weeklyRosters.Last(entry => entry.Key <= week).Value[team.Code];
        }
    }

    public Team GetTeamByReplayName(string replayName) =>
        replayTeams.GetValueOrDefault(replayName)
        ?? throw new FormatException($"No team has coach replay name '{replayName}'. Check league metadata.");

    public void ApplyTransactions(WeeklyTransactions batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        lock (syncRoot)
        {
            if (batch.Week < 1 || batch.Week <= weeklyRosters.Last().Key)
                throw new FormatException("Transaction weeks must be positive, unique, and applied in increasing order.");
            if (games.Keys.Any(game => game.Week >= batch.Week))
                throw new InvalidOperationException("Transactions cannot change a week with registered games or earlier weeks.");
            if (batch.Transactions is null)
                throw new FormatException("A weekly transaction file requires a transactions list.");

            var current = weeklyRosters.Last().Value;
            var next = current.ToDictionary(entry => entry.Key, entry => entry.Value.ToList(), StringComparer.OrdinalIgnoreCase);
            var moved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var additions = new List<Player>();
            foreach (RosterTransaction transaction in batch.Transactions)
            {
                if (transaction is null)
                    throw new FormatException("A transaction cannot be null.");
                string species = Text(transaction.Player, "transaction player");
                if (!moved.Add(species))
                    throw new FormatException($"'{species}' has multiple transactions in week {batch.Week}.");

                Team? source = transaction.FromTeam is null ? null : GetTeam(transaction.FromTeam);
                Team? destination = transaction.ToTeam is null ? null : GetTeam(transaction.ToTeam);
                if (source == destination)
                    throw new FormatException($"'{species}' must move between different teams or free agency.");

                players.TryGetValue(species, out Player? player);
                string? owner = current.FirstOrDefault(entry => entry.Value.Contains(player!)).Key;
                if (!string.Equals(owner, source?.Code, StringComparison.OrdinalIgnoreCase))
                    throw new FormatException($"The recorded owner of '{species}' does not match fromTeam.");

                if (player is null)
                {
                    if (aliases.ContainsKey(species))
                        throw new FormatException($"Use the canonical species name for '{species}' in transactions.");
                    player = new Player(this, species);
                    additions.Add(player);
                }

                if (source is not null)
                    next[source.Code].Remove(player);
                if (destination is not null)
                    next[destination.Code].Add(player);
            }

            ValidateSizes(next);
            var snapshot = Freeze(next);
            foreach (Player addition in additions)
                players.Add(addition.Species, addition);
            weeklyRosters.Add(batch.Week, snapshot);
        }
    }

    internal GameRoster CreateGameRoster(GameKey game, Team team) =>
        new(game, team, GetRoster(team.Code, game.Week));

    internal Player ResolvePlayer(GameRoster roster, string replaySpecies)
    {
        string canonical = aliases.GetValueOrDefault(replaySpecies) ?? replaySpecies;
        return roster.Players.SingleOrDefault(player =>
            player.Species.Equals(canonical, StringComparison.OrdinalIgnoreCase))
            ?? throw new FormatException(
                $"'{replaySpecies}' is absent from {roster.Team.Code}'s roster in week {roster.Game.Week}.");
    }

    internal void Register(BattleReplay replay)
    {
        lock (syncRoot)
        {
            if (replay.Game is not { } game || replay.Roster1 is not { } roster1 || replay.Roster2 is not { } roster2)
                throw new ArgumentException("A league game requires a key and both rosters.");
            foreach (GameRoster roster in new[] { roster1, roster2 })
            {
                if (GetTeam(roster.Team.Code) != roster.Team || roster.Game != game ||
                    !GetRoster(roster.Team.Code, game.Week).SequenceEqual(roster.Players))
                    throw new InvalidOperationException("The game's roster metadata changed while parsing.");
            }

            if (!games.TryAdd(game, replay))
                throw new InvalidOperationException($"Game '{game}' is already registered.");
        }
    }

    internal IReadOnlyDictionary<GameKey, GameRoster> GetTeamRosters(Team team) =>
        GetRosters(roster => roster.Team == team);

    internal IReadOnlyDictionary<GameKey, GameRoster> GetPlayerRosters(Player player) =>
        GetRosters(roster => roster.Players.Contains(player));

    private IReadOnlyDictionary<GameKey, GameRoster> GetRosters(Func<GameRoster, bool> includes)
    {
        lock (syncRoot)
        {
            var result = games.Values
                .SelectMany(game => new[] { game.Roster1!, game.Roster2! })
                .Where(includes)
                .ToDictionary(roster => roster.Game);
            return new ReadOnlyDictionary<GameKey, GameRoster>(result);
        }
    }

    private Team GetTeam(string code) => teams.GetValueOrDefault(Text(code, "team code"))
        ?? throw new FormatException($"Unknown team code '{code}'.");

    private static string Text(string? value, string description) => string.IsNullOrWhiteSpace(value)
        ? throw new FormatException($"Missing {description}.")
        : value.Trim();

    private static void ValidateSizes(Dictionary<string, List<Player>> rosters)
    {
        foreach ((string team, List<Player> roster) in rosters)
        {
            if (roster.Count is < 8 or > 12)
                throw new FormatException($"Team '{team}' has {roster.Count} players. A roster must have 8–12.");
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<Player>> Freeze(Dictionary<string, List<Player>> rosters) =>
        new ReadOnlyDictionary<string, IReadOnlyList<Player>>(
            rosters.ToDictionary(entry => entry.Key,
                entry => (IReadOnlyList<Player>)Array.AsReadOnly(entry.Value.ToArray()),
                StringComparer.OrdinalIgnoreCase));
}
