using System.Windows;
using OTPilot.Services;

namespace OTPilot.Views;

public partial class SettingsWindow : Window
{
    private readonly string _currentStorageName;

    public SettingsWindow(string currentStorageName)
    {
        InitializeComponent();
        _currentStorageName = currentStorageName;
        CurrentStorageText.Text = currentStorageName;
    }

    private async void ChangeStorage_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Changing your storage location will restart OTPilot.\n\n" +
            "Your existing accounts will remain in the current location and will not be migrated automatically.\n\n" +
            "Continue?",
            "Change Storage Location",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        var setup = new StorageSetupWindow { Owner = this };
        if (setup.ShowDialog() == true && setup.ResultConfig is not null)
        {
            await AppConfigService.SaveAsync(setup.ResultConfig);
            App.ExitForRestart();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
