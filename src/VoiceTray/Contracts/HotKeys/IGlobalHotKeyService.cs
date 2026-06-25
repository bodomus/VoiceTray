using System.Windows;
using VoiceTray.Contracts.Settings;

namespace VoiceTray.Contracts.HotKeys;

public interface IGlobalHotKeyService : IDisposable
{
    event EventHandler? HotKeyPressed;

    bool Register(Window window, HotKeySettings settings);
}
