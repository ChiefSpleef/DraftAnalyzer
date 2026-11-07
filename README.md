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

Each `GameEvent` keeps its Showdown command, arguments, replay line, and before/after states. Adjacent events share the state at their boundary. The state tracks active Pokémon and field effects. Damage, healing, and other activity outside those fields are available through the event arguments.
