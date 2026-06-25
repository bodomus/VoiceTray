using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using VoiceTray.Contracts.Tray;

namespace VoiceTray.Infrastructure.Tray;

public sealed class TrayIconService(ILogger<TrayIconService> logger) : ITrayIconService
{
    private NotifyIcon? _notifyIcon;

    public event EventHandler? OpenRequested;

    public event EventHandler? StartRequested;

    public event EventHandler? StopRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler? ExitRequested;

    public void Initialize()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = GetApplicationIcon(),
            Text = "VoiceTray",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
        logger.LogInformation("Tray icon initialized.");
    }

    private static Icon GetApplicationIcon()
        => Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? System.Windows.Forms.Application.ExecutablePath) ?? SystemIcons.Application;

    public void Dispose()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _notifyIcon = null;
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Start Dictation", null, (_, _) => StartRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Stop Dictation", null, (_, _) => StopRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Settings", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));
        return menu;
    }
}
