using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using OTPilot.Models;

namespace OTPilot.Views;

public partial class StorageSetupWindow : Window
{
    public AppConfig? ResultConfig { get; private set; }

    private string _selectedType = string.Empty;
    private string _customPath = string.Empty;

    private static readonly SolidColorBrush SelectedBrush = new(Color.FromRgb(37, 99, 235));
    private static readonly SolidColorBrush DefaultBrush  = new(Color.FromRgb(229, 231, 235));

    public StorageSetupWindow()
    {
        InitializeComponent();

        // Check if OneDrive is available on this machine
        var oneDrivePath = Environment.GetEnvironmentVariable("OneDriveCommercial")
                        ?? Environment.GetEnvironmentVariable("OneDrive");

        if (string.IsNullOrEmpty(oneDrivePath) || !Directory.Exists(oneDrivePath))
        {
            OneDriveCard.Opacity = 0.45;
            OneDriveCard.IsEnabled = false;
            OneDriveUnavailableText.Visibility = Visibility.Visible;
        }
    }

    // ── Card selection ────────────────────────────────────────────────────────

    private void OneDriveCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => SelectOption("OneDrive");

    private void LocalCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => SelectOption("Local");

    private void CustomCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => SelectOption("Custom");

    private void SelectOption(string type)
    {
        _selectedType = type;

        // Reset all cards
        OneDriveCard.BorderBrush = DefaultBrush;
        LocalCard.BorderBrush    = DefaultBrush;
        CustomCard.BorderBrush   = DefaultBrush;

        // Highlight selected card
        var selected = type switch
        {
            "OneDrive" => OneDriveCard,
            "Local"    => LocalCard,
            "Custom"   => CustomCard,
            _          => null
        };
        if (selected != null)
            selected.BorderBrush = SelectedBrush;

        // Show custom path picker only for Custom
        CustomPathPanel.Visibility = type == "Custom"
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Enable Get Started unless Custom with no path chosen yet
        GetStartedButton.IsEnabled = type != "Custom" || !string.IsNullOrWhiteSpace(_customPath);
    }

    // ── Custom path browser ───────────────────────────────────────────────────

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose a folder for your OTPilot vault"
        };

        if (dialog.ShowDialog() == true)
        {
            _customPath = dialog.FolderName;
            CustomPathTextBox.Text = _customPath;
            GetStartedButton.IsEnabled = true;
        }
    }

    // ── Confirm ───────────────────────────────────────────────────────────────

    private void GetStarted_Click(object sender, RoutedEventArgs e)
    {
        ResultConfig = new AppConfig
        {
            StorageType   = _selectedType,
            CustomPath    = _selectedType == "Custom" ? _customPath : null,
            SetupComplete = true
        };
        DialogResult = true;
        Close();
    }
}
