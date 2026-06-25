using System.Windows;
using VoiceTray.Infrastructure.Settings;

namespace VoiceTray.Infrastructure.HotKeys;

public interface IGlobalHotKeyService : IDisposable
{
    event EventHandler? HotKeyPressed;

    bool Register(Window window, HotKeySettings settings);
}
