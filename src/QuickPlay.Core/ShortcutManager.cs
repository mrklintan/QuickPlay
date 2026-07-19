namespace QuickPlay.Core;

public sealed class ShortcutManager(ApplicationSettings settings)
{
    public ApplicationCommand? Resolve(ShortcutGesture gesture)
    {
        foreach (var assignment in settings.Shortcuts)
        {
            if (assignment.Value.IsAssigned && assignment.Value == gesture) return assignment.Key;
        }
        return null;
    }
}
