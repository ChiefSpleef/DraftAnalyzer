namespace DraftAnalyzer.Core.Models;

public sealed record PlayerState(string Nickname, string Species)
{
    public Player? Player { get; init; }

    // HP as a percentage from 0 to 100. Null means the replay has not reported it.
    public double? Hp { get; init; }
}
