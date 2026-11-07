namespace DraftAnalyzer.Core.Models;

public sealed class Coach
{
    public Coach(string name, IEnumerable<string>? replayNames = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        string[] names = replayNames?.ToArray() ?? [name];
        if (names.Length == 0 || names.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("A coach must have at least one nonempty replay name.");
        ReplayNames = Array.AsReadOnly(names);
    }

    public string Name { get; }
    public IReadOnlyList<string> ReplayNames { get; }
}
