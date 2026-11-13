namespace DraftAnalyzer.Core.Models;

// HP is stored as a percentage from 0 to 100.
public sealed record PlayerState(Player Player, string Nickname, string Species, double Hp);
