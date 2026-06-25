using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using VoiceTray.Contracts.Clipboard;

namespace VoiceTray.Infrastructure.Clipboard;

public sealed class WindowsTextPasteService(
    IClipboardService clipboardService,
    ILogger<WindowsTextPasteService> logger) : ITextPasteService
{
    private const ushort VkControl = 0x11;
    private const ushort VkV = 0x56;
    private const uint KeyEventFKeyUp = 0x0002;
    private IntPtr _targetWindow;

    public void CaptureTargetWindow()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            _targetWindow = foregroundWindow;
        }
    }

    public async Task PasteAsync(string text, CancellationToken cancellationToken)
    {
        clipboardService.SetText(text);

        if (_targetWindow == IntPtr.Zero)
        {
            throw new InvalidOperationException("Target window is unknown.");
        }

        SetForegroundWindow(_targetWindow);
        await Task.Delay(150, cancellationToken).ConfigureAwait(false);

        var inputs = new[]
        {
            CreateKeyboardInput(VkControl, 0),
            CreateKeyboardInput(VkV, 0),
            CreateKeyboardInput(VkV, KeyEventFKeyUp),
            CreateKeyboardInput(VkControl, KeyEventFKeyUp)
        };

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent != inputs.Length)
        {
            logger.LogWarning("SendInput sent {SentCount} of {InputCount} events.", sent, inputs.Length);
            throw new InvalidOperationException("Failed to send Ctrl+V.");
        }
    }

    private static Input CreateKeyboardInput(ushort virtualKey, uint flags)
        => new()
        {
            Type = 1,
            Union = new InputUnion
            {
                KeyboardInput = new KeyboardInput
                {
                    VirtualKey = virtualKey,
                    Flags = flags
                }
            }
        };

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput KeyboardInput;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
