namespace QuickPlay.Core;

[Flags]
public enum ShortcutModifiers
{
    None = 0,
    Control = 1,
    Shift = 2,
    Alt = 4,
    Windows = 8
}

public sealed record ShortcutGesture(int Key, ShortcutModifiers Modifiers = ShortcutModifiers.None)
{
    public static ShortcutGesture Unassigned { get; } = new(0);

    public bool IsAssigned => Key != 0;

    public string DisplayText
    {
        get
        {
            if (!IsAssigned) return "Unassigned";
            var parts = new List<string>();
            if (Modifiers.HasFlag(ShortcutModifiers.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(ShortcutModifiers.Shift)) parts.Add("Shift");
            if (Modifiers.HasFlag(ShortcutModifiers.Alt)) parts.Add("Alt");
            if (Modifiers.HasFlag(ShortcutModifiers.Windows)) parts.Add("Win");
            parts.Add(KeyName(Key));
            return string.Join("+", parts);
        }
    }

    private static string KeyName(int key) => key switch
    {
        8 => "Backspace",
        9 => "Tab",
        13 => "Enter",
        19 => "Pause",
        27 => "Escape",
        32 => "Space",
        33 => "Page Up",
        34 => "Page Down",
        35 => "End",
        36 => "Home",
        37 => "Left",
        38 => "Up",
        39 => "Right",
        40 => "Down",
        45 => "Insert",
        46 => "Delete",
        >= 48 and <= 57 => ((char)key).ToString(),
        >= 65 and <= 90 => ((char)key).ToString(),
        >= 112 and <= 135 => $"F{key - 111}",
        _ => $"Key {key}"
    };
}
