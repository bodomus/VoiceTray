using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using VoiceTray.Contracts.HotKeys;
using VoiceTray.Contracts.Settings;

namespace VoiceTray.Infrastructure.HotKeys;

public sealed class WindowsGlobalHotKeyService(ILogger<WindowsGlobalHotKeyService> logger) : IGlobalHotKeyService
{
    private const int HotKeyId = 0x5654;
    private const int WmHotKey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;
    private HwndSource? _source;
    private IntPtr _windowHandle;
    private bool _registered;

    public event EventHandler? HotKeyPressed;

    public bool Register(Window window, HotKeySettings settings)
    {
        var hotKeyConfiguration = HotKeyConfigurationParser.TryParse(settings);
        if (!hotKeyConfiguration.IsValid)
        {
            logger.LogWarning(
                "Hotkey configuration is invalid. {ErrorMessage} Gesture={Gesture}, Modifiers={Modifiers}, Key={Key}, NoRepeat={NoRepeat}",
                hotKeyConfiguration.ErrorMessage,
                settings.Gesture,
                settings.Modifiers,
                settings.Key,
                settings.NoRepeat);
            return false;
        }

        _windowHandle = new WindowInteropHelper(window).EnsureHandle();
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(WndProc);

        var success = RegisterHotKey(
            _windowHandle,
            HotKeyId,
            hotKeyConfiguration.Modifiers,
            (uint)KeyInterop.VirtualKeyFromKey(hotKeyConfiguration.Key));
        _registered = success;

        if (success)
        {
            logger.LogInformation("Hotkey registered: {Gesture}", settings.Gesture);
        }
        else
        {
            logger.LogError("Hotkey registration failed: {Gesture}. Win32Error={Win32Error}", settings.Gesture, Marshal.GetLastWin32Error());
        }

        return success;
    }

    public void Dispose()
    {
        if (_registered)
        {
            UnregisterHotKey(_windowHandle, HotKeyId);
            _registered = false;
        }

        _source?.RemoveHook(WndProc);
        _source = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotKey && wParam.ToInt32() == HotKeyId)
        {
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

}
