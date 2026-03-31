using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OTPilot.Models;
using OTPilot.Services;

namespace OTPilot.ViewModels;

public partial class AddAccountViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _issuer = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _accountName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _secret = string.Empty;

    [ObservableProperty]
    private string _selectedAlgorithm = "SHA1";

    [ObservableProperty]
    private int _digits = 6;

    [ObservableProperty]
    private int _period = 30;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showAdvanced = false;

    public string[] AlgorithmOptions { get; } = { "SHA1", "SHA256", "SHA512" };
    public int[] DigitOptions { get; } = { 6, 8 };
    public int[] PeriodOptions { get; } = { 30, 60 };

    public TotpAccount? Result { get; private set; }

    public event Action? Confirmed;
    public event Action? Cancelled;
    public event Func<Task<string?>>? ScanRequested;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        ErrorMessage = string.Empty;

        var cleanSecret = Secret.Replace(" ", "").ToUpperInvariant();
        if (!IsValidBase32(cleanSecret))
        {
            ErrorMessage = "Invalid secret key. Please check and try again.";
            return;
        }

        Result = new TotpAccount
        {
            Issuer = Issuer.Trim(),
            AccountName = AccountName.Trim(),
            Secret = cleanSecret,
            Algorithm = SelectedAlgorithm,
            Digits = Digits,
            Period = Period
        };

        Confirmed?.Invoke();
    }

    private bool CanConfirm() =>
        !string.IsNullOrWhiteSpace(AccountName) &&
        !string.IsNullOrWhiteSpace(Secret);

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke();

    [RelayCommand]
    private async Task ScanQrCodeAsync()
    {
        if (ScanRequested is null) return;

        var uri = await ScanRequested.Invoke();
        if (string.IsNullOrWhiteSpace(uri)) return;

        var account = OtpAuthUriParser.Parse(uri);
        if (account is null)
        {
            ErrorMessage = "QR code found but could not be read as an authenticator account.";
            return;
        }

        Issuer = account.Issuer;
        AccountName = account.AccountName;
        Secret = account.Secret;
        SelectedAlgorithm = account.Algorithm;
        Digits = account.Digits;
        Period = account.Period;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void ToggleAdvanced() => ShowAdvanced = !ShowAdvanced;

    public void PopulateFromAccount(TotpAccount account)
    {
        Issuer = account.Issuer;
        AccountName = account.AccountName;
        Secret = account.Secret;
        SelectedAlgorithm = account.Algorithm;
        Digits = account.Digits;
        Period = account.Period;
    }

    private static bool IsValidBase32(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567=";
        return s.All(c => validChars.Contains(c));
    }
}
