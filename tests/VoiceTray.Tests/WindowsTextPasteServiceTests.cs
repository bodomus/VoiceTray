using Microsoft.Extensions.Logging.Abstractions;
using VoiceTray.Contracts.Clipboard;
using VoiceTray.Infrastructure.Clipboard;

namespace VoiceTray.Tests;

public sealed class WindowsTextPasteServiceTests
{
    [Fact]
    public async Task PasteAsync_DoesNotSendPasteShortcut_WhenTargetWindowIsUnavailable()
    {
        var clipboard = new CapturingClipboardService();
        var platform = new FakePastePlatform
        {
            ForegroundWindow = new IntPtr(42),
            IsWindowResult = false
        };
        var service = new WindowsTextPasteService(
            clipboard,
            NullLogger<WindowsTextPasteService>.Instance,
            platform);

        service.CaptureTargetWindow();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PasteAsync("recognized text", CancellationToken.None));

        Assert.Contains("no longer available", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("recognized text", clipboard.Text);
        Assert.False(platform.SendPasteShortcutCalled);
    }

    private sealed class CapturingClipboardService : IClipboardService
    {
        public string? Text { get; private set; }

        public void SetText(string text) => Text = text;

        public string? GetText() => Text;
    }

    private sealed class FakePastePlatform : WindowsTextPasteService.IWindowsPastePlatform
    {
        public IntPtr ForegroundWindow { get; set; }

        public bool IsWindowResult { get; set; } = true;

        public bool SetForegroundWindowResult { get; set; } = true;

        public bool SendPasteShortcutCalled { get; private set; }

        public IntPtr GetForegroundWindow() => ForegroundWindow;

        public bool IsWindow(IntPtr hWnd) => IsWindowResult;

        public bool SetForegroundWindow(IntPtr hWnd) => SetForegroundWindowResult;

        public bool SendPasteShortcut()
        {
            SendPasteShortcutCalled = true;
            return true;
        }
    }
}
