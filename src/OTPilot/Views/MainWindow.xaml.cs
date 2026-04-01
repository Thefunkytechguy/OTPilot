using System.Windows;
using System.Windows.Controls;
using OTPilot.ViewModels;

namespace OTPilot.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await _viewModel.InitializeAsync();
    }

    // ── Closing: minimise to tray instead of exiting ─────────────────────

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (App.IsExiting) return;
        e.Cancel = true;
        Hide();
    }

    // ── Sidebar buttons ───────────────────────────────────────────────────

    private async void AddAccount_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddAccountWindow();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && dialog.Result is not null)
        {
            try
            {
                await _viewModel.AddAccountAsync(dialog.Result);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save account.\n\n{ex.Message}",
                    "OTPilot — Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow { Owner = this };
        about.ShowDialog();
    }

    // ── Account card actions ──────────────────────────────────────────────

    private void CopyCode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not AccountViewModel account) return;

        var rawCode = account.CurrentCode;
        if (string.IsNullOrEmpty(rawCode)) return;

        try
        {
            Clipboard.SetText(rawCode);

            // Brief visual feedback on the button
            var originalContent = btn.Content;
            btn.Content = "✓";
            btn.IsEnabled = false;

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (_, _) =>
            {
                btn.Content = originalContent;
                btn.IsEnabled = true;
                timer.Stop();

                // Clear clipboard after 30 seconds (PCI hygiene)
                ScheduleClipboardClear(rawCode);
            };
            timer.Start();
        }
        catch
        {
            // Clipboard access can fail in some locked-down environments
            MessageBox.Show("Could not access clipboard.", "OTPilot", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ScheduleClipboardClear(string codeToMatch)
    {
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(28) // 28s after copy = 30s total
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                // Only clear if the clipboard still contains our code
                if (Clipboard.ContainsText() && Clipboard.GetText() == codeToMatch)
                    Clipboard.Clear();
            }
            catch { /* ignore */ }
        };
        timer.Start();
    }

    private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not AccountViewModel account) return;

        var confirm = MessageBox.Show(
            $"Remove \"{account.Issuer}\" ({account.AccountName})?",
            "Remove Account",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
            await _viewModel.DeleteAccountAsync(account);
    }
}
