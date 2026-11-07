namespace DraftAnalyzer.Core.Models;

public sealed record ActivePokemon(string Nickname, string Species)
{
    // HP as a percentage from 0 to 100. Null means the replay has not reported it.
    public double? Hp { get; init; }
}
