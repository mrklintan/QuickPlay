namespace QuickPlay.Core;

public static class ShortcutDefaults
{
    public static Dictionary<ApplicationCommand, ShortcutGesture> Create() => new()
    {
        [ApplicationCommand.PlayPause] = new(32),
        [ApplicationCommand.PreviousTrack] = new(38),
        [ApplicationCommand.NextTrack] = new(40),
        [ApplicationCommand.PreviousFolder] = new(38, ShortcutModifiers.Control),
        [ApplicationCommand.NextFolder] = new(40, ShortcutModifiers.Control),
        [ApplicationCommand.SeekBackwardShort] = new(37, ShortcutModifiers.Control),
        [ApplicationCommand.SeekForwardShort] = new(39, ShortcutModifiers.Control),
        [ApplicationCommand.SeekBackwardLong] = new(37),
        [ApplicationCommand.SeekForwardLong] = new(39),
        [ApplicationCommand.CopyCurrentTrack] = new(67, ShortcutModifiers.Control),
        [ApplicationCommand.DeleteCurrentTrack] = new(46)
    };

    public static string Label(ApplicationCommand command) => command switch
    {
        ApplicationCommand.PlayPause => "Play / Pause",
        ApplicationCommand.PreviousTrack => "Previous Track",
        ApplicationCommand.NextTrack => "Next Track",
        ApplicationCommand.PreviousFolder => "Previous Sibling Folder",
        ApplicationCommand.NextFolder => "Next Sibling Folder",
        ApplicationCommand.SeekBackwardShort => "Seek Backward Short",
        ApplicationCommand.SeekForwardShort => "Seek Forward Short",
        ApplicationCommand.SeekBackwardLong => "Seek Backward Long",
        ApplicationCommand.SeekForwardLong => "Seek Forward Long",
        ApplicationCommand.CopyCurrentTrack => "Copy Playing / Loaded Track",
        ApplicationCommand.DeleteCurrentTrack => "Move Current Track to Recycle Bin",
        _ => command.ToString()
    };
}
