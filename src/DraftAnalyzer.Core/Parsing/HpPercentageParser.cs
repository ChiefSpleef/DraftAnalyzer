using System.Globalization;

namespace DraftAnalyzer.Core.Parsing;

internal static class HpPercentageParser
{
    public static double Parse(string condition)
    {
        ReadOnlySpan<char> value = condition.AsSpan().TrimStart();
        int statusSeparator = value.IndexOf(' ');
        if (statusSeparator >= 0)
            value = value[..statusSeparator];

        int fractionSeparator = value.IndexOf('/');
        if (fractionSeparator < 0 && TryReadNumber(value, out int current) && current <= 100)
            return current;

        if (fractionSeparator >= 0 &&
            TryReadNumber(value[..fractionSeparator].TrimEnd('\\'), out int numerator) &&
            TryReadNumber(value[(fractionSeparator + 1)..], out int denominator) &&
            denominator > 0 && numerator <= denominator)
        {
            return 100.0 * numerator / denominator;
        }

        throw new FormatException($"Invalid HP value '{condition}'.");
    }

    private static bool TryReadNumber(ReadOnlySpan<char> value, out int number) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number);
}
