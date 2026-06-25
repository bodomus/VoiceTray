using System.Windows.Input;
using VoiceTray.Contracts.Settings;
using VoiceTray.Infrastructure.HotKeys;

namespace VoiceTray.Tests;

public sealed class HotKeyConfigurationParserTests
{
    [Fact]
    public void TryParse_ReturnsValidResult_ForDefaultHotKey()
    {
        var result = HotKeyConfigurationParser.TryParse(
            new HotKeySettings("Ctrl+Alt+Space", "Control,Alt", "Space", true));

        Assert.True(result.IsValid);
        Assert.Equal(Key.Space, result.Key);
        Assert.Null(result.ErrorMessage);
        Assert.NotEqual(0u, result.Modifiers);
    }

    [Fact]
    public void TryParse_ReturnsInvalidResult_ForUnknownModifier()
    {
        var result = HotKeyConfigurationParser.TryParse(
            new HotKeySettings("Ctrl+Odd+Space", "Control,Odd", "Space", true));

        Assert.False(result.IsValid);
        Assert.Contains("Unknown hotkey modifier", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParse_ReturnsInvalidResult_ForUnknownKey()
    {
        var result = HotKeyConfigurationParser.TryParse(
            new HotKeySettings("Ctrl+Alt+Nope", "Control,Alt", "Nope", true));

        Assert.False(result.IsValid);
        Assert.Contains("Unknown hotkey key", result.ErrorMessage, StringComparison.Ordinal);
    }
}
