namespace QuickPlay.Core;

public sealed class ApplicationSettings
{
    public static readonly TimeSpan DefaultAuditionStartPosition = TimeSpan.FromMinutes(1);
    private TimeSpan _auditionStartPosition = DefaultAuditionStartPosition;
    private TimeSpan _continuePlayStartPosition = TimeSpan.Zero;
    private double _shortSeekSeconds = 5;
    private double _longSeekSeconds = 30;
    private double _playedThresholdSeconds = 5;

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

    public bool ContinuePlay { get; set; } = true;

    public TimeSpan ContinuePlayStartPosition
    {
        get => _continuePlayStartPosition;
        set => _continuePlayStartPosition = value >= TimeSpan.Zero
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Continue Play start position cannot be negative.");
    }

    public bool RemovePlayedTracks { get; set; } = true;

    public double PlayedThresholdSeconds
    {
        get => _playedThresholdSeconds;
        set => _playedThresholdSeconds = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Played threshold cannot be negative.");
    }

    public Dictionary<ApplicationCommand, ShortcutGesture> Shortcuts { get; set; } = ShortcutDefaults.Create();
    public PlaylistLayoutSettings PlaylistLayout { get; set; } = new();
    public PlaylistSessionSettings PlaylistSession { get; set; } = new();

    public void EnsureDefaults()
    {
        Shortcuts ??= [];
        foreach (var assignment in ShortcutDefaults.Create())
            Shortcuts.TryAdd(assignment.Key, assignment.Value);
        PlaylistLayout ??= new PlaylistLayoutSettings();
        PlaylistLayout.EnsureValid();
        PlaylistSession ??= new PlaylistSessionSettings();
        PlaylistSession.EnsureValid();
    }
}
