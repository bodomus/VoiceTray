using Forms = System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace VoiceTray;

public partial class SettingsWindow
{
    private readonly SettingsWindowViewModel _viewModel;

    public SettingsWindow(SettingsWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool? dialogResult)
    {
        DialogResult = dialogResult;
        Close();
    }

    private void BrowseWhisperExecutable(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = _viewModel.WhisperExecutablePath
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.WhisperExecutablePath = dialog.FileName;
        }
    }

    private void BrowseWhisperModel(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Whisper models (*.bin)|*.bin|All files (*.*)|*.*",
            FileName = _viewModel.WhisperModelPath
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.WhisperModelPath = dialog.FileName;
        }
    }

    private void BrowseRecordingDirectory(object sender, System.Windows.RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            SelectedPath = Environment.ExpandEnvironmentVariables(_viewModel.RecordingDirectory)
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            _viewModel.RecordingDirectory = dialog.SelectedPath;
        }
    }
}
