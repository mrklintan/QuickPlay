using System.Globalization;

namespace QuickPlay.Core;

public sealed class NaturalStringComparer : IComparer<string?>
{
    public static NaturalStringComparer Instance { get; } = new();

    public int Compare(string? left, string? right)
    {
        var leftMissing = string.IsNullOrWhiteSpace(left);
        var rightMissing = string.IsNullOrWhiteSpace(right);
        if (leftMissing || rightMissing)
            return leftMissing == rightMissing ? 0 : leftMissing ? 1 : -1;

        left = left!.Trim();
        right = right!.Trim();
        if (TryParseNumber(left, out var leftNumber) && TryParseNumber(right, out var rightNumber))
            return leftNumber.CompareTo(rightNumber);

        var leftIndex = 0;
        var rightIndex = 0;
        while (leftIndex < left.Length && rightIndex < right.Length)
        {
            var leftDigit = char.IsDigit(left[leftIndex]);
            var rightDigit = char.IsDigit(right[rightIndex]);
            if (leftDigit && rightDigit)
            {
                var comparison = CompareDigitRuns(left, ref leftIndex, right, ref rightIndex);
                if (comparison != 0) return comparison;
                continue;
            }

            var leftEnd = FindRunEnd(left, leftIndex, digits: false);
            var rightEnd = FindRunEnd(right, rightIndex, digits: false);
            var textComparison = StringComparer.CurrentCultureIgnoreCase.Compare(
                left[leftIndex..leftEnd], right[rightIndex..rightEnd]);
            if (textComparison != 0) return textComparison;
            leftIndex = leftEnd;
            rightIndex = rightEnd;
        }

        return (left.Length - leftIndex).CompareTo(right.Length - rightIndex);
    }

    private static bool TryParseNumber(string value, out decimal number) =>
        decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) ||
        decimal.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out number);

    private static int CompareDigitRuns(string left, ref int leftIndex, string right, ref int rightIndex)
    {
        var leftEnd = FindRunEnd(left, leftIndex, digits: true);
        var rightEnd = FindRunEnd(right, rightIndex, digits: true);
        var leftSignificant = SkipZeroes(left, leftIndex, leftEnd);
        var rightSignificant = SkipZeroes(right, rightIndex, rightEnd);
        var leftLength = leftEnd - leftSignificant;
        var rightLength = rightEnd - rightSignificant;
        var comparison = leftLength.CompareTo(rightLength);
        if (comparison == 0)
        {
            for (var offset = 0; offset < leftLength; offset++)
            {
                comparison = left[leftSignificant + offset].CompareTo(right[rightSignificant + offset]);
                if (comparison != 0) break;
            }
        }
        if (comparison == 0)
            comparison = (leftEnd - leftIndex).CompareTo(rightEnd - rightIndex);

        leftIndex = leftEnd;
        rightIndex = rightEnd;
        return comparison;
    }

    private static int FindRunEnd(string value, int start, bool digits)
    {
        var index = start;
        while (index < value.Length && char.IsDigit(value[index]) == digits) index++;
        return index;
    }

    private static int SkipZeroes(string value, int start, int end)
    {
        var index = start;
        while (index < end - 1 && value[index] == '0') index++;
        return index;
    }
}
