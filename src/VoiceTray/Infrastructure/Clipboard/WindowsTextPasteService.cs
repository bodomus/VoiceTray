using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using VoiceTray.Contracts.Clipboard;

namespace VoiceTray.Infrastructure.Clipboard;

public sealed class WindowsTextPasteService(
    IClipboardService clipboardService,
    ILogger<WindowsTextPasteService> logger) : ITextPasteService
{
    private readonly IWindowsPastePlatform _platform = new WindowsPastePlatform();
    private IntPtr _targetWindow;

    internal WindowsTextPasteService(
        IClipboardService clipboardService,
        ILogger<WindowsTextPasteService> logger,
        IWindowsPastePlatform platform)
        : this(clipboardService, logger)
    {
        _platform = platform;
    }

    public void CaptureTargetWindow()
    {
        var foregroundWindow = _platform.GetForegroundWindow();
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

        if (!_platform.IsWindow(_targetWindow))
        {
            throw new InvalidOperationException("Target window is no longer available.");
        }

        if (!_platform.SetForegroundWindow(_targetWindow))
        {
            throw new InvalidOperationException("Failed to focus target window.");
        }

        await Task.Delay(150, cancellationToken).ConfigureAwait(false);

        if (_platform.GetForegroundWindow() != _targetWindow)
        {
            throw new InvalidOperationException("Target window did not regain focus.");
        }

        var sent = _platform.SendPasteShortcut();
        if (!sent)
        {
            logger.LogWarning("SendInput failed to send Ctrl+V.");
            throw new InvalidOperationException("Failed to send Ctrl+V.");
        }
    }

    internal interface IWindowsPastePlatform
    {
        IntPtr GetForegroundWindow();

        bool IsWindow(IntPtr hWnd);

        bool SetForegroundWindow(IntPtr hWnd);

        bool SendPasteShortcut();
    }

    private sealed class WindowsPastePlatform : IWindowsPastePlatform
    {
        private const ushort VkControl = 0x11;
        private const ushort VkV = 0x56;
        private const uint KeyEventFKeyUp = 0x0002;

        public bool SendPasteShortcut()
        {
            var inputs = new[]
            {
                CreateKeyboardInput(VkControl, 0),
                CreateKeyboardInput(VkV, 0),
                CreateKeyboardInput(VkV, KeyEventFKeyUp),
                CreateKeyboardInput(VkControl, KeyEventFKeyUp)
            };

            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
            return sent == inputs.Length;
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

        public IntPtr GetForegroundWindow() => NativeGetForegroundWindow();

        public bool IsWindow(IntPtr hWnd) => NativeIsWindow(hWnd);

        public bool SetForegroundWindow(IntPtr hWnd) => NativeSetForegroundWindow(hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr NativeGetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool NativeIsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool NativeSetForegroundWindow(IntPtr hWnd);

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
}
