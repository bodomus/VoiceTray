namespace VoiceTray;

public partial class MainWindow
{
    private bool _allowClose;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public void ShowAndActivate()
    {
        Show();
        WindowState = System.Windows.WindowState.Normal;
        Topmost = true;
        Activate();
        MemoTextBox.Focus();
        MemoTextBox.CaretIndex = MemoTextBox.Text.Length;
        Topmost = false;
    }

    public void AllowClose() => _allowClose = true;

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }
}
