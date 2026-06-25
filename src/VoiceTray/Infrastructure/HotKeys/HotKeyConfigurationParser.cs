using System.Windows.Input;
using VoiceTray.Contracts.Settings;

namespace VoiceTray.Infrastructure.HotKeys;

public static class HotKeyConfigurationParser
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;

    public static HotKeyConfigurationResult TryParse(HotKeySettings settings)
    {
        var modifiers = settings.NoRepeat ? ModNoRepeat : 0;
        var hasModifier = false;

        foreach (var modifier in settings.Modifiers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            hasModifier = true;
            var resolvedModifier = modifier.ToLowerInvariant() switch
            {
                "alt" => ModAlt,
                "control" or "ctrl" => ModControl,
                "shift" => ModShift,
                "win" or "windows" => ModWin,
                _ => 0u
            };

            if (resolvedModifier == 0)
            {
                return HotKeyConfigurationResult.Invalid($"Unknown hotkey modifier: {modifier}");
            }

            modifiers |= resolvedModifier;
        }

        if (!hasModifier)
        {
            return HotKeyConfigurationResult.Invalid("Hotkey must include at least one modifier.");
        }

        if (!Enum.TryParse<Key>(settings.Key, ignoreCase: true, out var key) || key == Key.None)
        {
            return HotKeyConfigurationResult.Invalid($"Unknown hotkey key: {settings.Key}");
        }

        return HotKeyConfigurationResult.Valid(modifiers, key);
    }
}

public sealed record HotKeyConfigurationResult(
    bool IsValid,
    uint Modifiers,
    Key Key,
    string? ErrorMessage)
{
    public static HotKeyConfigurationResult Valid(uint modifiers, Key key)
        => new(true, modifiers, key, null);

    public static HotKeyConfigurationResult Invalid(string errorMessage)
        => new(false, 0, Key.None, errorMessage);
}
