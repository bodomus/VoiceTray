namespace VoiceTray.Infrastructure.Clipboard;

public interface IClipboardService
{
    void SetText(string text);

    string? GetText();
}
