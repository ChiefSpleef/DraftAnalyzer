namespace DraftAnalyzer.Core.Models;

internal static class WeekRange
{
    internal static IEnumerable<int> Enumerate(int firstWeek, int lastWeek)
    {
        if (firstWeek < 1 || lastWeek < firstWeek)
            throw new ArgumentException("Week ranges must be positive and inclusive, with the first week at most the last.");
        return Enumerable.Range(firstWeek, checked(lastWeek - firstWeek + 1));
    }
}
