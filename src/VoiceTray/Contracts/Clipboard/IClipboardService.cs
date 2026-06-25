namespace VoiceTray.Contracts.Clipboard;

public interface IClipboardService
{
    void SetText(string text);

    string? GetText();
}
