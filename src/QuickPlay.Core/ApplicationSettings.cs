namespace QuickPlay.Core;

public sealed class ApplicationSettings
{
    public static readonly TimeSpan DefaultAuditionStartPosition = TimeSpan.FromMinutes(1);
    private TimeSpan _auditionStartPosition = DefaultAuditionStartPosition;
    private double _shortSeekSeconds = 5;
    private double _longSeekSeconds = 30;

    public TimeSpan AuditionStartPosition
    {
        get => _auditionStartPosition;
        set => _auditionStartPosition = value >= TimeSpan.Zero
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Audition start position cannot be negative.");
    }

    public double ShortSeekSeconds
    {
        get => _shortSeekSeconds;
        set => _shortSeekSeconds = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Short seek must be positive.");
    }

    public double LongSeekSeconds
    {
        get => _longSeekSeconds;
        set => _longSeekSeconds = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Long seek must be positive.");
    }

    public Dictionary<ApplicationCommand, ShortcutGesture> Shortcuts { get; set; } = ShortcutDefaults.Create();

    public void EnsureDefaults()
    {
        Shortcuts ??= [];
        foreach (var assignment in ShortcutDefaults.Create())
            Shortcuts.TryAdd(assignment.Key, assignment.Value);
    }
}
