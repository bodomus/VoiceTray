using VoiceTray.Contracts.Clipboard;

namespace VoiceTray.Infrastructure.Clipboard;

public sealed class WindowsClipboardService : IClipboardService
{
    public void SetText(string text) => System.Windows.Clipboard.SetText(text);

    public string? GetText() => System.Windows.Clipboard.ContainsText() ? System.Windows.Clipboard.GetText() : null;
}
