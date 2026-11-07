# DraftAnalyzer
A tool to parse through pokemon showdown replays, extract raw stats and information, and convert it all into data-driven metrics.

## Run the turn snapshot parser

From this directory, pass a saved Pokémon Showdown replay HTML file or a plain battle log. The projects target .NET 8.

```powershell
dotnet run --project src/DraftAnalyzer.Cli -- "C:\path\to\replay.html"
```

Each row shows the active Pokémon at the replay's turn marker, after any replacements before that marker. A dash indicates an empty slot. Species and forms use the labels reported by the replay, with nicknames shown in parentheses when they differ. An Illusion reveal updates later snapshots once the replay identifies the Pokémon.

`DraftAnalyzer.Core` exposes `ReplayParser.Parse(TextReader)` and the turn snapshot models. A future XAML app can reference this library directly. The CLI handles file input and display.

Pass `--events` before the path to see the activity between turn markers and the board and field after changes. Events before the first marker are stored in `BattleReplay.SetupEvents`. A replacement before the next turn marker belongs to the preceding event list and appears in the next turn's starting board.

Each `GameEvent` keeps its Showdown command, arguments, replay line, and before/after states. Adjacent events share the state at their boundary. The state tracks active Pokémon, their HP percentage, and field effects. HP is stored as a nullable `double` on a 0–100 scale, converted from the replay's reported fraction without rounding. Null indicates unreported HP. Switch-ins load HP from the replay, and damage, healing, and direct HP changes update it. A faint clears the slot. With `--events`, HP updates show the previous and new percentages, displayed to two decimal places.

## League metadata

`examples/league.json` demonstrates starting rosters and coach replay names. The example uses two replay coaches with illustrative eight-player rosters. `examples/transactions/week-02.json` demonstrates a trade. Replace these examples with your league's data.

```powershell
dotnet run --project src/DraftAnalyzer.Cli -- --league examples/league.json --transactions examples/transactions --week 1 --game 1 "C:\path\to\replay.html"
```

The parser matches each replay username to a coach's `replayNames` (defaulting to the coach name when omitted). Species names identify players within the league, and team codes identify teams. The optional `aliases` object maps replay labels to canonical species names, for example `"Ogerpon-Hearthflame-Tera": "Ogerpon-Hearthflame"` when that canonical species is on a starting roster. Names match without regard to case. League rosters contain 8–12 players, and each species can belong to only one team at a time.

Each transaction moves one canonical species from `fromTeam` to `toTeam`. Use `null` for free agency on either side. Both directions of a trade are separate entries in the same weekly file. All movements in a week's file are validated together against ownership at the start of that week, including final roster sizes, before any changes are published. Directory files are loaded in week order. For incremental loading, apply each week's batch once in increasing order.

Transactions take effect before that week's games. Parsing week 1 uses starting rosters even when week 2 transactions are loaded. Adding transactions for a week with registered games or an earlier week is rejected to preserve historical results.

## Model relationships

`LeagueLoader.Load(leaguePath, transactionsDirectory)` loads the metadata. Use `ReplayParser.Parse(reader, league, week, gameNumber)` to parse a game against it. The plain `Parse(reader)` entry point still supports inspecting a replay without league metadata.

- `League.Players[species]` holds one stable `Player` per canonical species, including players subsequently dropped to free agency.
- `PlayerState` links to that player and stores the nickname, current form, and HP at a point in the replay. Form and HP updates preserve the player reference.
- `League.GetRoster(teamCode, week)` returns the roster established by metadata and transactions for that week.
- `BattleReplay.Game` identifies a game by week, matchup, and game number within its league, independently of which team is p1.
- `BattleReplay.Roster1` and `Roster2` copy the full rosters for that game. Team preview is checked against them.
- `team.Rosters[gameKey]`, `player.Rosters[gameKey]`, and `player.GetTeam(gameKey)` retrieve membership for successfully parsed games. A failed parse leaves game history untouched.

Supply the game number within the matchup when parsing multiple games. Duplicate game keys are rejected. Plain replay inspection leaves league and roster links unset.
