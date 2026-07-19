using QuickPlay.Core;
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

namespace QuickPlay.WinUI;

public static class KeyboardGestureFactory
{
    public static ShortcutGesture Create(VirtualKey key)
    {
        var modifiers = ShortcutModifiers.None;
        if (IsDown(VirtualKey.Control)) modifiers |= ShortcutModifiers.Control;
        if (IsDown(VirtualKey.Shift)) modifiers |= ShortcutModifiers.Shift;
        if (IsDown(VirtualKey.Menu)) modifiers |= ShortcutModifiers.Alt;
        if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
            modifiers |= ShortcutModifiers.Windows;
        return new ShortcutGesture((int)key, modifiers);
    }

    public static bool IsModifier(VirtualKey key) =>
        key is VirtualKey.Control or VirtualKey.Shift or VirtualKey.Menu or
            VirtualKey.LeftWindows or VirtualKey.RightWindows;

    private static bool IsDown(VirtualKey key) =>
        InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
}
