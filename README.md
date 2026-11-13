# DraftAnalyzer

Parse Pokémon Showdown draft-league seasons into rosters, matches, games, and ordered battle events. The .NET 8 core library can also serve a future XAML app.

## Import a season

This example reads a season's local files from the ignored `data/` directory.

```powershell
dotnet run --project src/DraftAnalyzer.Cli -- --season data/replays --league data/league.json --transactions data/transactions
```

The replay directory contains `w1`, `w2`, ... folders, each with one subfolder per match. Each match contains its complete best-of-three replay set. Game numbers come from the replays' best-of-three headers, and the importer orders the games.

`--league` is required. Omit `--transactions` for a league with no roster changes. An invalid transaction or match stops import with an error identifying the affected week or folder.

From C#, call `SeasonImporter.Import(replayDirectory, leagueFile, transactionsDirectory)`. It returns the imported `League`.

## Metadata

`examples/league.json` contains fictional teams, rosters, coaches, and replay usernames. `examples/transactions/week-02.json` shows a trade between those teams.

Species names identify players within the league, and team codes identify teams. Each species has one league identity and can belong to only one team at a time. Rosters contain 8–12 players. Coach replay names default to the coach name when omitted. Names match without regard to case.

The optional `aliases` object maps replay labels to canonical species names on starting rosters.

Each weekly JSON file has a `week` and a `transactions` list. Each transaction moves a canonical `player` from `fromTeam` to `toTeam`, with `null` representing free agency. Both directions of a trade are entries in the same file. All movements are validated together against starting ownership and final roster sizes before being applied.

Transactions take effect before that week's matches. Old game rosters retain their membership after later trades.

## Model and queries

- `League.Players[species]` holds a stable `Player`, including after a drop to free agency.
- `PlayerState` holds that player, nickname, current form, and HP as a percentage double from 0 to 100.
- Each `GameEvent` links its before and after `BattleState` and preserves its replay command, arguments, and line number. Adjacent events share boundary states.
- Each turn has its starting state and ordered events. Events before the first turn are in `BattleReplay.SetupEvents`.
- `BattleMatch` contains its games and score. A match requires two wins, games numbered from 1 without gaps, and no games after the deciding win.
- `League.Weeks`, `League.Matches`, and `Team.Matches` index season data. Game keys use week, matchup, and game number.
- `BattleReplay.Roster1` and `Roster2` copy full game rosters. Teams and players expose these historical records through `Rosters[gameKey]`.

Use `GetGames(week)`, `GetMatches(week)`, or their inclusive `(firstWeek, lastWeek)` overloads on leagues, teams, and players. Player queries include games where the player was rostered, including bench players.

Membership queries work even without replays for that week. Use `player.GetTeam(week)`, `player.GetTeams(firstWeek, lastWeek)`, `team.GetRoster(week)`, or `team.GetRosters(firstWeek, lastWeek)`. A player's team is null during free agency. `League.GetWeek(week)` and `GetWeeks(firstWeek, lastWeek)` return roster, game, and match snapshots together.
