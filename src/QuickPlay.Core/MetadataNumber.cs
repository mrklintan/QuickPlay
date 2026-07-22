namespace QuickPlay.Core;

public static class MetadataNumber
{
    public static int? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var firstPart = value.Trim().Split('/', 2, StringSplitOptions.TrimEntries)[0];
        return int.TryParse(firstPart, out var number) && number > 0
            ? number
            : null;
    }
}
