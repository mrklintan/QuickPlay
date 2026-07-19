namespace QuickPlay.Core;

public static class ShortcutDefaults
{
    public static Dictionary<ApplicationCommand, ShortcutGesture> Create() => new()
    {
        [ApplicationCommand.PlayPause] = new(19),
        [ApplicationCommand.PreviousTrack] = new(38),
        [ApplicationCommand.NextTrack] = new(40),
        [ApplicationCommand.PreviousFolder] = new(38, ShortcutModifiers.Control),
        [ApplicationCommand.NextFolder] = new(40, ShortcutModifiers.Control),
        [ApplicationCommand.SeekBackwardShort] = new(37),
        [ApplicationCommand.SeekForwardShort] = new(39),
        [ApplicationCommand.SeekBackwardLong] = new(37, ShortcutModifiers.Shift),
        [ApplicationCommand.SeekForwardLong] = new(39, ShortcutModifiers.Shift),
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
        ApplicationCommand.CopyCurrentTrack => "Copy Current Track",
        ApplicationCommand.DeleteCurrentTrack => "Delete Current Track",
        _ => command.ToString()
    };
}
