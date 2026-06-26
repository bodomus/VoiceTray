using VoiceTray.Contracts.Settings;

namespace VoiceTray.Tests;

public sealed class AppHotKeyStatusTests
{
    [Fact]
    public void CreateHotKeyRegistrationFailureStatus_UsesConfiguredGesture()
    {
        var settings = new HotKeySettings("Ctrl+Shift+F9", "Control,Shift", "F9", true);

        var status = App.CreateHotKeyRegistrationFailureStatus(settings);

        Assert.Equal("Hotkey registration failed: Ctrl+Shift+F9 is unavailable.", status);
    }
}
