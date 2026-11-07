namespace DraftAnalyzer.Core.Metadata;

public sealed class WeeklyTransactions
{
    public required int Week { get; init; }
    public required List<RosterTransaction> Transactions { get; init; }
}
