namespace VoiceTray.Infrastructure.Clipboard;

public interface ITextPasteService
{
    void CaptureTargetWindow();

    Task PasteAsync(string text, CancellationToken cancellationToken);
}
