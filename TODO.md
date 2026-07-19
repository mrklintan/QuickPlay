# QuickPlay TODO

## Resolve menu focus and playback shortcut routing

Status: known issue in version 1.1.

After using the application menu, focus can remain in a state where arrow keys both navigate a menu or list control and execute the configured playback shortcut. Programmatically moving focus after menu commands has not fully resolved the WinUI focus-routing behavior.

Current workaround: click the track list before using playback shortcuts.

Future work:

- Identify the WinUI focus element and routed-event path after each menu closes.
- Ensure menu navigation consumes arrow keys while a menu or flyout is active.
- Restore the same effective focus state produced by clicking the track list.
- Regression-test File, Settings, Keyboard, and About menu paths with playback active.
